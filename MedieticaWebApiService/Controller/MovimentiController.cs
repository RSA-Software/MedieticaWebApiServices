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
	public class MovimentiController : ApiController
	{
		[HttpGet]
		[Route("api/movimenti")]
		public DefaultJson<MovimentiDb> Get(int codice = 0)
		{
			var json = new DefaultJson<MovimentiDb>();
			try
			{
				if (codice == 0)
				{
					using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
					{
						connection.Open();
						var cmd = new OdbcCommand { Connection = connection };
						cmd.CommandText = DbUtils.QueryAdapt(MovimentiDb.GetJoinQuery() + "WHERE mov_codice != ''");

						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var mov = new MovimentiDb();
							DbUtils.SqlRead(ref reader, ref mov);
							if (json.Data == null) json.Data = new List<MovimentiDb>();
							json.Data.Add(mov);
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

						var mov = new MovimentiDb();
						if (MovimentiDb.Search(ref cmd, codice, ref mov, true))
						{
							if (json.Data == null) json.Data = new List<MovimentiDb>();
							json.Data.Add(mov);
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
		[Route("api/movimenti")]
		public DefaultJson<MovimentiDb> CaricoProdotti([FromBody] DefaultJson<MovimentiDb> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Numero di record non corrispondente"));
			var json = new DefaultJson<MovimentiDb>();
			var mov_list = new List<MovimentiDb>();

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(mov_codice),0) FROM movimenti");
					var last = (int)cmd.ExecuteScalar();
					var utente = 0;
					foreach (var mov in value.Data)
					{
						var val = mov;

						val.mov_codice = 1 + last;
						val.mov_ana = val.mov_ana.Trim().ToUpper();
						if (utente == 0) utente = val.mov_ute;
						if (string.IsNullOrWhiteSpace(val.mov_ana))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Codice Articolo vuoto"));
							continue;
						}
						if (val.mov_ute == 0)
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Codice Utente vuoto"));
							continue;
						}
						if (utente != val.mov_ute) 
							throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il Codice Utente non può essere diverso all'interno di una transazione"));

						if (val.mov_dis == 0)
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Codice Distributore vuoto"));
							continue;
						}

						mov_list.Add(mov);
					}

					object obj = null;

					DbUtils.SqlWrite(ref cmd, MovimentiDb.WritePayment, DbMessage.DB_PAYMENT, ref mov_list, ref obj, true);

					if (json.Data == null) json.Data = new List<MovimentiDb>();
					json.Data = mov_list;
					json.RecordsTotal = mov_list.Count;
					
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
		[Route("api/movimenti")]
		public DefaultJson<MovimentiDb> Put(int codice, [FromBody]DefaultJson<MovimentiDb> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non corrispondente"));
			if (value.Data.Count != 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non valido"));
			var json = new DefaultJson<MovimentiDb>();

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					
					var mov = value.Data[0];
					if (codice <= 0) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Codice non valido"));
					if (mov.mov_codice != codice) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Id risorsa non corrisponde all'id dei dati"));

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					object obj = null;
					DbUtils.SqlWrite(ref cmd, MovimentiDb.Write, DbMessage.DB_UPDATE, ref mov, ref obj, true);
	
					if (json.Data == null) json.Data = new List<MovimentiDb>();
					json.Data.Add(mov);
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
		[Route("api/movimenti")]
		public void Delete(int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var val = new MovimentiDb();
					if (codice == 0 || !MovimentiDb.Search(ref cmd, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object objx = null;
					DbUtils.SqlWrite(ref cmd, MovimentiDb.Write, DbMessage.DB_DELETE, ref val, ref objx);

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
