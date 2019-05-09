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
        public static  MessageQueue send_Queue;
        private static bool consoleLog = true;
        public static string TOKEN;
        public static string BASE_URI;
        public static string APT_CODE;
        public static int BIG_RESET_TIME;
        public bool startListenLoop = true;
        private readonly Hashtable ht = new Hashtable();
        private readonly Hashtable resourceManagersTable = new Hashtable();
        private System.Threading.Thread m_ReceiveThread;
        private readonly string[] types = { "CheckIn", "Gate", "Stand", "Chute", "Carousel" };
        private readonly string[] notificationMsgs = {
           "CheckiInUpdatedNotification",
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


        public Controller() {

            foreach (String type in types) {
                resourceManagersTable.Add(type, new ResourceManager(type));
            }
        }

        public void  SetConsoleLogging(bool log) {
            Controller.consoleLog = log;
        }

        public void SetEventLogger(EventLog eventLogger) {
            eventLog1 = eventLogger;
            logEvents = true;
        }

        public void InitCommon() {

            SOP("Setting Parameters");
            this.SetParameters();
            SOP("Clearing Messages");
            this.ClearAllMessages();
            SOP("Starting Listener");
            this.StartMQListener();
            SOP("Setting resource Managers");
            this.SetAllResourceManagers();

            // Set a time to reset everything periodically
            SOP("Setting Big Reset Timer");
            this.resetTimer = new Timer {
                AutoReset = true,
                Interval = 1000 * 60 * BIG_RESET_TIME,  //87 Minutes
                Enabled = true
            };

            this.resetTimer.Elapsed += (source, eventArgs) => {
                Controller.SOP("Periodic Reset ");
                this.SetAllResourceManagers();
            };
        }

        public async void InitConsole() {

            this.InitCommon();

            foreach (String type in types) {
                Controller.SOP(resourceManagersTable[type].ToString());
            }

            foreach (String type in types) {
                ResourceManager rm = (ResourceManager)resourceManagersTable[type];
                Controller.SOP("Applying downgrade status for resources: " + type);
                await rm.ResetDowngrades();
            }
        }

        public async void InitService() {
            SOP("Start of INIT");
            this.InitCommon();
            SOP("End of INIT");

            foreach (String type in types) {
                ResourceManager rm = (ResourceManager)resourceManagersTable[type];
                SOP("Resetting Downgrasdes for " + type);
                 await rm.ResetDowngrades();
            }
            SOP("End of InitService");
        }

        public void Suspend() {
            SOP("Suspending");
            this.StopMQListener();
            SOP("Listener stopped");
            foreach (String type in types) {
                ResourceManager rm = (ResourceManager)resourceManagersTable[type];
                rm.Suspend();
                SOP("Stopped RM " + type);
            }

        }


        private void SetParameters() {

            const string userRoot = "HKEY_CURRENT_USER";
            const string subkey = "RegistrySetValueExample";
            const string keyName = userRoot + "\\" + subkey;

            APT_CODE = (string)Registry.GetValue(keyName, "IATAAirportCode", "DOH");
            Registry.SetValue(keyName, "IATAAirportCode", APT_CODE, RegistryValueKind.String);
            SOP(APT_CODE);

            restAPIGetBase = "/api/v1/"+APT_CODE+"/{0}s";
            SOP(restAPIGetBase);

            TOKEN = (string)Registry.GetValue(keyName, "Token", "b406564f-44aa-4e51-a80a-aa9ed9a04ec6");
            Registry.SetValue(keyName, "Token", TOKEN, RegistryValueKind.String);
            SOP(TOKEN);

            BASE_URI = (string)Registry.GetValue(keyName, "BaseURI", "http://localhost/");
            Registry.SetValue(keyName, "BaseURI", BASE_URI, RegistryValueKind.String);
            SOP(BASE_URI);

            BIG_RESET_TIME = (int)Registry.GetValue(keyName, "ResetTime", 87);
            Registry.SetValue(keyName, "ResetTime", BIG_RESET_TIME, RegistryValueKind.DWord);
            

            int earliestDowngradeOffSet = Int32.Parse((string)Registry.GetValue(keyName, "EarliestDowngradeOffset", "-20"));
            Registry.SetValue(keyName, "EarliestDowngradeOffset", earliestDowngradeOffSet, RegistryValueKind.String);
            earliestDowngrade = DateTime.Now.AddDays(earliestDowngradeOffSet);
            SOP(earliestDowngrade.ToString());

            int latestDowngradeOffSet = Int32.Parse((string)Registry.GetValue(keyName, "LatestDowngradeOffset", "20"));
            Registry.SetValue(keyName, "LatestDowngradeOffset", latestDowngradeOffSet, RegistryValueKind.String);
            latestDowngrade = DateTime.Now.AddDays(latestDowngradeOffSet);
            SOP(latestDowngrade.ToString());

            string notificationQueueName  = (string)Registry.GetValue(keyName, "NotificationQueue", ".\\Private$\\fromams");
            Registry.SetValue(keyName, "NotificationQueue", notificationQueueName, RegistryValueKind.String);
            m_Queue = new MessageQueue(notificationQueueName);
            SOP(notificationQueueName);

            string requestQueueName = (string)Registry.GetValue(keyName, "RequestQueue", ".\\Private$\\toams");
            Registry.SetValue(keyName, "RequestQueue", requestQueueName, RegistryValueKind.String);
            send_Queue = new MessageQueue(requestQueueName);
            SOP(requestQueueName);

 
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
        }

        private void ClearAllMessages() {
            try {
                m_Queue.Purge();
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return;
            }

            //Dont read them in to avoid allocation of memory
            Controller.SOP("Cleared Message From Queue.");
        }
        public void StartMQListener() {
            this.startListenLoop = true;
            m_ReceiveThread = new System.Threading.Thread(this.ListenToQueue);
            m_ReceiveThread.IsBackground = true;
            m_ReceiveThread.Name = "Listen For MSMQ Messages Thread";
            m_ReceiveThread.Start();
        }

        public void StopMQListener() {
            SOP("Stopping MQ Listener");
            try {
                this.startListenLoop = false;
                m_ReceiveThread.Abort();
            } catch (Exception ex) {
                SOP(ex.Message);
            }
        }

        private void ListenToQueue() {
            Controller.SOP("Listening for Message Notifications");

            while (startListenLoop) {
                using (Message msg = m_Queue.Receive()) {

                    Tuple<MessType, String> res = GetMessageType(msg);

                    if (res.Item1 == MessType.NonResource) {
                        continue;
                    } else {
                        this.ResetType(res.Item1);
                    }
                }
            }
        }

        private Tuple<MessType, String> GetMessageType(Message msg) {
            StreamReader reader = new StreamReader(msg.BodyStream);

            XNamespace ns = "http://www.sita.aero/ams6-xml-api-messages";
            XNamespace ns2 = "http://www.sita.aero/ams6-xml-api-datatypes";
            XElement xmlRoot = XDocument.Parse(reader.ReadToEnd()).Root;
            IEnumerable<XElement> elements = xmlRoot.Descendants();


            //Fliight or Movement Updates
            int fltmvts = (from n in elements
                           where (n.Name == ns + "FlightUpdatedNotification" || n.Name == ns + "MovementUpdatedNotification")
                           select n).Count();

            if (fltmvts > 0) {
                return Tuple.Create(MessType.NonResource, "Flight Or Movement");
            }

            //Resource Updates
            IEnumerable<XElement> res = from n in elements
                                        where (n.Name == ns + "CheckInUpdatedNotification" || n.Name == ns + "ChuteUpdatedNotification"
                                        || n.Name == ns + "GateUpdatedNotification" || n.Name == ns + "StandUpdatedNotification"
                                        || n.Name == ns + "CarouselInUpdatedNotification"
                                        )
                                        select n;
            if (res.Count() > 0) {
                IEnumerable<XElement> xNames = from n in elements where n.Name == ns2 + "ExternalName" select n;
                Controller.SOP("Resource Updated " + res.First().Name + "  " + xNames.First().Value);
                return Tuple.Create(MessType.NonResource, "Flight Or Movement");
            }

            foreach (String type in this.notificationMsgs) {

                IEnumerable<XElement> nodes = from n in elements where n.Name == ns + type select n;

                if (nodes.Count() > 0) {
                    Controller.SOP("Returning " + type);
                    return Tuple.Create(MessType.NonResource, "Resource or Downgrade Message");
                }
            }

            //Downgrades
            foreach (MessType type in ht.Keys) {

                IEnumerable<XElement> nodes = from n in elements where n.Name == ns + (string)ht[type] select n;

                if (nodes.Count() > 0) {
                    Controller.SOP("Returning Resource or Downgrade");
                    return Tuple.Create(type, "Resource or Downgrade Message");
                }
            }

            //Default
            Controller.SOP("Returning Non Resource");
            return Tuple.Create(MessType.NonResource, "NonResource");
        }
        private void ResetType(MessType type) {
            Controller.SOP(String.Format("Resetting type {0}", type));
            this.GetDownGradesForType(type);
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
            SOP("Update Checkin Downgrades");
            UpdateCheckInDownGrades();
            SOP("Update Gate Downgrades");
            UpdateGateDownGrades();
            SOP("Update Chute Downgrades");
            UpdateChuteDownGrades();
            SOP("Update Stand Downgrades");
            UpdateStandDownGrades();
            SOP("Update Carousel Downgrades");
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
            AMSIntegrationServiceClient client = new AMSIntegrationServiceClient();
            XmlElement x = client.GetGateDowngrades(TOKEN, earliestDowngrade, latestDowngrade, APT_CODE, WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv.AirportIdentifierType.IATACode);
            client.Close();
            ResourceManager rm = (ResourceManager)resourceManagersTable["Gate"];
            rm.SetDowngrades(this.CreateDowngrades(x, "GateDowngrade"), reset);
        }
        public void UpdateCheckInDownGrades(bool reset = false) {
            try {
                SOP("Getting client");
                AMSIntegrationServiceClient client = new AMSIntegrationServiceClient();
                SOP("Client Get");
                XmlElement x = client.GetCheckInDowngrades(TOKEN, earliestDowngrade, latestDowngrade, APT_CODE, WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv.AirportIdentifierType.IATACode);
                SOP("Client Close");
                client.Close();
                ResourceManager rm = (ResourceManager)resourceManagersTable["CheckIn"];
                SOP("RM Set Downgrade");
                rm.SetDowngrades(this.CreateDowngrades(x, "CheckInDowngrade"), reset);
            } catch (Exception ex) {
                SOP(ex.Message);
            }
        }

        public void UpdateChuteDownGrades(bool reset = false) {
            AMSIntegrationServiceClient client = new AMSIntegrationServiceClient();
            XmlElement x = client.GetChuteDowngrades(TOKEN, earliestDowngrade, latestDowngrade, APT_CODE, WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv.AirportIdentifierType.IATACode);
            client.Close();
            ResourceManager rm = (ResourceManager)resourceManagersTable["Chute"];
            rm.SetDowngrades(this.CreateDowngrades(x, "ChuteDowngrade"), reset);
        }

        public void UpdateStandDownGrades(bool reset = false) {
            AMSIntegrationServiceClient client = new AMSIntegrationServiceClient();
            XmlElement x = client.GetStandDowngrades(TOKEN, earliestDowngrade, latestDowngrade, APT_CODE, WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv.AirportIdentifierType.IATACode);
            client.Close();
            ResourceManager rm = (ResourceManager)resourceManagersTable["Stand"];
            rm.SetDowngrades(this.CreateDowngrades(x, "StandDowngrade"), reset);
        }

        public void UpdateCarouselDownGrades(bool reset = false) {
            AMSIntegrationServiceClient client = new AMSIntegrationServiceClient();
            XmlElement x = client.GetCarouselDowngrades(TOKEN, earliestDowngrade, latestDowngrade, APT_CODE, WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv.AirportIdentifierType.IATACode);
            client.Close();
            ResourceManager rm = (ResourceManager)resourceManagersTable["Carousel"];
            rm.SetDowngrades(this.CreateDowngrades(x, "CarouselDowngrade"), reset);
        }

        public static void SOP(string str) {
            if (consoleLog) {
                Console.WriteLine(str);
            }

            if (logEvents) {
                eventLog1.WriteEntry(str);

                string path = @"c:\Users\dave_\Desktop\Service.log";
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
        }
    }
}

