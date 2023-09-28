using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace MedieticaeWebApi.Filters
{
	public class AuthenticationFailureResult : IHttpActionResult
	{
		public AuthenticationFailureResult(string reason_phrase, HttpRequestMessage request)
		{
			ReasonPhrase = reason_phrase;
			Request = request;
		}

		public string ReasonPhrase { get; private set; }

		public HttpRequestMessage Request { get; private set; }

		public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellation_token)
		{
			return Task.FromResult(Execute());
		}

		private HttpResponseMessage Execute()
		{
			var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
			{
				RequestMessage = Request,
				ReasonPhrase = ReasonPhrase
			};
			return response;
		}
	}
}