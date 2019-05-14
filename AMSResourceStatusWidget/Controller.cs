using System;
using System.Collections.Generic;
using System.Linq;

using System.Messaging;
using System.IO;
using System.Collections;
using System.Xml.Linq;
using System.Timers;
using System.Diagnostics;

namespace AMSResourceStatusWidget {
    public class Controller {

        public static Parameters param;
        private static MessageQueue recvQueue;
        private bool startListenLoop = true;
        private readonly Hashtable ht = new Hashtable();
        private readonly Hashtable resourceManagersTable = new Hashtable();
        private System.Threading.Thread receiveThread;
        private readonly string[] types = { "CheckIn", "Gate", "Stand", "Chute", "Carousel" };
        private Timer resetTimer;
        private static EventLog eventLog1;
        private static bool logToConsole = false;

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
                    SOP(Parameters.ToString());
                    SOP("Start Clear Messagess");
                    this.ClearAllMessages();
                    SOP("Start MQ Listener");
                    this.StartMQListener();

                    // Populate the resource managers with their downgrades (hold off on setting them)
                    foreach (String type in types) {
                        ResourceManager rm = (ResourceManager)resourceManagersTable[type];
                        rm.UpdateDowngrades();
                    }

                    // Set a time to reset everything periodically
                    this.resetTimer = new Timer {
                        AutoReset = true,
                        Interval = 60000 * Parameters.BIG_RESET_TIME,  
                        Enabled = true
                    };

                    this.resetTimer.Elapsed += (source, eventArgs) =>
                    {
                        SOP("*****************************************************");
                        SOP("*****  Resetting all Resource Manager Downgrades  ***");
                        SOP("*****************************************************\n");
                        foreach (String type in types) {
                            ResourceManager rm = (ResourceManager)resourceManagersTable[type];
                            rm.UpdateDowngrades(true);
                        }
                        SOP("***************************************************************");
                        SOP("*****  Resetting all Resource Manager Downgrades - Complete ***");
                        SOP("***************************************************************\n");

                    };
                } catch (Exception ex) {
                    Controller.SOP("Error in function InitCommon", true);
                    Controller.SOP(ex.Message, true);
                }

                foreach (String type in types) {
                    ResourceManager rm = (ResourceManager)resourceManagersTable[type];
                    await rm.ResetDowngrades();
                    SOP(rm.ToString());
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

                param = new Parameters(eventLog1);

                recvQueue = new MessageQueue(Parameters.RECVQ);

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
                eventLog1.WriteEntry("Set Parameters Error");
                eventLog1.WriteEntry(ex.Message);
                Controller.SOP("Error in function SetParameters", true);
                Controller.SOP(ex.Message, true);
            }
        }

        private void ClearAllMessages() {
            try {
                recvQueue.Purge();
            } catch (Exception ex) {
                Controller.SOP("Error in function ClearAllMessages", true);
                Controller.SOP(ex.Message, true);
            }
        }

        public void StartMQListener() {
            try {
                this.startListenLoop = true;
                receiveThread = new System.Threading.Thread(this.ListenToQueue) {
                    IsBackground = true
                };
                receiveThread.Start();
            } catch (Exception ex) {
                Controller.SOP("Error in function StartMQListener", true);
                Controller.SOP(ex.Message, true);
            }
        }

        public void StopMQListener() {
            try {
                this.startListenLoop = false;
                receiveThread.Abort();
            } catch (Exception ex) {
                Controller.SOP("Error in function StopMQListener", true);
                Controller.SOP(ex.Message, true);
            }
        }

        private void ListenToQueue() {

            try {
                while (startListenLoop) {
                    using (Message msg = recvQueue.Receive()) {

                        MessType res = GetMessageType(msg);

                        if (res == MessType.NonResource) {
                            continue;
                        } else {
                            SOP(String.Format("Resetting type {0} at {1}", res, DateTime.Now));
                            this.UpdateDownGradesForType(res);
                        }
                    }
                }
            } catch (Exception ex) {
                Controller.SOP("Error in function ListenToQueue", true);
                Controller.SOP(ex.Message, true);
            }
        }

        private MessType GetMessageType(Message msg) {
            StreamReader reader = new StreamReader(msg.BodyStream);

            XNamespace ns = "http://www.sita.aero/ams6-xml-api-messages";
            XNamespace ns2 = "http://www.sita.aero/ams6-xml-api-datatypes";
            XElement xmlRoot = XDocument.Parse(reader.ReadToEnd()).Root;
            IEnumerable<XElement> elements = xmlRoot.Descendants();

            //Check  if it was one of the downgrade types we are interested in

            foreach (MessType type in ht.Keys) {
                IEnumerable<XElement> nodes = from n in elements where n.Name == ns + (string)ht[type] select n;
                if (nodes.Count() > 0) {
                    return type;
                }
            }

            return MessType.NonResource;
        }

        private void UpdateDownGradesForType(MessType type) {

            ResourceManager rm;

            switch (type) {
                case MessType.ChuteDowngradeCreated:
                case MessType.ChuteDowngradeUpdated:
                case MessType.ChuteDowngradeDeleted:
                    rm = (ResourceManager)resourceManagersTable["Chute"];
                    break;
                case MessType.GateDowngradeCreated:
                case MessType.GateDowngradeUpdated:
                case MessType.GateDowngradeDeleted:
                    rm = (ResourceManager)resourceManagersTable["Gate"];
                    break;
                case MessType.CheckInDowngradeCreated:
                case MessType.CheckInDowngradeUpdated:
                case MessType.CheckInDowngradeDeleted:
                    rm = (ResourceManager)resourceManagersTable["CheckIn"];
                    break;
                case MessType.StandDowngradeCreated:
                case MessType.StandDowngradeUpdated:
                case MessType.StandDowngradeDeleted:
                    rm = (ResourceManager)resourceManagersTable["Stand"];
                    break;
                case MessType.CarouselDowngradeCreated:
                case MessType.CarouselDowngradeUpdated:
                case MessType.CarouselDowngradeDeleted:
                    rm = (ResourceManager)resourceManagersTable["Carousel"];
                    break;
                default:
                    return;
            }
            rm.UpdateDowngrades(true);
        }

        public void SetConsoleLogging(bool log) {
            logToConsole = log;
        }

        public static void SOP(int v) {
            SOP(v.ToString());
        }
        public static void SOP(string str, bool error = false) {

            //File specified in the app config file
            try {
                if (Parameters.CONSOLE_LOG) {

                    string path = Parameters.LOGFILEPATH;
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
            } catch (Exception ex) {
                if (Parameters.LOGEVENTS) {
                    eventLog1.WriteEntry(ex.Message, EventLogEntryType.Error);
                }
            }

            //The console if running as a console app
            if (logToConsole) {
                Console.WriteLine(DateTime.Now + "  " + str);
            }

            // The event log
            if (Parameters.LOGEVENTS) {
                if (error) {
                    eventLog1.WriteEntry(str, EventLogEntryType.Error);
                } else {
                    if (!Parameters.EVENT_LOG_ERROR_ONLY) {
                        eventLog1.WriteEntry(str, EventLogEntryType.Information);
                    }
                }
            }
        }
    }
}

