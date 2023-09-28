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

	public class DitteController : ApiController
	{
		[HttpGet]
		[Route("api/ditte/blank")]
		[Route("api/ditte/blank/{ditta}")]
		public DefaultJson<DitteDb> Blank(int ditta = 0)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var json = new DefaultJson<DitteDb>();

					cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(dit_codice),0) AS codice FROM ditte");
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var dit = new DitteDb();
						dit.dit_codice = 1 + reader.GetInt32(reader.GetOrdinal("codice"));
						if (json.Data == null) json.Data = new List<DitteDb>();
						json.Data.Add(dit);
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
		[Route("api/ditte/get")]
		public DefaultJson<DitteDb> GetList(int top = 0, int skip = 0, string orderby = "", string search = "", string filter = "", bool inlinecount = false, bool joined = false)
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
					var json = new DefaultJson<DitteDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.DITTE, EndpointsOperations.VIEW);

					var str = search.ToUpper().SqlQuote(true, true);
					string query;
					var total = 0L;
					if (inlinecount)
					{
						if (joined)
							query = DitteDb.GetCountQuery();
						else
							query = "SELECT COUNT(*) FROM ditte";
						if (string.IsNullOrWhiteSpace(filter))
							query += " WHERE dit_codice > 0";
						else
							query += $" WHERE dit_codice > 0 AND ({filter})";

						if (!string.IsNullOrWhiteSpace(search))
						{
							if (joined)
								query += $" AND (dit_desc ILIKE {str} OR TRIM(CAST(dit_codice AS VARCHAR(15))) ILIKE {str})";
							else
								query += $" AND (dit_desc ILIKE {str} OR TRIM(CAST(dit_codice AS VARCHAR(15))) ILIKE {str})";
						}

						cmd.CommandText = DbUtils.QueryAdapt(query);
						total = (long)cmd.ExecuteScalar();
					}

					if (joined)
						query = DitteDb.GetJoinQuery();
					else
						query = "SELECT * FROM ditte";
						
					if (string.IsNullOrWhiteSpace(filter))
						query += " WHERE dit_codice > 0";
					else 
						query += " WHERE dit_codice > 0 AND (" + filter + ")";

					if (!string.IsNullOrWhiteSpace(search))
					{
						if (joined)
							query += $" AND (dit_desc ILIKE {str} OR TRIM(CAST(dit_codice AS VARCHAR(15))) ILIKE {str})";
						else
							query += $" AND (dit_desc ILIKE {str} OR TRIM(CAST(dit_codice AS VARCHAR(15))) ILIKE {str})";
					}
					if (string.IsNullOrWhiteSpace(orderby))
						query += " ORDER BY dit_codice";
					else
						query += " ORDER BY " + orderby;
					cmd.CommandText = DbUtils.QueryAdapt(query, top, skip);

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var dit = new DitteDb();
						DbUtils.SqlRead(ref reader, ref dit, joined ? null : DitteDb.GetJoinExcludeFields());
						if (json.Data == null) json.Data = new List<DitteDb>();
						json.Data.Add(dit);
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
		[Route("api/ditte/get/{codice}")]
		[Route("api/ditte/get/{codice}/{joined}")]
		[Route("api/ditte/get/{codice}/{joined}/{fulljoined}")]
		public DefaultJson<DitteDb> Get(int codice, bool joined = false, bool fulljoined = false)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.DITTE, EndpointsOperations.VIEW);

					var json = new DefaultJson<DitteDb>();
					var dit = new DitteDb();
					if (DitteDb.Search(ref cmd,  codice, ref dit, joined || fulljoined))
					{
						dit.img_list = new List<ImgDitteDb>();
						if (json.Data == null) json.Data = new List<DitteDb>();
						if (fulljoined)
						{
							DbUtils.GetTokenLevel(Request);

							cmd.CommandText = @"
							SELECT * 
							FROM imgditte 
							WHERE img_dit = ? AND img_codice = ? AND Mod(img_formato, 2) = 0
							ORDER BY img_formato
							";

							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = dit.dit_codice;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dit.dit_codice;
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var img = new ImgDitteDb();
								DbUtils.SqlRead(ref reader, ref img);
								dit.img_list.Add(img);
							}
							reader.Close();
						}
						json.Data.Add(dit);
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
		[Route("api/ditte/getsub/{ditApp}/{cantiere}/{codice}")]
		public DefaultJson<DitteDb> GetDittaSubappalto(int ditApp, int cantiere, int codice, bool joined = true)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var json = new DefaultJson<DitteDb>();
					var dit = new DitteDb();
					if (DitteDb.Search(ref cmd, codice, ref dit, joined))
					{
						cmd.CommandText = @"
						SELECT COUNT(*)
						FROM subappalti 
						WHERE sub_dit_app = ? AND sub_can_app = ? AND sub_dit_sub = ?";

						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditApp;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = cantiere;
						cmd.Parameters.Add("ditsub", OdbcType.Int).Value = dit.dit_codice;
						var tot  = (long)cmd.ExecuteScalar();
						if (tot > 0)
						{
							if (json.Data == null) json.Data = new List<DitteDb>();
							json.Data.Add(dit);
							json.RecordsTotal++;
						}
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
		[Route("api/ditte/post/{ditta}")]
		public DefaultJson<DitteDb> Post(int ditta, [FromBody] DefaultJson<DitteDb> value)
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
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.DITTE, EndpointsOperations.ADD);
					var codute = DbUtils.GetTokenUser(Request);

					var dit = new DitteDb();
					if (!DitteDb.Search(ref cmd, ditta, ref dit)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);

					cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(dit_codice),0) FROM ditte");
					var last = (int)cmd.ExecuteScalar();

					var json = new DefaultJson<DitteDb>();
					foreach (var mer in value.Data)
					{
						var val = mer;

						val.dit_codice = 1 + last;
						val.dit_rag_soc1 = val.dit_rag_soc1;
						val.dit_rag_soc2 = val.dit_rag_soc2;
						val.dit_desc = val.dit_rag_soc1 + " " + val.dit_rag_soc2;
						val.dit_desc = val.dit_desc.Trim();
						val.dit_codfis = val.dit_codfis.Trim().ToUpper();
						val.dit_piva = val.dit_piva.Trim().ToUpper();
						

						//
						// Aggiungere utente
						
						if (string.IsNullOrWhiteSpace(val.dit_rag_soc1))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Ragione Sociale vuota"));
							continue;
						}
						if (string.IsNullOrWhiteSpace(val.dit_piva))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Partita IVA vuota"));
							continue;
						}
						if (string.IsNullOrWhiteSpace(val.dit_codfis))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Codice Fiscale vuoto"));
							continue;
						}


						DbUtils.SqlWrite(ref cmd, DitteDb.Write, DbMessage.DB_INSERT, ref val, ref codute, true);
						last = val.dit_codice;

						if (json.Data == null) json.Data = new List<DitteDb>();
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
		[Route("api/ditte/put/{codice}")]
		public DefaultJson<DitteDb> Put(int codice, [FromBody]DefaultJson<DitteDb> value)
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

					var dit = value.Data[0];
					if (dit.dit_codice != codice) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Id risorsa non corrisponde all'id dei dati"));

					dit.dit_rag_soc1 = dit.dit_rag_soc1.Trim();
					dit.dit_rag_soc2 = dit.dit_rag_soc2.Trim();
					dit.dit_desc = dit.dit_rag_soc1 + " " + dit.dit_rag_soc2;
					dit.dit_desc = dit.dit_desc.Trim();
					dit.dit_codfis = dit.dit_codfis.Trim().ToUpper();
					dit.dit_piva = dit.dit_piva.Trim().ToUpper();

					if (string.IsNullOrWhiteSpace(dit.dit_rag_soc1)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Rgione Sociale vuota"));
					if (string.IsNullOrWhiteSpace(dit.dit_piva)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Partita IVA vuota"));
					if (string.IsNullOrWhiteSpace(dit.dit_codfis)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Codice Fiscale vuoto"));

					var codute = DbUtils.GetTokenUser(Request);
					var json = new DefaultJson<DitteDb>();
					DbUtils.SqlWrite(ref cmd, DitteDb.Write, DbMessage.DB_UPDATE, ref dit, ref codute, true);

					connection.Close();
					if (json.Data == null) json.Data = new List<DitteDb>();
					json.Data.Add(dit);
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
		[Route("api/ditte/delete/{codice}")]
		public void Delete(int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.DITTE, EndpointsOperations.DELETE);

					var val = new DitteDb();
					if (!DitteDb.Search(ref cmd, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					int codute = 0;
					DbUtils.SqlWrite(ref cmd, DitteDb.Write, DbMessage.DB_DELETE, ref val, ref codute);

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
