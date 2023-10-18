using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class PraticheDb
	{
		public long pra_codice { get; set; }
		public long pra_dit { get; set; }
		public long pra_cli { get; set; }
		public long pra_fin { get; set; }
		public long pra_inc { get; set; }
		public long pra_ges { get; set; }
		public long pra_cmr { get; set; }
		public long pra_set { get; set; }
		public long pra_str { get; set; }
		public DateTime? pra_data { get; set; }
		public DateTime? pra_data_inc { get; set; }
		public string pra_note { get; set; }
		public DateTime? pra_created_at { get; set; }
		public DateTime? pra_last_update { get; set; }
		public int pra_user { get; set; }

		//
		// Joined fields
		// 
		public string dit_desc { get; set; }
		public string cli_desc { get; set; }
		public string cli_codfis { get; set; }
		public string cli_piva { get; set; }
		public string fin_desc { get; set; }
		public string inc_desc { get; set; }
		public string ges_desc { get; set; }
		public string cmr_desc { get; set; }
		public string set_desc { get; set; }
		public string stru_desc { get; set; }


		public PraticheDb()
		{
			var pra_db = this;
			DbUtils.Initialize(ref pra_db);
		}

		private static readonly List<string> InsExcludeFields = new List<string>() { "pra_codice", "dit_desc", "cli_desc", "cli_codfis", "cli_piva", "fin_desc", "inc_desc", "ges_desc", "cmr_desc", "set_desc", "stru_desc" };
		private static readonly List<string> ExcludeFields = new List<string>() { "dit_desc", "cli_desc", "cli_codfis", "cli_piva", "fin_desc", "inc_desc", "ges_desc", "cmr_desc", "set_desc", "stru_desc" };

		private static readonly string JoinQuery = @"
		SELECT pratiche.*
		FROM pratiche
		LEFT JOIN ditte ON pra_dit = dit_codice
		LEFT JOIN clienti ON pra_cli = cli_codice
		LEFT JOIN finalita ON pra_fin = fin_codice
		LEFT JOIN incarichi ON pra_inc = inc_codice
		LEFT JOIN gestori ON pra_ges = ges_codice
		LEFT JOIN commerciali ON pra_cmr = cmr_codice
		LEFT JOIN settori ON pra_set = set_codice
		LEFT JOIN strumenti ON pra_str = str_codice
		";

		private static readonly string CountQuery = @"
		SELECT COUNT(*) 
		FROM pratiche";

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static string GetCountQuery()
		{
			return (CountQuery);
		}

		public static bool Search(ref OdbcCommand cmd, long codice, ref PraticheDb pra, bool joined = false,  bool writeLock = false)
		{
			if (pra != null) DbUtils.Initialize(ref pra);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref pra, joined, writeLock);
				}
			}

			var found = false;
			string sql;
			if (!joined)
				sql = "SELECT * FROM pratiche WHERE pra_codice = ?";
			else
				sql = GetJoinQuery() + " WHERE pra_codice = ?";
			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;
			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (pra != null) DbUtils.SqlRead(ref reader, ref pra, joined ? null : ExcludeFields );
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref PraticheDb pra, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref pra);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new PraticheDb();
				if (!Search(ref cmd, pra.pra_codice, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.pra_last_update != pra.pra_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref pra, "pretiche", null, InsExcludeFields);
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								DbUtils.SqlRead(ref reader, ref pra, ExcludeFields);
							}
							reader.Close();
							if (joined) Reload(ref cmd, ref pra, true);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								pra.pra_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref pra, "pratiche", "WHERE pra_codice = ?", ExcludeFields);
					cmd.Parameters.Add("@codice", OdbcType.BigInt).Value = pra.pra_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref pra, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM pratiche WHERE ges_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = pra.pra_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref PraticheDb pra, bool joined)
		{
			if (!Search(ref cmd, pra.pra_codice, ref pra, joined))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
