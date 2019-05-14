using AMSResourceStatusWidget;
using System;
using System.Diagnostics;

namespace ConsoleApp2 {
    class Program {
        private static Controller controller;

        static void Main(string[] args) {
            System.Diagnostics.EventLog eventLog1 = new System.Diagnostics.EventLog();
            if (!EventLog.SourceExists("AMSResourceStatus")) {
                EventLog.CreateEventSource("AMSResourceStatus", "AMSResourceStatusLog");
            }
            eventLog1.Source = "AMSResourceStatus";
            eventLog1.Log = "AMSResourceStatusLog";

            controller = new Controller(eventLog1);
            controller.SetConsoleLogging(true);
            controller.InitService();

            Console.ReadLine();

        }
    }
}
