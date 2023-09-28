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

	public class DipendentiGiornaleController : ApiController
	{
		[Route("api/dipendentigiornale/get")]
		public DefaultJson<DipendentiDb> GetList(int ditta = 0, int cantiere = 0, int top = 0, int skip = 0, string orderby = "", string search = "", string filter = "", bool inlinecount = false, bool joined = false )
		{
			if (filter.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger filter value"));
			if (search.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger search value"));
			if (orderby.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger orderby value"));

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					DbUtils.CheckAuthorization(cmd, Request, ditta, Endpoints.DIPENDENTI, EndpointsOperations.VIEW);

					var json = new DefaultJson<DipendentiDb>();
					var str = search.ToUpper().SqlQuote(true, true);
					string query;
					var total = 0L;
					if (inlinecount)
					{
						query = $@"
						SELECT COUNT(*)
						FROM dipcantieri
						INNER JOIN dipendenti ON dic_dit = dip_dit AND dic_dip = dip_codice
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
						) AS q ON dic_dit = q.can_dit AND dic_can = q.can_codice
						";
						if (string.IsNullOrWhiteSpace(filter))
							query += " WHERE dip_dit > 0";
						else
							query += $" WHERE dip_dit > 0 AND ({filter})";

						if (!string.IsNullOrWhiteSpace(search))
						{
							query += $" AND (dip_desc ILIKE {str} OR TRIM(CAST(dip_codice AS VARCHAR(15))) ILIKE {str})";
						}

						cmd.CommandText = DbUtils.QueryAdapt(query);
						total = (long)cmd.ExecuteScalar();
					}

					if (joined)
						query = $@"
						SELECT dipendenti.*, dit_desc, dit_piva, dit_codfis, img_data, NULL AS img_list, NULL AS unilav_list, NULL AS ammministrazione_list, NULL AS autorizzzazioni_list, NULL AS sicurezza_list, NULL AS tecnico_list, NULL AS corsi_list
						FROM dipcantieri
						INNER JOIN dipendenti ON dic_dit = dip_dit AND dic_dip = dip_codice
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
						) AS q ON dic_dit = q.can_dit AND dic_can = q.can_codice
						INNER JOIN ditte ON dip_dit = dit_codice
						LEFT JOIN imgdipendenti ON dip_dit = img_dit AND dip_codice = img_codice AND img_formato = 1
						";
					else
						query = $@"
						SELECT dipendenti.*
						FROM dipcantieri
						INNER JOIN dipendenti ON dic_dit = dip_dit AND dic_dip = dip_codice
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
						) AS q ON dic_dit = q.can_dit AND dic_can = q.can_codice
						";
					if (string.IsNullOrWhiteSpace(filter))
						query += $" WHERE dip_dit > 0";
					else
						query += $" WHERE dip_dit > 0 AND ({filter})";

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
						var dip = new DipendentiDb();
						DbUtils.SqlRead(ref reader, ref dip, joined ? null : DipendentiDb.GetJoinExcludeFields());
						if (json.Data == null) json.Data = new List<DipendentiDb>();
						json.Data.Add(dip);
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
