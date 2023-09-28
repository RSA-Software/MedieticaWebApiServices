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

	public class ModelliController : ApiController
	{
		[HttpGet]
		[Route("api/modelli/blank/{ditta}")]
		public DefaultJson<ModelliDb> Blank(int ditta)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<ModelliDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(mod_codice),0) AS codice FROM modelli WHERE mod_dit = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = ditta;
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var mod = new ModelliDb();
						mod.mod_dit = ditta;
						mod.mod_codice = 1 + reader.GetInt32(reader.GetOrdinal("codice"));
						if (json.Data == null) json.Data = new List<ModelliDb>();
						json.Data.Add(mod);
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


		[Route("api/modelli/get")]
		public DefaultJson<ModelliDb> GetList(int ditta = 0, int top = 0, int skip = 0, string orderby = "", string search = "", string filter = "", bool inlinecount = false, bool joined = false )
		{
			if (filter.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger filter value"));
			if (search.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger search value"));
			if (orderby.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger orderby value"));

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<ModelliDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					string base_filter;
					if (ditta == 1)
						base_filter = "mod_dit = 1";
					else
						base_filter = $"mod_dit IN (1,{ditta})";


					var str = search.ToUpper().SqlQuote(true, true);
					string query;
					var total = 0L;
					if (inlinecount)
					{
						query = "SELECT COUNT(*) FROM modelli";
						if (string.IsNullOrWhiteSpace(filter))
							query += $" WHERE {base_filter}";
						else
							query += $" WHERE {base_filter} AND ({filter})";

						if (!string.IsNullOrWhiteSpace(search))
						{
							query += $" AND (mod_desc ILIKE {str} OR TRIM(CAST(mod_codice AS VARCHAR(15))) ILIKE {str})";
						}

						cmd.CommandText = DbUtils.QueryAdapt(query);
						total = (long)cmd.ExecuteScalar();
					}

					if (joined)
						query = ModelliDb.GetJoinQuery();
					else
						query = "SELECT * FROM modelli";
					if (string.IsNullOrWhiteSpace(filter))
						query += $" WHERE {base_filter}";
					else
						query += $" WHERE {base_filter} AND ({filter})";

					if (!string.IsNullOrWhiteSpace(search))
					{
						query += $" AND (mod_desc ILIKE {str} OR TRIM(CAST(mod_codice AS VARCHAR(15))) ILIKE {str})";
					}

					if (string.IsNullOrWhiteSpace(orderby))
						query += " ORDER BY mod_dit DESC, mod_codice";
					else
						query += " ORDER BY " + orderby;
					cmd.CommandText = DbUtils.QueryAdapt(query, top, skip);

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var mod = new ModelliDb();
						DbUtils.SqlRead(ref reader, ref mod, joined ? null : ModelliDb.GetJoinExcludeFields());
						if (json.Data == null) json.Data = new List<ModelliDb>();
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
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}

		[HttpGet]
		[Route("api/modelli/get/{ditta}/{codice}")]
		[Route("api/modelli/get/{ditta}/{codice}/{joined}")]
		[Route("api/modelli/get/{ditta}/{codice}/{joined}/{fulljoined}")]
		public DefaultJson<ModelliDb> Get(int ditta, int codice, bool joined = false, bool fulljoined = false)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var json = new DefaultJson<ModelliDb>();
					var mod = new ModelliDb();
					if (ModelliDb.Search(ref cmd, ditta, codice, ref mod, joined || fulljoined))
					{
						mod.img_list = new List<ImgModelliDb>();
						if (json.Data == null) json.Data = new List<ModelliDb>();
						if (fulljoined)
						{
							DbUtils.GetTokenLevel(Request);

							cmd.CommandText = @"
							SELECT * 
							FROM imgmodelli 
							WHERE img_dit = ? AND img_codice = ? AND Mod(img_formato, 2) <> 0
							ORDER BY img_formato
							";

							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = mod.mod_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = mod.mod_codice;
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var img = new ImgModelliDb();
								DbUtils.SqlRead(ref reader, ref img);
								mod.img_list.Add(img);
							}
							reader.Close();
						}
						json.Data.Add(mod);
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
		[Route("api/modelli/post")]
		public DefaultJson<ModelliDb> Post([FromBody] DefaultJson<ModelliDb> value)
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

					var json = new DefaultJson<ModelliDb>();
					foreach (var mod in value.Data)
					{
						object obj = null;
						var val = mod;

						//
						// Impostiamo la ditta corretta per la gestione dei rivenditori
						//
						val.mod_verificato = true;
						if (val.mod_dit != 1)
						{
							var dit = new DitteDb();
							if (!DitteDb.Search(ref cmd, val.mod_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);
							if (dit.dit_reseller)
							{
								val.mod_dit = 1;
								val.mod_verificato = false;
							}
						}

						cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(mod_codice),0) FROM modelli WHERE mod_dit = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = val.mod_dit;

						val.mod_codice  = 1 + (int)cmd.ExecuteScalar();
						val.mod_desc = val.mod_desc.Trim();
						if (string.IsNullOrWhiteSpace(val.mod_desc))
						{
							if (value.Data.Count == 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Descrizione vuota"));
							continue;
						}


						val.mod_user = DbUtils.GetTokenUser(Request);
						DbUtils.SqlWrite(ref cmd, ModelliDb.Write, DbMessage.DB_INSERT, ref val, ref obj, true);
						if (json.Data == null) json.Data = new List<ModelliDb>();
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

		[HttpPut]
		[Route("api/modelli/put/{ditta}/{codice}")]
		public DefaultJson<ModelliDb> Put(int ditta, int codice, [FromBody]DefaultJson<ModelliDb> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non corrispondente"));
			if (value.Data.Count != 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non valido"));

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var mod = value.Data[0];
					if (mod.mod_dit != ditta || mod.mod_codice != codice) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Id risorsa non corrisponde all'id dei dati"));
					mod.mod_desc = mod.mod_desc.Trim();
					if (string.IsNullOrWhiteSpace(mod.mod_desc)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Descrizione vuota"));

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					object obj = null;
					DbUtils.SqlWrite(ref cmd, ModelliDb.Write, DbMessage.DB_UPDATE, ref mod, ref obj, true);
	
					var json = new DefaultJson<ModelliDb>();
					if (json.Data == null) json.Data = new List<ModelliDb>();
					json.Data.Add(mod);
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
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			
		}

		[HttpDelete]
		[Route("api/modelli/delete/{ditta}/{codice}")]
		public void Delete(int ditta, int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var val = new ModelliDb();
					if (!ModelliDb.Search(ref cmd, ditta, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object objx = null;
					DbUtils.SqlWrite(ref cmd, ModelliDb.Write, DbMessage.DB_DELETE, ref val, ref objx);

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
