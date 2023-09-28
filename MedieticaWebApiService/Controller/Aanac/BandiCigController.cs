using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using MedieticaWebApiService.Extensions;
using MedieticaWebApiService.Helpers;
using MedieticaWebApiService.Models.Anac;

namespace MedieticaWebApiService.Controller.Aanac
{
	[EnableCors("*", "*", "*")]

	public class BandiCigController : ApiController
	{
		[Route("api/bandicig/get")]
		public DefaultJson<BandiCigDb> GetList(int ditta = 0, int top = 0, int skip = 0, string orderby = "", string search = "", string filter = "", bool inlinecount = false, bool joined = false)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<BandiCigDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var str = search.ToUpper().SqlQuote(true, true);
					string query;
					var total = 0L;
					if (inlinecount)
					{
						query = "SELECT COUNT(*) FROM anac.bandi_cig";
						if (string.IsNullOrWhiteSpace(filter))
							query += " WHERE cig <> ''";
						else
							query += $" WHERE cig <> '' AND ({filter})";

						if (!string.IsNullOrWhiteSpace(search))
						{
							query += $" AND (oggetto_gara ILIKE {str} OR oggetto_gara ILIKE {str})";
						}
						cmd.CommandText = DbUtils.QueryAdapt(query);
						total = (long)cmd.ExecuteScalar();
					}

					query = "SELECT * FROM anac.bandi_cig";
					if (string.IsNullOrWhiteSpace(filter))
						query += " WHERE cig <> ''";
					else
						query += $" WHERE cig <> '' AND ({filter})";

					if (!string.IsNullOrWhiteSpace(search))
					{
						query += $" AND (oggetto_gara ILIKE {str} OR oggetto_gara ILIKE {str})";
					}

					if (string.IsNullOrWhiteSpace(orderby))
						query += " ORDER BY data_pubblicazione DESC";
					else
						query += " ORDER BY " + orderby;
					cmd.CommandText = DbUtils.QueryAdapt(query, top, skip);

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var ban = new BandiCigDb();
						DbUtils.SqlRead(ref reader, ref ban);
						if (json.Data == null) json.Data = new List<BandiCigDb>();
						json.Data.Add(ban);
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
