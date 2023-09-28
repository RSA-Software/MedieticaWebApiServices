using System;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class ScaDipendentiDb
	{
		public int scp_dit { get; set; }
		public int scp_codice { get; set; }
		public int scp_dip { get; set; }
		public DateTime? scp_data { get; set; }
		public string scp_desc { get; set; }
		public short scp_scad_alert_before { get; set; }
		public short scp_scad_alert_after { get; set; }
		public DateTime? scp_created_at { get; set; }
		public DateTime? scp_last_update { get; set; }

		public ScaDipendentiDb()
		{
			var scp_db = this;
			DbUtils.Initialize(ref scp_db);
		}

		public static bool Search(ref OdbcCommand cmd, int codDit, int codice, ref ScaDipendentiDb scp, bool writeLock = false)
		{
			if (scp != null) DbUtils.Initialize(ref scp);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codice, ref scp, writeLock);
				}
			}

			var found = false;

			var sql = "SELECT * FROM scadipendenti WHERE scp_dit = ? AND scp_codice = ?";

			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql,1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (scp != null) DbUtils.SqlRead(ref reader, ref scp);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref ScaDipendentiDb scm, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref scm);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new ScaDipendentiDb();
				if (!Search(ref cmd, scm.scp_dit, scm.scp_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.scp_last_update != scm.scp_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(scm.scp_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({scm.scp_dit} - {scm.scp_codice}) : desc", MCException.CampoObbligatorioErr);

				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, scm.scp_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				DipendentiDb dip = null;
				if (!DipendentiDb.Search(ref cmd, scm.scp_dit, scm.scp_dip, ref dip)) throw new MCException(MCException.DipendenteMsg, MCException.DipendenteErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref scm, "scadipendenti");
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref scm, "scadipendenti", "WHERE scp_dit = ? AND scp_codice = ?");
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = scm.scp_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = scm.scp_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({scm.scp_dit} - {scm.scp_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref scm, "scadipendenti");
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref scm);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								scm.scp_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref scm, "scadipendenti", "WHERE scp_dit = ? AND scp_codice = ?");
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = scm.scp_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = scm.scp_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref scm);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM scadipendenti WHERE scp_dit = ? AND scp_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = scm.scp_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = scm.scp_codice;
						cmd.ExecuteNonQuery();
					}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref ScaDipendentiDb scm)
		{
			if (!Search(ref cmd, scm.scp_dit, scm.scp_codice, ref scm)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
