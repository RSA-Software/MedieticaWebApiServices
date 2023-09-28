using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Dynamic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using MedieticaWebApiService.Helpers;
using MedieticaWebApiService.ViewModel;

namespace MedieticaWebApiService.Controller
{
	[EnableCors("*", "*", "*")]

	public class WidgetController : ApiController
	{
		[HttpGet]
		[Route("api/widget/scadenze/mezzi/{ditta}")]
		public GenericJson ScadenzeMezzi(int ditta)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var json = new GenericJson();

					const string sql = @"
					SELECT scm_data AS data, scm_desc AS sca_desc, scm_dit AS dit_codice, scm_mez AS codice, mez_desc AS desc, dit_desc
					FROM scamezzi
					INNER JOIN mezzi ON (scm_dit = mez_dit AND  scm_mez = mez_codice)
					INNER JOIN ditte ON scm_dit = dit_codice
					WHERE (scm_dit = ? OR scm_dit IN(
					    SELECT sub_dit_sub
						FROM subappalti
						INNER JOIN cantieri ON sub_dit_app = can_dit and sub_can_app = can_codice AND can_deleted = 0 AND can_data_fine IS NULL
						WHERE sub_dit_app = ?
						))
						AND (Now() >= (scm_data - INTERVAL '1 DAY' * scm_scad_alert_before))
						AND (scm_scad_alert_after = 0  OR Now() <= (scm_data + INTERVAL '1 DAY' * (scm_scad_alert_after + 1)))
					UNION
					SELECT dme_data_scadenza AS data, dme_desc AS sca_desc, dme_dit AS dit_codice, dme_mez AS codice, mez_desc AS desc, dit_desc
					FROM docmezzi
					INNER JOIN mezzi ON (dme_dit = mez_dit AND  dme_mez = mez_codice)
					INNER JOIN ditte ON dme_dit = dit_codice
					WHERE (dme_dit = ? OR dme_dit IN(
					    SELECT sub_dit_sub
						FROM subappalti
						INNER JOIN cantieri ON sub_dit_app = can_dit and sub_can_app = can_codice AND can_deleted = 0 AND can_data_fine IS NULL
						WHERE sub_dit_app = ?
						))
						AND (Now() >= (dme_data_scadenza - INTERVAL '1 DAY' * dme_scad_alert_before))
						AND (dme_scad_alert_after = 0  OR Now() <= (dme_data_scadenza + INTERVAL '1 DAY' * (dme_scad_alert_after + 1)))
					ORDER BY data DESC"; 

					cmd.CommandText = DbUtils.QueryAdapt(sql);
					cmd.Parameters.Clear();
					cmd.Parameters.Add("cod1", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("cod2", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("cod3", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("cod4", OdbcType.Int).Value = ditta;

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var row = new ExpandoObject() as IDictionary<string, object>;
						for (var idx = 0; idx < reader.FieldCount; idx++)
						{
							var obj = reader.GetValue(idx);
							if (obj is string val)
								row.Add(reader.GetName(idx), val.Trim());
							else
								row.Add(reader.GetName(idx), obj);
						}
						if (json.Data == null) json.Data = new List<ExpandoObject>();
						json.Data.Add((ExpandoObject)row);
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

		[HttpGet]
		[Route("api/widget/scadenze/dipendenti/{ditta}")]
		public GenericJson ScadenzeDipendenti(int ditta)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var json = new GenericJson();

					const string sql = @"
					SELECT scp_data AS data, scp_desc AS sca_desc, scp_dit AS ditta, scp_dip AS codice, dip_desc AS desc
					FROM scadipendenti
					INNER JOIN dipendenti ON (scp_dit = dip_dit AND  scp_dip = dip_codice)
					WHERE scp_dit = ?
						AND (Now() >= (scp_data - INTERVAL '1 DAY' * scp_scad_alert_before))
						AND (scp_scad_alert_after = 0  OR Now() <= (scp_data + INTERVAL '1 DAY' * (scp_scad_alert_after + 1)))
					UNION
					SELECT doc_data_scadenza AS data, doc_desc AS sca_desc, doc_dit AS ditta, doc_dip AS codice, dip_desc AS desc
					FROM documenti
					INNER JOIN dipendenti ON (doc_dit = dip_dit AND  doc_dip = dip_codice)
					WHERE doc_dit = ?
						AND (Now() >= (doc_data_scadenza - INTERVAL '1 DAY' * doc_scad_alert_before))
						AND (doc_scad_alert_after = 0  OR Now() <= (doc_data_scadenza + INTERVAL '1 DAY' * (doc_scad_alert_after + 1)))
					ORDER BY data DESC";

					cmd.CommandText = DbUtils.QueryAdapt(sql);
					cmd.Parameters.Clear();
					cmd.Parameters.Add("cod1", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("cod2", OdbcType.Int).Value = ditta;
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var row = new ExpandoObject() as IDictionary<string, object>;
						for (var idx = 0; idx < reader.FieldCount; idx++)
						{
							var obj = reader.GetValue(idx);
							if (obj is string val)
								row.Add(reader.GetName(idx), val.Trim());
							else
								row.Add(reader.GetName(idx), obj);
						}
						if (json.Data == null) json.Data = new List<ExpandoObject>();
						json.Data.Add((ExpandoObject)row);
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

		[HttpGet]
		[Route("api/widget/scadenze/cantieri/{ditta}")]
		public GenericJson ScadenzeCantieri(int ditta)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var json = new GenericJson();

					const string sql = @"
					SELECT scc_data AS data, scc_desc AS sca_desc, scc_dit AS dit_codice, scc_can AS codice, can_desc AS desc, dit_desc
					FROM scacantieri
					INNER JOIN cantieri ON (scc_dit = can_dit AND  scc_can = can_codice)
					INNER JOIN ditte ON scc_dit = dit_codice
					WHERE (scc_dit = ? OR scc_dit IN(
					    SELECT sub_dit_sub
						FROM subappalti
						INNER JOIN cantieri ON sub_dit_app = can_dit and sub_can_app = can_codice AND can_deleted = 0 AND can_data_fine IS NULL
						WHERE sub_dit_app = ?
						))
						AND (Now() >= (scc_data - INTERVAL '1 DAY' * scc_scad_alert_before))
						AND (scc_scad_alert_after = 0  OR Now() <= (scc_data + INTERVAL '1 DAY' * (scc_scad_alert_after + 1)))
					UNION
					SELECT dca_data_scadenza AS data, dca_desc AS sca_desc, dca_dit AS dit_codice, dca_can AS codice, can_desc AS desc, dit_desc
					FROM doccantieri
					INNER JOIN cantieri ON (dca_dit = can_dit AND dca_can = can_codice)
					INNER JOIN ditte ON dca_dit = dit_codice
					WHERE (dca_dit = ? OR dca_dit IN(
					    SELECT sub_dit_sub
						FROM subappalti
						INNER JOIN cantieri ON sub_dit_app = can_dit and sub_can_app = can_codice AND can_deleted = 0 AND can_data_fine IS NULL
						WHERE sub_dit_app = ?
						))
						AND (Now() >= (dca_data_scadenza - INTERVAL '1 DAY' * dca_scad_alert_before))
						AND (dca_scad_alert_after = 0  OR Now() <= (dca_data_scadenza + INTERVAL '1 DAY' * (dca_scad_alert_after + 1)))
					ORDER BY data DESC";
					cmd.CommandText = DbUtils.QueryAdapt(sql);
					cmd.Parameters.Clear();
					cmd.Parameters.Add("cod1", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("cod2", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("cod3", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("cod4", OdbcType.Int).Value = ditta;

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var row = new ExpandoObject() as IDictionary<string, object>;
						for (var idx = 0; idx < reader.FieldCount; idx++)
						{
							var obj = reader.GetValue(idx);
							if (obj is string val)
								row.Add(reader.GetName(idx), val.Trim());
							else
								row.Add(reader.GetName(idx), obj);
						}
						if (json.Data == null) json.Data = new List<ExpandoObject>();
						json.Data.Add((ExpandoObject)row);
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

		[HttpGet]
		[Route("api/widget/scadenze/ditte/{ditta}")]
		public GenericJson ScadenzeDitte(int ditta)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var json = new GenericJson();

					const string sql = @"
					SELECT scd_data AS data, scd_desc AS sca_desc, scd_dit AS dit_codice, scd_dit AS codice, '' AS desc, dit_desc
					FROM scaditte
					INNER JOIN ditte ON scd_dit = dit_codice
					WHERE (scd_dit = ? OR scd_dit IN(
					    SELECT sub_dit_sub
						FROM subappalti
						INNER JOIN cantieri ON sub_dit_app = can_dit and sub_can_app = can_codice AND can_deleted = 0 AND can_data_fine IS NULL
						WHERE sub_dit_app = ?
						))
						AND (Now() >= (scd_data - INTERVAL '1 DAY' * scd_scad_alert_before))
						AND (scd_scad_alert_after = 0  OR Now() <= (scd_data + INTERVAL '1 DAY' * (scd_scad_alert_after + 1)))
					UNION
					SELECT dod_data_scadenza AS data, dod_desc AS sca_desc, dod_dit AS dit_codice, dod_dit AS codice, '' AS desc, dit_desc
					FROM docditte
					INNER JOIN ditte ON dod_dit = dit_codice
					WHERE (dod_dit = ? OR dod_dit IN(
					    SELECT sub_dit_sub
						FROM subappalti
						INNER JOIN cantieri ON sub_dit_app = can_dit and sub_can_app = can_codice AND can_deleted = 0 AND can_data_fine IS NULL
						WHERE sub_dit_app = ?
						))
						AND (Now() >= (dod_data_scadenza - INTERVAL '1 DAY' * dod_scad_alert_before))
						AND (dod_scad_alert_after = 0  OR Now() <= (dod_data_scadenza + INTERVAL '1 DAY' * (dod_scad_alert_after + 1)))
					ORDER BY data DESC";

					cmd.CommandText = DbUtils.QueryAdapt(sql);
					cmd.Parameters.Clear();
					cmd.Parameters.Add("cod1", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("cod2", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("cod3", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("cod4", OdbcType.Int).Value = ditta;

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var row = new ExpandoObject() as IDictionary<string, object>;
						for (var idx = 0; idx < reader.FieldCount; idx++)
						{
							var obj = reader.GetValue(idx);
							if (obj is string val)
								row.Add(reader.GetName(idx), val.Trim());
							else
								row.Add(reader.GetName(idx), obj);
						}
						if (json.Data == null) json.Data = new List<ExpandoObject>();
						json.Data.Add((ExpandoObject)row);
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

		[HttpGet]
		[Route("api/widget/statistiche/{ditta}")]
		public DefaultJson<StatisticheDitta> Statistiche(int ditta)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var json = new DefaultJson<StatisticheDitta>();

					var sta = new StatisticheDitta();

					//
					// Contiamo il numero dei Cantieri Attivi e non
					//
					cmd.CommandText = "SELECT COUNT(*) FROM cantieri WHERE can_dit = ? AND can_data_fine IS NULL";
					cmd.Parameters.Clear();
					cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
					sta.cantieri_attivi = (long)cmd.ExecuteScalar();

					cmd.CommandText = "SELECT COUNT(*) FROM cantieri WHERE can_dit = ? AND can_data_fine IS NOT NULL";
					cmd.Parameters.Clear();
					cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
					sta.cantieri_cessati = (long)cmd.ExecuteScalar();

					//
					// Contiamo il numero dei Dipendenti attivi e non
					//
					cmd.CommandText = "SELECT COUNT(*) FROM dipendenti WHERE dip_dit = ? AND dip_data_fine_rapporto IS NULL;";
					cmd.Parameters.Clear();
					cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
					sta.dipendenti_attivi = (long)cmd.ExecuteScalar();

					cmd.CommandText = "SELECT COUNT(*) FROM dipendenti WHERE dip_dit = ? AND dip_data_fine_rapporto IS NOT NULL";
					cmd.Parameters.Clear();
					cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
					sta.dipendenti_cessati = (long)cmd.ExecuteScalar();

					//
					// Contiamo il numero dei Mezzi attivi e non
					//
					cmd.CommandText = "SELECT COUNT(*) FROM mezzi WHERE mez_dit = ? AND mez_data_dis IS NULL";
					cmd.Parameters.Clear();
					cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
					sta.mezzi_attivi = (long)cmd.ExecuteScalar();

					cmd.CommandText = "SELECT COUNT(*) FROM mezzi WHERE mez_dit = ? AND mez_data_dis IS NOT NULL";
					cmd.Parameters.Clear();
					cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
					sta.mezzi_cessati = (long)cmd.ExecuteScalar();

					//
					// Contiamo il numero dei Subappalti attivi e non
					//
					var sql = @"
					SELECT COUNT(*)
					FROM subappalti
					INNER JOIN cantieri ON sub_dit_app = can_dit and sub_can_app = can_codice AND can_deleted = 0 AND can_data_fine IS NULL
					WHERE sub_dit_app = ?";
					
					cmd.CommandText = sql;
					cmd.Parameters.Clear();
					cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
					sta.subappalti_attivi = (long)cmd.ExecuteScalar();

					sql = @"
					SELECT COUNT(*)
					FROM subappalti
					INNER JOIN cantieri ON sub_dit_app = can_dit and sub_can_app = can_codice AND can_deleted = 0 AND can_data_fine IS NOT NULL
					WHERE sub_dit_app = ?";

					cmd.CommandText = sql;
					cmd.Parameters.Clear();
					cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
					sta.subappalti_cessati = (long)cmd.ExecuteScalar();

					//
					// Leggiamo la lista dei documenti da compilare
					//
					sql = @"
					SELECT 0 AS tipo, doc_dip AS codice, dip_desc AS titolo, '' AS matricola, doc_desc AS desc, dit_codice, dit_desc
					FROM documenti
					LEFT JOIN ditte ON doc_dit = dit_codice
					LEFT JOIN dipendenti On (doc_dit = dip_dit AND doc_dip = dip_codice)
					WHERE (doc_dit = ? OR doc_dit IN(
					    SELECT sub_dit_sub
						FROM subappalti
						INNER JOIN cantieri ON sub_dit_app = can_dit and sub_can_app = can_codice AND can_deleted = 0 AND can_data_fine IS NULL
						WHERE sub_dit_app = ?
						)) AND doc_data IS NULL
					UNION ALL
					SELECT 1 AS tipo, dca_can AS codice, can_desc AS titolo, '' AS matricola, dca_desc AS desc, dit_codice, dit_desc
					FROM doccantieri
					LEFT JOIN cantieri ON (dca_dit = can_dit AND dca_can = can_codice)
					LEFT JOIN ditte ON dca_dit = dit_codice
					WHERE (dca_dit = ? OR dca_dit IN(
					    SELECT sub_dit_sub
						FROM subappalti
						INNER JOIN cantieri ON sub_dit_app = can_dit and sub_can_app = can_codice AND can_deleted = 0 AND can_data_fine IS NULL
						WHERE sub_dit_app = ?
						)) AND dca_data IS NULL
					UNION ALL
					SELECT 2 AS tipo, dod_dit AS codice, dit_desc AS titolo, '' AS matricola, dod_desc AS desc, dit_codice, dit_desc
					FROM docditte
					LEFT JOIN ditte ON (dod_dit = dit_codice)
					WHERE (dod_dit = ? OR dod_dit IN(
					    SELECT sub_dit_sub
						FROM subappalti
						INNER JOIN cantieri ON sub_dit_app = can_dit and sub_can_app = can_codice AND can_deleted = 0 AND can_data_fine IS NULL
						WHERE sub_dit_app = ?
						)) AND dod_data IS NULL
					UNION ALL
					SELECT 3 AS tipo, dme_dit AS codice, mez_desc AS titolo, mez_serial AS matricola, dme_desc AS desc, dit_codice, dit_desc
					FROM docmezzi
					LEFT JOIN mezzi ON (dme_dit = mez_dit AND dme_mez = mez_codice)
					LEFT JOIN ditte ON dme_dit = dit_codice
					WHERE (dme_dit = ? OR dme_dit IN(
					    SELECT sub_dit_sub
						FROM subappalti
						INNER JOIN cantieri ON sub_dit_app = can_dit and sub_can_app = can_codice AND can_deleted = 0 AND can_data_fine IS NULL
						WHERE sub_dit_app = ?
						)) AND dme_data IS NULL
					ORDER BY tipo, codice";
					cmd.CommandText = sql;
					cmd.Parameters.Clear();
					cmd.Parameters.Add("cod1", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("cod2", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("cod3", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("cod4", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("cod5", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("cod6", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("cod7", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("cod8", OdbcType.Int).Value = ditta;

					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var doc = new DocumentiDaCompilare();
						DbUtils.SqlRead(ref reader, ref doc);
						if (sta.doc_list == null) sta.doc_list = new List<DocumentiDaCompilare>();
						sta.doc_list.Add(doc);
					}
					reader.Close();

					if (json.Data == null) json.Data = new List<StatisticheDitta>();
					json.Data.Add(sta);
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
	}
}
