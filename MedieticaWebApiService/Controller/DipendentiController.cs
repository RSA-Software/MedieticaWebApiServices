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

	public class DipendentiController : ApiController
	{
		[HttpGet]
		[Route("api/dipendenti/blank/{ditta}")]
		public DefaultJson<DipendentiDb> Blank(int ditta)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<DipendentiDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(dip_codice),0) AS codice FROM dipendenti WHERE dip_dit = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = ditta;
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var dip = new DipendentiDb();
						dip.dip_dit = ditta;
						dip.dip_codice = 1 + reader.GetInt32(reader.GetOrdinal("codice"));
						if (json.Data == null) json.Data = new List<DipendentiDb>();
						json.Data.Add(dip);
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


		[Route("api/dipendenti/get")]
		public DefaultJson<DipendentiDb> GetList(int ditta = 0, int top = 0, int skip = 0, string orderby = "", string search = "", string filter = "", bool inlinecount = false, bool joined = false )
		{
			if (filter.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger filter value"));
			if (search.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger search value"));
			if (orderby.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger orderby value"));

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.DIPENDENTI, EndpointsOperations.VIEW);

					var json = new DefaultJson<DipendentiDb>();
					var str = search.ToUpper().SqlQuote(true, true);
					string query;
					var total = 0L;
					if (inlinecount)
					{
						query = "SELECT COUNT(*) FROM dipendenti";
						if (string.IsNullOrWhiteSpace(filter))
							query += $" WHERE dip_dit = {ditta} AND dip_deleted = 0";
						else
							query += $" WHERE dip_dit = {ditta} AND dip_deleted = 0 AND ({filter})";

						if (!string.IsNullOrWhiteSpace(search))
						{
							query += $" AND (dip_desc ILIKE {str} OR TRIM(CAST(dip_codice AS VARCHAR(15))) ILIKE {str})";
						}

						cmd.CommandText = DbUtils.QueryAdapt(query);
						total = (long)cmd.ExecuteScalar();
					}

					if (joined)
						query = DipendentiDb.GetJoinQuery();
					else
						query = "SELECT * FROM dipendenti";
					if (string.IsNullOrWhiteSpace(filter))
						query += $" WHERE dip_dit = {ditta} AND dip_deleted = 0";
					else
						query += $" WHERE dip_dit = {ditta} AND dip_deleted = 0 AND ({filter})";

					if (!string.IsNullOrWhiteSpace(search))
					{
						query += $" AND (dip_desc ILIKE {str} OR TRIM(CAST(dip_codice AS VARCHAR(15))) ILIKE {str})";
					}

					if (string.IsNullOrWhiteSpace(orderby))
						query += " ORDER BY dip_codice";
					else
						query += " ORDER BY " + orderby;
					cmd.CommandText = DbUtils.QueryAdapt(query, top, skip);

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var dip = new DipendentiDb();
						DbUtils.SqlRead(ref reader, ref dip, joined ? null : DipendentiDb.GetJoinExcludeFields());
						if (json.Data == null) json.Data = new List<DipendentiDb>();
						json.Data.Add(dip);
						json.RecordsTotal++;
					}
					reader.Close();
					

					if (json.Data != null)
					{
						foreach (var dip in json.Data)
						{
							dip.man_list = new List<DipMansioniDb>();
							cmd.CommandText = DipMansioniDb.GetJoinQuery() + " WHERE dma_dit = ? AND dma_dip = ? ORDER BY dma_dit, dma_dip, dma_man";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dip.dip_codice;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var man = new DipMansioniDb();
								DbUtils.SqlRead(ref reader, ref man);
								dip.man_list.Add(man);
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
		[Route("api/dipendenti/get/{ditta}/{codice}")]
		[Route("api/dipendenti/get/{ditta}/{codice}/{joined}")]
		[Route("api/dipendenti/get/{ditta}/{codice}/{joined}/{fulljoined}")]
		public DefaultJson<DipendentiDb> Get(int ditta, int codice, bool joined = false, bool fulljoined = false)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.DIPENDENTI, EndpointsOperations.VIEW);

					var json = new DefaultJson<DipendentiDb>();
					var dip = new DipendentiDb();
					if (DipendentiDb.Search(ref cmd, ditta, codice, ref dip, joined || fulljoined))
					{
						dip.img_list = new List<ImgDipendentiDb>();
						if (json.Data == null) json.Data = new List<DipendentiDb>();
						if (fulljoined)
						{
							DbUtils.GetTokenLevel(Request);

							cmd.CommandText = @"
							SELECT * 
							FROM imgdipendenti 
							WHERE img_dit = ? AND img_codice = ? AND Mod(img_formato, 2) = 0
							ORDER BY img_formato
							";

							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = dip.dip_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dip.dip_codice;
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var img = new ImgDipendentiDb();
								DbUtils.SqlRead(ref reader, ref img);
								dip.img_list.Add(img);
							}
							reader.Close();
						}
						json.Data.Add(dip);
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
		[Route("api/dipendenti/post")]
		public DefaultJson<DipendentiDb> Post([FromBody] DefaultJson<DipendentiDb> value)
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

					var json = new DefaultJson<DipendentiDb>();
					foreach (var dip in value.Data)
					{
						object obj = null;
						var val = dip;

						DbUtils.CheckAuthorization(cmd, Request, val.dip_dit, Endpoints.DIPENDENTI, EndpointsOperations.ADD);

						cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(dip_codice),0) FROM dipendenti WHERE dip_dit = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = val.dip_dit;
						
						val.dip_codice  = 1 + (int)cmd.ExecuteScalar();
						val.dip_desc = val.dip_desc.Trim();
						if (string.IsNullOrWhiteSpace(val.dip_cognome))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Cognome vuoto"));
							continue;
						}
						if (string.IsNullOrWhiteSpace(val.dip_nome))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Nome vuoto"));
							continue;
						}

						val.dip_user = cod_ute;
						DbUtils.SqlWrite(ref cmd, DipendentiDb.Write, DbMessage.DB_INSERT, ref val, ref obj, true);
						if (json.Data == null) json.Data = new List<DipendentiDb>();
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


		[HttpPost]
		[Route("api/dipendenti/mansioni/post/{coddit}/{coddip}")]
		public DefaultJson<DipMansioniDb> PostMansione(int coddit, int coddip, [FromBody] DefaultJson<DipMansioniDb> value)
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
					DbUtils.CheckAuthorization(cmd, Request, coddit, Endpoints.DIPENDENTI, EndpointsOperations.ADD);
					var cod_ute = DbUtils.GetTokenUser(Request);

					var json = new DefaultJson<DipMansioniDb>();
					foreach (var dma in value.Data)
					{
						object obj = null;
						var val = dma;

						if (dma.dma_dit != coddit) continue;
						if (dma.dma_dip != coddip) continue;
						if (dma.dma_man == 0) continue;

						val.dma_user = cod_ute;
						DbUtils.SqlWrite(ref cmd, DipMansioniDb.Write, DbMessage.DB_INSERT, ref val, ref obj, true);
					}

					cmd.CommandText = DipMansioniDb.GetJoinQuery() + " WHERE dma_dit = ? AND dma_dip = ? ORDER BY dma_dit, dma_dip, dma_man";
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = coddit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = coddip;
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var man = new DipMansioniDb();
						DbUtils.SqlRead(ref reader, ref man);
						if (json.Data == null) json.Data = new List<DipMansioniDb>();
						json.Data.Add(man);
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
		[Route("api/dipendenti/sedi/post/{coddit}/{coddip}")]
		public DefaultJson<DipSediDb> PostSede(int coddit, int coddip, [FromBody] DefaultJson<DipSediDb> value)
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
					DbUtils.CheckAuthorization(cmd, Request, coddit, Endpoints.DIPENDENTI, EndpointsOperations.ADD);
					var cod_ute = DbUtils.GetTokenUser(Request);

					var json = new DefaultJson<DipSediDb>();
					foreach (var dse in value.Data)
					{
						object obj = null;
						var val = dse;

						if (dse.dse_dit != coddit) continue;
						if (dse.dse_dip != coddip) continue;
						if (dse.dse_sed == 0) continue;

						val.dse_user = cod_ute;
						DbUtils.SqlWrite(ref cmd, DipSediDb.Write, DbMessage.DB_INSERT, ref val, ref obj, true);
					}

					cmd.CommandText = DipSediDb.GetJoinQuery() + " WHERE dse_dit = ? AND dse_dip = ? ORDER BY dse_dit, dse_dip, dse_sed";
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = coddit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = coddip;
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var sed = new DipSediDb();
						DbUtils.SqlRead(ref reader, ref sed);
						if (json.Data == null) json.Data = new List<DipSediDb>();
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
		[Route("api/dipendenti/put/{ditta}/{codice}/{checklist}")]
		public DefaultJson<DipendentiDb> Put(int ditta, int codice, bool checklist, [FromBody]DefaultJson<DipendentiDb> value)
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
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.DIPENDENTI, EndpointsOperations.UPDATE);

					var dip = value.Data[0];
					if (dip.dip_dit != ditta || dip.dip_codice != codice) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Id risorsa non corrisponde all'id dei dati"));
					dip.dip_desc = dip.dip_desc.Trim();
					if (string.IsNullOrWhiteSpace(dip.dip_cognome)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Cognome vuoto"));
					if (string.IsNullOrWhiteSpace(dip.dip_nome)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Nome vuoto"));

					dip.dip_user = DbUtils.GetTokenUser(Request);

					object obj = null;
					if (checklist)
						DbUtils.SqlWrite(ref cmd, DipendentiDb.Write, DbMessage.DB_REWRITE, ref dip, ref obj, true);
					else
						DbUtils.SqlWrite(ref cmd, DipendentiDb.Write, DbMessage.DB_UPDATE, ref dip, ref obj, true);

					var json = new DefaultJson<DipendentiDb>();
					if (json.Data == null) json.Data = new List<DipendentiDb>();
					json.Data.Add(dip);
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
		[Route("api/dipendenti/delete/{ditta}/{codice}")]
		public void Delete(int ditta, int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.DIPENDENTI, EndpointsOperations.DELETE);

					var val = new DipendentiDb();
					if (!DipendentiDb.Search(ref cmd, ditta, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					if (DbUtils.GetTokenLevel(Request) != (short)UserLevel.SUPERADMIN)
					{
						val.dip_user = DbUtils.GetTokenUser(Request);
						val.dip_deleted = true;

						object objx = null;
						DbUtils.SqlWrite(ref cmd, DipendentiDb.Write, DbMessage.DB_UPDATE, ref val, ref objx);
					}
					else
					{
						object objx = null;
						DbUtils.SqlWrite(ref cmd, DipendentiDb.Write, DbMessage.DB_DELETE, ref val, ref objx);
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
		}


		[HttpDelete]
		[Route("api/dipendenti/mansioni/delete/{ditta}/{codice}/{mansione}")]
		public void DeleteMansioni(int ditta, int codice, int mansione)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.DIPENDENTI, EndpointsOperations.DELETE);

					var val = new DipMansioniDb();
					if (!DipMansioniDb.Search(ref cmd, ditta, codice, mansione, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object objx = null;
					DbUtils.SqlWrite(ref cmd, DipMansioniDb.Write, DbMessage.DB_DELETE, ref val, ref objx);

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

		[HttpDelete]
		[Route("api/dipendenti/sedi/delete/{ditta}/{codice}/{sede}")]
		public void DeleteSedi(int ditta, int codice, int sede)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.DIPENDENTI, EndpointsOperations.DELETE);

					var val = new DipSediDb();
					if (!DipSediDb.Search(ref cmd, ditta, codice, sede, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object objx = null;
					DbUtils.SqlWrite(ref cmd, DipSediDb.Write, DbMessage.DB_DELETE, ref val, ref objx);

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
