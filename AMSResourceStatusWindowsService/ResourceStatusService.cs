using System.ServiceProcess;
using AMSResourceStatusWidget;
using System.Runtime.InteropServices;
using System.Diagnostics;


namespace AMSResourceStatusWindowsService {

    public enum ServiceState {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus {
        public int dwServiceType;
        public ServiceState dwCurrentState;
        public int dwControlsAccepted;
        public int dwWin32ExitCode;
        public int dwServiceSpecificExitCode;
        public int dwCheckPoint;
        public int dwWaitHint;
    };
    public partial class ResourceStatusService : ServiceBase {

        Controller controller;
        public ResourceStatusService() {

            CanStop = true;
 
            InitializeComponent();
            eventLog1 = new System.Diagnostics.EventLog();
            if (!EventLog.SourceExists("AMSResourceStatus")) {
                EventLog.CreateEventSource("AMSResourceStatus", "AMSResourceStatusLog");
            }
            eventLog1.Source = "AMSResourceStatus";
            eventLog1.Log = "AMSResourceStatusLog";
        }

        protected override void OnStart(string[] args) {
            eventLog1.WriteEntry("Starting AMS Resource Status Widget", EventLogEntryType.Information);

            ServiceStatus serviceStatus = new ServiceStatus {
                dwCurrentState = ServiceState.SERVICE_START_PENDING,
                dwWaitHint = 100000
            };
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            controller = new Controller(eventLog1);
            controller.InitService();

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            eventLog1.WriteEntry("Started AMS Resource Status Widget", EventLogEntryType.Information);

        }

        protected override void OnStop() {
            eventLog1.WriteEntry("Stopping AMS Resource Status Widget", EventLogEntryType.Information);

            ServiceStatus serviceStatus = new ServiceStatus {
                dwCurrentState = ServiceState.SERVICE_STOP_PENDING,
                dwWaitHint = 100000
            };
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            
            controller.Suspend();

            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            eventLog1.WriteEntry("Stopped AMS Resource Status Widget", EventLogEntryType.Information);

        }


        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);
    }
}
