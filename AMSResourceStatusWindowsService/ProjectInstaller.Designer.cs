using Microsoft.Win32;

namespace AMSResourceStatusWindowsService {
    partial class ProjectInstaller {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProjectInstaller));
            this.serviceProcessInstaller1 = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstaller1 = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceProcessInstaller1
            // 
            this.serviceProcessInstaller1.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcessInstaller1.Password = null;
            this.serviceProcessInstaller1.Username = null;
            // 
            // serviceInstaller1
            // 
            this.serviceInstaller1.Description = resources.GetString("serviceInstaller1.Description");
            this.serviceInstaller1.DisplayName = "AMS Resource Status Widget - Proof Of Concept";
            this.serviceInstaller1.ServiceName = "AMSResourceStatusWidget";
            this.serviceInstaller1.StartType = System.ServiceProcess.ServiceStartMode.Automatic;


            // Set the defualt registry keys
            const string userRoot = "HKEY_LOCAL_MACHINE\\SOFTWARE\\SITA";
            const string subkey = "AMSResourceStatusWidget";
            const string keyName = userRoot + "\\" + subkey;


            Registry.SetValue(keyName, "IATAAirportCode", "DOH", RegistryValueKind.String);
            Registry.SetValue(keyName, "Token", "b406564f-44aa-4e51-a80a-aa9ed9a04ec6", RegistryValueKind.String);
            Registry.SetValue(keyName, "LogFilePath", "C:\\", RegistryValueKind.String);
            Registry.SetValue(keyName, "BaseURI", "http://localhost/", RegistryValueKind.String);
            Registry.SetValue(keyName, "ResetTime", "107", RegistryValueKind.String);
            Registry.SetValue(keyName, "ConsoleLog", 0, RegistryValueKind.DWord);
            Registry.SetValue(keyName, "EarliestDowngradeOffset", "-20", RegistryValueKind.String);
            Registry.SetValue(keyName, "LatestDowngradeOffset", "20", RegistryValueKind.String);
            Registry.SetValue(keyName, "NotificationQueue", ".\\Private$\\fromams", RegistryValueKind.String);
            Registry.SetValue(keyName, "RequestQueue", ".\\Private$\\toams", RegistryValueKind.String);
 

            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceProcessInstaller1,
            this.serviceInstaller1});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller1;
        private System.ServiceProcess.ServiceInstaller serviceInstaller1;
    }
}