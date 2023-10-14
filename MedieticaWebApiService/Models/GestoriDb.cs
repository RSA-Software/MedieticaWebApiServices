using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class GestoriDb
	{
		public long ges_codice { get; set; }
		public string ges_cognome { get; set; }
		public string ges_nome { get; set; }
		public string ges_desc { get; set; }
		public string ges_tel1 { get; set; }
		public string ges_tel2 { get; set; }
		public string ges_cell { get; set; }
		public string ges_email { get; set; }
		public DateTime? ges_created_at { get; set; }
		public DateTime ges_last_update { get; set; }
		public int ges_user { get; set; }


		public GestoriDb()
		{
			var ges_db = this;
			DbUtils.Initialize(ref ges_db);
		}

		private static readonly List<string> InsExcludeFields = new List<string>() { "ges_codice",};
		private static readonly List<string> ExcludeFields = new List<string>() { "" };

		private static readonly string JoinQuery = @"
		SELECT gestori.*
		FROM gestori
		";

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}



		public static bool Search(ref OdbcCommand cmd, long codice, ref GestoriDb ges, bool joined = false,  bool writeLock = false)
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
				sql = "SELECT * FROM gestori WHERE ges_codice = ?";
			else
				sql = GetJoinQuery() + " WHERE ges_codice = ?";
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

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref GestoriDb ges, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref ges);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new GestoriDb();
				if (!Search(ref cmd, ges.ges_codice, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.ges_last_update != ges.ges_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(ges.ges_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({ges.ges_codice}) : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref ges, "gestori", null, InsExcludeFields);
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								DbUtils.SqlRead(ref reader, ref ges, ExcludeFields);
							}
							reader.Close();
							if (joined) Reload(ref cmd, ref ges, true);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								ges.ges_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref ges, "gestori", "WHERE ges_codice = ?", ExcludeFields);
					cmd.Parameters.Add("@codice", OdbcType.BigInt).Value = ges.ges_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref ges, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM gestori WHERE ges_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = ges.ges_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref GestoriDb ges, bool joined)
		{
			if (!Search(ref cmd, ges.ges_codice, ref ges, joined))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
