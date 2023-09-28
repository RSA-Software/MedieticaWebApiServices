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

	public class DocDitteController : ApiController
	{
		[HttpGet]
		[Route("api/docditte/blank/{ditta}")]
		public DefaultJson<DocDitteDb> Blank(int ditta)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<DocDitteDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(dod_codice),0) AS codice FROM docditte WHERE dod_dit = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = ditta;
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var dod = new DocDitteDb();
						dod.dod_dit = ditta;
						dod.dod_data = DateTime.Now;
						dod.dod_livello = (short)UserLevel.PRIVATE;
						dod.dod_codice = 1 + reader.GetInt32(reader.GetOrdinal("codice"));
						if (json.Data == null) json.Data = new List<DocDitteDb>();
						json.Data.Add(dod);
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


		[Route("api/docditte/get")]
		public DefaultJson<DocDitteDb> GetList(int ditta = 0, int top = 0, int skip = 0, string orderby = "", string search = "", string filter = "", bool inlinecount = false, bool joined = false)
		{
			if (filter.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger filter value"));
			if (search.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger search value"));
			if (orderby.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger orderby value"));

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<DocDitteDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var str = search.ToUpper().SqlQuote(true, true);
					string query;
					var total = 0L;
					if (inlinecount)
					{
						if (joined)
							query = DocDitteDb.GetCountQuery();
						else
							query = "SELECT COUNT(*) FROM docditte";
						if (string.IsNullOrWhiteSpace(filter))
							query += $" WHERE dod_dit = {ditta}";
						else
							query += $" WHERE ({filter})";

						if (!string.IsNullOrWhiteSpace(search))
						{
							if (joined)
								query += $" AND (dod_desc ILIKE {str} OR TRIM(CAST(dod_codice AS VARCHAR(15))) ILIKE {str} OR tdo_desc ILIKE {str})";
							else
								query += $" AND (dod_desc ILIKE {str} OR TRIM(CAST(dod_codice AS VARCHAR(15))) ILIKE {str})";
						}
						cmd.CommandText = DbUtils.QueryAdapt(query);
						total = (long)cmd.ExecuteScalar();
					}

					if (joined)
						query = DocDitteDb.GetJoinQuery();
					else
						query = "SELECT * FROM docditte";
					if (string.IsNullOrWhiteSpace(filter))
						query += $" WHERE dod_dit = {ditta}";
					else
						query += $" WHERE ({filter})";

					if (!string.IsNullOrWhiteSpace(search))
					{
						if (joined)
							query += $" AND (dod_desc ILIKE {str} OR TRIM(CAST(dod_codice AS VARCHAR(15))) ILIKE {str} OR tdo_desc ILIKE {str})";
						else
							query += $" AND (dod_desc ILIKE {str} OR TRIM(CAST(dod_codice AS VARCHAR(15))) ILIKE {str})";
					}

					if (string.IsNullOrWhiteSpace(orderby))
						query += " ORDER BY dod_codice";
					else
						query += " ORDER BY " + orderby;
					cmd.CommandText = DbUtils.QueryAdapt(query, top, skip);

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var dod = new DocDitteDb();
						DbUtils.SqlRead(ref reader, ref dod, joined ? null : DocDitteDb.GetJoinExcludeFields());
						if (json.Data == null) json.Data = new List<DocDitteDb>();
						json.Data.Add(dod);
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
		[Route("api/docditte/get/{ditta}/{codice}")]
		[Route("api/docditte/get/{ditta}/{codice}/{joined}")]
		public DefaultJson<DocDitteDb> Get(int ditta, int codice, bool joined = false)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var json = new DefaultJson<DocDitteDb>();
					var dod = new DocDitteDb();
					if (DocDitteDb.Search(ref cmd, ditta, codice, ref dod, joined))
					{
						if (json.Data == null) json.Data = new List<DocDitteDb>();
						json.Data.Add(dod);
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
		[Route("api/docditte/post")]
		public DefaultJson<DocDitteDb> Post([FromBody] DefaultJson<DocDitteDb> value)
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

					var json = new DefaultJson<DocDitteDb>();
					foreach (var dod in value.Data)
					{
						object obj = null;
						var val = dod;

						cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(dod_codice),0) FROM docditte WHERE dod_dit = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = val.dod_dit;

						val.dod_codice = 1 + (int)cmd.ExecuteScalar();
						val.dod_desc = val.dod_desc.Trim();
						if (string.IsNullOrWhiteSpace(val.dod_desc))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Descrizione vuota"));
							continue;
						}

						DbUtils.SqlWrite(ref cmd, DocDitteDb.Write, DbMessage.DB_INSERT, ref val, ref obj, true);
						if (json.Data == null) json.Data = new List<DocDitteDb>();
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
		[Route("api/docditte/put/{ditta}/{codice}")]
		public DefaultJson<DocDitteDb> Put(int ditta, int codice, [FromBody]DefaultJson<DocDitteDb> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non corrispondente"));
			if (value.Data.Count != 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non valido"));


			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var dod = value.Data[0];
					if (dod.dod_dit != ditta || dod.dod_codice != codice) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Id risorsa non corrisponde all'id dei dati"));
					dod.dod_desc = dod.dod_desc.Trim();
					if (string.IsNullOrWhiteSpace(dod.dod_desc)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Descrizione vuota"));

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					object obj = null;
					DbUtils.SqlWrite(ref cmd, DocDitteDb.Write, DbMessage.DB_UPDATE, ref dod, ref obj, true);
	
					var json = new DefaultJson<DocDitteDb>();
					if (json.Data == null) json.Data = new List<DocDitteDb>();
					json.Data.Add(dod);
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
		[Route("api/docditte/delete/{ditta}/{codice}")]
		public void Delete(int ditta, int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var val = new DocDitteDb();
					if (!DocDitteDb.Search(ref cmd, ditta, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object objx = null;
					DbUtils.SqlWrite(ref cmd, DocDitteDb.Write, DbMessage.DB_DELETE, ref val, ref objx);
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
