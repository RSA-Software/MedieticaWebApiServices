using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using MedieticaWebApiService.Helpers;
using MedieticaWebApiService.Models;

namespace MedieticaWebApiService.Controller
{ 
	[EnableCors("*", "*", "*")]
	public class ArtAnagController : ApiController
	{
		[HttpGet]
		[Route("api/artanag")]
		public DefaultJson<ArtAnagDb> Get(string codice = "", bool joined = false)
		{
			var json = new DefaultJson<ArtAnagDb>();
			try
			{
				if (string.IsNullOrWhiteSpace(codice))
				{
					using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
					{
						connection.Open();
						var cmd = new OdbcCommand { Connection = connection };
						if (joined)
							cmd.CommandText = DbUtils.QueryAdapt(ArtAnagDb.GetJoinQuery() + "WHERE ana_codice != ''");
						else
							cmd.CommandText = DbUtils.QueryAdapt("SELECT * FROM artanag WHERE ana_codice != ''");
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var ana = new ArtAnagDb();
							DbUtils.SqlRead(ref reader, ref ana);
							if (json.Data == null) json.Data = new List<ArtAnagDb>();
							json.Data.Add(ana);
							json.RecordsTotal++;
						}
						reader.Close();
						connection.Close();
					}
				}
				else
				{
					using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
					{
						connection.Open();
						var cmd = new OdbcCommand { Connection = connection };
					
						var ana = new ArtAnagDb();
						if (ArtAnagDb.Search(ref cmd, codice, ref ana, joined))
						{
							if (json.Data == null) json.Data = new List<ArtAnagDb>();
							json.Data.Add(ana);
							json.RecordsTotal++;
						}

						connection.Close();
					}
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
			if (json.RecordsTotal == 0)
			{
				throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Record non trovato : " + codice));
			}
			return (json);
		}

		[HttpPost]
		[Route("api/artanag")]
		public DefaultJson<ArtAnagDb> Post([FromBody] DefaultJson<ArtAnagDb> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non corrispondente"));
			var json = new DefaultJson<ArtAnagDb>();

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					foreach (var ana in value.Data)
					{
						object obj = null;
						var val = ana;

						val.ana_codice = val.ana_codice.Trim().ToUpper();
						val.ana_desc = val.ana_desc.Trim();
						if (string.IsNullOrWhiteSpace(val.ana_desc))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Descrizione vuota"));
							continue;
						}
						DbUtils.SqlWrite(ref cmd, ArtAnagDb.Write, DbMessage.DB_INSERT, ref val, ref obj, true);
						if (json.Data == null) json.Data = new List<ArtAnagDb>();
						json.Data.Add(ana);
						json.RecordsTotal++;
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
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			return (json);
		}

		[HttpPut]
		[Route("api/artanag")]
		public DefaultJson<ArtAnagDb> Put(string codice, [FromBody]DefaultJson<ArtAnagDb> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non corrispondente"));
			if (value.Data.Count != 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non valido"));
			var json = new DefaultJson<ArtAnagDb>();

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var ana = value.Data[0];
					if (string.IsNullOrWhiteSpace(codice) || string.Compare(ana.ana_codice, codice, StringComparison.CurrentCulture) != 0)
						throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Id risorsa non corrisponde all'id dei dati"));

					ana.ana_desc = ana.ana_desc.Trim();
					if (string.IsNullOrWhiteSpace(ana.ana_desc)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Descrizione vuota"));

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					object obj = null;
					DbUtils.SqlWrite(ref cmd, ArtAnagDb.Write, DbMessage.DB_UPDATE, ref ana, ref obj, true);
					if (json.Data == null) json.Data = new List<ArtAnagDb>();
					json.Data.Add(ana);
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

		[HttpDelete]
		[Route("api/artanag")]
		public void Delete(string codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var val = new ArtAnagDb();
					if (string.IsNullOrWhiteSpace(codice) || !ArtAnagDb.Search(ref cmd, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object objx = null;
					DbUtils.SqlWrite(ref cmd, ArtAnagDb.Write, DbMessage.DB_DELETE, ref val, ref objx);

					connection.Close();
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
