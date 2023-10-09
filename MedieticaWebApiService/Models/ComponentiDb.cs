using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class ComponentiDb
	{
		public long cfa_codice { get; set; }
		public long cfa_cli { get; set; }
		public long cfa_att { get; set; }
		public string cfa_cognome { get; set; }
		public string cfa_nome { get; set; }
		public string cfa_desc { get; set; }
		public DateTime? cfa_data_nascita { get; set; }
		public string cfa_luogo_nascita { get; set; }
		public string cfa_prov_nascita { get; set; }
		public string cfa_cap_nascita { get; set; }
		public string cfa_codfis { get; set; }
		public string cfa_parentela { get; set; }
		public DateTime? cfa_created_at { get; set; }
		public DateTime cfa_last_update { get; set; }
		public int cfa_user { get; set; }

		//
		// Joined fields
		//
		public string att_desc { get; set; }


		public ComponentiDb()
		{
			var cfa_db = this;
			DbUtils.Initialize(ref cfa_db);
		}

		private static readonly List<string> InsExcludeFields = new List<string>() { "cfa_codice", "att_desc" };
		private static readonly List<string> ExcludeFields = new List<string>() { "att_desc" };

		private static readonly string JoinQuery = @"
		SELECT componenti.*, att_desc
		FROM componenti
		LEFT JOIN attivita ON cfa_att = att.codice
		";

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}



		public static bool Search(ref OdbcCommand cmd, long codice, ref ComponentiDb cnt, bool joined = false,  bool writeLock = false)
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
			string sql;
			if (!joined)
				sql = "SELECT * FROM componenti WHERE cfa_codice = ?";
			else
				sql = GetJoinQuery() + " WHERE cfa_codice = ?";
			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;
			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (cnt != null) DbUtils.SqlRead(ref reader, ref cnt, joined ? null : ExcludeFields );
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref ComponentiDb cfa, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref cfa);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new ComponentiDb();
				if (!Search(ref cmd, cfa.cfa_codice, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.cfa_last_update != cfa.cfa_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(cfa.cfa_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({cfa.cfa_codice}) : desc", MCException.CampoObbligatorioErr);

				ClientiDb cli = null;
				if (cfa.cfa_cli == 0 || !ClientiDb.Search(ref cmd, cfa.cfa_cli, ref cli)) throw new MCException(MCException.ClientiMsg, MCException.ClientiErr);

			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref cfa, "componenti", null, InsExcludeFields);
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								DbUtils.SqlRead(ref reader, ref cfa, ExcludeFields);
							}
							reader.Close();
							if (joined) Reload(ref cmd, ref cfa, true);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								cfa.cfa_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref cfa, "componenti", "WHERE cfa_codice = ?", ExcludeFields);
					cmd.Parameters.Add("@codice", OdbcType.BigInt).Value = cfa.cfa_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref cfa, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM componenti WHERE cfa_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = cfa.cfa_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref ComponentiDb cfa, bool joined)
		{
			if (!Search(ref cmd, cfa.cfa_codice, ref cfa, joined))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
