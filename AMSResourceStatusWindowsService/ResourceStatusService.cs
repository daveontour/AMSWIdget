using System.ServiceProcess;
using AMSResourceStatusWidget;
using System.Runtime.InteropServices;

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
            if (!System.Diagnostics.EventLog.SourceExists("AMSResourceStatus")) {
                System.Diagnostics.EventLog.CreateEventSource(
                    "AMSResourceStatus", "AMSResourceStatusLog");
            }
            eventLog1.Source = "AMSResourceStatus";
            eventLog1.Log = "AMSResourceStatusLog";
        }

        protected override void OnStart(string[] args) {
            Controller.SOP("OnStart Start");
            eventLog1.WriteEntry("Starting AMSResourceStatusWidget");
            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            controller = new Controller();
            controller.SetConsoleLogging(false);
            controller.SetEventLogger(eventLog1);
            controller.InitService();
            Controller.SOP("OnStart Controller Init Complete");

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            Controller.SOP("OnStart Stop");

        }

        protected override void OnStop() {
            eventLog1.WriteEntry("Stopping AMSResourceStatusWidget");
            Controller.SOP("OnStop Start");

            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            
            Controller.SOP("OnStop Controller Suspend Start");
            controller.Suspend();
            Controller.SOP("OnStop Controller Suspend Start");

            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            Controller.SOP("OnStop Stop");

        }


        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);
    }
}
