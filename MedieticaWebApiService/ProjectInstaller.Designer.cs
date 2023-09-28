namespace MedieticaWebApiService
{
	partial class ProjectInstaller
	{
		/// <summary>
		/// Variabile di progettazione necessaria.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Pulire le risorse in uso.
		/// </summary>
		/// <param name="disposing">ha valore true se le risorse gestite devono essere eliminate, false in caso contrario.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Codice generato da Progettazione componenti

		/// <summary>
		/// Metodo necessario per il supporto della finestra di progettazione. Non modificare
		/// il contenuto del metodo con l'editor di codice.
		/// </summary>
		private void InitializeComponent()
		{
			this.MedieticaWebApiServiceInstaller = new System.ServiceProcess.ServiceProcessInstaller();
			this.MedieticaWebApiService = new System.ServiceProcess.ServiceInstaller();
			// 
			// MedieticaWebApiServiceInstaller
			// 
			this.MedieticaWebApiServiceInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
			this.MedieticaWebApiServiceInstaller.Password = null;
			this.MedieticaWebApiServiceInstaller.Username = null;
			this.MedieticaWebApiServiceInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.FacileWebApiServiceInstaller_AfterInstall);
			// 
			// MedieticaWebApiService
			// 
			this.MedieticaWebApiService.ServiceName = "MedieticaWebApiService";
			this.MedieticaWebApiService.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.FacileWebApiService_AfterInstall);
			// 
			// ProjectInstaller
			// 
			this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.MedieticaWebApiServiceInstaller,
            this.MedieticaWebApiService});

		}

		#endregion

		private System.ServiceProcess.ServiceProcessInstaller MedieticaWebApiServiceInstaller;
		private System.ServiceProcess.ServiceInstaller MedieticaWebApiService;
	}
}