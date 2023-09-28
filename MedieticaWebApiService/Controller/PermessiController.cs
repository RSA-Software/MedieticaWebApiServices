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

	public class PermessiController : ApiController
	{

		[HttpGet]
		[Route("api/permessi/get/{codgru}")]
		public DefaultJson<Permessi> Get(int codgru)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))

				{
					var json = new DefaultJson<Permessi>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					UtentiGruppiDb usg = null;
					if (!UtentiGruppiDb.Search(ref cmd, codgru, ref usg)) return (json);

					var str = "SELECT * FROM endpoints ORDER BY end_codice";
					cmd.CommandText = DbUtils.QueryAdapt(str);
					cmd.Parameters.Clear();
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var end = new EndpointsDb();
						var per = new Permessi();
						DbUtils.SqlRead(ref reader, ref end);
						per.per_end = end.end_codice;
						per.end_desc = end.end_desc;
						if (json.Data == null) json.Data = new List<Permessi>();
						json.Data.Add(per);
						json.RecordsTotal++;
					}
					reader.Close();

					if (json.Data != null)
					{
						str = "SELECT * FROM permessi WHERE per_usg = ? ORDER BY per_end";
						cmd.CommandText = DbUtils.QueryAdapt(str);
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codgru", OdbcType.Int).Value = codgru;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var per = new PermessiDb();
							DbUtils.SqlRead(ref reader, ref per);

							foreach (var pxx in json.Data)
							{
								if (pxx.per_end == per.per_end)
								{
									pxx.per_view = per.per_view;
									pxx.per_add = per.per_add;
									pxx.per_update = per.per_update;
									pxx.per_delete = per.per_delete;
									pxx.per_special1 = per.per_special1;
									pxx.per_special2 = per.per_special2;
									pxx.per_special3 = per.per_special3;
									break;
								}
							}
						}
						reader.Close();
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
		public DefaultJson<PermessiDb> Post([FromBody] DefaultJson<PermessiDb> value)
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

					var json = new DefaultJson<PermessiDb>();
					foreach (var per in value.Data)
					{
						object obj = null;
						var val = per;

						DbUtils.SqlWrite(ref cmd, PermessiDb.Write, DbMessage.DB_INSERT, ref val, ref obj);

						if (json.Data == null) json.Data = new List<PermessiDb>();
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
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}

	
		[HttpDelete]
		[Route("api/permessi/delete/{codgru}/{codend}")]
		public void Delete(int codgru, int codend)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var val = new PermessiDb();
					if (!PermessiDb.Search(ref cmd, codgru, codend, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object objx = null;
					DbUtils.SqlWrite(ref cmd, PermessiDb.Write, DbMessage.DB_DELETE, ref val, ref objx);
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
