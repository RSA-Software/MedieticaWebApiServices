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

	public class SediDitteController : ApiController
	{
		[HttpGet]
		[Route("api/sediditte/blank/{ditta}")]
		public DefaultJson<SediDitteDb> Blank(int ditta)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<SediDitteDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(sed_codice),0) AS codice FROM sedditte WHERE sed_dit = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = ditta;
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var sed = new SediDitteDb();
						sed.sed_dit = ditta;
						sed.sed_codice = 1 + reader.GetInt32(reader.GetOrdinal("codice"));
						if (json.Data == null) json.Data = new List<SediDitteDb>();
						json.Data.Add(sed);
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


		[Route("api/sediditte/get")]
		public DefaultJson<SediDitteDb> GetList(int ditta = 0, int top = 0, int skip = 0, string orderby = "", string search = "", string filter = "", bool inlinecount = false)
		{
			if (filter.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger filter value"));
			if (search.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger search value"));
			if (orderby.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger orderby value"));

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<SediDitteDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var str = search.ToUpper().SqlQuote(true, true);
					string query;
					var total = 0L;
					if (inlinecount)
					{
						query = "SELECT COUNT(*) FROM sedditte";
						if (string.IsNullOrWhiteSpace(filter))
							query += $" WHERE sed_dit = {ditta}";
						else
							query += $" WHERE ({filter})";

						if (!string.IsNullOrWhiteSpace(search))
						{
							query += $" AND (sed_citta ILIKE {str} OR sed_indirizzo ILIKE {str} OR TRIM(CAST(sed_codice AS VARCHAR(15))) ILIKE {str})";
						}
						cmd.CommandText = DbUtils.QueryAdapt(query);
						total = (long)cmd.ExecuteScalar();
					}

					query = "SELECT * FROM sedditte";
					if (string.IsNullOrWhiteSpace(filter))
						query += $" WHERE sed_dit = {ditta}";
					else
						query += $" WHERE ({filter})";

					if (!string.IsNullOrWhiteSpace(search))
					{
						query += $" AND (sed_citta ILIKE {str} OR sed_indirizzo ILIKE {str} OR TRIM(CAST(sed_codice AS VARCHAR(15))) ILIKE {str})";
					}

					if (string.IsNullOrWhiteSpace(orderby))
						query += " ORDER BY sed_codice";
					else
						query += " ORDER BY " + orderby;
					cmd.CommandText = DbUtils.QueryAdapt(query, top, skip);

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var sed = new SediDitteDb();
						DbUtils.SqlRead(ref reader, ref sed);
						if (json.Data == null) json.Data = new List<SediDitteDb>();
						json.Data.Add(sed);
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
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}

		[HttpGet]
		[Route("api/sediditte/get/{ditta}/{codice}")]
		public DefaultJson<SediDitteDb> Get(int ditta, int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var json = new DefaultJson<SediDitteDb>();
					var sed = new SediDitteDb();
					if (SediDitteDb.Search(ref cmd, ditta, codice, ref sed))
					{
						if (json.Data == null) json.Data = new List<SediDitteDb>();
						json.Data.Add(sed);
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
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}



		[HttpPost]
		[Route("api/sediditte/post")]
		public DefaultJson<SediDitteDb> Post([FromBody] DefaultJson<SediDitteDb> value)
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

					var cod_ute = DbUtils.GetTokenUser(Request);

					var json = new DefaultJson<SediDitteDb>();
					foreach (var scd in value.Data)
					{
						object obj = null;
						var val = scd;

						cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(sed_codice),0) FROM sedditte WHERE sed_dit = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = val.sed_dit;

						val.sed_codice = 1 + (int)cmd.ExecuteScalar();
						val.sed_indirizzo = val.sed_indirizzo.Trim();
						val.sed_citta = val.sed_citta.Trim();
						if (string.IsNullOrWhiteSpace(val.sed_indirizzo))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Indirizzo vuoto"));
							continue;
						}
						if (string.IsNullOrWhiteSpace(val.sed_citta))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Città vuota"));
							continue;
						}

						val.sed_user = cod_ute;
						DbUtils.SqlWrite(ref cmd, SediDitteDb.Write, DbMessage.DB_INSERT, ref val, ref obj);
						if (json.Data == null) json.Data = new List<SediDitteDb>();
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
		[Route("api/sediditte/put/{ditta}/{codice}")]
		public DefaultJson<SediDitteDb> Put(int ditta, int codice, [FromBody]DefaultJson<SediDitteDb> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non corrispondente"));
			if (value.Data.Count != 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non valido"));


			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					
					var sed = value.Data[0];
					if (sed.sed_dit != ditta || sed.sed_codice != codice) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Id risorsa non corrisponde all'id dei dati"));
					sed.sed_indirizzo = sed.sed_indirizzo.Trim();
					sed.sed_citta = sed.sed_citta.Trim();
					if (string.IsNullOrWhiteSpace(sed.sed_indirizzo)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Indirizzo vuoto"));
					if (string.IsNullOrWhiteSpace(sed.sed_citta)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Città vuota"));
	
					sed.sed_user = DbUtils.GetTokenUser(Request);

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					object obj = null;
					DbUtils.SqlWrite(ref cmd, SediDitteDb.Write, DbMessage.DB_UPDATE, ref sed, ref obj);
	
					var json = new DefaultJson<SediDitteDb>();
					if (json.Data == null) json.Data = new List<SediDitteDb>();
					json.Data.Add(sed);
					json.RecordsTotal++;

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

		[HttpDelete]
		[Route("api/sediditte/delete/{ditta}/{codice}")]
		public void Delete(int ditta, int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var val = new SediDitteDb();
					if (!SediDitteDb.Search(ref cmd, ditta, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object objx = null;
					DbUtils.SqlWrite(ref cmd, SediDitteDb.Write, DbMessage.DB_DELETE, ref val, ref objx);

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
		}

	}
}
