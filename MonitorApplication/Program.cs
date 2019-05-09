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

namespace MonitorApplication {


    class Program {

        public MessageQueue m_Queue;
        private System.Threading.Thread m_ReceiveThread;
        private bool startListenLoop = true;
        private static bool consoleLog = true;

        private readonly Hashtable ht = new Hashtable();

        private readonly string[] notificationMsgs = {
           "CheckiInUpdatedNotification",
           "GateUpdatedNotification",
           "ChuteUpdatedNotification",
           "StandUpdatedNotification",
           "CarouselUpdatedNotification"
            };

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

        static void Main(string[] args) {

            Program p = new Program();

        }

        public Program() {

            m_Queue = new MessageQueue(".\\Private$\\fromams2");

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

            this.StartMQListener();

            Console.ReadLine();
        }

        private void ClearAllMessages() {
            try {
                m_Queue.Purge();
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return;
            }

            //Dont read them in to avoid allocation of memory
            SOP("Cleared Message From Queue.");
        }
        public void StartMQListener() {
            this.startListenLoop = true;
            m_ReceiveThread = new System.Threading.Thread(this.ListenToQueue);
            m_ReceiveThread.IsBackground = true;
            m_ReceiveThread.Name = "Listen For MSMQ Messages Thread";
            m_ReceiveThread.Start();
        }

        public void StopMQListener() {
            this.startListenLoop = false;
            m_ReceiveThread.Name = "Shutting down MSMQ Messages Thread";
            m_ReceiveThread.Abort();
        }

        private void ListenToQueue() {
            SOP("Listening for Message Notifications");

            while (startListenLoop) {
                using (Message msg = m_Queue.Receive()) {

                    Tuple<MessType, String> res = GetMessageType(msg);


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
                SOP("Flight or Movement Update: " + DateTime.Now);
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
                SOP("Resource Updated " + res.First().Name + "  " + xNames.First().Value);
                return Tuple.Create(MessType.NonResource, "Flight Or Movement");
            }

            foreach (String type in this.notificationMsgs) {

                IEnumerable<XElement> nodes = from n in elements where n.Name == ns + type select n;

                if (nodes.Count() > 0) {
                    SOP("Resource Update Message (" + type + ") " + DateTime.Now);
                    return Tuple.Create(MessType.NonResource, "Resource Update Message ("+type+") "+ DateTime.Now);
                }
            }

            //Downgrades
            foreach (MessType type in ht.Keys) {

                IEnumerable<XElement> nodes = from n in elements where n.Name == ns + (string)ht[type] select n;

                if (nodes.Count() > 0) {
                    SOP("Resource Downgrade Message (" + type + ") " + DateTime.Now);
                    return Tuple.Create(type, "Resource or Downgrade Message");
                }
            }

            //Default
            SOP("Returning Non Resource");
            return Tuple.Create(MessType.NonResource, "NonResource");
        }

        public static void SOP(string str) {
            if (consoleLog) {
                Console.WriteLine(str);
            }
        }
    }
}
