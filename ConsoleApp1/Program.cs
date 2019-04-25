using System;
using System.Collections.Generic;
using System.Linq;

using System.Messaging;
using System.Xml;
using System.IO;
using System.Collections;
using System.Xml.Linq;
using System.Timers;

namespace ConsoleApp1 {

    class Program {
        private MessageQueue m_Queue;
        private MessageQueue send_Queue;
        public static string TOKEN;
        public static string BASE_URI;
        public static string APT_CODE;
        public static string MSGQ_NAME = ".\\Private$\\toams";
        private String updateDesk;
        private Hashtable ht = new Hashtable();
        private Hashtable resourceManagersTable = new Hashtable();
        private System.Threading.Thread m_ReceiveThread;
        private string[] types = { "CheckIn", "Gate", "Stand", "Chute", "Carousel" };
        private string[] notificationMsgs = {
           "CheckiInUpdatedNotification",
           "GateUpdatedNotification",
           "ChuteUpdatedNotification",
           "StandUpdatedNotification",
           "CarouselUpdatedNotification"
            };
        public static String restAPIGetBase = "/api/v1/DOH/{0}s";
        DateTime to = DateTime.Now.AddDays(10);
        DateTime from = DateTime.Now.AddDays(-20);
        Timer resetTimer;

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


        public Program() {

            foreach (String type in types) {
                resourceManagersTable.Add(type, new ResourceManager(type));
            }
        }

        public async void ConsoleInit() {

            this.SetParameters();
            this.ClearAllMessages();
            this.StartMQListener();

            this.SetAllResourceManagers();

            foreach (String type in types) {
                Console.WriteLine(resourceManagersTable[type].ToString());
            }

            foreach (String type in types) {
                ResourceManager rm = (ResourceManager)resourceManagersTable[type];
                Console.WriteLine("Applying downgrade status for resources: " + type);
                await rm.resetDowngrades();
            }


            this.resetTimer = new Timer();
            this.resetTimer.AutoReset = true;
            this.resetTimer.Interval = 1000 * 60 * 87;  //87 Minutes
            this.resetTimer.Elapsed += (source, eventArgs) =>  {
                Console.WriteLine("Periodic Reset ");
                this.SetAllResourceManagers();
            };
            this.resetTimer.Enabled = true;


        }
        static void Main(string[] args) {

            Program p = new Program();
            p.ConsoleInit();

            Console.ReadLine();
        }

        private void SetParameters() {


            this.m_Queue = new MessageQueue(".\\Private$\\fromams");
            this.send_Queue = new MessageQueue(".\\Private$\\toams");
            TOKEN = "b406564f-44aa-4e51-a80a-aa9ed9a04ec6";
            BASE_URI = "http://localhost/";
            APT_CODE = "DOH";
            MSGQ_NAME = ".\\Private$\\fromamstoresourcewidget";

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
            //ht.Add(MessType.CheckInUpdated, "CheckInUpdatedNotification");
            //ht.Add(MessType.GateUpdated, "GateUpdatedNotification");
            //ht.Add(MessType.ChuteUpdated, "ChuteUpdatedNotification");
            //ht.Add(MessType.StandUpdated, "StandUpdatedNotification");
            //ht.Add(MessType.CarouselUpdated, "CarouselUpdatedNotification");

        }

        private void ClearAllMessages() {
            Message[] msgs = m_Queue.GetAllMessages();
            m_Queue.Purge();
            Console.WriteLine("Cleared {0} Message From Queue.", msgs.Length);
        }
        public void StartMQListener() {
            m_ReceiveThread = new System.Threading.Thread(this.ListenToQueue);
            m_ReceiveThread.IsBackground = true;
            m_ReceiveThread.Name = "Listen For MSMQ Messages Thread";
            m_ReceiveThread.Start();
        }

        private void ListenToQueue() {
            Console.WriteLine("Listening for Message Notifications");
            int i = 0;
            while (true) {
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
                Console.WriteLine("Resource Updated " + res.First().Name + "  " + xNames.First().Value);
                return Tuple.Create(MessType.NonResource, "Flight Or Movement");
            }

            foreach (String type in this.notificationMsgs) {

                IEnumerable<XElement> nodes = from n in elements where n.Name == ns + type select n;

                if (nodes.Count() > 0) {
                    Console.WriteLine("Returning " + type);
                    return Tuple.Create(MessType.NonResource, "Resource or Downgrade Message");
                }
            }

            //Downgrades
            foreach (MessType type in ht.Keys) {

                IEnumerable<XElement> nodes = from n in elements where n.Name == ns + (string)ht[type] select n;

                if (nodes.Count() > 0) {
                    Console.WriteLine("Returning Resource or Downgrade");
                    return Tuple.Create(type, "Resource or Downgrade Message");
                }
            }

            //Default
            Console.WriteLine("Returning Non Resource");
            return Tuple.Create(MessType.NonResource, "NonResource");
        }
        private void ResetType(MessType type) {
            Console.WriteLine("Resetting type {0}", type);
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

        public void UpdateGateDownGrades(Boolean reset = false) {
            AMSIntegrationServiceClient client = new AMSIntegrationServiceClient();
            XmlElement x = client.GetGateDowngrades(TOKEN, from, to, APT_CODE, WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv.AirportIdentifierType.IATACode);
            ResourceManager rm = (ResourceManager)resourceManagersTable["Gate"];
            rm.SetDowngrades(this.CreateDowngrades(x, "GateDowngrade"));
            client.Close();
            if (reset) {
                rm.resetDowngrades();
            }
        }
        public void UpdateCheckInDownGrades(Boolean reset = false) {
            AMSIntegrationServiceClient client = new AMSIntegrationServiceClient();
            XmlElement x = client.GetCheckInDowngrades(TOKEN, from, to, APT_CODE, WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv.AirportIdentifierType.IATACode);
            ResourceManager rm = (ResourceManager)resourceManagersTable["CheckIn"];
            rm.SetDowngrades(this.CreateDowngrades(x, "CheckInDowngrade"));
            client.Close();
            if (reset) {
                rm.resetDowngrades();
            }
        }

        public void UpdateChuteDownGrades(Boolean reset = false) {
            AMSIntegrationServiceClient client = new AMSIntegrationServiceClient();
            XmlElement x = client.GetChuteDowngrades(TOKEN, from, to, APT_CODE, WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv.AirportIdentifierType.IATACode);
            ResourceManager rm = (ResourceManager)resourceManagersTable["Chute"];
            rm.SetDowngrades(this.CreateDowngrades(x, "ChuteDowngrade"));
            client.Close();
            if (reset) {
                rm.resetDowngrades();
            }
        }

        public void UpdateStandDownGrades(Boolean reset = false) {
            AMSIntegrationServiceClient client = new AMSIntegrationServiceClient();
            XmlElement x = client.GetStandDowngrades(TOKEN, from, to, APT_CODE, WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv.AirportIdentifierType.IATACode);
            ResourceManager rm = (ResourceManager)resourceManagersTable["Stand"];
            rm.SetDowngrades((this.CreateDowngrades(x, "StandDowngrade")));
            client.Close();
            if (reset) {
                rm.resetDowngrades();
            }
        }

        public void UpdateCarouselDownGrades(Boolean reset = false) {
            AMSIntegrationServiceClient client = new AMSIntegrationServiceClient();
            XmlElement x = client.GetCarouselDowngrades(TOKEN, from, to, APT_CODE, WorkBridge.Modules.AMS.AMSIntegrationWebAPI.Srv.AirportIdentifierType.IATACode);
            ResourceManager rm = (ResourceManager)resourceManagersTable["Carousel"];
            rm.SetDowngrades(this.CreateDowngrades(x, "CarouselDowngrade"));
            client.Close();
            if (reset) {
                rm.resetDowngrades();
            }
        }

    }
}
