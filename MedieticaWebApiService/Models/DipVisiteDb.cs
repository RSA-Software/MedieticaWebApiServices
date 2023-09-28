using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class DipVisiteDb
	{
		public int dvi_dit { get; set; }
		public int dvi_codice { get; set; }
		public int dvi_dip { get; set; }
		public string dvi_medico { get; set; }
		public DateTime? dvi_data { get; set; }
		public DateTime? dvi_created_at { get; set; }
		public DateTime? dvi_last_update { get; set; }
		public int dvi_user { get; set; }

		public DateTime? next_data { get; set; }


		private static readonly List<string> ExcludeFields = new List<string>() { "next_data" };

		public DipVisiteDb()
		{
			var dma_db = this;
			DbUtils.Initialize(ref dma_db);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static bool Search(ref OdbcCommand cmd, long codDit, int codice, ref DipVisiteDb dvi, bool writeLock = false)
		{
			if (dvi != null) DbUtils.Initialize(ref dvi);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codice, ref dvi, writeLock);
				}
			}

			var found = false;
			var sql = "SELECT * FROM dipvisite WHERE dvi_dit = ? AND dvi_codice = ?";
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (dvi != null) DbUtils.SqlRead(ref reader, ref dvi, ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref DipVisiteDb dvi, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref dvi);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new DipVisiteDb();
				if (!Search(ref cmd, dvi.dvi_dit, dvi.dvi_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.dvi_last_update != dvi.dvi_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, dvi.dvi_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				DipendentiDb dip = null;
				if (!DipendentiDb.Search(ref cmd, dvi.dvi_dit, dvi.dvi_dip, ref dip)) throw new MCException(MCException.DipendenteMsg, MCException.DipendenteErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
				do
				{
					var next_data = dvi.next_data;

					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref dvi, "dipvisite", null, ExcludeFields);
						cmd.ExecuteNonQuery();
						Reload(ref cmd, ref dvi);

						//
						// Creiamo la successiva scadenza
						//
						if (next_data != null && next_data > dvi.dvi_data)
						{
							var scp = new ScaDipendentiDb();
							cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(scp_codice),0) AS codice FROM scadipendenti WHERE scp_dit = ?");
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = dvi.dvi_dit;
							scp.scp_codice = 1 + (int)cmd.ExecuteScalar();
							scp.scp_desc = "Visita Medica";
							scp.scp_dit = dvi.dvi_dit;
							scp.scp_dip = dvi.dvi_dip;
							scp.scp_data = next_data;
							scp.scp_scad_alert_before = 15;
							ScaDipendentiDb.Write(ref cmd, DbMessage.DB_INSERT, ref scp, ref obj);
						}
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							dvi.dvi_codice++;
							continue;
						}
						throw;
					}
					break;
				} while (true);
				break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dvi, "dipvisite", "WHERE dvi_codice = ?", ExcludeFields);
					cmd.Parameters.Add("codice", OdbcType.BigInt).Value = dvi.dvi_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref dvi);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM dipvisite WHERE dvi_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = dvi.dvi_codice; cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref DipVisiteDb dvi)
		{
			if (!Search(ref cmd, dvi.dvi_dit,  dvi.dvi_codice, ref dvi)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
