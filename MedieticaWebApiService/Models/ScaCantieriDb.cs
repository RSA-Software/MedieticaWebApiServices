using System;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class ScaCantieriDb
	{
		public int scc_dit { get; set; }
		public int scc_codice { get; set; }
		public int scc_can { get; set; }
		public DateTime? scc_data { get; set; }
		public string scc_desc { get; set; }
		public short scc_scad_alert_before { get; set; }
		public short scc_scad_alert_after { get; set; }
		public DateTime? scc_created_at { get; set; }
		public DateTime? scc_last_update { get; set; }

		public ScaCantieriDb()
		{
			var scc_db = this;
			DbUtils.Initialize(ref scc_db);
		}

		public static bool Search(ref OdbcCommand cmd, int codDit, int codice, ref ScaCantieriDb scc, bool writeLock = false)
		{
			if (scc != null) DbUtils.Initialize(ref scc);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codice, ref scc, writeLock);
				}
			}

			var found = false;

			var sql = "SELECT * FROM scacantieri WHERE scc_dit = ? AND scc_codice = ?";

			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql,1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (scc != null) DbUtils.SqlRead(ref reader, ref scc);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref ScaCantieriDb scm, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref scm);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new ScaCantieriDb();
				if (!Search(ref cmd, scm.scc_dit, scm.scc_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.scc_last_update != scm.scc_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(scm.scc_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({scm.scc_dit} - {scm.scc_codice}) : desc", MCException.CampoObbligatorioErr);

				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, scm.scc_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				CantieriDb can = null;
				if (!CantieriDb.Search(ref cmd, scm.scc_dit, scm.scc_can, ref can)) throw new MCException(MCException.CantiereMsg, MCException.CantiereErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref scm, "scacantieri");
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref scm, "scacantieri", "WHERE scc_dit = ? AND scc_codice = ?");
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = scm.scc_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = scm.scc_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({scm.scc_dit} - {scm.scc_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref scm, "scacantieri");
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref scm);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								scm.scc_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref scm, "scacantieri", "WHERE scc_dit = ? AND scc_codice = ?");
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = scm.scc_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = scm.scc_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref scm);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM scacantieri WHERE scc_dit = ? AND scc_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = scm.scc_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = scm.scc_codice;
						cmd.ExecuteNonQuery();
					}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref ScaCantieriDb scm)
		{
			if (!Search(ref cmd, scm.scc_dit, scm.scc_codice, ref scm)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
