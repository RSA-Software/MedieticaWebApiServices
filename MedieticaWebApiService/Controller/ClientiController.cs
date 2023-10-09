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

	public class ClientiController : ApiController
	{
		[HttpGet]
		[Route("api/clienti/blank")]
		[Route("api/clienti/blank/{ditta}")]
		public DefaultJson<ClientiDb> Blank(int ditta = 0)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var json = new DefaultJson<ClientiDb>();

					cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(cli_codice),0) AS codice FROM clienti");
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var cli = new ClientiDb();
						cli.cli_tipo = (short)ClientiTipo.CLI_TYPE_SELECT;
						cli.cli_protesti = -1;
						cli.cli_cronaca_giud = -1;
						cli.cli_codice = 1 + reader.GetInt64(reader.GetOrdinal("codice"));
						if (json.Data == null) json.Data = new List<ClientiDb>();
						json.Data.Add(cli);
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
		[Route("api/clienti/get")]
		public DefaultJson<ClientiDb> GetList(int top = 0, int skip = 0, string orderby = "", string search = "", string filter = "", bool inlinecount = false, bool joined = false)
		{
			if (filter.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger filter value"));
			if (search.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger search value"));
			if (orderby.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger orderby value"));

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<ClientiDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.DITTE, EndpointsOperations.VIEW);

					var str = search.ToUpper().SqlQuote(true, true);
					string query;
					var total = 0L;
					if (inlinecount)
					{
						if (joined)
							query = ClientiDb.GetCountQuery();
						else
							query = "SELECT COUNT(*) FROM clienti";
						if (string.IsNullOrWhiteSpace(filter))
							query += " WHERE cli_codice > 0";
						else
							query += $" WHERE cli_codice > 0 AND ({filter})";

						if (!string.IsNullOrWhiteSpace(search))
						{
							if (joined)
								query += $" AND (cli_desc ILIKE {str} OR TRIM(CAST(cli_codice AS VARCHAR(15))) ILIKE {str})";
							else
								query += $" AND (cli_desc ILIKE {str} OR TRIM(CAST(cli_codice AS VARCHAR(15))) ILIKE {str})";
						}

						cmd.CommandText = DbUtils.QueryAdapt(query);
						total = (long)cmd.ExecuteScalar();
					}

					if (joined)
						query = ClientiDb.GetJoinQuery();
					else
						query = "SELECT * FROM clienti";
						
					if (string.IsNullOrWhiteSpace(filter))
						query += " WHERE cli_codice > 0";
					else 
						query += " WHERE cli_codice > 0 AND (" + filter + ")";

					if (!string.IsNullOrWhiteSpace(search))
					{
						if (joined)
							query += $" AND (cli_desc ILIKE {str} OR TRIM(CAST(cli_codice AS VARCHAR(15))) ILIKE {str})";
						else
							query += $" AND (cli_desc ILIKE {str} OR TRIM(CAST(cli_codice AS VARCHAR(15))) ILIKE {str})";
					}
					if (string.IsNullOrWhiteSpace(orderby))
						query += " ORDER BY cli_codice";
					else
						query += " ORDER BY " + orderby;
					cmd.CommandText = DbUtils.QueryAdapt(query, top, skip);

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var cli = new ClientiDb();
						DbUtils.SqlRead(ref reader, ref cli, joined ? null : ClientiDb.GetJoinExcludeFields());
						if (json.Data == null) json.Data = new List<ClientiDb>();
						json.Data.Add(cli);
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
		[Route("api/clienti/get/{codice}")]
		[Route("api/clienti/get/{codice}/{joined}")]
		[Route("api/clienti/get/{codice}/{joined}/{fulljoined}")]
		public DefaultJson<ClientiDb> Get(int codice, bool joined = false, bool fulljoined = false)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.DITTE, EndpointsOperations.VIEW);

					var json = new DefaultJson<ClientiDb>();
					var cli = new ClientiDb();
					if (ClientiDb.Search(ref cmd,  codice, ref cli, joined || fulljoined))
					{
						cli.img_list = new List<ImgClientiDb>();
						if (json.Data == null) json.Data = new List<ClientiDb>();
						if (fulljoined)
						{
							DbUtils.GetTokenLevel(Request);

							cmd.CommandText = @"
							SELECT * 
							FROM imgclienti 
							WHERE img_dit = ? AND img_codice = ? AND Mod(img_formato, 2) = 0
							ORDER BY img_formato
							";

							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = cli.cli_codice;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = cli.cli_codice;
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var img = new ImgClientiDb();
								DbUtils.SqlRead(ref reader, ref img);
								cli.img_list.Add(img);
							}
							reader.Close();
						}
						json.Data.Add(cli);
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
		[Route("api/clienti/post")]
		public DefaultJson<ClientiDb> Post([FromBody] DefaultJson<ClientiDb> value)
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

					var json = new DefaultJson<ClientiDb>();
					foreach (var cli in value.Data)
					{
						object obj = null;
						var val = cli;

						cli.cli_rag_soc1 = val.cli_rag_soc1.Trim();
						val.cli_rag_soc2 = val.cli_rag_soc2.Trim();
						val.cli_desc = (val.cli_rag_soc1 + " " + val.cli_rag_soc2).Trim();
						val.cli_codfis = val.cli_codfis.Trim().ToUpper();
						val.cli_piva = val.cli_piva.Trim().ToUpper();
						val.cli_user = codute;

						if (string.IsNullOrWhiteSpace(val.cli_rag_soc1))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Ragione Sociale/Cognome vuota"));
							continue;
						}

						if (cli.cli_tipo != (short)ClientiTipo.CLI_TYPE_IMPRESE && string.IsNullOrWhiteSpace(val.cli_rag_soc2))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Nome vuoto"));
							continue;
						}

						if (cli.cli_tipo == (short)ClientiTipo.CLI_TYPE_IMPRESE && string.IsNullOrWhiteSpace(val.cli_piva))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Partita IVA vuota"));
							continue;
						}
						if (cli.cli_tipo == (short)ClientiTipo.CLI_TYPE_PRIVATI && string.IsNullOrWhiteSpace(val.cli_codfis))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Codice Fiscale vuoto"));
							continue;
						}

						DbUtils.SqlWrite(ref cmd, ClientiDb.Write, DbMessage.DB_INSERT, ref val, ref obj, true);

						if (json.Data == null) json.Data = new List<ClientiDb>();
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
		[Route("api/clienti/put/{codice}")]
		public DefaultJson<ClientiDb> Put(int codice, [FromBody]DefaultJson<ClientiDb> value)
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

					var cli = value.Data[0];
					if (cli.cli_codice != codice) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Id risorsa non corrisponde all'id dei dati"));

					cli.cli_rag_soc1 = cli.cli_rag_soc1.Trim();
					cli.cli_rag_soc2 = cli.cli_rag_soc2.Trim();
					cli.cli_desc = (cli.cli_rag_soc1 + " " + cli.cli_rag_soc2).Trim();
					cli.cli_codfis = cli.cli_codfis.Trim().ToUpper();
					cli.cli_piva = cli.cli_piva.Trim().ToUpper();
					cli.cli_user = DbUtils.GetTokenUser(Request);

					if (string.IsNullOrWhiteSpace(cli.cli_rag_soc1)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Ragione Sociale /Cognome vuota"));
					if (cli.cli_tipo == (short)ClientiTipo.CLI_TYPE_PRIVATI && string.IsNullOrWhiteSpace(cli.cli_rag_soc2)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Nome vuoto"));
					if (cli.cli_tipo == (short)ClientiTipo.CLI_TYPE_IMPRESE && string.IsNullOrWhiteSpace(cli.cli_piva)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Partita IVA vuota"));
					if (cli.cli_tipo == (short)ClientiTipo.CLI_TYPE_PRIVATI && string.IsNullOrWhiteSpace(cli.cli_codfis)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Codice Fiscale vuoto"));

					object obj = null;
					
					var json = new DefaultJson<ClientiDb>();
					DbUtils.SqlWrite(ref cmd, ClientiDb.Write, DbMessage.DB_UPDATE, ref cli, ref obj, true);

					connection.Close();
					if (json.Data == null) json.Data = new List<ClientiDb>();
					json.Data.Add(cli);
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
		[Route("api/clienti/delete/{codice}")]
		public void Delete(int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, 0, Endpoints.DITTE, EndpointsOperations.DELETE);

					var val = new ClientiDb();
					if (!ClientiDb.Search(ref cmd, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object obj = null;
					DbUtils.SqlWrite(ref cmd, ClientiDb.Write, DbMessage.DB_DELETE, ref val, ref obj);

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
