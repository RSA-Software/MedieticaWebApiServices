using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class CommercialiDb
	{
		public long cmr_codice { get; set; }
		public string cmr_cognome { get; set; }
		public string cmr_nome { get; set; }
		public string cmr_desc { get; set; }
		public string cmr_tel1 { get; set; }
		public string cmr_tel2 { get; set; }
		public string cmr_cell { get; set; }
		public string cmr_email { get; set; }
		public DateTime? cmr_created_at { get; set; }
		public DateTime cmr_last_update { get; set; }
		public int cmr_user { get; set; }


		public CommercialiDb()
		{
			var cmr_db = this;
			DbUtils.Initialize(ref cmr_db);
		}

		private static readonly List<string> InsExcludeFields = new List<string>() { "cmr_codice",};
		private static readonly List<string> ExcludeFields = new List<string>() { "" };

		private static readonly string JoinQuery = @"
		SELECT commerciali.*
		FROM commerciali
		";

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}



		public static bool Search(ref OdbcCommand cmd, long codice, ref CommercialiDb cmr, bool joined = false,  bool writeLock = false)
		{
			if (cmr != null) DbUtils.Initialize(ref cmr);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref cmr, joined, writeLock);
				}
			}

			var found = false;
			string sql;
			if (!joined)
				sql = "SELECT * FROM commerciali WHERE cmr_codice = ?";
			else
				sql = GetJoinQuery() + " WHERE cmr_codice = ?";
			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;
			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (cmr != null) DbUtils.SqlRead(ref reader, ref cmr, joined ? null : ExcludeFields );
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref CommercialiDb cmr, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref cmr);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new CommercialiDb();
				if (!Search(ref cmd, cmr.cmr_codice, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.cmr_last_update != cmr.cmr_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(cmr.cmr_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({cmr.cmr_codice}) : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref cmr, "commerciali", null, InsExcludeFields);
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								DbUtils.SqlRead(ref reader, ref cmr, ExcludeFields);
							}
							reader.Close();
							if (joined) Reload(ref cmd, ref cmr, true);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								cmr.cmr_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref cmr, "commerciali", "WHERE cmr_codice = ?", ExcludeFields);
					cmd.Parameters.Add("@codice", OdbcType.BigInt).Value = cmr.cmr_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref cmr, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM commerciali WHERE cmr_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = cmr.cmr_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref CommercialiDb cmr, bool joined)
		{
			if (!Search(ref cmd, cmr.cmr_codice, ref cmr, joined))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
