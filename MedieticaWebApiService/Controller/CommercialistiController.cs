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

	public class CommercialistiController : ApiController
	{
		[HttpGet]
		[Route("api/commercialisti/blank")]
		[Route("api/commercialisti/blank/{cliente}")]
		public DefaultJson<CommercialistiDb> Blank(int cliente = 0)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var json = new DefaultJson<CommercialistiDb>();

					cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(cmm_codice),0) AS codice FROM commercialisti");
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var cmm = new CommercialistiDb();
						cmm.cmm_codice = 1 + reader.GetInt64(reader.GetOrdinal("codice"));
						if (json.Data == null) json.Data = new List<CommercialistiDb>();
						json.Data.Add(cmm);
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
		[Route("api/commercialisti/get")]
		public DefaultJson<CommercialistiDb> GetList(int top = 0, int skip = 0, string orderby = "", string search = "", string filter = "", bool inlinecount = false, bool joined = false)
		{
			if (filter.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger filter value"));
			if (search.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger search value"));
			if (orderby.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger orderby value"));

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<CommercialistiDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.DITTE, EndpointsOperations.VIEW);

					var str = search.ToUpper().SqlQuote(true, true);
					string query;
					var total = 0L;
					if (inlinecount)
					{
						query = "SELECT COUNT(*) FROM commercialisti";
						if (string.IsNullOrWhiteSpace(filter))
							query += " WHERE cmm_codice > 0";
						else
							query += $" WHERE cmm_codice > 0 AND ({filter})";

						if (!string.IsNullOrWhiteSpace(search))
						{
							if (joined)
								query += $" AND (cmm_desc ILIKE {str} OR cmm_citta ILIKE {str} OR cmm_tel1 ILIKE {str}  OR cmm_tel2 ILIKE {str}  OR cmm_cell ILIKE {str} OR TRIM(CAST(cmm_codice AS VARCHAR(15))) ILIKE {str})";
							else
								query += $" AND (cmm_desc ILIKE {str} OR cmm_citta ILIKE {str} OR cmm_tel1 ILIKE {str}  OR cmm_tel2 ILIKE {str}  OR cmm_cell ILIKE {str} OR TRIM(CAST(cmm_codice AS VARCHAR(15))) ILIKE {str})";
						}

						cmd.CommandText = DbUtils.QueryAdapt(query);
						total = (long)cmd.ExecuteScalar();
					}

					if (joined)
						query = CommercialistiDb.GetJoinQuery();
					else
						query = "SELECT * FROM commercialisti";
						
					if (string.IsNullOrWhiteSpace(filter))
						query += " WHERE cmm_codice > 0";
					else 
						query += " WHERE cmm_codice > 0 AND (" + filter + ")";

					if (!string.IsNullOrWhiteSpace(search))
					{
						if (joined)
							query += $" AND (cmm_desc ILIKE {str} OR cmm_citta ILIKE {str} OR cmm_tel1 ILIKE {str}  OR cmm_tel2 ILIKE {str}  OR cmm_cell ILIKE {str} OR TRIM(CAST(cmm_codice AS VARCHAR(15))) ILIKE {str})";
						else
							query += $" AND (cmm_desc ILIKE {str} OR cmm_citta ILIKE {str} OR cmm_tel1 ILIKE {str}  OR cmm_tel2 ILIKE {str}  OR cmm_cell ILIKE {str} OR TRIM(CAST(cmm_codice AS VARCHAR(15))) ILIKE {str})";
					}
					if (string.IsNullOrWhiteSpace(orderby))
						query += " ORDER BY cmm_codice";
					else
						query += " ORDER BY " + orderby;
					cmd.CommandText = DbUtils.QueryAdapt(query, top, skip);

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var cmm = new CommercialistiDb();
						DbUtils.SqlRead(ref reader, ref cmm, joined ? null : CommercialistiDb.GetJoinExcludeFields());
						if (json.Data == null) json.Data = new List<CommercialistiDb>();
						json.Data.Add(cmm);
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
		[Route("api/commercialisti/get/{codice}")]
		[Route("api/commercialisti/get/{codice}/{joined}")]
		[Route("api/commercialisti/get/{codice}/{joined}/{fulljoined}")]
		public DefaultJson<CommercialistiDb> Get(int codice, bool joined = false, bool fulljoined = false)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.DITTE, EndpointsOperations.VIEW);

					var json = new DefaultJson<CommercialistiDb>();
					var cmm = new CommercialistiDb();
					if (CommercialistiDb.Search(ref cmd,  codice, ref cmm, joined || fulljoined))
					{
						if (json.Data == null) json.Data = new List<CommercialistiDb>();
						json.Data.Add(cmm);
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
		[Route("api/commercialisti/post")]
		public DefaultJson<CommercialistiDb> Post([FromBody] DefaultJson<CommercialistiDb> value)
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

					var json = new DefaultJson<CommercialistiDb>();
					foreach (var cmm in value.Data)
					{
						object obj = null;
						var val = cmm;

						val.cmm_cognome = val.cmm_cognome.Trim();
						val.cmm_nome = val.cmm_nome.Trim();
						val.cmm_desc = (val.cmm_cognome + " " + val.cmm_nome).Trim();
						val.cmm_user = codute;

						if (string.IsNullOrWhiteSpace(val.cmm_nome))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Nome vuoto"));
							continue;
						}
						if (string.IsNullOrWhiteSpace(val.cmm_cognome))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Cognome vuoto"));
							continue;
						}

						DbUtils.SqlWrite(ref cmd, CommercialistiDb.Write, DbMessage.DB_INSERT, ref val, ref obj, true);

						if (json.Data == null) json.Data = new List<CommercialistiDb>();
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
		[Route("api/commercialisti/put/{codice}")]
		public DefaultJson<CommercialistiDb> Put(int codice, [FromBody]DefaultJson<CommercialistiDb> value)
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

					var cmm = value.Data[0];
					if (cmm.cmm_codice != codice) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Id risorsa non corrisponde all'id dei dati"));

					cmm.cmm_cognome = cmm.cmm_cognome.Trim();
					cmm.cmm_nome = cmm.cmm_nome.Trim();
					cmm.cmm_desc = (cmm.cmm_cognome + " " + cmm.cmm_nome).Trim();
					cmm.cmm_user = DbUtils.GetTokenUser(Request);

					if (string.IsNullOrWhiteSpace(cmm.cmm_cognome)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Cognome vuoto"));
					if (string.IsNullOrWhiteSpace(cmm.cmm_nome)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Nome vuoto"));

					object obj = null;
					
					var json = new DefaultJson<CommercialistiDb>();
					DbUtils.SqlWrite(ref cmd, CommercialistiDb.Write, DbMessage.DB_UPDATE, ref cmm, ref obj, true);

					connection.Close();
					if (json.Data == null) json.Data = new List<CommercialistiDb>();
					json.Data.Add(cmm);
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
		[Route("api/commercialisti/delete/{codice}")]
		public void Delete(int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.DITTE, EndpointsOperations.DELETE);

					var val = new CommercialistiDb();
					if (!CommercialistiDb.Search(ref cmd, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object obj = null;
					DbUtils.SqlWrite(ref cmd, CommercialistiDb.Write, DbMessage.DB_DELETE, ref val, ref obj);

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
