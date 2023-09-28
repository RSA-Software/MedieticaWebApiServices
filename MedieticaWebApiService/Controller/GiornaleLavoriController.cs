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

	public class GiornaleLavoriController : ApiController
	{
		//
		// Giornale Lavori
		//
		#region GIORNALE
		[HttpGet]
		[Route("api/giornalelavori/blank/{ditta}")]
		public DefaultJson<GiornaleLavoriDb> Blank(int ditta)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<GiornaleLavoriDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(gio_codice),0) AS codice FROM giornalelav WHERE gio_dit = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = ditta;
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var gio = new GiornaleLavoriDb();
						gio.gio_codice = 1 + reader.GetInt32(reader.GetOrdinal("codice"));
						gio.gio_dit = ditta;
						gio.gio_temperatura_min = 13;
						gio.gio_temperatura_max = 25;
						gio.gio_data = DateTime.Now;
						if (json.Data == null) json.Data = new List<GiornaleLavoriDb>();
						json.Data.Add(gio);
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

		[Route("api/giornalelavori/get")]
		public DefaultJson<GiornaleLavoriDb> GetList(int ditta = 0, int top = 0, int skip = 0, string orderby = "", string search = "", string filter = "", bool inlinecount = false, bool joined = false )
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
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.GIORNALE_LAVORI_CANTIERI, EndpointsOperations.VIEW);

					var json = new DefaultJson<GiornaleLavoriDb>();
					var base_filter = $"gio_dit = {ditta}";
					var str = search.ToUpper().SqlQuote(true, true);
					string query;
					var total = 0L;
					if (inlinecount)
					{
						query = "SELECT COUNT(*) FROM giornalelav";
						if (string.IsNullOrWhiteSpace(filter))
							query += $" WHERE {base_filter}";
						else
							query += $" WHERE {base_filter} AND ({filter})";

						if (!string.IsNullOrWhiteSpace(search))
						{
							query += $" AND (gio_desc ILIKE {str} OR TRIM(CAST(gio_codice AS VARCHAR(15))) ILIKE {str})";
						}

						cmd.CommandText = DbUtils.QueryAdapt(query);
						total = (long)cmd.ExecuteScalar();
					}

					if (joined)
						query = GiornaleLavoriDb.GetJoinQuery();
					else
						query = "SELECT * FROM giornalelav";
					if (string.IsNullOrWhiteSpace(filter))
						query += $" WHERE {base_filter}";
					else
						query += $" WHERE {base_filter} AND ({filter})";

					if (!string.IsNullOrWhiteSpace(search))
					{
						query += $" AND (gio_desc ILIKE {str} OR TRIM(CAST(gio_codice AS VARCHAR(15))) ILIKE {str})";
					}

					if (string.IsNullOrWhiteSpace(orderby))
						query += " ORDER BY gio_dit, gio_can, gio_data DESC, gio_codice DESC";
					else
						query += " ORDER BY " + orderby;
					cmd.CommandText = DbUtils.QueryAdapt(query, top, skip);

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var mod = new GiornaleLavoriDb();
						DbUtils.SqlRead(ref reader, ref mod, joined ? null : GiornaleLavoriDb.GetJoinExcludeFields());
						if (json.Data == null) json.Data = new List<GiornaleLavoriDb>();
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
		[Route("api/giornalelavori/get/{ditta}/{codice}")]
		[Route("api/giornalelavori/get/{ditta}/{codice}/{joined}")]
		[Route("api/giornalelavori/get/{ditta}/{codice}/{joined}/{fulljoined}")]
		public DefaultJson<GiornaleLavoriDb> Get(int ditta, int codice, bool joined = false, bool fulljoined = false)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.GIORNALE_LAVORI_CANTIERI, EndpointsOperations.VIEW);

					var json = new DefaultJson<GiornaleLavoriDb>();
					var gio = new GiornaleLavoriDb();
					if (GiornaleLavoriDb.Search(ref cmd, ditta, codice, ref gio, joined || fulljoined))
					{
						if (json.Data == null) json.Data = new List<GiornaleLavoriDb>();
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
		[Route("api/giornalelavori/post")]
		public DefaultJson<GiornaleLavoriDb> Post([FromBody] DefaultJson<GiornaleLavoriDb> value)
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

					var json = new DefaultJson<GiornaleLavoriDb>();
					foreach (var gio in value.Data)
					{
						object obj = null;
						var val = gio;

						DbUtils.CheckAuthorization(cmd, Request, val.gio_dit, Endpoints.GIORNALE_LAVORI_CANTIERI, EndpointsOperations.ADD);
						val.gio_desc = val.gio_desc.Trim();
						val.gio_utente = codute;
						if (string.IsNullOrWhiteSpace(val.gio_desc))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Descrizione vuota"));
							continue;
						}
						cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(gio_codice),0) FROM giornalelav WHERE gio_dit = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = val.gio_dit;
						val.gio_codice = 1 + (int)cmd.ExecuteScalar();

						DbUtils.SqlWrite(ref cmd, GiornaleLavoriDb.Write, DbMessage.DB_INSERT, ref val, ref obj, true);
						if (json.Data == null) json.Data = new List<GiornaleLavoriDb>();
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
		[Route("api/giornalelavori/put/{ditta}/{codice}")]
		public DefaultJson<GiornaleLavoriDb> Put(int ditta, int codice, [FromBody]DefaultJson<GiornaleLavoriDb> value)
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
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.GIORNALE_LAVORI_CANTIERI, EndpointsOperations.UPDATE);
					var codute = DbUtils.GetTokenUser(Request);


					var gio = value.Data[0];
					if (gio.gio_dit != ditta || gio.gio_codice != codice) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Id risorsa non corrisponde all'id dei dati"));
					gio.gio_desc = gio.gio_desc.Trim();
					if (string.IsNullOrWhiteSpace(gio.gio_desc)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Descrizione vuota"));

					gio.gio_utente = codute;
					object obj = null;
					DbUtils.SqlWrite(ref cmd, GiornaleLavoriDb.Write, DbMessage.DB_UPDATE, ref gio, ref obj, true);

					var json = new DefaultJson<GiornaleLavoriDb>();
					if (json.Data == null) json.Data = new List<GiornaleLavoriDb>();
					json.Data.Add(gio);
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
		[Route("api/giornalelavori/delete/{ditta}/{codice}")]
		public void Delete(int ditta, int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.GIORNALE_LAVORI_CANTIERI, EndpointsOperations.DELETE);

					var val = new GiornaleLavoriDb();
					if (!GiornaleLavoriDb.Search(ref cmd, ditta, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object objx = null;
					DbUtils.SqlWrite(ref cmd, GiornaleLavoriDb.Write, DbMessage.DB_DELETE, ref val, ref objx);

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
		[Route("api/giornalelavori/exporttopdf/{ditta}/{codice}")]
		public DefaultJson<Reports> Print(int ditta, int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.GIORNALE_LAVORI_CANTIERI, EndpointsOperations.VIEW);

					var dit = new DitteDb();
					if (!DitteDb.Search(ref cmd, ditta, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

					var can = new CantieriDb();
					if (!CantieriDb.Search(ref cmd, ditta, codice, ref can)) throw new MCException(MCException.CantiereMsg, MCException.CantiereErr);
					connection.Close();

					var cry = new Crystal();
					cry.CryOpen("giornale_lavori.rpt", "Giornale dei Lavori");
					cry.CrySetDnsLessConnInfo(DbUtils.GetConnectionString(0, true), DbUtils.GetDatabase(), DbUtils.GetStartupOptions().User, DbUtils.GetStartupOptions().Password);
					cry.CrySetOrientation(PaperOrientation.Landscape);

					cry.CrySelect($"{{giornalelav.gio_dit}} = {ditta} AND {{giornalelav.gio_can}} = {codice}");
					cry.CryStringParam("Ditta", "", dit.dit_desc);
					cry.CryStringParam("Committente", "", can.can_ente_appaltante);
					cry.CryStringParam("Lavori", "", can.can_desc);
					cry.CryStringParam("Contratto", "", can.can_estremi_contratto);
					cry.CryLongParam("Codice", "", can.can_codice);
					cry.CryStringParam("Direttore", "", can.can_direttore_lavori);

					cry.CrySort("{giornalelav.gio_data}");
					cry.CrySort("{giornalelav.gio_codice}");

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


		#endregion // GIORNALE

		//
		//  Mezzi
		//
		#region MEZZI
		[HttpPost]
		[Route("api/giornalelavori/mezzi/post")]
		public DefaultJson<GiornaleLavoriDb> PostMezzi([FromBody] DefaultJson<GiornaleLavoriMezziDb> value)
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

					var json = new DefaultJson<GiornaleLavoriDb>();
					foreach (var gme in value.Data)
					{
						object obj = null;
						var val = gme;
						var cod = gme.gme_gio;
						var dit = gme.gme_dit;

						if (gme.gme_gio != 0 && gme.gme_dit != 0 && gme.gme_mez != 0 && gme.gme_mez_dit != 0)
						{
							DbUtils.CheckAuthorization(cmd, Request, gme.gme_dit, Endpoints.GIORNALE_LAVORI_CANTIERI, EndpointsOperations.UPDATE);
							try
							{

								val.gme_utente = codute;
								DbUtils.SqlWrite(ref cmd, GiornaleLavoriMezziDb.Write, DbMessage.DB_INSERT, ref val, ref obj);

								var gio = new GiornaleLavoriDb();
								if (!GiornaleLavoriDb.Search(ref cmd, dit, cod, ref gio, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);

								if (json.Data == null) json.Data = new List<GiornaleLavoriDb>();
								json.Data.Add(gio);
								json.RecordsTotal++;
							}
							catch (OdbcException ex)
							{
								if (!DbUtils.IsDupKeyErr(ex)) throw;
							}
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

		[HttpDelete]
		[Route("api/giornalelavori/mezzi/delete/{codice}")]
		public DefaultJson<GiornaleLavoriDb> DeleteMezzi(int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					
					var val = new GiornaleLavoriMezziDb();
					if (!GiornaleLavoriMezziDb.Search(ref cmd, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					DbUtils.CheckAuthorization(cmd, Request, val.gme_dit, Endpoints.GIORNALE_LAVORI_CANTIERI, EndpointsOperations.UPDATE);

					var cod = val.gme_gio;
					var dit = val.gme_dit;
					object objx = null;
					DbUtils.SqlWrite(ref cmd, GiornaleLavoriMezziDb.Write, DbMessage.DB_DELETE, ref val, ref objx);

					var gio = new GiornaleLavoriDb();
					if (!GiornaleLavoriDb.Search(ref cmd, dit, cod, ref gio, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);

					var json = new DefaultJson<GiornaleLavoriDb>();
					if (json.Data == null) json.Data = new List<GiornaleLavoriDb>();
					json.Data.Add(gio);
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
		#endregion // MEZZI

		//
		//  Dipendenti
		//
		#region DIPENDENTI
		[HttpPost]
		[Route("api/giornalelavori/dipendenti/post")]
		public DefaultJson<GiornaleLavoriDb> PostDipendenti([FromBody] DefaultJson<GiornaleLavoriDipendentiDb> value)
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

					var json = new DefaultJson<GiornaleLavoriDb>();
					foreach (var gdi in value.Data)
					{
						object obj = null;
						var val = gdi;
						var cod = gdi.gdi_gio;
						var dit = gdi.gdi_dit;
						
						if (gdi.gdi_gio != 0 && gdi.gdi_dit != 0 && gdi.gdi_dip != 0 && gdi.gdi_dip_dit != 0)
						{
							DbUtils.CheckAuthorization(cmd, Request, gdi.gdi_dit, Endpoints.GIORNALE_LAVORI_CANTIERI, EndpointsOperations.UPDATE);
							try
							{
								val.gdi_utente = codute;
								DbUtils.SqlWrite(ref cmd, GiornaleLavoriDipendentiDb.Write, DbMessage.DB_INSERT, ref val, ref obj);

								var gio = new GiornaleLavoriDb();
								if (!GiornaleLavoriDb.Search(ref cmd, dit, cod, ref gio, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);

								if (json.Data == null) json.Data = new List<GiornaleLavoriDb>();
								json.Data.Add(gio);
								json.RecordsTotal++;
							}
							catch (OdbcException ex)
							{
								if (!DbUtils.IsDupKeyErr(ex)) throw;
							}
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

		[HttpDelete]
		[Route("api/giornalelavori/dipendenti/delete/{codice}")]
		public DefaultJson<GiornaleLavoriDb> DeleteDipendenti(int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					
					var val = new GiornaleLavoriDipendentiDb();
					if (!GiornaleLavoriDipendentiDb.Search(ref cmd, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					DbUtils.CheckAuthorization(cmd, Request, val.gdi_dit, Endpoints.GIORNALE_LAVORI_CANTIERI, EndpointsOperations.UPDATE);


					var cod = val.gdi_gio;
					var dit = val.gdi_dit;
					object objx = null;
					DbUtils.SqlWrite(ref cmd, GiornaleLavoriDipendentiDb.Write, DbMessage.DB_DELETE, ref val, ref objx);

					var gio = new GiornaleLavoriDb();
					if (!GiornaleLavoriDb.Search(ref cmd, dit, cod, ref gio, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);

					var json = new DefaultJson<GiornaleLavoriDb>();
					if (json.Data == null) json.Data = new List<GiornaleLavoriDb>();
					json.Data.Add(gio);
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
		#endregion // DIPENDENTI
	}
}
