using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
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
            eventLog1.WriteEntry("Starting AMSResourceStatusWidget");
            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            controller = new Controller();
            controller.SetConsoleLogging(false);
            controller.ServiceInit();

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

        }

        protected override void OnPause() {
            eventLog1.WriteEntry("Pausing AMSResourceStatusWidget");

        }

        protected override void OnContinue() {
            eventLog1.WriteEntry("Continuing AMSResourceStatusWidget");

        }

        protected override void OnStop() {
            eventLog1.WriteEntry("Stopping AMSResourceStatusWidget");

        }

        protected override void OnShutdown() {
            eventLog1.WriteEntry("Shutting Down AMSResourceStatusWidget");
            // Update the service state to Stop Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            // Code for stopping goes here

            // Update the service state to Stopped.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);
    }
}
