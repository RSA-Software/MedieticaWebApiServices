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
using MedieticaWebApiService.ViewModel;

namespace MedieticaWebApiService.Controller
{
	[EnableCors("*", "*", "*")]

	public class MezziCantieriController : ApiController
	{
		//
		// status : 0 - tutti i mezzi    1 - Solo mezzi associati    - 2 - solo mezzi non associati
		//
		[Route("api/mezzicantieri/get")]
		public DefaultJson<MezziCantieri> GetList(int ditta = 0, int cantiere = 0, int status = 0, int top = 0, int skip = 0, string orderby = "", string search = "", string filter = "", bool inlinecount = false, bool joined = false )
		{
			if (filter.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger filter value"));
			if (search.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger search value"));
			if (orderby.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger orderby value"));

			if (ditta == 0 || cantiere == 0) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Ditta e Cantiere devono essere entrambi diversi da 0"));
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<MezziCantieri>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var str = search.ToUpper().SqlQuote(true, true);
					string query;
					var total = 0L;
					if (inlinecount)
					{
						if (status == 1)
							query = $@"
							SELECT COUNT(*) 
							FROM mezzi
							INNER JOIN mezcantieri ON (mez_dit = mec_dit AND mez_codice = mec_mez AND mec_can = {cantiere})
							WHERE mez_data_dis IS NULL";
						else if (status == 2)
						{
							query = $@"
							SELECT COUNT(*)
							FROM mezzi
							LEFT JOIN mezcantieri ON (mez_dit = mec_dit AND mez_codice = mec_mez AND mec_can = {cantiere})
							WHERE mec_mez IS NULL AND mez_data_dis IS NULL";
						}
						else query = @"SELECT COUNT(*) FROM mezzi WHERE mez_data_dis IS NULL";

						if (string.IsNullOrWhiteSpace(filter))
							query += $" AND mez_dit = {ditta}";
						else
							query += $" AND mez_dit = {ditta} AND ({filter})";

						if (!string.IsNullOrWhiteSpace(search))
						{
							query += $" AND (mez_desc ILIKE {str} OR TRIM(CAST(mez_codice AS VARCHAR(15))) ILIKE {str})";
						}

						cmd.CommandText = DbUtils.QueryAdapt(query);
						total = (long)cmd.ExecuteScalar();
					}

					if (status == 1)
					{
						query = $@"
						SELECT mez_dit, mez_codice, mez_desc, mez_serial, mez_targa, mez_telaio, 1 AS mez_enabled, mar_desc, tip_desc,
						(CASE
							WHEN imz.img_data IS NOT NULL THEN imz.img_data
						    ELSE imm.img_data
						END) AS img_data
						FROM mezzi
						INNER JOIN mezcantieri ON (mez_dit = mec_dit AND mez_codice = mec_mez AND mec_can = {cantiere})
						LEFT JOIN imgmezzi AS imz ON mez_dit = imz.img_dit AND mez_codice = imz.img_codice AND imz.img_formato = 1
						LEFT JOIN imgmodelli AS imm ON mez_dit_mod = imm.img_dit AND mez_mod = imm.img_codice AND imm.img_formato = 1
						LEFT JOIN modelli ON mez_dit = mod_dit AND mez_mod = mod_codice
						LEFT JOIN marchi ON mod_mar = mar_codice
						LEFT JOIN tipologie ON mod_tip = tip_codice
						WHERE mez_data_dis IS NULL";
					}
					else if (status == 2)
					{
						query = $@"
						SELECT mez_dit, mez_codice, mez_desc, mez_serial, mez_targa, mez_telaio, 0 AS mez_enabled, mar_desc, tip_desc,
						(CASE
							WHEN imz.img_data IS NOT NULL THEN imz.img_data
						    ELSE imm.img_data
						END) AS img_data
						FROM mezzi
						LEFT JOIN mezcantieri ON (mez_dit = mec_dit AND mez_codice = mec_mez AND mec_can = {cantiere})
						LEFT JOIN imgmezzi AS imz ON mez_dit = imz.img_dit AND mez_codice = imz.img_codice AND imz.img_formato = 1
						LEFT JOIN imgmodelli AS imm ON mez_dit_mod = imm.img_dit AND mez_mod = imm.img_codice AND imm.img_formato = 1
						LEFT JOIN modelli ON mez_dit = mod_dit AND mez_mod = mod_codice
						LEFT JOIN marchi ON mod_mar = mar_codice
						LEFT JOIN tipologie ON mod_tip = tip_codice
						WHERE mec_mez IS NULL AND mez_data_dis IS NULL";
					}
					else
					{
						query = $@"
						SELECT mez_dit, mez_codice, mez_desc, mez_serial, mez_targa, mez_telaio, mar_desc, tip_desc,
						(CASE
							WHEN imz.img_data IS NOT NULL THEN imz.img_data
						    ELSE imm.img_data
						END) AS img_data,
						(SELECT COUNT(*) FROM mezcantieri WHERE mec_dit = mezzi.mez_dit AND mec_can = {cantiere} AND mec_mez = mezzi.mez_codice)::INTEGER AS mez_enabled
						FROM mezzi
						LEFT JOIN imgmezzi AS imz ON mez_dit = imz.img_dit AND mez_codice = imz.img_codice AND imz.img_formato = 1
						LEFT JOIN imgmodelli AS imm ON mez_dit_mod = imm.img_dit AND mez_mod = imm.img_codice AND imm.img_formato = 1
						LEFT JOIN modelli ON mez_dit = mod_dit AND mez_mod = mod_codice
						LEFT JOIN marchi ON mod_mar = mar_codice
						LEFT JOIN tipologie ON mod_tip = tip_codice
						WHERE mez_data_dis IS NULL";
					}

					if (string.IsNullOrWhiteSpace(filter))
						query += $" AND mez_dit = {ditta}";
					else
						query += $" AND mez_dit = {ditta} AND ({filter})";

					if (!string.IsNullOrWhiteSpace(search))
					{
						query += $" AND (mez_desc ILIKE {str} OR TRIM(CAST(mez_codice AS VARCHAR(15))) ILIKE {str})";
					}

					if (string.IsNullOrWhiteSpace(orderby))
						query += " ORDER BY mez_codice";
					else
						query += " ORDER BY " + orderby;
					cmd.CommandText = DbUtils.QueryAdapt(query, top, skip);

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var mca = new MezziCantieri();
						DbUtils.SqlRead(ref reader, ref mca);
						if (json.Data == null) json.Data = new List<MezziCantieri>();
						json.Data.Add(mca);
						json.RecordsTotal++;
					}
					reader.Close();

					if (json.Data != null)
					{
						foreach (var mec in json.Data)
						{
							if (mec.can_list == null) mec.can_list = new List<string>();

							query = @"
							SELECT can_desc
							FROM mezcantieri
							LEFT JOIN cantieri ON (mec_dit = can_dit AND mec_can = can_codice)
							WHERE mec_dit = ? AND mec_mez = ?
							ORDER BY mec_dit, mec_can";
							cmd.CommandText = DbUtils.QueryAdapt(query);
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = mec.mez_dit;
							cmd.Parameters.Add("codmez", OdbcType.Int).Value = mec.mez_codice;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var desc = "";
								if (!reader.IsDBNull(reader.GetOrdinal("can_desc")))
									desc = reader.GetString(reader.GetOrdinal("can_desc")).Trim();
								mec.can_list.Add(desc);
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
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}

		[HttpPost]
		[Route("api/mezzicantieri/post")]
		public DefaultJson<MezziCantieri> Post([FromBody] DefaultJson<MezziCantieriDb> value)
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

					var json = new DefaultJson<MezziCantieri>();
					foreach (var mec in value.Data)
					{
						object obj = null;
						var val = mec;
						DbUtils.SqlWrite(ref cmd, MezziCantieriDb.Write, DbMessage.DB_INSERT, ref val, ref obj, true);

						var mez = new MezziDb();
						if (!MezziDb.Search(ref cmd, val.mec_dit, val.mec_mez, ref mez, true)) throw new MCException(MCException.NotFoundMsg, MCException.NotFoundErr);

						var xxx = new MezziCantieri();
						xxx.mez_dit = mez.mez_dit;
						xxx.mez_codice = mez.mez_codice;
						xxx.mez_desc = mez.mez_desc;
						xxx.mez_serial = mez.mez_serial;
						xxx.mez_targa = mez.mez_targa;
						xxx.mez_telaio = mez.mez_telaio;
						xxx.mez_enabled = 1;
						xxx.img_data = mez.img_data;

						if (json.Data == null) json.Data = new List<MezziCantieri>();
						json.Data.Add(xxx);
						json.RecordsTotal++;
					}
					if (json.Data != null)
					{
						foreach (var mec in json.Data)
						{
							if (mec.can_list == null) mec.can_list = new List<string>();

							var query = @"
							SELECT can_desc
							FROM mezcantieri
							LEFT JOIN cantieri ON (mec_dit = can_dit AND mec_can = can_codice)
							WHERE mec_dit = ? AND mec_mez = ?
							ORDER BY mec_dit, mec_can";
							cmd.CommandText = DbUtils.QueryAdapt(query);
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = mec.mez_dit;
							cmd.Parameters.Add("codmez", OdbcType.Int).Value = mec.mez_codice;
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var desc = "";
								if (!reader.IsDBNull(reader.GetOrdinal("can_desc")))
									desc = reader.GetString(reader.GetOrdinal("can_desc")).Trim();
								mec.can_list.Add(desc);
							}
							reader.Close();
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
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}
		
		[HttpDelete]
		[Route("api/mezzicantieri/delete/{ditta}/{cantiere}/{codice}")]
		public DefaultJson<MezziCantieri> Delete(int ditta, int cantiere, int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var val = new MezziCantieriDb();
					if (!MezziCantieriDb.Search(ref cmd, ditta, cantiere, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object objx = null;
					DbUtils.SqlWrite(ref cmd, MezziCantieriDb.Write, DbMessage.DB_DELETE, ref val, ref objx);
					
					var mez = new MezziDb();
					if (!MezziDb.Search(ref cmd, val.mec_dit, val.mec_mez, ref mez, true)) throw new MCException(MCException.NotFoundMsg, MCException.NotFoundErr);

					var json = new DefaultJson<MezziCantieri>();
					var xxx = new MezziCantieri();
					xxx.mez_dit = mez.mez_dit;
					xxx.mez_codice = mez.mez_codice;
					xxx.mez_desc = mez.mez_desc;
					xxx.mez_serial = mez.mez_serial;
					xxx.mez_targa = mez.mez_targa;
					xxx.mez_telaio = mez.mez_telaio;
					xxx.mez_enabled = 0;
					xxx.img_data = mez.img_data;

					if (json.Data == null) json.Data = new List<MezziCantieri>();
					json.Data.Add(xxx);
					json.RecordsTotal++;

					if (json.Data != null)
					{
						foreach (var mec in json.Data)
						{
							if (mec.can_list == null) mec.can_list = new List<string>();

							var query = @"
							SELECT can_desc
							FROM mezcantieri
							LEFT JOIN cantieri ON (mec_dit = can_dit AND mec_can = can_codice)
							WHERE mec_dit = ? AND mec_mez = ?
							ORDER BY mec_dit, mec_can";
							cmd.CommandText = DbUtils.QueryAdapt(query);
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = mec.mez_dit;
							cmd.Parameters.Add("codmez", OdbcType.Int).Value = mec.mez_codice;
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var desc = "";
								if (!reader.IsDBNull(reader.GetOrdinal("can_desc")))
									desc = reader.GetString(reader.GetOrdinal("can_desc")).Trim();
								mec.can_list.Add(desc);
							}
							reader.Close();
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
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}

	}
}
