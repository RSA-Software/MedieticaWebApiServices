using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Odbc;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Filters;
using System.Web.Http.Results;
using MedieticaWebApiService.Helpers;
using MedieticaWebApiService.Models;

namespace MedieticaeWebApi.Filters
{
	public class AddChallengeOnUnauthorizedResult : IHttpActionResult
	{
		public AddChallengeOnUnauthorizedResult(AuthenticationHeaderValue challenge, IHttpActionResult inner_result)
		{
			Challenge = challenge;
			InnerResult = inner_result;
		}

		public AuthenticationHeaderValue Challenge { get; private set; }

		public IHttpActionResult InnerResult { get; private set; }

		public async Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellation_token)
		{
			HttpResponseMessage response = await InnerResult.ExecuteAsync(cancellation_token);

			if (response.StatusCode == HttpStatusCode.Unauthorized)
			{
				// Only add one challenge per authentication scheme.
				if (response.Headers.WwwAuthenticate.All(h => h.Scheme != Challenge.Scheme))
				{
					response.Headers.WwwAuthenticate.Add(Challenge);
				}
			}

			return response;
		}
	}
	
	public class Users
	{
		public string user_name { get; set; }
		public string password { get; set; }
		public DateTime last_update { get; set; }
		

		public Users()
		{
			user_name = "";
			password = "";
			last_update = DateTime.Now;
		}

		public Users(string usr, string pwd)
		{
			user_name = usr;
			password = pwd;
			last_update = DateTime.Now;
		}
	}
	
	public class AuthenticationFilter : IAuthenticationFilter
	{
		private static List<Users> _users;

		public bool AllowMultiple => false;

		private string ComputeSha256Hash(string raw_data)
		{
			// Create a SHA256   
			using (SHA256 sha256_hash = SHA256.Create())
			{
				// ComputeHash - returns byte array  
				var bytes = sha256_hash.ComputeHash(Encoding.UTF8.GetBytes(raw_data));

				// Convert byte array to a string   
				var builder = new StringBuilder();
				for (var i = 0; i < bytes.Length; i++)
				{
					builder.Append(bytes[i].ToString("x2"));
				}
				return builder.ToString();
			}
		}


		private Tuple<string, string> ExtractUserNameAndPassword(string str)
		{
			var decoded_authentication_token = Encoding.UTF8.GetString(Convert.FromBase64String(str));
			var username_password_array = decoded_authentication_token.Split(':');
			var data = Tuple.Create(username_password_array[0], username_password_array[1]);
			return (data);
		}

		public async Task<IPrincipal> TryAuthenticateAsync(string user_name, string password, CancellationToken cancellation_token)
		{
			IPrincipal princ = null;
			var found = false;

			//
			// Cerchiamo se presente nella cache
			//
			if (_users != null)
			{
				for (var idx = 0; idx < _users.Count(); idx++)
				{
					if (_users[idx].user_name == user_name && _users[idx].password == password)
					{
						found = true;
						break;
					}
					var t = DateTime.Now - _users[idx].last_update;
					if (t.Days > 0) found = false;
					if (t.Hours > 0) found = false;
					if (t.Minutes > DbUtils.GetStartupOptions().CacheUtenti) found = false;
					_users.RemoveAt(idx);
					idx--;
				}
			}

			//
			// Se non trovato nella cache cerchiamo nel Database
			//
			if (!found)
			{
				try
				{
					using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
					{
						const string sql = @"
						SELECT *
						FROM utenti
						WHERE ute_email = ? AND ute_password = crypt(?,ute_password)
						";
						
						connection.Open();
						var cmd = new OdbcCommand {Connection = connection};
						cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
						cmd.Parameters.Clear();
						cmd.Parameters.Add("usr", OdbcType.VarChar).Value = user_name;
						cmd.Parameters.Add("pwd_clear", OdbcType.VarChar).Value = password;
						var reader = await cmd.ExecuteReaderAsync(cancellation_token);
						while (reader.Read())
						{
							found = true;
							break;
						}
						reader.Close();
						connection.Close();
						if (found)
						{
							var usr = new Users(user_name, password);
							if (_users == null) _users = new List<Users>();
							_users.Add(usr);
						}
					}
				}
				catch (OdbcException ex)
				{
					if (!EventLog.SourceExists(DbUtils.GetStartupOptions().AppName)) EventLog.CreateEventSource(DbUtils.GetStartupOptions().AppName, DbUtils.GetStartupOptions().LogName);
					EventLog.WriteEntry(DbUtils.GetStartupOptions().AppName, "Authentication error : " + ex.Message, EventLogEntryType.Warning);
				}
				catch (DbException ex)
				{
					if (!EventLog.SourceExists(DbUtils.GetStartupOptions().AppName)) EventLog.CreateEventSource(DbUtils.GetStartupOptions().AppName, DbUtils.GetStartupOptions().LogName);
					EventLog.WriteEntry(DbUtils.GetStartupOptions().AppName, "Authentication error : " + ex.Message, EventLogEntryType.Warning);
				}
			}

			princ = found ? new ClaimsPrincipal(new GenericIdentity(user_name)) : null;
			return princ;
		}


		public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellation_token)
		{
			// 1. Look for credentials in the request.
			HttpRequestMessage request = context.Request;

          
			if (request.Method == HttpMethod.Post)
			{
				if (request.RequestUri.ToString().ToLower().Contains("/utenti"))
				{
					context.Principal = new ClaimsPrincipal(new GenericIdentity("anonymus"));
					return;
				}
			}
			if (request.Method == HttpMethod.Get)
			{
				if (request.RequestUri.ToString().ToLower().Contains("/test"))
				{
					context.Principal = new ClaimsPrincipal(new GenericIdentity("anonymus"));
					return;
				}
			}

			//
			// Escludiamo dall'autenticazione gli endpoints necessari alla visualizzazione del qrcode
			//
			if (request.Method == HttpMethod.Get)
			{
				if (request.RequestUri.ToString().ToLower().Contains("/qrcode"))
				{
					context.Principal = new ClaimsPrincipal(new GenericIdentity("anonymus"));
					return;
				}
				if (request.RequestUri.ToString().ToLower().Contains("/allegati/getbyid/"))
				{
					context.Principal = new ClaimsPrincipal(new GenericIdentity("anonymus"));
					return;
				}
				if (request.RequestUri.ToString().ToLower().Contains("/imgmodelli"))
				{
					context.Principal = new ClaimsPrincipal(new GenericIdentity("anonymus"));
					return;
				}
				if (request.RequestUri.ToString().ToLower().Contains("/imgmezzi"))
				{
					context.Principal = new ClaimsPrincipal(new GenericIdentity("anonymus"));
					return;
				}
			}


			AuthenticationHeaderValue authorization = request.Headers.Authorization;

			// 2. If there are no credentials, do nothing.
			if (authorization == null)
			{
				context.ErrorResult = new AuthenticationFailureResult("Missing Authorization Header Request", request);
				return;
			}

			// 3. If there are credentials but the filter does not recognize the 
			//    authentication scheme, do nothing.
			if (authorization.Scheme != "Basic" && authorization.Scheme != "OAuth")
			{
				context.ErrorResult = new AuthenticationFailureResult("Basic or JWT Token Authentication Requested", request);
				return;
			}
			
			// 4. If there are credentials that the filter understands, try to validate them.
			// 5. If the credentials are bad, set the error result.
			if (String.IsNullOrEmpty(authorization.Parameter))
			{
				context.ErrorResult = new AuthenticationFailureResult("Missing credentials", request);
				return;
			}

			// In caso di autorizzazione tramite token Verifichiamo la validità
			if (authorization.Scheme == "OAuth")
			{
				var token = "";
				var start = authorization.Parameter.IndexOf("oauth_token=", StringComparison.Ordinal);
				var end = authorization.Parameter.Length;
				if (start >= 0)
				{
					start += 13;
					while (start < end)
					{
						if (authorization.Parameter[start] == '"') break;
						token += authorization.Parameter[start];
						start++;
					}
				}

				if (string.IsNullOrEmpty(token))
				{
					context.ErrorResult = new AuthenticationFailureResult("Missing credentials", request);
					return;
				}


				var glob = new Chilkat.Global();
				if (!glob.UnlockBundle(DbUtils.GetUnlockCode())) {context.ErrorResult = new AuthenticationFailureResult("Chilkat Unlock Code Error", request); return; }

				var jwt = new Chilkat.Jwt();
				if (!jwt.VerifyJwt(token, "FWX322HYYMOKHGRT1560OPN67XZATR27")) { context.ErrorResult = new AuthenticationFailureResult("Invalid Token", request); return; }

				var leeway = 60;
				if (!jwt.IsTimeValid(token, leeway)) { context.ErrorResult = new AuthenticationFailureResult("Expired Token", request); return; }
				var payload = jwt.GetPayload(token);
				var json = new Chilkat.JsonObject();
				if (!json.Load(payload)) { context.ErrorResult = new AuthenticationFailureResult("Invalid Payload", request); return; }

				var iss = json.StringOf("iss").Trim();
				var aud = json.StringOf("aud").Trim();
				if (string.Compare(iss, "https://www.rsaweb.com", StringComparison.CurrentCulture) != 0) { context.ErrorResult = new AuthenticationFailureResult("Invalid Issuer", request); return; }
				if (string.Compare(aud, request.Headers.Host, StringComparison.CurrentCulture) != 0) { context.ErrorResult = new AuthenticationFailureResult("Invalid Audience", request); return; }
				
				var str = json.StringOf("sub");
				context.Principal = new ClaimsPrincipal(new GenericIdentity(!string.IsNullOrWhiteSpace(str) ? str : "DefaultUser"));
				return;
			}

			Tuple<string, string> user_name_and_pasword = ExtractUserNameAndPassword(authorization.Parameter);
			if (user_name_and_pasword == null)
			{
				context.ErrorResult = new AuthenticationFailureResult("Invalid credentials", request);
			}

			if (user_name_and_pasword != null)
			{
				var user_name = user_name_and_pasword.Item1;
				var password = user_name_and_pasword.Item2;

				var principal = await TryAuthenticateAsync(user_name, password, cancellation_token);
				//IPrincipal principal = TryAuthenticate(user_name, password, cancellation_token);
				if (principal == null)
				{
					context.ErrorResult = new AuthenticationFailureResult("Invalid username or password", request);
				}

				// 6. If the credentials are valid, set principal.
				else
				{
					context.Principal = principal;
				}
			}
			else context.ErrorResult = new AuthenticationFailureResult("Invalid username or password", request);
		}

		public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellation_token)
		{
			var challenge = new AuthenticationHeaderValue("Basic");
			context.Result = new AddChallengeOnUnauthorizedResult(challenge, context.Result);
			return Task.FromResult(0);
		}
	}
}