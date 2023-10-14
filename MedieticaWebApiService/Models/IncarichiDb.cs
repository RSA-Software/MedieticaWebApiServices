using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class IncarichiDb
	{
		public long inc_codice { get; set; }
		public string inc_desc { get; set; }
		public DateTime? inc_created_at { get; set; }
		public DateTime inc_last_update { get; set; }
		public int inc_user { get; set; }


		public IncarichiDb()
		{
			var inc_db = this;
			DbUtils.Initialize(ref inc_db);
		}

		private static readonly List<string> InsExcludeFields = new List<string>() { "inc_codice",};
		private static readonly List<string> ExcludeFields = new List<string>() { "" };

		private static readonly string JoinQuery = @"
		SELECT incarichi.*
		FROM incarichi
		";

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}



		public static bool Search(ref OdbcCommand cmd, long codice, ref IncarichiDb ges, bool joined = false,  bool writeLock = false)
		{
			if (ges != null) DbUtils.Initialize(ref ges);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref ges, joined, writeLock);
				}
			}

			var found = false;
			string sql;
			if (!joined)
				sql = "SELECT * FROM incarichi WHERE inc_codice = ?";
			else
				sql = GetJoinQuery() + " WHERE inc_codice = ?";
			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;
			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (ges != null) DbUtils.SqlRead(ref reader, ref ges, joined ? null : ExcludeFields );
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref IncarichiDb inc, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref inc);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new IncarichiDb();
				if (!Search(ref cmd, inc.inc_codice, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.inc_last_update != inc.inc_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(inc.inc_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({inc.inc_codice}) : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref inc, "incarichi", null, InsExcludeFields);
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								DbUtils.SqlRead(ref reader, ref inc, ExcludeFields);
							}
							reader.Close();
							if (joined) Reload(ref cmd, ref inc, true);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								inc.inc_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref inc, "incarichi", "WHERE inc_codice = ?", ExcludeFields);
					cmd.Parameters.Add("@codice", OdbcType.BigInt).Value = inc.inc_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref inc, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM incarichi WHERE inc_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = inc.inc_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref IncarichiDb inc, bool joined)
		{
			if (!Search(ref cmd, inc.inc_codice, ref inc, joined))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
