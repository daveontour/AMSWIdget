using System;
using System.Diagnostics;
using System.Configuration;

namespace AMSResourceStatusWidget {
    public class Parameters {

        public static string TOKEN;
        public static string BASE_URI;
        public static string APT_CODE;
        public static int BIG_RESET_TIME;
        public static string LOGFILEPATH;
        public static bool CONSOLE_LOG;
        public static int EARLIEST_DOWNGRADE_DAYS;
        public static int LATEST_DOWNGRADE_DAYS;
        public static string RECVQ;
        public static string RESTAPIBASE;
        public static string SENDQ;
        public static DateTime EARLIEST_DOWNGRADE;
        public static DateTime LATEST_DOWNGRADE;
        public static bool LOGEVENTS;

        public static int cl;
        public static int le;

        public Parameters(EventLog eventLog1) {

            eventLog1.WriteEntry("Start Parameters");

            try {
    
                APT_CODE = (string)ConfigurationManager.AppSettings["IATAAirportCode"];
                TOKEN = (string)ConfigurationManager.AppSettings["Token"];
                LOGFILEPATH = (string)ConfigurationManager.AppSettings["LogFilePath"];
                BASE_URI = (string)ConfigurationManager.AppSettings["BaseURI"];
                BIG_RESET_TIME = Int32.Parse((string)ConfigurationManager.AppSettings["ResetTime"]);
                RECVQ = (string)ConfigurationManager.AppSettings["NotificationQueue"];
                SENDQ = (string)ConfigurationManager.AppSettings["RequestQueue"];
                EARLIEST_DOWNGRADE_DAYS = Int32.Parse((string)ConfigurationManager.AppSettings["EarliestDowngradeOffset"]);
                LATEST_DOWNGRADE_DAYS = Int32.Parse((string)ConfigurationManager.AppSettings["LatestDowngradeOffset"]);
                EARLIEST_DOWNGRADE = DateTime.Now.AddDays(Parameters.EARLIEST_DOWNGRADE_DAYS);
                LATEST_DOWNGRADE = DateTime.Now.AddDays(Parameters.LATEST_DOWNGRADE_DAYS);
                RESTAPIBASE = @"/api/v1/" + APT_CODE + "/{0}s";

                cl = Int32.Parse((string)ConfigurationManager.AppSettings["ConsoleLog"]);
                le = Int32.Parse((string)ConfigurationManager.AppSettings["EventLog"]);

                if (cl > 0) {
                    CONSOLE_LOG = true;
                } else {
                    CONSOLE_LOG = false;
                }

                if (le > 0) {
                    LOGEVENTS = true;
                } else {
                    LOGEVENTS = false;
                }

            } catch (Exception ex) {
                eventLog1.WriteEntry(ex.Message);
            }
        }

        public static String ToString() {
            return String.Format("\nConfiguration Parameters \nAirportCode: {0}\nSecurity Token: {1}\nReceive Queue: {2}\nSend Queue: {3}\nLog File Path: {4}\nRestAPI Base: {5}\nEarliest Downgrade: {6}\nLatest Downgrade: {7}\nEarliest Days: {8}\nLatest Days: {9}\nConsole Logging: {10}\nEvent Logging: {11}\n",
                        Parameters.APT_CODE,
                        Parameters.TOKEN,
                        Parameters.RECVQ,
                        Parameters.SENDQ,
                        Parameters.LOGFILEPATH,
                        Parameters.RESTAPIBASE,
                        Parameters.EARLIEST_DOWNGRADE.ToString(),
                        Parameters.LATEST_DOWNGRADE.ToString(),
                        Parameters.EARLIEST_DOWNGRADE_DAYS,
                        Parameters.LATEST_DOWNGRADE_DAYS,
                        Parameters.cl,
                        Parameters.le);
        }
    }
}