using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;
using MedieticaeWebApi.Filters;
using MedieticaWebApiService.Helpers;
using MedieticaWebApiService.Models;
using Newtonsoft.Json;

namespace MedieticaWebApiService
{
	static class Program
	{
		public static UtentiDb ute = null;

		public static Startup startup_options;
		public static bool flag_nosync = false;
		public static bool flag_service = false;
		public static bool flag_remove = false;
		public static bool flag_modify = false;

		public static string app_name = "MedieticaWebApiService";
		public static string log_name = "Application";
		
		/// <summary>
		/// Punto di ingresso principale dell'applicazione.
		/// </summary>
		static void Main()
		{
			var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
			path = System.IO.Path.GetDirectoryName(path);
			if (path != null) Directory.SetCurrentDirectory(path);
			
#if DEBUG
		System.Diagnostics.Debugger.Launch();
#endif

#if (DEBUG)
			if (path != null) path = Directory.GetParent(path)?.FullName;
			if (path != null) path = Directory.GetParent(path)?.FullName;
			var cfg_path = path + @"\cfg\configD.json";
#else
			var cfg_path = path + @"\cfg\config.json";
#endif
			ute = null;
			startup_options = new Startup();
			startup_options.AppName = "MedieticaWebApi";
			startup_options.LogName = "Application";
			if (!EventLog.SourceExists(app_name)) EventLog.CreateEventSource(app_name, log_name);
			try
			{
				using (var r = new StreamReader(cfg_path))
				{
					var json = r.ReadToEnd();
					startup_options = JsonConvert.DeserializeObject<Startup>(json);
					startup_options.AppName = "MedieticaWebApi";
					startup_options.LogName = "Medietica Web Api Service";

					if (string.IsNullOrWhiteSpace(startup_options.Host)) throw new MCException(MCException.InvalidHostMsg, MCException.InvalidHostErr);
					if (string.IsNullOrWhiteSpace(startup_options.Archivio)) throw new MCException(MCException.InvalidDbMsg, MCException.InvalidDbErr);
					if (startup_options.DbPort <= 0) throw new MCException(MCException.InvalidPortMsg, MCException.InvalidPortErr);
					if (string.IsNullOrWhiteSpace(startup_options.User)) throw new MCException(MCException.InvalidUserMsg, MCException.InvalidUserErr);
					if (string.IsNullOrWhiteSpace(startup_options.Password)) throw new MCException(MCException.InvalidPasswordMsg, MCException.InvalidPasswordErr);

					if (startup_options.CacheUtenti == 0) startup_options.CacheUtenti = 30;
					if (startup_options.DurataToken == 0) startup_options.DurataToken = 60;

					if (string.IsNullOrWhiteSpace(startup_options.ReportPath)) startup_options.ReportPath = path + @"\Reports";
				}
			}
			catch (MCException ex)
			{
				EventLog.WriteEntry(app_name, ex.Message, EventLogEntryType.Warning);
				Environment.Exit(1);
				return;
			}
			catch (OdbcException ex)
			{
				EventLog.WriteEntry(app_name, ex.Message, EventLogEntryType.Warning);
				Environment.Exit(2);
			}
			catch (DirectoryNotFoundException ex)
			{
				EventLog.WriteEntry(app_name, ex.Message + "\n\n" + path, EventLogEntryType.Warning);
				Environment.Exit(3);
				return;
			}
			catch (Exception ex)
			{
				EventLog.WriteEntry(app_name, ex.Message, EventLogEntryType.Warning);
				Environment.Exit(4);
				return;
			}

#if CONSOLEDEBUG
			new SelfHostingService(startup_options.ssl).Start();
			Console.WriteLine("Press Enter to close");
			Console.ReadLine();
#else
			var services_to_run = new ServiceBase[]
			{
				new SelfHostingService(startup_options.ssl)
			};
			ServiceBase.Run(services_to_run);
#endif
		}
		
	}
}
