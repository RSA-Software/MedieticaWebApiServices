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
	public class UtentiDitteController : ApiController
	{
		[HttpGet]
		[Route("api/uteditte/blank")]
		[Route("api/uteditte/blank/{utente}")]
		[Route("api/uteditte/blank/{utente}/{ditta}")]
		public DefaultJson<UtentiDitteDb> Blank(int utente = 0, int ditta = 0)
		{
			try
			{
				var json = new DefaultJson<UtentiDitteDb>();
				var utd = new UtentiDitteDb();
				utd.utd_ute = utente;
				utd.utd_dit = ditta;
				if (json.Data == null) json.Data = new List<UtentiDitteDb>();
				json.Data.Add(utd);
				json.RecordsTotal++;

				return (json);
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
		[Route("api/uteditte/get")]
		public DefaultJson<UtentiDitteDb> GetList(int top = 0, int skip = 0, string orderby = "", string search = "", string filter = "", bool inlinecount = false, bool joined = false)
		{
			if (filter.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger filter value"));
			if (search.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger search value"));
			if (orderby.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger orderby value"));

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<UtentiDitteDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var str = search.ToUpper().SqlQuote(true, true);
					string query;
					var total = 0L;
					if (inlinecount)
					{
						query = joined ? UtentiDitteDb.GetCountJoinQuery() : "SELECT COUNT(*) FROM uteditte";
						if (string.IsNullOrWhiteSpace(filter))
							query += " WHERE utd_ute > 0";
						else
							query += $" WHERE ({filter})";

						if (!string.IsNullOrWhiteSpace(search))
						{
							if (joined)
								query += $" AND (dit_desc ILIKE {str} OR dit_citta ILIKE {str} dit_indirizzo ILIKE {str} OR dit_cap ILIKE {str}  OR dit_prov ILIKE {str} OR TRIM(CAST(utd_ute AS VARCHAR(15))) ILIKE {str})";
							else
								query += $" AND (TRIM(CAST(utd_ute AS VARCHAR(15))) ILIKE {str})";
						}
						cmd.CommandText = DbUtils.QueryAdapt(query);
						total = (long)cmd.ExecuteScalar();
					}

					if (joined)
						query = UtentiDitteDb.GetJoinQuery();
					else
						query = "SELECT * FROM uteditte"; 

					if (string.IsNullOrWhiteSpace(filter))
						query += " WHERE utd_ute > 0";
					else
						query += $" WHERE ({filter})";

					if (!string.IsNullOrWhiteSpace(search))
					{
						if (joined)
							query += $" AND (dit_desc ILIKE {str} OR dit_citta ILIKE {str} dit_indirizzo ILIKE {str} OR dit_cap ILIKE {str}  OR dit_prov ILIKE {str} OR TRIM(CAST(utd_ute AS VARCHAR(15))) ILIKE {str})";
						else
							query += $" AND (TRIM(CAST(utd_ute AS VARCHAR(15))) ILIKE {str})";
					}

					if (string.IsNullOrWhiteSpace(orderby))
						query += " ORDER BY utd_ute, utd_default DESC, utd_dit";
					else
						query += " ORDER BY " + orderby;
					cmd.CommandText = DbUtils.QueryAdapt(query, top, skip);

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var mer = new UtentiDitteDb();
						DbUtils.SqlRead(ref reader, ref mer, joined ? null : UtentiDitteDb.GetJoinExcludeFields());
						if (json.Data == null) json.Data = new List<UtentiDitteDb>();
						json.Data.Add(mer);
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
		[Route("api/uteditte/get/{utente}/{ditta}")]
		[Route("api/uteditte/get/{utente}/{ditta}/{joined}")]
		public DefaultJson<UtentiDitteDb> Get(int utente, int ditta, bool joined = false)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var json = new DefaultJson<UtentiDitteDb>();
					var utd = new UtentiDitteDb();
					if (UtentiDitteDb.Search(ref cmd, utente, ditta, ref utd, joined))
					{
						if (json.Data == null) json.Data = new List<UtentiDitteDb>();
						json.Data.Add(utd);
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
		[Route("api/uteditte/post")]
		public DefaultJson<UtentiDitteDb> Post([FromBody] DefaultJson<UtentiDitteDb> value)
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

					var json = new DefaultJson<UtentiDitteDb>();
					foreach (var utd in value.Data)
					{
						object obj = null;
						var val = utd;

						DbUtils.SqlWrite(ref cmd, UtentiDitteDb.Write, DbMessage.DB_INSERT, ref val, ref obj, true);

						if (json.Data == null) json.Data = new List<UtentiDitteDb>();
						json.Data.Add(utd);
						json.RecordsTotal++;
					}
					connection.Close();
					return (json);
				}
			}
			catch (MCException ex)
			{
				if (ex.GetError() == MCException.DuplicateErr)
				{
					var json = new DefaultJson<UtentiDitteDb>();
					if (json.Data == null) json.Data = new List<UtentiDitteDb>();
					json.RecordsTotal++;
					return (json);
				}
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

		[HttpPut]
		[Route("api/uteditte/put/{utente}/{ditta}")]
		public DefaultJson<UtentiDitteDb> Put(int utente, int ditta, [FromBody]DefaultJson<UtentiDitteDb> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non corrispondente"));
			if (value.Data.Count != 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non valido"));
			var json = new DefaultJson<UtentiDitteDb>();

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var utd = value.Data[0];
					if (utd.utd_ute != utente || utd.utd_dit != ditta) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Id risorsa non corrisponde all'id dei dati"));

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					object obj = null;
					DbUtils.SqlWrite(ref cmd, UtentiDitteDb.Write, DbMessage.DB_UPDATE, ref utd, ref obj, true);
	
					if (json.Data == null) json.Data = new List<UtentiDitteDb>();
					json.Data.Add(utd);
					json.RecordsTotal++;
					
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
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			return (json);
		}

		[HttpPut]
		[Route("api/uteditte/default/{utente}/{ditta}")]
		public DefaultJson<UtentiDitteDb> Pwd(int utente, int ditta, [FromBody] DefaultJson<ChangePassword> value)
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

					var utd = new UtentiDitteDb();
					if (!UtentiDitteDb.Search(ref cmd, utente, ditta, ref utd, false)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					cmd.CommandText = @"
					UPDATE uteditte SET utd_default = 0, ute_last_update = Now()
					WHERE utd_ute = ?";
					cmd.Parameters.Clear();
					cmd.Parameters.Add("codice", OdbcType.Int).Value = utente;
					cmd.ExecuteNonQuery();

					cmd.CommandText = @"
					UPDATE uteditte SET utd_default = 1, ute_last_update = Now()
					WHERE utd_ute = ? AND utd_dit = ?";
					cmd.Parameters.Clear();
					cmd.Parameters.Add("codice", OdbcType.Int).Value = utente;
					cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
					cmd.ExecuteNonQuery();
					
					if (!UtentiDitteDb.Search(ref cmd, utente, ditta, ref utd, true)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));
					connection.Close();

					var json = new DefaultJson<UtentiDitteDb>();
					if (json.Data == null) json.Data = new List<UtentiDitteDb>();
					json.Data.Add(utd);
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
		[Route("api/uteditte/delete/{ditta}/{utente}")]
		public void Delete(int ditta, int utente)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var val = new UtentiDitteDb();
					if (!UtentiDitteDb.Search(ref cmd, utente, ditta, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object objx = null;
					DbUtils.SqlWrite(ref cmd, UtentiDitteDb.Write, DbMessage.DB_DELETE, ref val, ref objx);
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
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}
	}
}
