using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class FinalitaDb
	{
		public long fin_codice { get; set; }
		public string fin_desc { get; set; }
		public DateTime? fin_created_at { get; set; }

		public DateTime? fin_last_update { get; set; }
		public int fin_user { get; set; }

		public FinalitaDb()
		{
			var fin_db = this;
			DbUtils.Initialize(ref fin_db);
		}

		private static readonly List<string> ExcludeFields = new List<string>() { "fin_codice" };

		public static bool Search(ref OdbcCommand cmd, long codice, ref FinalitaDb fin, bool writeLock = false)
		{
			if (fin != null) DbUtils.Initialize(ref fin);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref fin, writeLock);
				}
			}

			var found = false;

			var sql = DbUtils.QueryAdapt("SELECT * FROM finalita WHERE fin_codice = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (fin != null) DbUtils.SqlRead(ref reader, ref fin);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref FinalitaDb fin, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref fin);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new FinalitaDb();
				if (!Search(ref cmd, fin.fin_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.fin_last_update != fin.fin_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(fin.fin_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({fin.fin_codice}) : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref fin, "finalita", null, ExcludeFields);
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								DbUtils.SqlRead(ref reader, ref fin);
							}
							reader.Close();
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								fin.fin_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref fin, "finalita", "WHERE fin_codice = ?");
					cmd.Parameters.Add("@codice", OdbcType.BigInt).Value = fin.fin_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref fin);
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

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM finalita WHERE fin_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = fin.fin_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref FinalitaDb fin)
		{
			if (!Search(ref cmd, fin.fin_codice, ref fin))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
