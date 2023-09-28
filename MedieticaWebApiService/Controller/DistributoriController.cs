using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using MedieticaWebApiService.Helpers;
using MedieticaWebApiService.Models;
using MedieticaWebApiService.ViewModel;

namespace MedieticaWebApiService.Controller
{
	[EnableCors("*", "*", "*")]

	public class DistributoriController : ApiController
	{
		[HttpGet]
		[Route("api/distributori")]
		public DefaultJson<DistributoriDb> Get(int codice = 0)
		{
			var json = new DefaultJson<DistributoriDb>();
			try
			{
				if (codice == 0)
				{
					using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
					{
						connection.Open();
						var cmd = new OdbcCommand { Connection = connection };
						cmd.CommandText = DbUtils.QueryAdapt("SELECT * FROM distributori WHERE dis_codice > 0");
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var dis = new DistributoriDb();
							DbUtils.Initialize(ref dis);
							DbUtils.SqlRead(ref reader, ref dis);
							if (json.Data == null) json.Data = new List<DistributoriDb>();
							json.Data.Add(dis);
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

						var dis = new DistributoriDb();
						if (DistributoriDb.Search(ref cmd, codice, ref dis))
						{
							if (json.Data == null) json.Data = new List<DistributoriDb>();
							json.Data.Add(dis);
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

		[HttpGet]
		[Route("api/distributori/articoli")]
		public DefaultJson<Distributori> GetJoinedById(int codice)
		{
			if (codice <= 0) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Codice id non corretto."));
			var json = new DefaultJson<Distributori>();
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var dis = new DistributoriDb();
					if (DistributoriDb.Search(ref cmd, codice, ref dis))
					{
						var dix = new Distributori();
						dix.dis = dis;
						cmd.CommandText = DbUtils.QueryAdapt(DistributoriDb.GetJoinQuery() + " WHERE aco_dis = ? ORDER BY ana_desc");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("@codice", OdbcType.Int).Value = codice;
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var dsa = new DistributoriArt();
							DbUtils.SqlRead(ref reader, ref dsa);
							if (dix.articoli == null) dix.articoli = new List<DistributoriArt>();
							dix.articoli.Add(dsa);
						}
						reader.Close();
						connection.Close();

						if (json.Data == null) json.Data = new List<Distributori>();
						json.Data.Add(dix);
						json.RecordsTotal++;
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
		[Route("api/distributori")]

		public DefaultJson<DistributoriDb> Post([FromBody] DefaultJson<DistributoriDb> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Numero di record non corrispondente"));
			var json = new DefaultJson<DistributoriDb>();

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(dis_codice),0) FROM distributori");
					var last = (int)cmd.ExecuteScalar();

					foreach (var dis in value.Data)
					{
						object obj = null;
						var val = dis;

						val.dis_codice = 1 + last;
						val.dis_desc = val.dis_desc.ToUpper().Trim();

						if (string.IsNullOrWhiteSpace(val.dis_desc))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Descrizione vuota"));
							continue;
						}

						DbUtils.SqlWrite(ref cmd, DistributoriDb.Write, DbMessage.DB_INSERT, ref val, ref obj);
						last = val.dis_codice;

						if (!DistributoriDb.Search(ref cmd, val.dis_codice, ref val)) continue;
						if (json.Data == null) json.Data = new List<DistributoriDb>();
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
		[Route("api/distributori")]
		public DefaultJson<DistributoriDb> Put(int codice, [FromBody]DefaultJson<DistributoriDb> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non corrispondente"));
			if (value.Data.Count != 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non valido"));
			var json = new DefaultJson<DistributoriDb>();

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var dis = value.Data[0];
					if (dis.dis_codice != codice) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Id risorsa non corrisponde all'id dei dati"));
					dis.dis_desc = dis.dis_desc.ToUpper().Trim();

					if (string.IsNullOrWhiteSpace(dis.dis_desc)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Descrizione vuota"));

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					object obj = null;
					DbUtils.SqlWrite(ref cmd, DistributoriDb.Write, DbMessage.DB_UPDATE, ref dis, ref obj);

					if (!DistributoriDb.Search(ref cmd, dis.dis_codice, ref dis)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));
					if (json.Data == null) json.Data = new List<DistributoriDb>();
					json.Data.Add(dis);
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
		[Route("api/distributori")]
		public void Delete(int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var val = new DistributoriDb();
					if (codice == 0 || !DistributoriDb.Search(ref cmd, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object objx = null;
					DbUtils.SqlWrite(ref cmd, DistributoriDb.Write, DbMessage.DB_DELETE, ref val, ref objx);

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
