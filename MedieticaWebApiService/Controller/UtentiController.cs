using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using MedieticaWebApiService.Extensions;
using MedieticaWebApiService.Helpers;
using MedieticaWebApiService.Models;

namespace MedieticaWebApiService.Controller
{ 
	[EnableCors("*", "*", "*")]
	public class UtentiController : ApiController
	{
		[HttpGet]
		[Route("api/utenti/blank")]
		[Route("api/utenti/blank/{ditta}")]

		public DefaultJson<UtentiDb> Blank(int ditta = 0)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var json = new DefaultJson<UtentiDb>();

					cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(ute_codice),0) AS codice FROM utenti");
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var ute = new UtentiDb();
						ute.ute_codice = 1 + reader.GetInt32(reader.GetOrdinal("codice"));
						if (json.Data == null) json.Data = new List<UtentiDb>();
						json.Data.Add(ute);
						json.RecordsTotal++;
					}
					reader.Close();
					connection.Close();

					return (json);
				}
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

		[HttpGet]
		[Route("api/utenti/get")]
		public DefaultJson<UtentiDb> GetList(int top = 0, int skip = 0, string orderby = "", string search = "", string filter = "", bool inlinecount = false, bool joined = false)
		{
			if (filter.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger filter value"));
			if (search.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger search value"));
			if (orderby.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger orderby value"));

			try
			{
				var ditte = DbUtils.GetTokenDitte(Request);
				var level = DbUtils.GetTokenLevel(Request);

				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.UTENTI, EndpointsOperations.VIEW);

					var json = new DefaultJson<UtentiDb>();
					var str = search.ToUpper().SqlQuote(true, true);
					string query;
					var total = 0L;
					if (inlinecount)
					{
						query = "SELECT COUNT(*) FROM utenti";
						if (string.IsNullOrWhiteSpace(filter))
							query += " WHERE ute_codice > 0";
						else
							query += $" WHERE ute_codice > 0 AND ({filter})";

						if (!string.IsNullOrWhiteSpace(search))
						{
							query += $" AND (ute_desc ILIKE {str} OR TRIM(CAST(ute_codice AS VARCHAR(15))) ILIKE {str})";
						}

						if (level != (short)UserLevel.SUPERADMIN) query += $" AND ute_codice IN (SELECT utd_ute FROM uteditte WHERE utd_dit IN({ditte}))";

						cmd.CommandText = DbUtils.QueryAdapt(query);
						total = (long)cmd.ExecuteScalar();
					}

					if (joined)
						query = UtentiDb.GetJoinQuery();
					else
						query = "SELECT * FROM utenti";
					if (string.IsNullOrWhiteSpace(filter))

						if (string.IsNullOrWhiteSpace(filter))
							query += " WHERE ute_codice > 0";
						else
							query += " WHERE ute_codice > 0 AND (" + filter + ")";

					if (!string.IsNullOrWhiteSpace(search))
					{
						query += $" AND (ute_desc ILIKE {str} OR TRIM(CAST(ute_codice AS VARCHAR(15))) ILIKE {str})";
					}

					if (level != (short)UserLevel.SUPERADMIN) query += $" AND ute_codice IN (SELECT utd_ute FROM uteditte WHERE utd_dit IN({ditte}))";

					if (string.IsNullOrWhiteSpace(orderby))
						query += " ORDER BY ute_codice";
					else
						query += " ORDER BY " + orderby;
					cmd.CommandText = DbUtils.QueryAdapt(query, top, skip);

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var mer = new UtentiDb();
						DbUtils.SqlRead(ref reader, ref mer, joined ? null : UtentiDb.GetJoinExcludeFields());
						if (json.Data == null) json.Data = new List<UtentiDb>();
						json.Data.Add(mer);
						json.RecordsTotal++;
					}

					reader.Close();

					if (joined && json.Data != null)
					{
						foreach (var ute in json.Data)
						{
							cmd.CommandText = UteUsgDb.GetJoinQuery() + " WHERE utg_ute = ? ORDER BY utg_created_at";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("codice", OdbcType.Int).Value = ute.ute_codice;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var utg = new UteUsgDb();
								DbUtils.SqlRead(ref reader, ref utg);
								if (ute.utg_list == null) ute.utg_list = new List<UteUsgDb>();
								ute.utg_list.Add(utg);
							}

							reader.Close();
						}
					}

					connection.Close();

					if (inlinecount) json.RecordsTotal = total;
					return (json);
				}
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
			catch (HttpResponseException)
			{
				throw;
			}
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}


		[HttpGet]
		[Route("api/utenti/get/{codice}")]
		[Route("api/utenti/get/{codice}/{joined}/{fulljoined}")]
		public DefaultJson<UtentiDb> Get(int codice, bool joined = false, bool fulljoined = false)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.UTENTI, EndpointsOperations.VIEW);

					var json = new DefaultJson<UtentiDb>();
					var ute = new UtentiDb();
					if (UtentiDb.Search(ref cmd, codice, ref ute, joined || fulljoined))
					{
						cmd.CommandText = UteUsgDb.GetJoinQuery() + " WHERE utg_ute = ? ORDER BY utg_created_at";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.Int).Value = ute.ute_codice;
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var utg = new UteUsgDb();
							DbUtils.SqlRead(ref reader, ref utg);
							if (ute.utg_list == null) ute.utg_list = new List<UteUsgDb>();
							ute.utg_list.Add(utg);
						}
						reader.Close();
						

						ute.img_list = new List<ImgUtentiDb>();
						if (json.Data == null) json.Data = new List<UtentiDb>();
						if (fulljoined)
						{
							DbUtils.GetTokenLevel(Request);

							cmd.CommandText = @"
							SELECT * 
							FROM imgutenti 
							WHERE img_codice = ? AND Mod(img_formato, 2) = 0
							ORDER BY img_formato
							";

							cmd.Parameters.Clear();
							cmd.Parameters.Add("codice", OdbcType.Int).Value = ute.ute_codice;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var img = new ImgUtentiDb();
								DbUtils.SqlRead(ref reader, ref img);
								ute.img_list.Add(img);
							}
							reader.Close();
						}

						json.Data.Add(ute);
						json.RecordsTotal++;
					}
					connection.Close();
					return (json);
				}
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
			catch (HttpResponseException)
			{
				throw;
			}
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}

		[HttpGet]
		[Route("api/utenti/getprofile/{codice}")]
		[Route("api/utenti/getprofile/{codice}/{joined}/{fulljoined}")]
		public DefaultJson<UtentiDb> GetProfile(int codice, bool joined = false, bool fulljoined = false)
		{
			var cod_ute = DbUtils.GetTokenUser(Request);
			if (cod_ute != codice) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg)); ;

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var json = new DefaultJson<UtentiDb>();
					var ute = new UtentiDb();
					if (UtentiDb.Search(ref cmd, codice, ref ute, joined || fulljoined))
					{
						cmd.CommandText = UteUsgDb.GetJoinQuery() + " WHERE utg_ute = ? ORDER BY utg_created_at";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.Int).Value = ute.ute_codice;
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var utg = new UteUsgDb();
							DbUtils.SqlRead(ref reader, ref utg);
							if (ute.utg_list == null) ute.utg_list = new List<UteUsgDb>();
							ute.utg_list.Add(utg);
						}
						reader.Close();


						ute.img_list = new List<ImgUtentiDb>();
						if (json.Data == null) json.Data = new List<UtentiDb>();
						if (fulljoined)
						{
							DbUtils.GetTokenLevel(Request);

							cmd.CommandText = @"
							SELECT * 
							FROM imgutenti 
							WHERE img_codice = ? AND Mod(img_formato, 2) = 0
							ORDER BY img_formato
							";

							cmd.Parameters.Clear();
							cmd.Parameters.Add("codice", OdbcType.Int).Value = ute.ute_codice;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var img = new ImgUtentiDb();
								DbUtils.SqlRead(ref reader, ref img);
								ute.img_list.Add(img);
							}
							reader.Close();
						}

						json.Data.Add(ute);
						json.RecordsTotal++;
					}
					connection.Close();
					return (json);
				}
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
			catch (HttpResponseException)
			{
				throw;
			}
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}


		[HttpPost]
		[Route("api/utenti/post/{ditta}")]
		public DefaultJson<UtentiDb> Post(int ditta, [FromBody] DefaultJson<UtentiDb> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non corrispondente"));
			var json = new DefaultJson<UtentiDb>();

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.UTENTI, EndpointsOperations.ADD);

					var level = DbUtils.GetTokenLevel(Request);
					var coddit = level != (short)UserLevel.SUPERADMIN ? ditta : 0;

					foreach (var ute in value.Data)
					{
						var val = ute;

						val.ute_codice = 1;
						cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(ute_codice),0) AS codice FROM utenti");
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							val.ute_codice += reader.GetInt32(reader.GetOrdinal("codice"));
						}
						reader.Close();

						val.ute_desc = val.ute_rag_soc1 + " " + val.ute_rag_soc2;
						val.ute_desc = val.ute_desc.Trim();
						if (string.IsNullOrWhiteSpace(val.ute_desc))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Descrizione vuota"));
							continue;
						}

						DbUtils.SqlWrite(ref cmd, UtentiDb.Write, DbMessage.DB_INSERT, ref val, ref coddit, true);

						if (json.Data == null) json.Data = new List<UtentiDb>();
						json.Data.Add(ute);
						json.RecordsTotal++;
					}
					connection.Close();
				}
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
			catch (HttpResponseException)
			{
				throw;
			}
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			return (json);
		}

		[HttpPost]
		[Route("api/utenti/addgroup")]
		public DefaultJson<UtentiDb> AddGroup([FromBody] DefaultJson<UteUsgDb> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non corrispondente"));
			var json = new DefaultJson<UtentiDb>();

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.UTENTI, EndpointsOperations.UPDATE);
					var level = DbUtils.GetTokenLevel(Request);

					foreach (var utg in value.Data)
					{
						object obj = null;
						var val = utg;
						if (utg.utg_ute != 0 && utg.utg_usg != 0)
						{
							if (utg.utg_usg == 3 && level != (short)UserLevel.SUPERADMIN) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg));

							DbUtils.SqlWrite(ref cmd, UteUsgDb.Write, DbMessage.DB_INSERT, ref val, ref obj, true);

							var ute = new UtentiDb();
							if (UtentiDb.Search(ref cmd, utg.utg_ute, ref ute, true))
							{
								if (json.Data == null) json.Data = new List<UtentiDb>();
								json.Data.Add(ute);
								json.RecordsTotal++;
							}
						}
					}
					connection.Close();
				}
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
			catch (HttpResponseException)
			{
				throw;
			}
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			return (json);
		}

		[HttpDelete]
		[Route("api/utenti/removegroup/{codUte}/{codUsg}")]
		public DefaultJson<UtentiDb> RemoveGroup(int codUte, int codUsg)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.UTENTI, EndpointsOperations.UPDATE);

					if (codUsg == 3)
					{
						if (DbUtils.GetTokenLevel(Request) != (short)UserLevel.SUPERADMIN) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg));
					}

					var utg = new UteUsgDb();
					if (codUte == 0) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Codice Utente non valido"));
					if (codUsg == 0) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Codice Gruppo Utente non valido"));
					if (!UteUsgDb.Search(ref cmd, codUte, codUsg, ref utg)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object objx = null;
					DbUtils.SqlWrite(ref cmd, UteUsgDb.Write, DbMessage.DB_DELETE, ref utg, ref objx);

					var json = new DefaultJson<UtentiDb>();
					var ute = new UtentiDb();
					if (UtentiDb.Search(ref cmd, utg.utg_ute, ref ute, true))
					{
						if (json.Data == null) json.Data = new List<UtentiDb>();
						json.Data.Add(ute);
						json.RecordsTotal++;
					}
					
					connection.Close();
					return (json);
				}
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
			catch (HttpResponseException)
			{
				throw;
			}
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}

		[HttpPut]
		[Route("api/utenti/put/{codice}")]
		public DefaultJson<UtentiDb> Put(int codice, [FromBody]DefaultJson<UtentiDb> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non corrispondente"));
			if (value.Data.Count != 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non valido"));

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.UTENTI, EndpointsOperations.UPDATE);

					var ute = value.Data[0];
					if (ute.ute_codice <= 0) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Codice non valido"));
					if (ute.ute_codice != codice) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Id risorsa non corrisponde all'id dei dati"));

					if (DbUtils.GetTokenLevel(Request) < ute.ute_level) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg));

					ute.ute_desc = ute.ute_desc.Trim();
					if (string.IsNullOrWhiteSpace(ute.ute_desc)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Descrizione vuota"));

					int obj = 0;
					DbUtils.SqlWrite(ref cmd, UtentiDb.Write, DbMessage.DB_UPDATE, ref ute, ref obj, true);
					connection.Close();

					var json = new DefaultJson<UtentiDb>();
					if (json.Data == null) json.Data = new List<UtentiDb>();
					json.Data.Add(ute);
					json.RecordsTotal++;

					return (json);
				}
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
			catch (HttpResponseException)
			{
				throw;
			}
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}

		[HttpPut]
		[Route("api/utenti/profile/{codice}")]
		public DefaultJson<UtentiDb> Profile(int codice, [FromBody] DefaultJson<UtentiDb> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non corrispondente"));
			if (value.Data.Count != 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non valido"));

			var cod_ute = DbUtils.GetTokenUser(Request);
			if (cod_ute != codice) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg)); ;
			
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					

					var ute = value.Data[0];
					if (ute.ute_codice <= 0) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Codice non valido"));
					if (ute.ute_codice != codice) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Id risorsa non corrisponde all'id dei dati"));

					ute.ute_desc = ute.ute_desc.Trim();
					if (string.IsNullOrWhiteSpace(ute.ute_desc)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Descrizione vuota"));

					var old = new UtentiDb();
					if (UtentiDb.Search(ref cmd, codice, ref old))
					{
						ute.ute_level = old.ute_level;
						
						int obj = 0;
						DbUtils.SqlWrite(ref cmd, UtentiDb.Write, DbMessage.DB_UPDATE, ref ute, ref obj, true);
					}

					connection.Close();

					var json = new DefaultJson<UtentiDb>();
					if (json.Data == null) json.Data = new List<UtentiDb>();
					json.Data.Add(ute);
					json.RecordsTotal++;

					return (json);
				}
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
			catch (HttpResponseException)
			{
				throw;
			}
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}

		[HttpPut]
		[Route("api/utenti/pwd/{codice}")]
		public DefaultJson<UtentiDb> Pwd(int codice, [FromBody] DefaultJson<ChangePassword> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non corrispondente"));
			if (value.Data.Count != 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non valido"));

			try
			{
				var cod_ute = DbUtils.GetTokenUser(Request);
				var level = DbUtils.GetTokenLevel(Request);
				if (cod_ute != codice && level != (short)UserLevel.SUPERADMIN) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg)); ;

				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var ute = new UtentiDb();
					if (!UtentiDb.Search(ref cmd, codice, ref ute, false)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					cmd.CommandText = @"
					UPDATE utenti SET ute_password = CRYPT(?, GEN_SALT('bf')), ute_last_update = Now()
					WHERE ute_codice = ?";
					cmd.Parameters.Clear();
					cmd.Parameters.Add("pswd", OdbcType.Text).Value = value.Data[0].new_password;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
					cmd.ExecuteNonQuery();

					if (!UtentiDb.Search(ref cmd, codice, ref ute, true)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));
					connection.Close();

					var json = new DefaultJson<UtentiDb>();
					if (json.Data == null) json.Data = new List<UtentiDb>();
					json.Data.Add(ute);
					json.RecordsTotal++;

					return (json);
				}
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
			catch (HttpResponseException)
			{
				throw;
			}
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}


		[HttpDelete]
		[Route("api/utenti/delete/{codice}")]
		public void Delete(int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.UTENTI, EndpointsOperations.DELETE);

					var val = new UtentiDb();
					if (codice != 0 && !UtentiDb.Search(ref cmd, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					if (DbUtils.GetTokenLevel(Request) < val.ute_level) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg));


					int objx = 0;
					DbUtils.SqlWrite(ref cmd, UtentiDb.Write, DbMessage.DB_DELETE, ref val, ref objx);

					connection.Close();
				}
			}
			catch (MCException ex)
			{
				if (ex.ErrorCode == MCException.CancelErr)
				{
					var res = new McResponse(ExceptionsType.MC_EXCEPTION, ex.GetError(), ex.Message, ex.GetStackTrace());
					throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.Forbidden, res));
				}
				else
				{
					var res = new McResponse(ExceptionsType.MC_EXCEPTION, ex.GetError(), ex.Message, ex.GetStackTrace());
					throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
				}
			}
			catch (OdbcException ex)
			{
				var err = 0;
				if (ex.Errors.Count > 0) err = ex.Errors[0].NativeError;
				var res = new McResponse(ExceptionsType.ODBC_EXCEPTION, err, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			catch (HttpResponseException)
			{
				throw;
			}
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}
	}
}
