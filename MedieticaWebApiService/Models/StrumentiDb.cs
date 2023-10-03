using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class StrumentiDb
	{
		public long str_codice { get; set; }
		public string str_desc { get; set; }
		public DateTime? str_created_at { get; set; }

		public DateTime? str_last_update { get; set; }
		public int str_user { get; set; }

		public StrumentiDb()
		{
			var str_db = this;
			DbUtils.Initialize(ref str_db);
		}

		private static readonly List<string> ExcludeFields = new List<string>() { "str_codice" };

		public static bool Search(ref OdbcCommand cmd, long codice, ref StrumentiDb str, bool writeLock = false)
		{
			if (str != null) DbUtils.Initialize(ref str);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref str, writeLock);
				}
			}

			var found = false;

			var sql = DbUtils.QueryAdapt("SELECT * FROM strumenti WHERE str_codice = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (str != null) DbUtils.SqlRead(ref reader, ref str);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref StrumentiDb str, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref str);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new StrumentiDb();
				if (!Search(ref cmd, str.str_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.str_last_update != str.str_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(str.str_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({str.str_codice}) : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref str, "strumenti", null, ExcludeFields);
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								DbUtils.SqlRead(ref reader, ref str);
							}
							reader.Close();
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								str.str_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref str, "strumenti", "WHERE str_codice = ?");
					cmd.Parameters.Add("@codice", OdbcType.BigInt).Value = str.str_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref str);
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

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM strumenti WHERE str_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = str.str_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref StrumentiDb str)
		{
			if (!Search(ref cmd, str.str_codice, ref str))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
