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

	public class ManutenzioniController : ApiController
	{
		[HttpGet]
		[Route("api/manutenzioni/blank/{ditta}")]
		public DefaultJson<ManutenzioniDb> Blank(int ditta)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<ManutenzioniDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(mnt_codice),0) AS codice FROM manutenzioni WHERE mnt_dit = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = ditta;
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var mnt = new ManutenzioniDb();
						mnt.mnt_dit = ditta;
						mnt.mnt_data = DateTime.Now;
						mnt.mnt_codice = 1 + reader.GetInt32(reader.GetOrdinal("codice"));
						if (json.Data == null) json.Data = new List<ManutenzioniDb>();
						json.Data.Add(mnt);
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


		[Route("api/manutenzioni/get")]
		public DefaultJson<ManutenzioniDb> GetList(int ditta = 0, int top = 0, int skip = 0, string orderby = "", string search = "", string filter = "", bool inlinecount = false)
		{
			if (filter.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger filter value"));
			if (search.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger search value"));
			if (orderby.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger orderby value"));

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<ManutenzioniDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var str = search.ToUpper().SqlQuote(true, true);
					string query;
					var total = 0L;
					if (inlinecount)
					{
						query = "SELECT COUNT(*) FROM manutenzioni";
						if (string.IsNullOrWhiteSpace(filter))
							query += $" WHERE mnt_dit = {ditta}";
						else
							query += $" WHERE mnt_dit = {ditta} AND ({filter})";

						if (!string.IsNullOrWhiteSpace(search))
						{
							query += $" AND (mnt_desc ILIKE {str} OR TRIM(CAST(mnt_codice AS VARCHAR(15))) ILIKE {str})";
						}
						cmd.CommandText = DbUtils.QueryAdapt(query);
						total = (long)cmd.ExecuteScalar();
					}

					query = "SELECT *, NULL AS next_data FROM manutenzioni";
					if (string.IsNullOrWhiteSpace(filter))
						query += $" WHERE mnt_dit = {ditta}";
					else
						query += $" WHERE mnt_dit = {ditta} AND ({filter})";

					if (!string.IsNullOrWhiteSpace(search))
					{
						query += $" AND (mnt_desc ILIKE {str} OR TRIM(CAST(mnt_codice AS VARCHAR(15))) ILIKE {str})";
					}

					if (string.IsNullOrWhiteSpace(orderby))
						query += " ORDER BY mnt_codice";
					else
						query += " ORDER BY " + orderby;
					cmd.CommandText = DbUtils.QueryAdapt(query, top, skip);

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var mnt = new ManutenzioniDb();
						DbUtils.SqlRead(ref reader, ref mnt);
						if (json.Data == null) json.Data = new List<ManutenzioniDb>();
						json.Data.Add(mnt);
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
		[Route("api/manutenzioni/get/{ditta}/{codice}")]
		public DefaultJson<ManutenzioniDb> Get(int ditta, int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var json = new DefaultJson<ManutenzioniDb>();
					var imb = new ManutenzioniDb();
					if (ManutenzioniDb.Search(ref cmd, ditta, codice, ref imb))
					{
						if (json.Data == null) json.Data = new List<ManutenzioniDb>();
						json.Data.Add(imb);
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
		[Route("api/manutenzioni/post")]
		public DefaultJson<ManutenzioniDb> Post([FromBody] DefaultJson<ManutenzioniDb> value)
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

					var json = new DefaultJson<ManutenzioniDb>();
					foreach (var mnt in value.Data)
					{
						object obj = null;
						var val = mnt;

						cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(mnt_codice),0) FROM manutenzioni WHERE mnt_dit = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = val.mnt_dit;

						val.mnt_codice = 1 + (int)cmd.ExecuteScalar();
						val.mnt_desc = val.mnt_desc.Trim();
						if (string.IsNullOrWhiteSpace(val.mnt_desc))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Descrizione vuota"));
							continue;
						}

						DbUtils.SqlWrite(ref cmd, ManutenzioniDb.Write, DbMessage.DB_INSERT, ref val, ref obj);
						if (json.Data == null) json.Data = new List<ManutenzioniDb>();
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

		[HttpGet]
		[Route("api/manutenzioni/exporttopdf/{ditta}/{codice}")]
		[Route("api/manutenzioni/exporttopdf/{ditta}/{codice}/{tipo}")]
		[Route("api/manutenzioni/exporttopdf/{ditta}/{codice}/{tipo}/{search}")]
		public DefaultJson<Reports> Print(int ditta, int codice, short tipo = -1, string search = "")
		{
			if (search.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger search value"));

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.GIORNALE_LAVORI_CANTIERI, EndpointsOperations.DELETE);

					var dit = new DitteDb();
					if (!DitteDb.Search(ref cmd, ditta, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

					var can = new CantieriDb();
					if (!CantieriDb.Search(ref cmd, ditta, codice, ref can)) throw new MCException(MCException.CantiereMsg, MCException.CantiereErr);
					connection.Close();

					var cry = new Crystal();
					cry.CryOpen("manutenzioni.rpt", "Manutenzioni");
					cry.CrySetDnsLessConnInfo(DbUtils.GetConnectionString(0, true), DbUtils.GetDatabase(), DbUtils.GetStartupOptions().User, DbUtils.GetStartupOptions().Password);
					cry.CrySetOrientation(PaperOrientation.Landscape);

					var select = $"{{manutenzioni.mnt_dit}} = {ditta} AND {{manutenzioni.mnt_mez}} = {codice}";
					if (tipo != -1) select += $" AND {{manutenzioni.mnt_tipo}} = {tipo}";
					if (!string.IsNullOrWhiteSpace(search)) select += $@" AND LooksLike(UpperCase({{manutenzioni.mnt_desc}}), UpperCase(""{search}*"")) = TRUE";
					cry.CrySelect(select);

					/*
					cry.CryStringParam("Ditta", "", dit.dit_desc);
					cry.CryStringParam("Cantiere", "", can.can_desc);
					*/
					cry.CrySort("{manutenzioni.mnt_data}");
					cry.CrySort("{manutenzioni.mnt_codice}");

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



		[HttpPut]
		[Route("api/manutenzioni/put/{ditta}/{codice}")]
		public DefaultJson<ManutenzioniDb> Put(int ditta, int codice, [FromBody]DefaultJson<ManutenzioniDb> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non corrispondente"));
			if (value.Data.Count != 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non valido"));


			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var mnt = value.Data[0];
					if (mnt.mnt_dit != ditta || mnt.mnt_codice != codice) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Id risorsa non corrisponde all'id dei dati"));
					mnt.mnt_desc = mnt.mnt_desc.Trim();
					if (string.IsNullOrWhiteSpace(mnt.mnt_desc)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Descrizione vuota"));

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					object obj = null;
					DbUtils.SqlWrite(ref cmd, ManutenzioniDb.Write, DbMessage.DB_UPDATE, ref mnt, ref obj);
	
					var json = new DefaultJson<ManutenzioniDb>();
					if (json.Data == null) json.Data = new List<ManutenzioniDb>();
					json.Data.Add(mnt);
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
		[Route("api/manutenzioni/delete/{ditta}/{codice}")]
		public void Delete(int ditta, int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var val = new ManutenzioniDb();
					if (!ManutenzioniDb.Search(ref cmd, ditta, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object objx = null;
					DbUtils.SqlWrite(ref cmd, ManutenzioniDb.Write, DbMessage.DB_DELETE, ref val, ref objx);

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
