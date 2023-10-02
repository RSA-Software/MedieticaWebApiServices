using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class TipologieAttivitaDb
	{
		public long tat_codice { get; set; }
		public string tat_desc { get; set; }
		public DateTime? tat_created_at { get; set; }

		public DateTime? tat_last_update { get; set; }
		public int tat_user { get; set; }

		public TipologieAttivitaDb()
		{
			var tat_db = this;
			DbUtils.Initialize(ref tat_db);
		}

		private static readonly List<string> ExcludeFields = new List<string>() { "tat_codice" };

		public static bool Search(ref OdbcCommand cmd, long codice, ref TipologieAttivitaDb tat, bool writeLock = false)
		{
			if (tat != null) DbUtils.Initialize(ref tat);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref tat, writeLock);
				}
			}

			var found = false;

			var sql = DbUtils.QueryAdapt("SELECT * FROM tipologia_attivita WHERE tat_codice = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (tat != null) DbUtils.SqlRead(ref reader, ref tat);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref TipologieAttivitaDb tat, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref tat);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new TipologieAttivitaDb();
				if (!Search(ref cmd, tat.tat_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.tat_last_update != tat.tat_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(tat.tat_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({tat.tat_codice}) : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref tat, "tipologia_attivita", null, ExcludeFields);
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								DbUtils.SqlRead(ref reader, ref tat);
							}
							reader.Close();
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								tat.tat_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref tat, "tipologia_attivita", "WHERE tat_codice = ?");
					cmd.Parameters.Add("@codice", OdbcType.BigInt).Value = tat.tat_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref tat);
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

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM tipologia_attivita WHERE tat_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = tat.tat_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref TipologieAttivitaDb tat)
		{
			if (!Search(ref cmd, tat.tat_codice, ref tat))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
