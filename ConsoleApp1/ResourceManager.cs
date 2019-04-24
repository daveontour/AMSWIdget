using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;
using System.Collections;
using System.Messaging;
using System.Xml;

namespace ConsoleApp1 {
    class ResourceManager {

        private Timer onTimer;
        private Timer offTimer;
        private List<Downgrade> downgrades = new List<Downgrade>();
        private List<Downgrade> startOrder = new List<Downgrade>();
        private List<Downgrade> endOrder = new List<Downgrade>();
        private string resourceType;
        private Hashtable statusTable = new Hashtable();

        public ResourceManager(String type) {
            this.resourceType = type;
        }

        public ResourceManager(String type, List<Downgrade> downs) {
            this.resourceType = type;
            this.SetDowngrades(downs);
        }

        public async Task<String> GetCurrentResourceStatus(String id, Boolean refresh) {
            if (refresh) {
                await this.GetCurrentStatus();
            }
            return (String)statusTable[id];
        }

        public void SetDowngrades(List<Downgrade> downs) {

            this.downgrades.Clear();
            this.startOrder.Clear();
            this.endOrder.Clear();

            foreach (Downgrade d in downs) {
                //Filter out any old downgrades
                if (d.from < DateTime.Now && d.to < DateTime.Now) {
                    continue;
                } else {
                    this.downgrades.Add(d);
                }
            }

            // Sort them into date order
            this.downgrades.Sort((x, y) => x.from.CompareTo(y.from));
            this.startOrder = this.downgrades;

           foreach (Downgrade d in this.downgrades) {
                this.endOrder.Add(new Downgrade(d));
            }
            this.endOrder.Sort((x, y) => x.to.CompareTo(y.to));

            foreach(Downgrade d in this.startOrder ){
                Console.WriteLine(d);
            }
            foreach (Downgrade d in this.endOrder) {
                Console.WriteLine(d);
            }
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

                client.BaseAddress = new Uri(Program.BASE_URI);
                client.DefaultRequestHeaders.Add("Authorization", Program.TOKEN);

                var result = await client.GetAsync(String.Format(Program.restAPIGetBase, resourceType));

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

        public bool IsResourceDowngradesActive(string resourceID) {

            foreach (Downgrade d in this.downgrades) {

                if (!d.IsActive() ) {
                    continue;
                }

                if (!d.resourceExternalName.Contains(resourceID, StringComparer.OrdinalIgnoreCase)) {
                    continue;
                }         
                return true;
            }

            return false;
        }


        public async Task resetDowngrades() {
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

            foreach (DictionaryEntry s in statusTable ) {

                string resource = (string)s.Key;
                string currentStatus = (string)s.Value;

                
                Boolean bDown = IsResourceDowngradesActive(resource);

                if (currentStatus != "SERVICEABLE" && !bDown) {
                    Console.WriteLine(resourceType + "  Status = " + currentStatus + " Should be SERVICEABLE");
                    this.SendStatusUpdateMessage(resource, "SERVICEABLE");
                }
                if (currentStatus != "UNSERVICEABLE" && bDown) {
                    Console.WriteLine(resourceType+ "  Status = " + currentStatus + " Should be UNSERVICEABLE");
                    this.SendStatusUpdateMessage(resource, "UNSERVICEABLE");
                }
            }
        }

        public void SendStatusUpdateMessage(String resourceID, String status ) {
            /*
             * Sends a message to AMS to update the S---_Status custom field
             * for the particular resource type 
             * 
             * Uses the MQ Request Queue to send the message (would prefer to use the RestAPI)
             */

            MessageQueue queue = new MessageQueue(".\\Private$\\toams");
            XmlDocument xmlDoc = new XmlDocument();

            Message msg = new Message {
                Formatter = new XmlMessageFormatter()
            };

            string update = string.Format(MQMessTemplate.GetMQMessTemplate(this.resourceType), Program.TOKEN, resourceID, Program.APT_CODE, status);
            xmlDoc.LoadXml(update);
            msg.Body = xmlDoc;
            queue.Send(msg, "Resource Status Update");
        }

        public override String ToString() {
            int total = downgrades.Count();
            int active = 0;
            foreach (Downgrade d in this.downgrades) {
                if (d.IsActive()) {
                    active = active + 1;
                }
            }

            return String.Format("Resource: {0}  Total Downgrades: {1},  Active Downgrades: {2}", resourceType, total, active);
        }
    }
}
