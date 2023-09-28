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

	public class DipendentiCantieriController : ApiController
	{
		//
		// status : 0 - tutti i dipendenti    1 - Solo dipendenti associati    - 2 - solo dipendenti non associati
		//
		[Route("api/dipendenticantieri/get")]
		public DefaultJson<DipendentiCantieri> GetList(int ditta = 0, int cantiere = 0, int status = 0, int top = 0, int skip = 0, string orderby = "", string search = "", string filter = "", bool inlinecount = false, bool joined = false )
		{
			if (filter.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger filter value"));
			if (search.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger search value"));
			if (orderby.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger orderby value"));

			if (ditta == 0 || cantiere == 0) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Ditta e Cantiere devono essere entrambi diversi da 0"));
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<DipendentiCantieri>();

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
							FROM dipendenti
							INNER JOIN dipcantieri ON (dip_dit = dic_dit AND dip_codice = dic_dip AND dic_can = {cantiere})
							WHERE dip_data_fine_rapporto IS NULL";
						else if (status == 2)
						{
							query = $@"
							SELECT COUNT(*)
							FROM dipendenti
							LEFT JOIN dipcantieri ON (dip_dit = dic_dit AND dip_codice = dic_dip AND dic_can = {cantiere})
							WHERE dic_dip IS NULL AND dip_data_fine_rapporto IS NULL";
						}
						else query = @"SELECT COUNT(*) FROM dipendenti WHERE dip_data_fine_rapporto IS NULL";

						if (string.IsNullOrWhiteSpace(filter))
							query += $" AND dip_dit = {ditta}";
						else
							query += $" AND dip_dit = {ditta} AND ({filter})";

						if (!string.IsNullOrWhiteSpace(search))
						{
							query += $" AND (dip_desc ILIKE {str} OR TRIM(CAST(dip_codice AS VARCHAR(15))) ILIKE {str})";
						}

						cmd.CommandText = DbUtils.QueryAdapt(query);
						total = (long)cmd.ExecuteScalar();
					}

					if (status == 1)
					{
						query = $@"
						SELECT dip_dit, dip_codice, dip_cognome, dip_nome, dip_desc, dip_codfis, img_data, 1 AS dip_enabled, NULL AS can_list
						FROM dipendenti
						INNER JOIN dipcantieri ON (dip_dit = dic_dit AND dip_codice = dic_dip AND dic_can = {cantiere})
						LEFT JOIN imgdipendenti ON dip_dit = img_dit AND dip_codice = img_codice AND img_formato = 1
						WHERE dip_data_fine_rapporto IS NULL";
					}
					else if (status == 2)
					{
						query = $@"
						SELECT dip_dit, dip_codice, dip_cognome, dip_nome, dip_desc, dip_codfis, img_data, 0 AS dip_enabled, NULL AS can_list
						FROM dipendenti
						LEFT JOIN dipcantieri ON (dip_dit = dic_dit AND dip_codice = dic_dip AND dic_can = {cantiere})
						LEFT JOIN imgdipendenti ON dip_dit = img_dit AND dip_codice = img_codice AND img_formato = 1
						WHERE dic_dip IS NULL AND dip_data_fine_rapporto IS NULL";
					}
					else
					{
						query = $@"
						SELECT dip_dit, dip_codice, dip_cognome, dip_nome, dip_desc, dip_codfis, img_data,
						(SELECT COUNT(*) FROM dipcantieri WHERE dic_dit = dipendenti.dip_dit AND dic_can = {cantiere} AND dic_dip = dipendenti.dip_codice)::INTEGER AS dip_enabled, NULL AS can_list
						FROM dipendenti
						LEFT JOIN imgdipendenti ON dip_dit = img_dit AND dip_codice = img_codice AND img_formato = 1
						WHERE dip_data_fine_rapporto IS NULL";
					}

					if (string.IsNullOrWhiteSpace(filter))
						query += $" AND dip_dit = {ditta}";
					else
						query += $" AND dip_dit = {ditta} AND ({filter})";

					if (!string.IsNullOrWhiteSpace(search))
					{
						query += $" AND (dip_desc ILIKE {str} OR TRIM(CAST(dip_codice AS VARCHAR(15))) ILIKE {str})";
					}

					if (string.IsNullOrWhiteSpace(orderby))
						query += " ORDER BY dip_codice";
					else
						query += " ORDER BY " + orderby;
					cmd.CommandText = DbUtils.QueryAdapt(query, top, skip);

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var dip = new DipendentiCantieri();
						DbUtils.SqlRead(ref reader, ref dip);
						if (json.Data == null) json.Data = new List<DipendentiCantieri>();
						json.Data.Add(dip);
						json.RecordsTotal++;
					}
					reader.Close();

					if (json.Data != null)
					{
						foreach (var dip in json.Data)
						{
							if (dip.can_list == null) dip.can_list = new List<string>();

							query = @"
							SELECT can_desc
							FROM dipcantieri
							LEFT JOIN cantieri ON (dic_dit = can_dit AND dic_can = can_codice)
							WHERE dic_dit = ? AND dic_dip = ?
							ORDER BY dic_dit, dic_can";
							cmd.CommandText = DbUtils.QueryAdapt(query);
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
							cmd.Parameters.Add("coddip", OdbcType.Int).Value = dip.dip_codice;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var desc = "";
								if (!reader.IsDBNull(reader.GetOrdinal("can_desc")))
									desc = reader.GetString(reader.GetOrdinal("can_desc")).Trim();
								dip.can_list.Add(desc);
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
		[Route("api/dipendenticantieri/post")]
		public DefaultJson<DipendentiCantieri> Post([FromBody] DefaultJson<DipendentiCantieriDb> value)
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

					var json = new DefaultJson<DipendentiCantieri>();
					foreach (var dic in value.Data)
					{
						object obj = null;
						var val = dic;
						DbUtils.SqlWrite(ref cmd, DipendentiCantieriDb.Write, DbMessage.DB_INSERT, ref val, ref obj, true);

						var dip = new DipendentiDb();
						if (!DipendentiDb.Search(ref cmd, val.dic_dit, val.dic_dip, ref dip, true)) throw new MCException(MCException.NotFoundMsg, MCException.NotFoundErr);

						var xxx = new DipendentiCantieri();
						xxx.dip_dit = dip.dip_dit;
						xxx.dip_codice = dip.dip_codice;
						xxx.dip_cognome = dip.dip_cognome;
						xxx.dip_nome = dip.dip_nome;
						xxx.dip_desc = dip.dip_desc;
						xxx.dip_codfis = dip.dip_codfis;
						xxx.dip_enabled = 1;
						xxx.img_data = dip.img_data;

						if (json.Data == null) json.Data = new List<DipendentiCantieri>();
						json.Data.Add(xxx);
						json.RecordsTotal++;
					}

					if (json.Data != null)
					{
						foreach (var dip in json.Data)
						{
							if (dip.can_list == null) dip.can_list = new List<string>();

							var query = @"
							SELECT can_desc
							FROM dipcantieri
							LEFT JOIN cantieri ON (dic_dit = can_dit AND dic_can = can_codice)
							WHERE dic_dit = ? AND dic_dip = ?
							ORDER BY dic_dit, dic_can";
							cmd.CommandText = DbUtils.QueryAdapt(query);
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
							cmd.Parameters.Add("coddip", OdbcType.Int).Value = dip.dip_codice;
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var desc = "";
								if (!reader.IsDBNull(reader.GetOrdinal("can_desc")))
									desc = reader.GetString(reader.GetOrdinal("can_desc")).Trim();
								dip.can_list.Add(desc);
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
		[Route("api/dipendenticantieri/delete/{ditta}/{cantiere}/{codice}")]
		public DefaultJson<DipendentiCantieri> Delete(int ditta, int cantiere, int codice)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var val = new DipendentiCantieriDb();
					if (!DipendentiCantieriDb.Search(ref cmd, ditta, cantiere, codice, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object objx = null;
					DbUtils.SqlWrite(ref cmd, DipendentiCantieriDb.Write, DbMessage.DB_DELETE, ref val, ref objx);

					
					var dip = new DipendentiDb();
					if (!DipendentiDb.Search(ref cmd, val.dic_dit, val.dic_dip, ref dip, true)) throw new MCException(MCException.NotFoundMsg, MCException.NotFoundErr);

					var json = new DefaultJson<DipendentiCantieri>();
					var xxx = new DipendentiCantieri();
					xxx.dip_dit = dip.dip_dit;
					xxx.dip_codice = dip.dip_codice;
					xxx.dip_cognome = dip.dip_cognome;
					xxx.dip_nome = dip.dip_nome;
					xxx.dip_desc = dip.dip_desc;
					xxx.dip_codfis = dip.dip_codfis;
					xxx.dip_enabled = 0;
					xxx.img_data = dip.img_data;

					if (json.Data == null) json.Data = new List<DipendentiCantieri>();
					json.Data.Add(xxx);
					json.RecordsTotal++;

					if (json.Data != null)
					{
						foreach (var dix in json.Data)
						{
							if (dix.can_list == null) dix.can_list = new List<string>();

							var query = @"
							SELECT can_desc
							FROM dipcantieri
							LEFT JOIN cantieri ON (dic_dit = can_dit AND dic_can = can_codice)
							WHERE dic_dit = ? AND dic_dip = ?
							ORDER BY dic_dit, dic_can";
							cmd.CommandText = DbUtils.QueryAdapt(query);
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = dix.dip_dit;
							cmd.Parameters.Add("coddip", OdbcType.Int).Value = dix.dip_codice;
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var desc = "";
								if (!reader.IsDBNull(reader.GetOrdinal("can_desc")))
									desc = reader.GetString(reader.GetOrdinal("can_desc")).Trim();
								dix.can_list.Add(desc);
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
