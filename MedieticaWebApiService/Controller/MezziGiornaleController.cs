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

	public class MezziGiornaleController : ApiController
	{

		[Route("api/mezzigiornale/get")]
		public DefaultJson<MezziDb> GetList(int ditta = 0, int cantiere = 0, int top = 0, int skip = 0, string orderby = "", string search = "", string filter = "", bool inlinecount = false, bool joined = false )
		{
			if (filter.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger filter value"));
			if (search.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger search value"));
			if (orderby.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger orderby value"));

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<MezziDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var str = search.ToUpper().SqlQuote(true, true);
					string query;
					var total = 0L;
					if (inlinecount)
					{
						query = $@"
						SELECT COUNT(*)
						FROM mezcantieri
						INNER JOIN mezzi ON mec_dit = mez_dit AND mec_mez = mez_codice
						INNER JOIN (
	 						SELECT can_dit, can_codice
							FROM cantieri
							WHERE can_dit = {ditta}
							AND can_codice = {cantiere}
							UNION
							SELECT sub_dit_sub AS can_dit, sub_can_sub AS can_codice
							FROM subappalti
							WHERE sub_dit_app = {ditta}
							AND sub_can_app = {cantiere}
						) AS q ON mec_dit = q.can_dit AND mec_can = q.can_codice
						";
						if (string.IsNullOrWhiteSpace(filter))
							query += " WHERE mez_dit > 0";
						else
							query += $" WHERE mez_dit > 0 AND ({filter})";

						if (!string.IsNullOrWhiteSpace(search))
						{
							query += $" AND (mez_desc ILIKE {str} OR mez_serial ILIKE {str} OR mez_targa ILIKE {str} OR mez_telaio ILIKE {str} OR TRIM(CAST(mez_codice AS VARCHAR(15))) ILIKE {str})";
						}

						cmd.CommandText = DbUtils.QueryAdapt(query);
						total = (long)cmd.ExecuteScalar();
					}

					if (joined)
						query = $@"
						 SELECT mezzi.*, mod_mar, mod_tip, mod_ver, mod_desc, mod_cod_for, tip_desc, mar_desc, ver_desc, ver_funzionamento_anni, ver_integrita_anni, ver_interna_anni, mod_manuale_uso, mod_marchio_ce, mod_rispondenza_all_v,
							mod_formazione, mod_corso, dit_desc, NULL AS img_list, NULL AS doc_list, NULL AS man_list, NULL AS vid_list,
							(CASE
								WHEN imz.img_data IS NOT NULL THEN imz.img_data
								ELSE imm.img_data
							END) AS img_data
						 FROM mezcantieri
						 INNER JOIN mezzi ON mec_dit = mez_dit AND mec_mez = mez_codice
						 INNER JOIN (
	 						SELECT can_dit, can_codice
							FROM cantieri
							WHERE can_dit = {ditta}
							AND can_codice = {cantiere}
							UNION
							SELECT sub_dit_sub AS can_dit, sub_can_sub AS can_codice
							FROM subappalti
							WHERE sub_dit_app = {ditta}
							AND sub_can_app = {cantiere}
						 ) AS q ON mec_dit = q.can_dit AND mec_can = q.can_codice
						LEFT JOIN modelli ON mez_dit_mod = mod_dit AND mez_mod = mod_codice
						LEFT JOIN marchi ON mod_mar = mar_codice
						LEFT JOIN tipologie ON mod_tip = tip_codice
						LEFT JOIN verifiche ON mod_ver = ver_codice
						LEFT JOIN ditte ON mez_dit = dit_codice
						LEFT JOIN imgmezzi AS imz ON mez_dit = imz.img_dit AND mez_codice = imz.img_codice AND imz.img_formato = 1
						LEFT JOIN imgmodelli AS imm ON mez_dit_mod = imm.img_dit AND mez_mod = imm.img_codice AND imm.img_formato = 1
						";
					else
						query = $@"
						 SELECT mezzi.*
						 FROM mezcantieri
						 INNER JOIN mezzi ON mec_dit = mez_dit AND mec_mez = mez_codice
						 INNER JOIN (
	 						SELECT can_dit, can_codice
							FROM cantieri
							WHERE can_dit = {ditta}
							AND can_codice = {cantiere}
							UNION
							SELECT sub_dit_sub AS can_dit, sub_can_sub AS can_codice
							FROM subappalti
							WHERE sub_dit_app = {ditta}
							AND sub_can_app = {cantiere}
						 ) AS q ON mec_dit = q.can_dit AND mec_can = q.can_codice
						";
					if (string.IsNullOrWhiteSpace(filter))
						query += " WHERE mez_dit > 0";
					else
						query += $" WHERE mez_dit > 0 AND ({filter})";

					if (!string.IsNullOrWhiteSpace(search))
					{
						query += $" AND (mez_desc ILIKE {str} OR mez_serial ILIKE {str} OR mez_targa ILIKE {str} OR mez_telaio ILIKE {str} OR TRIM(CAST(mez_codice AS VARCHAR(15))) ILIKE {str})";
					}

					if (string.IsNullOrWhiteSpace(orderby))
						query += " ORDER BY mez_codice";
					else
						query += " ORDER BY " + orderby;
					cmd.CommandText = DbUtils.QueryAdapt(query, top, skip);

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var mez = new MezziDb();
						DbUtils.SqlRead(ref reader, ref mez, joined ? null : MezziDb.GetJoinExcludeFields());
						if (json.Data == null) json.Data = new List<MezziDb>();
						json.Data.Add(mez);
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
	}
}
