using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using MedieticaWebApiService.Extensions;
using CrystalDecisions.Shared;
using MedieticaWebApiService.Helpers;
using MedieticaWebApiService.Models;
using MedieticaWebApiService.ViewModel;

namespace MedieticaWebApiService.Controller
{
	[EnableCors("*", "*", "*")]

	public class CertificatiPagamentoController : ApiController
	{
		[HttpGet]
		[Route("api/certificatipagamento/blank/{ditta}")]
		public DefaultJson<CertificatiPagamentoDb> Blank(int ditta)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<CertificatiPagamentoDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(cpa_codice),0) AS codice FROM certificatipag WHERE cpa_dit = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = ditta;
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var cpa = new CertificatiPagamentoDb();
						cpa.cpa_codice = 1 + reader.GetInt32(reader.GetOrdinal("codice"));
						cpa.cpa_dit = ditta;
						cpa.cpa_data = DateTime.Now;
						if (json.Data == null) json.Data = new List<CertificatiPagamentoDb>();
						json.Data.Add(cpa);
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

		[Route("api/certificatipagamento/get")]
		public DefaultJson<CertificatiPagamentoDb> GetList(int ditta = 0, int top = 0, int skip = 0, string orderby = "", string search = "", string filter = "", bool inlinecount = false, bool joined = false )
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
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.CERTIFICATI_DI_PAGAMENTO, EndpointsOperations.VIEW);

					var json = new DefaultJson<CertificatiPagamentoDb>();
					var base_filter = $"cpa_dit = {ditta}";
					var str = search.ToUpper().SqlQuote(true, true);
					string query;
					var total = 0L;
					if (inlinecount)
					{
						query = @"
						SELECT COUNT(*) 
						FROM certificatipag
						LEFT JOIN ditte s ON cpa_sub = s.dit_codice
						";
						if (string.IsNullOrWhiteSpace(filter))
							query += $" WHERE {base_filter}";
						else
							query += $" WHERE {base_filter} AND ({filter})";

						if (!string.IsNullOrWhiteSpace(search))
						{
							if (joined)
								query += $" AND (cpa_desc ILIKE {str} OR s.dit_desc ILIKE {str} OR s.dit_codfis ILIKE {str} OR TRIM(CAST(cpa_codice AS VARCHAR(15))) ILIKE {str})";
							else
								query += $" AND (cpa_desc ILIKE {str} OR TRIM(CAST(cpa_codice AS VARCHAR(15))) ILIKE {str})";
						}

						cmd.CommandText = DbUtils.QueryAdapt(query);
						total = (long)cmd.ExecuteScalar();
					}

					if (joined)
						query = CertificatiPagamentoDb.GetJoinQuery();
					else
						query = "SELECT * FROM certificatipag";
					if (string.IsNullOrWhiteSpace(filter))
						query += $" WHERE {base_filter}";
					else
						query += $" WHERE {base_filter} AND ({filter})";

					if (!string.IsNullOrWhiteSpace(search))
					{
						if (joined)
							query += $" AND (cpa_desc ILIKE {str} OR s.dit_desc ILIKE {str} OR s.dit_codfis ILIKE {str} OR TRIM(CAST(cpa_codice AS VARCHAR(15))) ILIKE {str})";
						else
							query += $" AND (cpa_desc ILIKE {str} OR TRIM(CAST(cpa_codice AS VARCHAR(15))) ILIKE {str})";
					}

					if (string.IsNullOrWhiteSpace(orderby))
						query += " ORDER BY cpa_dit, cpa_can, cpa_data DESC, cpa_codice DESC";
					else
						query += " ORDER BY " + orderby;
					cmd.CommandText = DbUtils.QueryAdapt(query, top, skip);

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var mod = new CertificatiPagamentoDb();
						DbUtils.SqlRead(ref reader, ref mod, joined ? null : CertificatiPagamentoDb.GetJoinExcludeFields());
						if (json.Data == null) json.Data = new List<CertificatiPagamentoDb>();
						json.Data.Add(mod);
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
		[Route("api/certificatipagamento/get/{ditta}/{codice}")]
		[Route("api/certificatipagamento/get/{ditta}/{codice}/{joined}")]
		[Route("api/certificatipagamento/get/{ditta}/{codice}/{joined}/{fulljoined}")]
		public DefaultJson<CertificatiPagamentoDb> Get(int ditta, int codice, bool joined = false, bool fulljoined = false)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.CERTIFICATI_DI_PAGAMENTO, EndpointsOperations.VIEW);

					var json = new DefaultJson<CertificatiPagamentoDb>();
					var gio = new CertificatiPagamentoDb();
					if (CertificatiPagamentoDb.Search(ref cmd, ditta, codice, ref gio, joined || fulljoined))
					{
						if (json.Data == null) json.Data = new List<CertificatiPagamentoDb>();
						json.Data.Add(gio);
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
		[Route("api/certificatipagamento/post")]
		public DefaultJson<CertificatiPagamentoDb> Post([FromBody] DefaultJson<CertificatiPagamentoDb> value)
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
					var codute = DbUtils.GetTokenUser(Request); 

					var json = new DefaultJson<CertificatiPagamentoDb>();
					foreach (var gio in value.Data)
					{
						object obj = null;
						var val = gio;

						DbUtils.CheckAuthorization(cmd, Request, val.cpa_dit, Endpoints.CERTIFICATI_DI_PAGAMENTO, EndpointsOperations.ADD);
						val.cpa_desc = val.cpa_desc.Trim();
						val.cpa_utente = codute;
						if (string.IsNullOrWhiteSpace(val.cpa_desc))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Descrizione vuota"));
							continue;
						}
						if (!val.cpa_data.HasValue)
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Data vuota"));
							continue;
						}
						if (val.cpa_data_fat.HasValue && val.cpa_data_fat.Value < val.cpa_data.Value) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "La data della Fatture deve essere successiva o uguale alla data del documento"));

						if (val.cpa_firma_sub) DbUtils.CheckAuthorization(cmd, Request, val.cpa_dit, Endpoints.CERTIFICATI_DI_PAGAMENTO, EndpointsOperations.SPECIAL1);
						if (val.cpa_firma_dir) DbUtils.CheckAuthorization(cmd, Request, val.cpa_dit, Endpoints.CERTIFICATI_DI_PAGAMENTO, EndpointsOperations.SPECIAL2);
						if (val.cpa_firma_amm) DbUtils.CheckAuthorization(cmd, Request, val.cpa_dit, Endpoints.CERTIFICATI_DI_PAGAMENTO, EndpointsOperations.SPECIAL3);

						cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(cpa_codice),0) FROM certificatipag WHERE cpa_dit = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = val.cpa_dit;
						val.cpa_codice = 1 + (int)cmd.ExecuteScalar();

						DbUtils.SqlWrite(ref cmd, CertificatiPagamentoDb.Write, DbMessage.DB_INSERT, ref val, ref obj, true);
						if (json.Data == null) json.Data = new List<CertificatiPagamentoDb>();
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
		[Route("api/certificatipagamento/put/{ditta}/{codice}")]
		public DefaultJson<CertificatiPagamentoDb> Put(int ditta, int codice, [FromBody]DefaultJson<CertificatiPagamentoDb> value)
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
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.CERTIFICATI_DI_PAGAMENTO, EndpointsOperations.UPDATE);
					var codute = DbUtils.GetTokenUser(Request);

					var cpa = value.Data[0];
					if (cpa.cpa_dit != ditta || cpa.cpa_codice != codice) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Id risorsa non corrisponde all'id dei dati"));
					cpa.cpa_desc = cpa.cpa_desc.Trim();
					if (string.IsNullOrWhiteSpace(cpa.cpa_desc)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Descrizione vuota"));
					if (!cpa.cpa_data.HasValue) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Data vuota"));

					if (cpa.cpa_data_fat.HasValue && cpa.cpa_data_fat.Value < cpa.cpa_data.Value ) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "La data della Fatture deve essere successiva o uguale alla data del documento"));

					//
					// Leggiamo il vecchio record
					//
					var val = new CertificatiPagamentoDb();
					if (!CertificatiPagamentoDb.Search(ref cmd, ditta, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));
					if (!val.cpa_firma_sub && cpa.cpa_firma_sub) DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.CERTIFICATI_DI_PAGAMENTO, EndpointsOperations.SPECIAL1);
					if (!val.cpa_firma_dir && cpa.cpa_firma_dir) DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.CERTIFICATI_DI_PAGAMENTO, EndpointsOperations.SPECIAL2);
					if (!val.cpa_firma_amm && cpa.cpa_firma_amm) DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.CERTIFICATI_DI_PAGAMENTO, EndpointsOperations.SPECIAL3);

					cpa.cpa_utente = codute;
					object obj = null;
					DbUtils.SqlWrite(ref cmd, CertificatiPagamentoDb.Write, DbMessage.DB_UPDATE, ref cpa, ref obj, true);

					var json = new DefaultJson<CertificatiPagamentoDb>();
					if (json.Data == null) json.Data = new List<CertificatiPagamentoDb>();
					json.Data.Add(cpa);
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
		[Route("api/certificatipagamento/delete/{ditta}/{codice}")]
		public void Delete(int ditta, int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.CERTIFICATI_DI_PAGAMENTO, EndpointsOperations.DELETE);

					var val = new CertificatiPagamentoDb();
					if (!CertificatiPagamentoDb.Search(ref cmd, ditta, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object objx = null;
					DbUtils.SqlWrite(ref cmd, CertificatiPagamentoDb.Write, DbMessage.DB_DELETE, ref val, ref objx);

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

		[HttpGet]
		[Route("api/certificatipagamento/exporttopdf/{ditta}/{codice}")]
		public DefaultJson<Reports> Print(int ditta, int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.CERTIFICATI_DI_PAGAMENTO, EndpointsOperations.VIEW);

					var dit = new DitteDb();
					if (!DitteDb.Search(ref cmd, ditta, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

					var can = new CantieriDb();
					if (!CantieriDb.Search(ref cmd, ditta, codice, ref can)) throw new MCException(MCException.CantiereMsg, MCException.CantiereErr);
					connection.Close();

					var cry = new Crystal();
					cry.CryOpen("certificati_pagamento.rpt", "Certificati di Pagamento");
					cry.CrySetDnsLessConnInfo(DbUtils.GetConnectionString(0, true), DbUtils.GetDatabase(), DbUtils.GetStartupOptions().User, DbUtils.GetStartupOptions().Password);
					cry.CrySetOrientation(PaperOrientation.Landscape);

					cry.CrySelect($"{{certificatipag.cpa_dit}} = {ditta} AND {{certificatipag.cpa_can}} = {codice}");
					cry.CryStringParam("Ditta", "", dit.dit_desc);
					cry.CryStringParam("Cantiere", "", can.can_desc);

					cry.CrySort("{certificatipag.gio_data}");
					cry.CrySort("{certificatipag.gio_codice}");

					cry.CryEsegui();
					cry.CryClose();

					var content = File.ReadAllBytes(cry.pdf_path + @"/" + cry.pdf_file);
					var rep = new Reports();
					rep.rep_name = cry.pdf_file;
					rep.rep_data = Convert.ToBase64String(content);

#if !DEBUG
					File.Delete(cry.pdf_path + @"/" + cry.pdf_file);
#endif

					var json = new DefaultJson<Reports>();

					if (json.Data == null) json.Data = new List<Reports>();
					json.Data.Add(rep);
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
	}
}
