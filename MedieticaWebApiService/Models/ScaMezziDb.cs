using System;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class ScaMezziDb
	{
		public int scm_dit { get; set; }
		public int scm_codice { get; set; }
		public int scm_mez { get; set; }
		public DateTime? scm_data { get; set; }
		public string scm_desc { get; set; }
		public short scm_scad_alert_before { get; set; }
		public short scm_scad_alert_after { get; set; }
		public DateTime? scm_created_at { get; set; }
		public DateTime? scm_last_update { get; set; }

		public ScaMezziDb()
		{
			var scm_db = this;
			DbUtils.Initialize(ref scm_db);
		}

		public static bool Search(ref OdbcCommand cmd, int codDit, int codice, ref ScaMezziDb scm, bool writeLock = false)
		{
			if (scm != null) DbUtils.Initialize(ref scm);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codice, ref scm, writeLock);
				}
			}

			var found = false;

			var sql = "SELECT * FROM scamezzi WHERE scm_dit = ? AND scm_codice = ?";

			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql,1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (scm != null) DbUtils.SqlRead(ref reader, ref scm);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref ScaMezziDb scm, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref scm);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new ScaMezziDb();
				if (!Search(ref cmd, scm.scm_dit, scm.scm_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.scm_last_update != scm.scm_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(scm.scm_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({scm.scm_dit} - {scm.scm_codice}) : desc", MCException.CampoObbligatorioErr);

				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, scm.scm_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				MezziDb mez = null;
				if (!MezziDb.Search(ref cmd, scm.scm_dit, scm.scm_mez, ref mez)) throw new MCException(MCException.MezzoMsg, MCException.MezzoErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref scm, "scamezzi");
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref scm, "scamezzi", "WHERE scm_dit = ? AND scm_codice = ?");
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = scm.scm_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = scm.scm_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({scm.scm_dit} - {scm.scm_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref scm, "scamezzi");
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref scm);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								scm.scm_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref scm, "scamezzi", "WHERE scm_dit = ? AND scm_codice = ?");
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = scm.scm_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = scm.scm_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref scm);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM scamezzi WHERE scm_dit = ? AND scm_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = scm.scm_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = scm.scm_codice;
						cmd.ExecuteNonQuery();
					}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref ScaMezziDb scm)
		{
			if (!Search(ref cmd, scm.scm_dit, scm.scm_codice, ref scm)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
