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

	public class CatMercController : ApiController
	{
		[HttpGet]
		[Route("api/gruppi")]
		public DefaultJson<CatMercDb> Get(int codice = 0)
		{
			var json = new DefaultJson<CatMercDb>();
			try
			{
				if (codice == 0)
				{
					using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
					{
						connection.Open();
						var cmd = new OdbcCommand { Connection = connection };
						cmd.CommandText = DbUtils.QueryAdapt("SELECT * FROM gruppi WHERE mer_codice > 0");

						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var mer = new CatMercDb();
							DbUtils.Initialize(ref mer);
							DbUtils.SqlRead(ref reader, ref mer);
							if (json.Data == null) json.Data = new List<CatMercDb>();
							json.Data.Add(mer);
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

						var mer = new CatMercDb();
						if (CatMercDb.Search(ref cmd, codice, ref mer))
						{
							if (json.Data == null) json.Data = new List<CatMercDb>();
							json.Data.Add(mer);
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
		[Route("api/gruppi")]
		public DefaultJson<CatMercDb> Post([FromBody] DefaultJson<CatMercDb> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non corrispondente"));
			var json = new DefaultJson<CatMercDb>();

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(mer_codice),0) FROM gruppi");
					var last = (int)cmd.ExecuteScalar();

					foreach (var mer in value.Data)
					{
						object obj = null;
						var val = mer;

						val.mer_codice = 1 + last;
						val.mer_desc = val.mer_desc.ToUpper().Trim();

						if (string.IsNullOrWhiteSpace(val.mer_desc))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Descrizione vuota"));
							continue;
						}

						DbUtils.SqlWrite(ref cmd, CatMercDb.Write, DbMessage.DB_INSERT, ref val, ref obj);
						last = val.mer_codice;

						if (!CatMercDb.Search(ref cmd, val.mer_codice, ref val)) continue;
						if (json.Data == null) json.Data = new List<CatMercDb>();
						json.Data.Add(val);
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
		[Route("api/gruppi")]
		public DefaultJson<CatMercDb> Put(int codice, [FromBody]DefaultJson<CatMercDb> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non corrispondente"));
			if (value.Data.Count != 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non valido"));
			var json = new DefaultJson<CatMercDb>();

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var mer = value.Data[0];
					if (mer.mer_codice != codice) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Id risorsa non corrisponde all'id dei dati"));
					mer.mer_desc = mer.mer_desc.ToUpper().Trim();
					if (string.IsNullOrWhiteSpace(mer.mer_desc)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Descrizione vuota"));

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					object obj = null;
					DbUtils.SqlWrite(ref cmd, CatMercDb.Write, DbMessage.DB_UPDATE, ref mer, ref obj, false);

					if (!CatMercDb.Search(ref cmd, mer.mer_codice, ref mer)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));
					if (json.Data == null) json.Data = new List<CatMercDb>();
					json.Data.Add(mer);
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
		[Route("api/gruppi")]
		public void Delete(int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					
					var val = new CatMercDb();
					if (!CatMercDb.Search(ref cmd, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));
	
					object objx = null;
					DbUtils.SqlWrite(ref cmd, CatMercDb.Write, DbMessage.DB_DELETE, ref val, ref objx);
					
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
		}

	}
}
