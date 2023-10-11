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

	public class RappresentantiController : ApiController
	{
		[HttpGet]
		[Route("api/rappresentanti/blank")]
		[Route("api/rappresentanti/blank/{cliente}")]
		public DefaultJson<RappresentantiDb> Blank(int cliente = 0)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var json = new DefaultJson<RappresentantiDb>();

					cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(rap_codice),0) AS codice FROM rappresentanti");
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var rap = new RappresentantiDb();
						rap.rap_codice = 1 + reader.GetInt64(reader.GetOrdinal("codice"));
						if (json.Data == null) json.Data = new List<RappresentantiDb>();
						json.Data.Add(rap);
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
		[Route("api/rappresentanti/get")]
		public DefaultJson<RappresentantiDb> GetList(int top = 0, int skip = 0, string orderby = "", string search = "", string filter = "", bool inlinecount = false, bool joined = false)
		{
			if (filter.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger filter value"));
			if (search.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger search value"));
			if (orderby.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger orderby value"));

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<RappresentantiDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.DITTE, EndpointsOperations.VIEW);

					var str = search.ToUpper().SqlQuote(true, true);
					string query;
					var total = 0L;
					if (inlinecount)
					{
						query = "SELECT COUNT(*) FROM rappresentanti";
						if (string.IsNullOrWhiteSpace(filter))
							query += " WHERE rap_codice > 0";
						else
							query += $" WHERE rap_codice > 0 AND ({filter})";

						if (!string.IsNullOrWhiteSpace(search))
						{
							if (joined)
								query += $" AND (rap_desc ILIKE {str} OR TRIM(CAST(rap_codice AS VARCHAR(15))) ILIKE {str})";
							else
								query += $" AND (rap_desc ILIKE {str} OR TRIM(CAST(rap_codice AS VARCHAR(15))) ILIKE {str})";
						}

						cmd.CommandText = DbUtils.QueryAdapt(query);
						total = (long)cmd.ExecuteScalar();
					}

					if (joined)
						query = RappresentantiDb.GetJoinQuery();
					else
						query = "SELECT * FROM rappresentanti";
						
					if (string.IsNullOrWhiteSpace(filter))
						query += " WHERE rap_codice > 0";
					else 
						query += " WHERE rap_codice > 0 AND (" + filter + ")";

					if (!string.IsNullOrWhiteSpace(search))
					{
						if (joined)
							query += $" AND (rap_desc ILIKE {str} OR TRIM(CAST(rap_codice AS VARCHAR(15))) ILIKE {str})";
						else
							query += $" AND (rap_desc ILIKE {str} OR TRIM(CAST(rap_codice AS VARCHAR(15))) ILIKE {str})";
					}
					if (string.IsNullOrWhiteSpace(orderby))
						query += " ORDER BY rap_codice";
					else
						query += " ORDER BY " + orderby;
					cmd.CommandText = DbUtils.QueryAdapt(query, top, skip);

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var rap = new RappresentantiDb();
						DbUtils.SqlRead(ref reader, ref rap, joined ? null : RappresentantiDb.GetJoinExcludeFields());
						if (json.Data == null) json.Data = new List<RappresentantiDb>();
						json.Data.Add(rap);
						json.RecordsTotal++;
					}
					reader.Close();
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
		[Route("api/rappresentanti/get/{codice}")]
		[Route("api/rappresentanti/get/{codice}/{joined}")]
		[Route("api/rappresentanti/get/{codice}/{joined}/{fulljoined}")]
		public DefaultJson<RappresentantiDb> Get(int codice, bool joined = false, bool fulljoined = false)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.DITTE, EndpointsOperations.VIEW);

					var json = new DefaultJson<RappresentantiDb>();
					var rap = new RappresentantiDb();
					if (RappresentantiDb.Search(ref cmd,  codice, ref rap, joined || fulljoined))
					{
						if (json.Data == null) json.Data = new List<RappresentantiDb>();
						json.Data.Add(rap);
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
		[Route("api/rappresentanti/post")]
		public DefaultJson<RappresentantiDb> Post([FromBody] DefaultJson<RappresentantiDb> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non corrispondente"));
			
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.DITTE, EndpointsOperations.ADD);
					var codute = DbUtils.GetTokenUser(Request);

					var json = new DefaultJson<RappresentantiDb>();
					foreach (var rap in value.Data)
					{
						object obj = null;
						var val = rap;

						rap.rap_cognome = val.rap_cognome.Trim();
						val.rap_nome = val.rap_nome.Trim();
						val.rap_desc = (val.rap_cognome + " " + val.rap_nome).Trim();
						val.rap_codfis = val.rap_codfis.Trim().ToUpper();
						val.rap_user = codute;

						if (string.IsNullOrWhiteSpace(val.rap_nome))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Nome vuoto"));
							continue;
						}
						if (string.IsNullOrWhiteSpace(val.rap_cognome))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Cognome vuoto"));
							continue;
						}

						DbUtils.SqlWrite(ref cmd, RappresentantiDb.Write, DbMessage.DB_INSERT, ref val, ref obj, true);

						if (json.Data == null) json.Data = new List<RappresentantiDb>();
						json.Data.Add(val);
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
		[Route("api/rappresentanti/put/{codice}")]
		public DefaultJson<RappresentantiDb> Put(int codice, [FromBody]DefaultJson<RappresentantiDb> value)
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
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.DITTE, EndpointsOperations.UPDATE);

					var rap = value.Data[0];
					if (rap.rap_codice != codice) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Id risorsa non corrisponde all'id dei dati"));

					rap.rap_cognome = rap.rap_cognome.Trim();
					rap.rap_nome = rap.rap_nome.Trim();
					rap.rap_desc = (rap.rap_cognome + " " + rap.rap_nome).Trim();
					rap.rap_codfis = rap.rap_codfis.Trim().ToUpper();
					rap.rap_user = DbUtils.GetTokenUser(Request);

					if (string.IsNullOrWhiteSpace(rap.rap_cognome)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Cognome vuoto"));
					if (string.IsNullOrWhiteSpace(rap.rap_nome)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Nome vuoto"));

					object obj = null;
					
					var json = new DefaultJson<RappresentantiDb>();
					DbUtils.SqlWrite(ref cmd, RappresentantiDb.Write, DbMessage.DB_UPDATE, ref rap, ref obj, true);

					connection.Close();
					if (json.Data == null) json.Data = new List<RappresentantiDb>();
					json.Data.Add(rap);
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
		[Route("api/rappresentanti/delete/{codice}")]
		public void Delete(int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.DITTE, EndpointsOperations.DELETE);

					var val = new RappresentantiDb();
					if (!RappresentantiDb.Search(ref cmd, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object obj = null;
					DbUtils.SqlWrite(ref cmd, RappresentantiDb.Write, DbMessage.DB_DELETE, ref val, ref obj);

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
			catch (HttpRequestException)
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
