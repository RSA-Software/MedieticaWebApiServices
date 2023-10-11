using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class CommercialistiDb
	{
		public long cmm_codice { get; set; }
		public long cmm_cli { get; set; }
		public string cmm_cognome { get; set; }
		public string cmm_nome { get; set; }
		public string cmm_desc { get; set; }
		public string cmm_citta { get; set; }
		public string cmm_indirizzo { get; set; }
		public string cmm_prov { get; set; }
		public string cmm_cap { get; set; }
		public string cmm_tel1 { get; set; }
		public string cmm_tel2 { get; set; }
		public string cmm_cell { get; set; }
		public string cmm_email { get; set; }
		public DateTime? cmm_created_at { get; set; }
		public DateTime cmm_last_update { get; set; }
		public int cmm_user { get; set; }


		public CommercialistiDb()
		{
			var cmm_db = this;
			DbUtils.Initialize(ref cmm_db);
		}

		private static readonly List<string> InsExcludeFields = new List<string>() { "cmm_codice",};
		private static readonly List<string> ExcludeFields = new List<string>() { "" };

		private static readonly string JoinQuery = @"
		SELECT commercialisti.*
		FROM commercialisti
		";

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}



		public static bool Search(ref OdbcCommand cmd, long codice, ref CommercialistiDb cmm, bool joined = false,  bool writeLock = false)
		{
			if (cmm != null) DbUtils.Initialize(ref cmm);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref cmm, joined, writeLock);
				}
			}

			var found = false;
			string sql;
			if (!joined)
				sql = "SELECT * FROM commercialisti WHERE cmm_codice = ?";
			else
				sql = GetJoinQuery() + " WHERE cmm_codice = ?";
			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;
			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (cmm != null) DbUtils.SqlRead(ref reader, ref cmm, joined ? null : ExcludeFields );
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref CommercialistiDb cmm, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref cmm);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new CommercialistiDb();
				if (!Search(ref cmd, cmm.cmm_codice, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.cmm_last_update != cmm.cmm_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(cmm.cmm_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({cmm.cmm_codice}) : desc", MCException.CampoObbligatorioErr);

				ClientiDb cli = null;
				if (cmm.cmm_cli == 0 || !ClientiDb.Search(ref cmd, cmm.cmm_cli, ref cli)) throw new MCException(MCException.ClientiMsg, MCException.ClientiErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref cmm, "commercialisti", null, InsExcludeFields);
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								DbUtils.SqlRead(ref reader, ref cmm, ExcludeFields);
							}
							reader.Close();
							if (joined) Reload(ref cmd, ref cmm, true);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								cmm.cmm_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref cmm, "commercialisti", "WHERE cmm_codice = ?", ExcludeFields);
					cmd.Parameters.Add("@codice", OdbcType.BigInt).Value = cmm.cmm_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref cmm, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM commercialisti WHERE cmm_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = cmm.cmm_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref CommercialistiDb cmm, bool joined)
		{
			if (!Search(ref cmd, cmm.cmm_codice, ref cmm, joined))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
