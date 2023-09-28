using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Odbc;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using Chilkat;
using MedieticaeWebApi.Filters;
using MedieticaWebApiService.Helpers;
using MedieticaWebApiService.Models;
using Newtonsoft.Json;
using RestSharp;
using StringBuilder = System.Text.StringBuilder;

namespace MedieticaWebApiService.Controller
{
	public class CantieriData
	{
		public int can_dit { get; set; }
		public int can_codice { get; set; }
		public short can_tipo { get; set; }
		public string can_desc { get; set; }
		public string dit_desc { get; set; }
		public string dit_piva { get; set; }
		public string dit_codfis { get; set; }
		
		public CantieriData()
		{
			can_dit = 0;
			can_codice = 0;
			can_desc = "";
			dit_desc = "";
			dit_piva = "";
			dit_codfis = "";
			can_tipo = 0;
		}
	}

	public class UserData
	{
		public UtentiDb user;
		public DateTime? start_token { get; set; }
		public int expires_in { get; set; }
		public string token { get; set; }
		public DitteDb ditta { get; set; }

		public List<DitteDb> dit_list;
		public List<CantieriData> can_list;
		public List<PermessiDb> per_list; 

		public UserData()
		{
			user = null;
			expires_in = 0;
			token = "";
			ditta = null;
			dit_list = null;
			can_list = null;
			per_list = null;
		}
	}

	public class Credentials
	{
		public string user { get; set; }
		public string password { get; set; }
		public bool dev_mode { get; set; }

		public Credentials()
		{
			user = "";
			password = "";
			dev_mode = false;
		}
	}

	[EnableCors("*", "*", "*")]

	public class UserController : ApiController
	{

		[HttpPost]
		[OverrideAuthentication]
		public DefaultJson<UserData> Login([FromBody] Credentials val)
		{
			if (val == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Unauthorized, "Null credentials"));
			if (string.IsNullOrWhiteSpace(val.user)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Unauthorized, "Invalid user"));
			if (string.IsNullOrWhiteSpace(val.password)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Unauthorized, "Invalid password"));

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var rec = new UserData();
					var sql = UtentiDb.GetJoinQuery() + "\n" + "WHERE ute_email = ? AND ute_password = crypt(?, ute_password)";

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var json = new DefaultJson<UserData>();
					cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
					cmd.Parameters.Clear();
					cmd.Parameters.Add("@usr", OdbcType.VarChar).Value = val.user;
					cmd.Parameters.Add("@pwd", OdbcType.VarChar).Value = val.password;
					var reader = cmd.ExecuteReader();
					var found = false;
					while (reader.Read())
					{
						found = true;
						rec.user = new UtentiDb();
						DbUtils.SqlRead(ref reader, ref rec.user);
						break;
					}
					reader.Close();

					if (found)
					{
						var dit_arr = new List<int>();

						sql = "SELECT * FROM uteditte WHERE utd_ute = ? ORDER BY utd_default DESC";
						cmd.CommandText = DbUtils.QueryAdapt(sql, 20);
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codute", OdbcType.Int).Value = rec.user.ute_codice;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var utd = new UtentiDitteDb();
							DbUtils.SqlRead(ref reader, ref utd, UtentiDitteDb.GetJoinExcludeFields());
							dit_arr.Add(utd.utd_dit);
						}
						reader.Close();

						var first = true;
						foreach (var utd in dit_arr)
						{
							var dit = new DitteDb();
							if (DitteDb.Search(ref cmd, utd, ref dit, true))
							{
								if (first) rec.ditta = dit;
								if (rec.dit_list == null) rec.dit_list = new List<DitteDb>();
								rec.dit_list.Add(dit);
								first = false;
							}
						}
						if (first) throw new HttpResponseException(HttpStatusCode.Unauthorized);
						
						if (rec.user.ute_type == (int)UserType.SPECIAL)
						{
							sql = @"
							SELECT usc_can AS can_codice, usc_tipo AS can_tipo, can_desc, usc_dit AS can_dit, dit_desc, dit_piva, dit_codfis
							FROM usrcantieri
							INNER JOIN ditte ON usc_dit = dit_codice
							INNER JOIN cantieri ON (usc_dit = can_dit AND usc_can = can_codice)
							WHERE usc_email = ?";
							cmd.CommandText = sql;
							cmd.Parameters.Clear();
							cmd.Parameters.Add("email", OdbcType.VarChar).Value = rec.user.ute_email;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var xxx= new CantieriData();
								DbUtils.SqlRead(ref reader, ref xxx);
								if (rec.can_list == null) rec.can_list = new List<CantieriData>();
								rec.can_list.Add(xxx);
							}
							reader.Close();
						}

						//
						// Leggiamo i gruppi
						//
						var utg_arr = new List<int>();
						cmd.CommandText = "SELECT * FROM uteusg WHERE utg_ute = ?";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codute", OdbcType.Int).Value = rec.user.ute_codice;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var utg = new UteUsgDb();
							DbUtils.SqlRead(ref reader, ref utg, UteUsgDb.GetJoinExcludeFields());
							utg_arr.Add(utg.utg_usg);
						}
						reader.Close();
						
						//
						// Leggiamo i permessi
						//
						UtentiDb.LoadPermessi(ref cmd, rec.user.ute_codice, ref rec.per_list);


						var glob = new Global();
						if (!glob.UnlockBundle(DbUtils.GetUnlockCode())) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Conflict, "Chilkat Unlock Code Invalid"));

						var jwt = new Jwt();

						//  Build the JOSE header
						var jose = new JsonObject();
						if (!jose.AppendString("alg", "HS256")) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Conflict, "Chilkat Function Fails"));
						if (!jose.AppendString("typ", "JWT")) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Conflict, "Chilkat Function Fails"));

						var claims = new JsonObject();
						if (!claims.AppendString("iss", "https://www.rsaweb.com")) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Conflict, "Chilkat Function Fails"));
						if (!claims.AppendString("sub", val.user)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Conflict, "Chilkat Function Fails"));
						if (!claims.AppendString("aud", Request.Headers.Host)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Conflict, "Chilkat Function Fails"));

						if (!claims.AppendString("level", rec.user.ute_level.ToString())) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Conflict, "Chilkat Function Fails"));
						if (!claims.AppendString("type", rec.user.ute_type.ToString())) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Conflict, "Chilkat Function Fails"));
						if (!claims.AppendString("user", rec.user.ute_codice.ToString())) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Conflict, "Chilkat Function Fails"));
						if (!claims.AppendString("ditte", string.Join(",", dit_arr.Select(n => n.ToString()).ToArray()))) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Conflict, "Chilkat Function Fails"));
						if (!claims.AppendString("gruppi", string.Join(",", utg_arr.Select(n => n.ToString()).ToArray()))) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Conflict, "Chilkat Function Fails"));

						//  Set the timestamp of when the JWT was created to now.
						var cur_date_time = jwt.GenNumericDate(0);
						if (!claims.AddIntAt(-1, "iat", cur_date_time)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Conflict, "Chilkat Function Fails"));

						//  Set the "not process before" timestamp to now.
						if (!claims.AddIntAt(-1, "nbf", cur_date_time)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Conflict, "Chilkat Function Fails"));

						//  Set the timestamp defining an expiration time (end time) for the token
						//  to be now + 1 hour (3600 seconds)
						if (!claims.AddIntAt(-1, "exp", cur_date_time + DbUtils.GetStartupOptions().DurataToken * 60)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Conflict, "Chilkat Function Fails"));
						rec.expires_in = DbUtils.GetStartupOptions().DurataToken * 60;
						rec.start_token = DateTime.Now;

						//  Produce the smallest possible JWT:
						jwt.AutoCompact = true;

						rec.token = jwt.CreateJwt(jose.Emit(), claims.Emit(), "FWX322HYYMOKHGRT1560OPN67XZATR27");

						if (json.Data == null) json.Data = new List<UserData>();
						json.Data.Add(rec);
						json.RecordsTotal = 1;
					}

					connection.Close();

					if (json.RecordsTotal == 0) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Unauthorized, "Utente o password non validi"));
					return (json);

				}
			}
			catch (MCException ex)
			{
				throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message));
			}
			catch (OdbcException ex)
			{
				throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message + " " + ex.StackTrace));
			}
			catch (HttpResponseException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message + " " + ex.StackTrace));
			}
		}


		[HttpPut]
		[OverrideAuthentication]
		[Route("api/user/logout/{codice}")]
		[Route("api/user/logout/{codice}/{dev_mode}")]
		public void Logout(int codice, bool devMode = false)
		{
			try
			{
			}
			catch (MCException ex)
			{
				var res = new McResponse(ExceptionsType.MC_EXCEPTION, ex.GetError(), ex.Message, ex.GetStackTrace());
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			catch (OdbcException ex)
			{
				var err = 0;
				if (ex.Errors.Count > 0) err = ex.Errors[0].NativeError;
				var res = new McResponse(ExceptionsType.ODBC_EXCEPTION, err, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}
	}
}
