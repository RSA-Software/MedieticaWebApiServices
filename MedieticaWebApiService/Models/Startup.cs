using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedieticaWebApiService.Models
{
	public class Startup
	{
		public Startup()
		{
			Host = "";
			Archivio = "";
			DbPort = 0;
			User = "";
			Password = "";
			TcpPort = 0;
			DbType = 0;
			CacheUtenti = 0;
			DurataToken = 0;
			DocPath = "";
			EmailTemplate = "";
			AppName = "";
			LogName = "";
			ssl = false;
			SmtpServer = "";
			SmtpPort = 0;
			SmtpUser = "";
			SmtpPassword = "";
			SmtpSsl = false;
			ReportPath = "";
		}

		//
		// Dati inizializzati nel Json di configurazione
		//
		public string Host { get; set; }
		public string Archivio { get; set; }
		public int DbPort { get; set; }
		public string User { get; set; }
		public string Password { get; set; }
		public int TcpPort { get; set; }
		public int DbType { get; set; }
		public int CacheUtenti { get; set; }
		public int DurataToken { get; set; }
		public string DocPath{ get; set; }
		public string EmailTemplate { get; set; }
		public bool ssl { get; set; }
		public string SmtpServer { get; set; }
		public int SmtpPort { get; set; }
		public string SmtpUser { get; set; }
		public string SmtpPassword { get; set; }
		public bool SmtpSsl { get; set; }
		public string ReportPath { get; set; }


		//
		// Dati inizializzati all'avvio del software
		// 
		public string AppName { get; set; }
		public string LogName { get; set; }
	}
}
