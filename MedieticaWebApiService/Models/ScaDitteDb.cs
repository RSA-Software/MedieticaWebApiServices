using System;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class ScaDitteDb
	{
		public int scd_dit { get; set; }
		public int scd_codice { get; set; }
		public DateTime? scd_data { get; set; }
		public string scd_desc { get; set; }
		public short scd_scad_alert_before { get; set; }
		public short scd_scad_alert_after { get; set; }
		public DateTime? scd_created_at { get; set; }
		public DateTime? scd_last_update { get; set; }

		public ScaDitteDb()
		{
			var scd_db = this;
			DbUtils.Initialize(ref scd_db);
		}

		public static bool Search(ref OdbcCommand cmd, int codDit, int codice, ref ScaDitteDb scd, bool writeLock = false)
		{
			if (scd != null) DbUtils.Initialize(ref scd);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codice, ref scd, writeLock);
				}
			}

			var found = false;

			var sql = "SELECT * FROM scaditte WHERE scd_dit = ? AND scd_codice = ?";

			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql,1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (scd != null) DbUtils.SqlRead(ref reader, ref scd);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref ScaDitteDb scd, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref scd);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new ScaDitteDb();
				if (!Search(ref cmd, scd.scd_dit, scd.scd_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.scd_last_update != scd.scd_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(scd.scd_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({scd.scd_dit} - {scd.scd_codice}) : desc", MCException.CampoObbligatorioErr);

				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, scd.scd_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref scd, "scaditte");
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref scd, "scaditte", "WHERE scd_dit = ? AND scd_codice = ?");
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = scd.scd_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = scd.scd_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({scd.scd_dit} - {scd.scd_codice})", MCException.DuplicateErr);
								throw;
							}
						}
						else throw;
					}
					break;

				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref scd, "scaditte");
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref scd);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								scd.scd_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref scd, "scaditte", "WHERE scd_dit = ? AND scd_codice = ?");
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = scd.scd_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = scd.scd_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref scd);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM scaditte WHERE scd_dit = ? AND scd_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = scd.scd_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = scd.scd_codice;
						cmd.ExecuteNonQuery();
					}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref ScaDitteDb scd)
		{
			if (!Search(ref cmd, scd.scd_dit, scd.scd_codice, ref scd)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
