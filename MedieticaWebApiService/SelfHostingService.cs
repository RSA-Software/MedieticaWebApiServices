using System;
using System.Net.Http;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;
using System.Web.Http.SelfHost.Channels;
using MedieticaeWebApi.Filters;
using System.ServiceModel.Channels;

namespace MedieticaWebApiService
{
	public class Interceptor : DelegatingHandler
	{
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var response = await base.SendAsync(request, cancellationToken);
			response.Headers.Add("Access-Control-Allow-Origin", "*");
			response.Headers.Add("Access-Control-Allow-Methods", "GET,POST,PATCH,DELETE,PUT,OPTIONS");
			response.Headers.Add("Access-Control-Allow-Headers", "Origin, Content-Type, X-Auth-Token, content-type");
			return response;
		}

	}

	public class ExtendedHttpSelfHostConfiguration : HttpSelfHostConfiguration
	{
		public ExtendedHttpSelfHostConfiguration(string baseAddress) : base(baseAddress) { }
		public ExtendedHttpSelfHostConfiguration(Uri baseAddress) : base(baseAddress) { }
		

		protected override BindingParameterCollection OnConfigureBinding(HttpBinding httpBinding)
		{
			if (BaseAddress.ToString().ToLower().Contains("https://"))
			{
				httpBinding.Security.Mode = HttpBindingSecurityMode.Transport;
			}

			return base.OnConfigureBinding(httpBinding);
		}
	}

	public partial class SelfHostingService : ServiceBase
	{
		private bool ssl { get; set; }

		public SelfHostingService(bool sslStatus = false)
		{
			ssl = sslStatus;
			InitializeComponent();
		}
#if DEBUG
		public void Start()
        {
			OnStart(null);
        }	
#endif
		protected override void OnStart(string[] args)
		{
			// #if DEBUG
			// 			System.Diagnostics.Debugger.Launch();
			// #endif

			var tcp_port = Program.startup_options.TcpPort != 0 ? Program.startup_options.TcpPort.ToString() : "4059";

			if (ssl)
			{
				var uri = "https://localhost:" + tcp_port;
				var config = new ExtendedHttpSelfHostConfiguration(uri);

				// Servizi e configurazione dell'API Web
				config.EnableCors();
/*
				config.MaxReceivedMessageSize = 209715200;	// 200 MB
				config.MaxBufferSize = 209715200;           // 200 MB
*/

				config.MaxReceivedMessageSize = 10485760;  // 10 MB
				config.MaxBufferSize = 10485760;           // 10 MB
				config.ReceiveTimeout = new TimeSpan(0, 30, 0);
				config.SendTimeout = new TimeSpan(0, 30, 0);
				config.MapHttpAttributeRoutes();  // Enable attribute based routing
				config.Formatters.JsonFormatter.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Local;

				config.Routes.MapHttpRoute(
					name: "API",
					routeTemplate: "api/{controller}/{action}/{id}/{id2}/{id3}",
					defaults: new { id = RouteParameter.Optional, id2 = RouteParameter.Optional, id3 = RouteParameter.Optional },
					constraints: null
				);

				config.Filters.Add(new AuthenticationFilter());

				var server = new HttpSelfHostServer(config);
				server.OpenAsync().Wait();

			}
			else
			{
				var uri = "http://localhost:" + tcp_port;
				var config = new HttpSelfHostConfiguration(uri);
				
				// Servizi e configurazione dell'API Web
				config.EnableCors();
/*
				config.MaxReceivedMessageSize = 209715200;	// 200 MB
				config.MaxBufferSize = 209715200;           // 200 MB
*/
				config.MaxReceivedMessageSize = 10485760;  // 10 MB
				config.MaxBufferSize = 10485760;           // 10 MB
				config.ReceiveTimeout = new TimeSpan(0, 30, 0);
				config.SendTimeout = new TimeSpan(0, 30, 0);
				config.MapHttpAttributeRoutes();  // Enable attribute based routing
				config.Formatters.JsonFormatter.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Local;

				config.Routes.MapHttpRoute(
					name: "API",
					routeTemplate: "api/{controller}/{action}/{id}/{id2}/{id3}",
					defaults: new { id = RouteParameter.Optional, id2 = RouteParameter.Optional, id3 = RouteParameter.Optional },
					constraints: null
				);

				config.Filters.Add(new AuthenticationFilter());

				var server = new HttpSelfHostServer(config);
				server.OpenAsync().Wait();
			}
		}

		protected override void OnStop()
		{
		}
	}
}
