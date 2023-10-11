using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class SociDb
	{
		public long soc_codice { get; set; }
		public long soc_cli { get; set; }
		public string soc_desc { get; set; }
		public DateTime? soc_data_nascita { get; set; }
		public string soc_luogo_nascita { get; set; }
		public string soc_prov_nascita { get; set; }
		public string soc_cap_nascita { get; set; }
		public string soc_codfis { get; set; }
		public bool soc_esposto { get; set; }
		public bool soc_disponibile { get; set; }
		public double soc_percentuale { get; set; }
		public short soc_funzione { get; set; }
		public string soc_note { get; set; }
		public DateTime? soc_created_at { get; set; }
		public DateTime? soc_last_update { get; set; }
		public int soc_user { get; set; }


		public SociDb()
		{
			var soc_db = this;
			DbUtils.Initialize(ref soc_db);
		}

		private static readonly List<string> InsExcludeFields = new List<string>() { "soc_codice", "sco_desc" };
		private static readonly List<string> ExcludeFields = new List<string>() { "" };

		private static readonly string JoinQuery = @"
		SELECT soci.*
		FROM soci
		";

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}



		public static bool Search(ref OdbcCommand cmd, long codice, ref SociDb soc, bool joined = false,  bool writeLock = false)
		{
			if (soc != null) DbUtils.Initialize(ref soc);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref soc, joined, writeLock);
				}
			}

			var found = false;
			string sql;
			if (!joined)
				sql = "SELECT * FROM soci WHERE soc_codice = ?";
			else
				sql = GetJoinQuery() + " WHERE soc_codice = ?";
			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;
			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (soc != null) DbUtils.SqlRead(ref reader, ref soc, joined ? null : ExcludeFields );
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref SociDb soc, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref soc);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new SociDb();
				if (!Search(ref cmd, soc.soc_codice, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.soc_last_update != soc.soc_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(soc.soc_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({soc.soc_codice}) : desc", MCException.CampoObbligatorioErr);

				ClientiDb cli = null;
				if (soc.soc_cli == 0 || !ClientiDb.Search(ref cmd, soc.soc_cli, ref cli)) throw new MCException(MCException.ClientiMsg, MCException.ClientiErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref soc, "soci", null, InsExcludeFields);
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								DbUtils.SqlRead(ref reader, ref soc, ExcludeFields);
							}
							reader.Close();
							if (joined) Reload(ref cmd, ref soc, true);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								soc.soc_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref soc, "soci", "WHERE soc_codice = ?", ExcludeFields);
					cmd.Parameters.Add("@codice", OdbcType.BigInt).Value = soc.soc_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref soc, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM soci WHERE soc_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = soc.soc_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref SociDb soc, bool joined)
		{
			if (!Search(ref cmd, soc.soc_codice, ref soc, joined))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
