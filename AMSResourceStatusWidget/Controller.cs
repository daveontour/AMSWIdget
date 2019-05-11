using System;
using System.Collections.Generic;
using System.Linq;

using System.Messaging;
using System.Xml;
using System.IO;
using System.Collections;
using System.Xml.Linq;
using System.Timers;
using Microsoft.Win32;
using System.Diagnostics;

namespace AMSResourceStatusWidget {
    public class Controller {

        public static MessageQueue m_Queue;
        public static MessageQueue send_Queue;
        private static int consoleLog = 0;
        public static string TOKEN;
        public static string BASE_URI;
        public static string APT_CODE;
        public static int BIG_RESET_TIME;
        public bool startListenLoop = true;
        private readonly Hashtable ht = new Hashtable();
        private readonly Hashtable resourceManagersTable = new Hashtable();
        private System.Threading.Thread m_ReceiveThread;
        private static string LOGFILEPATH;
        private readonly string[] types = { "CheckIn", "Gate", "Stand", "Chute", "Carousel" };
        private readonly string[] notificationMsgs = {
           "CheckInUpdatedNotification",
           "GateUpdatedNotification",
           "ChuteUpdatedNotification",
           "StandUpdatedNotification",
           "CarouselUpdatedNotification"
            };
        public static string restAPIGetBase;
        DateTime earliestDowngrade;
        DateTime latestDowngrade;
        Timer resetTimer;
        private static EventLog eventLog1;
        private static bool logEvents = false;

        enum MessType {
            NonResource,
            CheckInDowngradeCreated,
            CheckInDowngradeUpdated,
            CheckInDowngradeDeleted,
            GateDowngradeCreated,
            GateDowngradeUpdated,
            GateDowngradeDeleted,
            ChuteDowngradeCreated,
            ChuteDowngradeUpdated,
            ChuteDowngradeDeleted,
            StandDowngradeCreated,
            StandDowngradeUpdated,
            StandDowngradeDeleted,
            CarouselDowngradeCreated,
            CarouselDowngradeUpdated,
            CarouselDowngradeDeleted,
        }

        public Controller(EventLog eventLogger) {
            eventLog1 = eventLogger;
            foreach (String type in types) {
                resourceManagersTable.Add(type, new ResourceManager(type));
            }
        }

        public async void InitService() {
            try {
                try {
                    this.SetParameters();
                    this.ClearAllMessages();
                    this.StartMQListener();
                    this.SetAllResourceManagers();

                    // Set a time to reset everything periodically
                    this.resetTimer = new Timer {
                        AutoReset = true,
                        Interval = 1000 * 60 * BIG_RESET_TIME,  //87 Minutes
                        Enabled = true
                    };

                    this.resetTimer.Elapsed += (source, eventArgs) =>
                    {
                        this.SetAllResourceManagers();
                    };
                } catch (Exception ex) {
                    Controller.SOP("Error in function InitCommon", true);
                    Controller.SOP(ex.Message, true);
                }

                foreach (String type in types) {
                    ResourceManager rm = (ResourceManager)resourceManagersTable[type];
                    await rm.ResetDowngrades();
                }
            } catch (Exception ex) {
                Controller.SOP("Error in function InitService", true);
                Controller.SOP(ex.Message, true);
            }
        }

        public void Suspend() {
            try {
                this.StopMQListener();
                foreach (String type in types) {
                    ResourceManager rm = (ResourceManager)resourceManagersTable[type];
                    rm.Suspend();
                }
            } catch (Exception ex) {
                Controller.SOP("Error in function Suspend", true);
                Controller.SOP(ex.Message, true);
            }

        }

        private void SetParameters() {
            try {
                const string userRoot = "HKEY_LOCAL_MACHINE\\SOFTWARE\\SITA";
                const string subkey = "AMSResourceStatusWidget";
                const string keyName = userRoot + "\\" + subkey;

                APT_CODE = (string)Registry.GetValue(keyName, "IATAAirportCode", "DOH");
//                Registry.SetValue(keyName, "IATAAirportCode", APT_CODE, RegistryValueKind.String);

                restAPIGetBase = "/api/v1/" + APT_CODE + "/{0}s";

                TOKEN = (string)Registry.GetValue(keyName, "Token", "b406564f-44aa-4e51-a80a-aa9ed9a04ec6");
  //              Registry.SetValue(keyName, "Token", TOKEN, RegistryValueKind.String);

                LOGFILEPATH = (string)Registry.GetValue(keyName, "LogFilePath", "c:\\");
    //            Registry.SetValue(keyName, "LogFilePath", LOGFILEPATH, RegistryValueKind.String);

                BASE_URI = (string)Registry.GetValue(keyName, "BaseURI", "http://localhost/");
      //          Registry.SetValue(keyName, "BaseURI", BASE_URI, RegistryValueKind.String);

                int big_reset = Int32.Parse((string)Registry.GetValue(keyName, "ResetTime", "107"));
        //        Registry.SetValue(keyName, "ResetTime", big_reset, RegistryValueKind.String);
                BIG_RESET_TIME = big_reset;

                consoleLog = (int)Registry.GetValue(keyName, "ConsoleLog", 0);
       //         Registry.SetValue(keyName, "ConsoleLog", consoleLog, RegistryValueKind.DWord);

                int earliestDowngradeOffSet = Int32.Parse((string)Registry.GetValue(keyName, "EarliestDowngradeOffset", "-20"));
        //        Registry.SetValue(keyName, "EarliestDowngradeOffset", earliestDowngradeOffSet, RegistryValueKind.String);
                earliestDowngrade = DateTime.Now.AddDays(earliestDowngradeOffSet);

                int latestDowngradeOffSet = Int32.Parse((string)Registry.GetValue(keyName, "LatestDowngradeOffset", "20"));
        //        Registry.SetValue(keyName, "LatestDowngradeOffset", latestDowngradeOffSet, RegistryValueKind.String);
                latestDowngrade = DateTime.Now.AddDays(latestDowngradeOffSet);

                string notificationQueueName = (string)Registry.GetValue(keyName, "NotificationQueue", ".\\Private$\\fromams");
        //        Registry.SetValue(keyName, "NotificationQueue", notificationQueueName, RegistryValueKind.String);
                m_Queue = new MessageQueue(notificationQueueName);

                string requestQueueName = (string)Registry.GetValue(keyName, "RequestQueue", ".\\Private$\\toams");
        //        Registry.SetValue(keyName, "RequestQueue", requestQueueName, RegistryValueKind.String);
                send_Queue = new MessageQueue(requestQueueName);

                ht.Add(MessType.GateDowngradeCreated, "GateDowngradeCreatedNotification");
                ht.Add(MessType.GateDowngradeUpdated, "GateDowngradeUpdatedNotification");
                ht.Add(MessType.GateDowngradeDeleted, "GateDowngradeDeletedNotification");
                ht.Add(MessType.CarouselDowngradeCreated, "CarouselDowngradeCreatedNotification");
                ht.Add(MessType.CarouselDowngradeUpdated, "CarouselDowngradeUpdatedNotification");
                ht.Add(MessType.CarouselDowngradeDeleted, "CarouselDowngradeDeletedNotification");
                ht.Add(MessType.ChuteDowngradeCreated, "ChuteDowngradeCreatedNotification");
                ht.Add(MessType.ChuteDowngradeUpdated, "ChuteDowngradeUpdatedNotification");
                ht.Add(MessType.ChuteDowngradeDeleted, "ChuteDowngradeDeletedNotification");
                ht.Add(MessType.StandDowngradeCreated, "StandDowngradeCreatedNotification");
                ht.Add(MessType.StandDowngradeUpdated, "StandDowngradeUpdatedNotification");
                ht.Add(MessType.StandDowngradeDeleted, "StandDowngradeDeletedNotification");
                ht.Add(MessType.CheckInDowngradeCreated, "CheckInDowngradeCreatedNotification");
                ht.Add(MessType.CheckInDowngradeUpdated, "CheckInDowngradeUpdatedNotification");
                ht.Add(MessType.CheckInDowngradeDeleted, "CheckInDowngradeDeletedNotification");
            } catch (Exception ex) {
                Controller.SOP("Error in function SetParameters", true);
                Controller.SOP(ex.Message, true);
            }
        }

        private void ClearAllMessages() {
            try {
                m_Queue.Purge();
            } catch (Exception ex) {
                Controller.SOP("Error in function ClearAllMessages", true);
                Controller.SOP(ex.Message, true);
            }
        }

        public void StartMQListener() {
            try {
                this.startListenLoop = true;
                m_ReceiveThread = new System.Threading.Thread(this.ListenToQueue);
                m_ReceiveThread.IsBackground = true;
                m_ReceiveThread.Start();
            } catch (Exception ex) {
                Controller.SOP("Error in function StartMQListener", true);
                Controller.SOP(ex.Message, true);
            }
        }

        public void StopMQListener() {
            try {
                this.startListenLoop = false;
                m_ReceiveThread.Abort();
            } catch (Exception ex) {
                Controller.SOP("Error in function StopMQListener", true);
                Controller.SOP(ex.Message, true);
            }
        }

        private void ListenToQueue() {

            try {
                while (startListenLoop) {
                    using (Message msg = m_Queue.Receive()) {

                        Tuple<MessType, String> res = GetMessageType(msg);

                        if (res.Item1 == MessType.NonResource) {
                            continue;
                        } else {
                            SOP(String.Format("Resetting type {0} at {1}", res.Item1, DateTime.Now));
                            this.GetDownGradesForType(res.Item1);
                        }
                    }
                }
            } catch (Exception ex) {
                Controller.SOP("Error in function ListenToQueue", true);
                Controller.SOP(ex.Message, true);
            }
        }

        private Tuple<MessType, String> GetMessageType(Message msg) {
            StreamReader reader = new StreamReader(msg.BodyStream);

            XNamespace ns = "http://www.sita.aero/ams6-xml-api-messages";
 //           XNamespace ns2 = "http://www.sita.aero/ams6-xml-api-datatypes";
            XElement xmlRoot = XDocument.Parse(reader.ReadToEnd()).Root;
            IEnumerable<XElement> elements = xmlRoot.Descendants();


            //Fliight or Movement Updates
            //int fltmvts = (from n in elements
            //               where (n.Name == ns + "FlightUpdatedNotification" || n.Name == ns + "MovementUpdatedNotification")
            //               select n).Count();

            //if (fltmvts > 0) {
            //    return Tuple.Create(MessType.NonResource, "Flight Or Movement");
            //}

            ////Resource Updates
            //IEnumerable<XElement> res = from n in elements
            //                            where (n.Name == ns + "CheckInUpdatedNotification" || n.Name == ns + "ChuteUpdatedNotification"
            //                            || n.Name == ns + "GateUpdatedNotification" || n.Name == ns + "StandUpdatedNotification"
            //                            || n.Name == ns + "CarouselInUpdatedNotification"
            //                            )
            //                            select n;
            //if (res.Count() > 0) {
            //    IEnumerable<XElement> xNames = from n in elements where n.Name == ns2 + "ExternalName" select n;
            //    Controller.SOP("Resource Updated " + res.First().Name + "  " + xNames.First().Value);
            //    return Tuple.Create(MessType.NonResource, "Flight Or Movement");
            //}

            //foreach (String type in this.notificationMsgs) {

            //    IEnumerable<XElement> nodes = from n in elements where n.Name == ns + type select n;

            //    if (nodes.Count() > 0) {
            //        Controller.SOP("Returning " + type);
            //        return Tuple.Create(MessType.NonResource, "Resource or Downgrade Message");
            //    }
            //}

            //Downgrades
            foreach (MessType type in ht.Keys) {

                IEnumerable<XElement> nodes = from n in elements where n.Name == ns + (string)ht[type] select n;

                if (nodes.Count() > 0) {
                    Controller.SOP("Returning Resource or Downgrade");
                    return Tuple.Create(type, "Resource or Downgrade Message");
                }
            }

            //Default
            return Tuple.Create(MessType.NonResource, "NonResource");
        }
 
        private void GetDownGradesForType(MessType type) {

            switch (type) {
                case MessType.ChuteDowngradeCreated:
                case MessType.ChuteDowngradeUpdated:
                case MessType.ChuteDowngradeDeleted:
                    UpdateChuteDownGrades(true);
                    break;
                case MessType.GateDowngradeCreated:
                case MessType.GateDowngradeUpdated:
                case MessType.GateDowngradeDeleted:
                    UpdateGateDownGrades(true);
                    break;
                case MessType.CheckInDowngradeCreated:
                case MessType.CheckInDowngradeUpdated:
                case MessType.CheckInDowngradeDeleted:
                    UpdateCheckInDownGrades(true);
                    break;
                case MessType.StandDowngradeCreated:
                case MessType.StandDowngradeUpdated:
                case MessType.StandDowngradeDeleted:
                    UpdateStandDownGrades(true);
                    break;
                case MessType.CarouselDowngradeCreated:
                case MessType.CarouselDowngradeUpdated:
                case MessType.CarouselDowngradeDeleted:
                    UpdateCarouselDownGrades(true);
                    break;

            }
        }

        public void SetAllResourceManagers() {
            UpdateCheckInDownGrades();
            UpdateGateDownGrades();
            UpdateChuteDownGrades();
            UpdateStandDownGrades();
            UpdateCarouselDownGrades();
        }

        /*
         * Extracts the individual downgrade messages from the XML
         * and then passes them to the constructor of Downgrades to 
         * create the Downgrade object
         */
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

        public void UpdateGateDownGrades(bool reset = false) {
            try {
                AMSIntegrationServiceClient client = new AMSIntegrationServiceClient();
                XmlElement x = client.GetGateDowngrades(TOKEN, earliestDowngrade, latestDowngrade, APT_CODE, WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv.AirportIdentifierType.IATACode);
                client.Close();
                ResourceManager rm = (ResourceManager)resourceManagersTable["Gate"];
                rm.SetDowngrades(this.CreateDowngrades(x, "GateDowngrade"), reset);
            } catch (Exception ex) {
                SOP(ex.Message, true);
            }

        }
        public void UpdateCheckInDownGrades(bool reset = false) {
            try {
                AMSIntegrationServiceClient client = new AMSIntegrationServiceClient();
                XmlElement x = client.GetCheckInDowngrades(TOKEN, earliestDowngrade, latestDowngrade, APT_CODE, WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv.AirportIdentifierType.IATACode);
                client.Close();
                ResourceManager rm = (ResourceManager)resourceManagersTable["CheckIn"];
                rm.SetDowngrades(this.CreateDowngrades(x, "CheckInDowngrade"), reset);
            } catch (Exception ex) {
                SOP(ex.Message, true);
            }
        }

        public void UpdateChuteDownGrades(bool reset = false) {
            try {
                AMSIntegrationServiceClient client = new AMSIntegrationServiceClient();
                XmlElement x = client.GetChuteDowngrades(TOKEN, earliestDowngrade, latestDowngrade, APT_CODE, WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv.AirportIdentifierType.IATACode);
                client.Close();
                ResourceManager rm = (ResourceManager)resourceManagersTable["Chute"];
                rm.SetDowngrades(this.CreateDowngrades(x, "ChuteDowngrade"), reset);
            } catch (Exception ex) {
                SOP(ex.Message, true);
            }
        }

        public void UpdateStandDownGrades(bool reset = false) {
            try {
                AMSIntegrationServiceClient client = new AMSIntegrationServiceClient();
                XmlElement x = client.GetStandDowngrades(TOKEN, earliestDowngrade, latestDowngrade, APT_CODE, WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv.AirportIdentifierType.IATACode);
                client.Close();
                ResourceManager rm = (ResourceManager)resourceManagersTable["Stand"];
                rm.SetDowngrades(this.CreateDowngrades(x, "StandDowngrade"), reset);
            } catch (Exception ex) {
                SOP(ex.Message, true);
            }
        }

        public void UpdateCarouselDownGrades(bool reset = false) {
            try {
                AMSIntegrationServiceClient client = new AMSIntegrationServiceClient();
                XmlElement x = client.GetCarouselDowngrades(TOKEN, earliestDowngrade, latestDowngrade, APT_CODE, WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv.AirportIdentifierType.IATACode);
                client.Close();
                ResourceManager rm = (ResourceManager)resourceManagersTable["Carousel"];
                rm.SetDowngrades(this.CreateDowngrades(x, "CarouselDowngrade"), reset);
            } catch (Exception ex) {
                SOP(ex.Message, true);
            }
        }

        public static void SOP(string str, bool error = false) {
            if (consoleLog > 0) {
                Console.WriteLine(str);

                if (consoleLog > 0) {
                    string path = LOGFILEPATH + @"\AMSStatusWidgetService.log";
                    if (!File.Exists(path)) {
                        // Create a file to write to.
                        using (StreamWriter sw = File.CreateText(path)) {
                            sw.WriteLine("Start of Log ");
                        }
                    }

                    using (StreamWriter sw = File.AppendText(path)) {
                        sw.WriteLine(str);
                    }
                }

                if (logEvents) {
                    if (error) {
                        eventLog1.WriteEntry(str, EventLogEntryType.Error);
                    } else {
                        eventLog1.WriteEntry(str, EventLogEntryType.Information);
                    }
                }
            }
        }
    }
}

