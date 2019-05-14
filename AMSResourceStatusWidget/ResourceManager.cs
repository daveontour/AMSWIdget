using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;
using System.Collections;
using System.Messaging;
using System.Xml;
using WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv;

namespace AMSResourceStatusWidget {
    class ResourceManager {

        private Timer startTimer;
        private Timer stopTimer;
        private readonly List<Downgrade> downgrades = new List<Downgrade>();
        private readonly Stack<Downgrade> startStack = new Stack<Downgrade>();
        private readonly Stack<Downgrade> stopStack = new Stack<Downgrade>();
        private bool validStart = false;
        private bool validStop = false;
        private string resourceType;
        private readonly Hashtable statusTable = new Hashtable();

        public ResourceManager(String type) {
            this.resourceType = type;
        }

        public ResourceManager(String type, List<Downgrade> downs) {
            this.resourceType = type;
            this.SetDowngrades(downs);
        }

        public void Suspend() {

            this.validStart = false;
            this.validStop = false;

            try {
                this.startTimer.Stop();
            } catch (Exception) {
                // Controller.SOP("On Timer wasn't running");
            }
            try {
                this.stopTimer.Stop();
            } catch (Exception) {
                //   Controller.SOP("Off Timer wasn't running");
            }
            this.downgrades.Clear();
            this.startStack.Clear();
            this.stopStack.Clear();
        }
        public async Task<String> GetCurrentResourceStatus(String id, Boolean refresh) {
            if (refresh) {
                await this.GetCurrentStatus();
            }
            return (String)statusTable[id];
        }

        public async void SetDowngrades(List<Downgrade> downs, bool reset = false) {

            this.validStart = false;
            this.validStop = false;

            try {
                this.startTimer.Stop();
            } catch (Exception) {
                // Controller.SOP("On Timer wasn't running");
            }
            try {
                this.stopTimer.Stop();
            } catch (Exception) {
                //   Controller.SOP("Off Timer wasn't running");
            }
            this.downgrades.Clear();
            this.startStack.Clear();
            this.stopStack.Clear();

            foreach (Downgrade d in downs) {
                //Filter out any old downgrades
                if (d.to < DateTime.Now) {
                    continue;
                } else {
                    this.downgrades.Add(d);
                }
            }

            if (this.downgrades.Count() == 0) {
                if (reset) {
                    await this.ResetDowngrades();
                }
                return;
            }

            // Sort them into start downgrade date order and put them on a stack
            this.downgrades.Sort((x, y) => -1 * x.from.CompareTo(y.from));
            foreach (Downgrade d in this.downgrades) {
                this.startStack.Push(d);
            }

            // Sort them into stop downgrade date order and put them on a stack
            this.downgrades.Sort((x, y) => -1 * x.to.CompareTo(y.to));
            foreach (Downgrade d in this.downgrades) {
                this.stopStack.Push(d);
            }

            this.SetNextStartTimer();
            this.SetNextStopTimer();

            if (reset) {
                await this.ResetDowngrades();
            }
        }

        private void SetNextStartTimer() {
            try {
                this.startTimer.Stop();
            } catch (Exception) {
                // Controller.SOP("Start Timer wasn't running");
            }

            if (startStack.Count == 0) {
                // No timer left to set
                return;
            }

            if (this.startStack.Peek().from >= DateTime.Now || validStart) {
                startTimer = new Timer {
                    AutoReset = false,
                };

                validStart = true;

                // The NextTimer may be that same as the previous time or the loop may 
                // have takem longer to circle round, so the "next" time might be in the past
                // but if we got here, all the ones in the real past have been weeded out so
                // using the Max allows for the case when the "start" may be inn the past
                startTimer.Interval = Math.Max(1, (this.startStack.Peek().from - DateTime.Now).TotalMilliseconds);
                if (startTimer.Interval > Int32.MaxValue) {
                    startTimer.Enabled = false;
                    Controller.SOP("Start Timer not set for " + this.resourceType + ". Too far in advance. " + this.startStack.Peek().from);
                    return;
                } else {
                    startTimer.Enabled = true;
                    Controller.SOP("Start Timer set for " + this.resourceType + " at " + this.startStack.Peek().from);
                }

                startTimer.Elapsed += async (source, eventArgs) =>
                {
                    Controller.SOP("Start Timer for " + this.resourceType + " gone off");
                    await ResetDowngrades();
                    Controller.SOP("Start Timer for " + this.resourceType + " completed");
                    this.startStack.Pop();
                    this.SetNextStartTimer();
                };

            } else {
                //Remove downgrades that may have already started
                this.startStack.Pop();
                // Loop around until all are removed or timer is set
                this.SetNextStartTimer();
            }
        }

        private void SetNextStopTimer() {
            try {
                this.stopTimer.Stop();
            } catch (Exception) {
                //  Controller.SOP("Stop Timer wasn't running");
            }

            if (stopStack.Count == 0) {
                // No timer left to set
                return;
            }

            if (this.stopStack.Peek().to >= DateTime.Now || validStop) {
                stopTimer = new Timer {
                    AutoReset = false
                };
                validStop = true;

                // The NextTimer may be that same as the previous time or the loop may 
                // have takem longer to circle round, so the "next" time might be in the past
                // but if we got here, all the ones in the real past have been weeded out so
                // using the Max allows for the case when the "start" may be inn the past
                stopTimer.Interval = Math.Max(1, (this.stopStack.Peek().to - DateTime.Now).TotalMilliseconds);
                if (stopTimer.Interval > Int32.MaxValue) {
                    stopTimer.Enabled = false;
                    Controller.SOP("Stop Timer not set for " + this.resourceType + ". Too far in advance. " + this.stopStack.Peek().to);
                    return;
                } else {
                    stopTimer.Enabled = true;
                    Controller.SOP("Stop Timer set for " + this.resourceType + " at " + this.stopStack.Peek().to);
                }

                stopTimer.Elapsed += async (source, eventArgs) =>
                {
                    Controller.SOP("Stop Timer for " + this.resourceType + " gone off");
                    await ResetDowngrades();
                    Controller.SOP("Stop Timer for " + this.resourceType + " completed");
                    this.stopStack.Pop();
                    this.SetNextStopTimer();
                };

            } else {
                //Remove downgrades that may have already started
                this.stopStack.Pop();
                // Loop around until all are removed or timer is set
                this.SetNextStopTimer();
            }

        }

        public HashSet<String> GetDowngradedResource() {

            /*
             * Find all the active downgrades and add the resources affected by 
             * it to the set of unavailable resources
             */
            HashSet<String> unavailResources = new HashSet<String>();
            foreach (Downgrade d in this.downgrades) {
                if (!d.IsActive()) {
                    continue;
                }
                foreach (String resource in d.resourceExternalName) {
                    unavailResources.Add(resource);
                }
            }
            return unavailResources;
        }

        public async Task GetCurrentStatus() {
            /*
             * Gets the current S---_Status of all the resources of this type
             * and puts them into a table mapped by resourceID
             * 
             * The RestAPI is used to get the resource type and the returned
             * XML is parsed to get the status
             */

            this.statusTable.Clear();

            using (var client = new HttpClient()) {

                client.BaseAddress = new Uri(Parameters.BASE_URI);
                client.DefaultRequestHeaders.Add("Authorization", Parameters.TOKEN);

                var result = await client.GetAsync(String.Format(Parameters.RESTAPIBASE, resourceType));

                XElement xmlRoot = XDocument.Parse(await result.Content.ReadAsStringAsync()).Root;

                foreach (XElement e in from n in xmlRoot.Descendants() where (n.Name == "FixedResource") select n) {
                    IEnumerable<XElement> customFields = e.Element("CustomFields").Elements("CustomField");
                    if (customFields.Count() == 0) {
                        continue;
                    }

                    try {
                        XElement statusElement = (from n in customFields where n.Element("Name").Value == "S---_Status" select n).First();

                        string status = "Undefined";
                        if (statusElement != null) {
                            status = statusElement.Element("Value").Value;
                        }
                        statusTable.Add(e.Element("Id").Value, status);
                    } catch (Exception) {
                        statusTable.Add(e.Element("Id").Value, "Undefined");
                    }
                }
            }
        }

        public async Task ResetDowngrades() {
            /*
             * Call this function at the start or reset of the service or downgrades
             * 
             * It gets the current status of all the resources for this type,
             * checks if there is any downgrade active agains that resource and 
             * updates the S---_Status if necessary.
             * 
             * Avoid any unnecessary updates by only changing the S---_Status if required
             */

            await this.GetCurrentStatus();
            HashSet<string> unavailResources = this.GetDowngradedResource();

            foreach (DictionaryEntry s in statusTable) {

                string resource = (string)s.Key;
                string currentStatus = (string)s.Value;
                bool bDown = unavailResources.Contains(resource, StringComparer.OrdinalIgnoreCase);

                if (currentStatus != "SERVICEABLE" && !bDown) {
                    this.SendStatusUpdateMessage(resource, "SERVICEABLE");
                }
                if (currentStatus != "UNSERVICEABLE" && bDown) {
                    this.SendStatusUpdateMessage(resource, "UNSERVICEABLE");
                }
            }
        }

        public void SendStatusUpdateMessage(String resourceID, String status) {
            /*
             * Sends a message to AMS to update the S---_Status custom field
             * for the particular resource type 
             * 
             * Uses the MQ Request Queue to send the message (would prefer to use the RestAPI)
             */

            XmlDocument xmlDoc = new XmlDocument();

            Message msg = new Message {
                Formatter = new XmlMessageFormatter()
            };

            string update = "Error";
            try {
                update = string.Format(MQMessTemplate.GetMQMessTemplate(this.resourceType), Parameters.TOKEN, resourceID, Parameters.APT_CODE, status);
                xmlDoc.LoadXml(update);
                MessageQueue sendQueue = new MessageQueue(Parameters.SENDQ);
                msg.Body = xmlDoc;
                sendQueue.Send(msg, "Resource Status Update");
                Controller.SOP(String.Format("{0} Update Message Sent, Resource ID: {1}, New Status: {2}", this.resourceType, resourceID, status));
             } catch (Exception ex) {
                Controller.SOP(update, true);
                Controller.SOP(ex.Message, true);
            }
        }

        public void UpdateDowngrades(bool reset=false) {

            Controller.SOP("Updating downgrades for type " + this.resourceType);

            try {

                AMSIntegrationServiceClient client = new AMSIntegrationServiceClient();
                XmlElement x;
                switch (this.resourceType) {
                    case "CheckIn":
                        x = client.GetCheckInDowngrades(Parameters.TOKEN, Parameters.EARLIEST_DOWNGRADE, Parameters.LATEST_DOWNGRADE, Parameters.APT_CODE, AirportIdentifierType.IATACode);
                        SetDowngrades(this.CreateDowngrades(x, "CheckInDowngrade"), reset);
                        break;
                    case "Gate":
                        x = client.GetGateDowngrades(Parameters.TOKEN, Parameters.EARLIEST_DOWNGRADE, Parameters.LATEST_DOWNGRADE, Parameters.APT_CODE, AirportIdentifierType.IATACode);
                        SetDowngrades(this.CreateDowngrades(x, "GateDowngrade"), reset);
                        break;
                    case "Chute":
                        x = client.GetChuteDowngrades(Parameters.TOKEN, Parameters.EARLIEST_DOWNGRADE, Parameters.LATEST_DOWNGRADE, Parameters.APT_CODE, AirportIdentifierType.IATACode);
                        SetDowngrades(this.CreateDowngrades(x, "ChuteDowngrade"), reset);
                        break;
                    case "Stand":
                        x = client.GetStandDowngrades(Parameters.TOKEN, Parameters.EARLIEST_DOWNGRADE, Parameters.LATEST_DOWNGRADE, Parameters.APT_CODE, AirportIdentifierType.IATACode);
                        SetDowngrades(this.CreateDowngrades(x, "StandDowngrade"), reset);
                        break;
                    case "Carousel":
                        x = client.GetCarouselDowngrades(Parameters.TOKEN, Parameters.EARLIEST_DOWNGRADE, Parameters.LATEST_DOWNGRADE, Parameters.APT_CODE, AirportIdentifierType.IATACode);
                        SetDowngrades(this.CreateDowngrades(x, "CarouselDowngrade"), reset);
                        break;
                }
                client.Close();

                Controller.SOP(this.ToString());

            } catch (Exception ex) {
                Controller.SOP(ex.Message, true);
            }
        }

        private List<Downgrade> CreateDowngrades(System.Xml.XmlElement x, String type) {
            List<Downgrade> dgs = new List<Downgrade>();

            XNamespace ns = "http://www.sita.aero/ams6-xml-api-datatypes";
            XElement el = XDocument.Parse(x.InnerXml).Root;

            IEnumerable<XElement> downs = from n in el.Descendants()
                                          where (n.Name == ns + type)
                                          select n;

            foreach (XElement xl in downs) {
                dgs.Add(new Downgrade(xl, ns, type));
            }
            return dgs;
        }

        public override String ToString() {
            int total = downgrades.Count();
            int active = 0;
            foreach (Downgrade d in this.downgrades) {
                if (d.IsActive()) {
                    active = active + 1;
                }
            }
         
            string s =  String.Format("Resource: {0}  Total Downgrades: {1},  Active Downgrades: {2}", resourceType, total, active);

            foreach (Downgrade itm in startStack) {
                string ss = String.Format("{2} Downgrade ID: {0}, Start Time: {1}", itm.UniqueID, itm.from, this.resourceType);
                s = s + "\n" + ss;
            }
            foreach (Downgrade itm in stopStack) {
                string ss = String.Format("{2} Downgrade ID: {0}, Stop Time: {1}", itm.UniqueID, itm.to, this.resourceType);
                s = s + "\n" + ss;
            }
            return s;
        }
    }
}
