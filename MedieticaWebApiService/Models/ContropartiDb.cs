using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class ContropartiDb
	{
		public long cnt_codice { get; set; }
		public string cnt_desc { get; set; }
		public DateTime? cnt_created_at { get; set; }

		public DateTime? cnt_last_update { get; set; }
		public int cnt_user { get; set; }

		public ContropartiDb()
		{
			var cnt_db = this;
			DbUtils.Initialize(ref cnt_db);
		}

		private static readonly List<string> ExcludeFields = new List<string>() { "cnt_codice" };

		public static bool Search(ref OdbcCommand cmd, long codice, ref ContropartiDb cnt, bool writeLock = false)
		{
			if (cnt != null) DbUtils.Initialize(ref cnt);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref cnt, writeLock);
				}
			}

			var found = false;

			var sql = DbUtils.QueryAdapt("SELECT * FROM controparti WHERE cnt_codice = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (cnt != null) DbUtils.SqlRead(ref reader, ref cnt);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref ContropartiDb cnt, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref cnt);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new ContropartiDb();
				if (!Search(ref cmd, cnt.cnt_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.cnt_last_update != cnt.cnt_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(cnt.cnt_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({cnt.cnt_codice}) : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref cnt, "controparti", null, ExcludeFields);
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								DbUtils.SqlRead(ref reader, ref cnt);
							}
							reader.Close();
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								cnt.cnt_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref cnt, "controparti", "WHERE cnt_codice = ?");
					cmd.Parameters.Add("@codice", OdbcType.BigInt).Value = cnt.cnt_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref cnt);
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

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM controparti WHERE cnt_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = cnt.cnt_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref ContropartiDb cnt)
		{
			if (!Search(ref cmd, cnt.cnt_codice, ref cnt))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
