using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class SettoriDb
	{
		public long set_codice { get; set; }
		public string set_desc { get; set; }
		public DateTime? set_created_at { get; set; }

		public DateTime? set_last_update { get; set; }
		public int set_user { get; set; }

		public SettoriDb()
		{
			var set_db = this;
			DbUtils.Initialize(ref set_db);
		}

		private static readonly List<string> ExcludeFields = new List<string>() { "set_codice" };

		public static bool Search(ref OdbcCommand cmd, long codice, ref SettoriDb set, bool writeLock = false)
		{
			if (set != null) DbUtils.Initialize(ref set);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref set, writeLock);
				}
			}

			var found = false;

			var sql = DbUtils.QueryAdapt("SELECT * FROM settori WHERE set_codice = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (set != null) DbUtils.SqlRead(ref reader, ref set);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref SettoriDb set, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref set);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new SettoriDb();
				if (!Search(ref cmd, set.set_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.set_last_update != set.set_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(set.set_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({set.set_codice}) : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref set, "settori", null, ExcludeFields);
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								DbUtils.SqlRead(ref reader, ref set);
							}
							reader.Close();
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								set.set_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref set, "settori", "WHERE set_codice = ?");
					cmd.Parameters.Add("@codice", OdbcType.BigInt).Value = set.set_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref set);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
/*
						cmd.CommandText = DbUtils.QueryAdapt("SELECT COUNT(*) FROM ditte WHERE dit_gru = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.Int).Value = gru.gru_codice;
						var num = Convert.ToInt32(cmd.ExecuteScalar());
						if (num > 0) throw new MCException(MCException.CancelMsg, MCException.CancelErr);
*/

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM settori WHERE set_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = set.set_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref SettoriDb set)
		{
			if (!Search(ref cmd, set.set_codice, ref set))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
