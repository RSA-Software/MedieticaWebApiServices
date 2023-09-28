using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class UtentiDitteDb
	{
		public int utd_dit { get; set; }
		public int utd_ute { get; set; }
		public bool utd_default { get; set; }
		public DateTime? utd_created_at { get; set; }
		public DateTime? utd_last_update { get; set; }

		//
		// Tabelle Relazionate
		//
		public string img_data { get; set; }
		public string dit_desc { get; set; }
		public string dit_indirizzo { get; set; }
		public string dit_citta { get; set; }
		public string dit_cap { get; set; }
		public string dit_prov { get; set; }
		public string dit_piva { get; set; }
		public string dit_codfis { get; set; }
		

		private static readonly List<string> ExcludeFields = new List<string>() { "img_data", "dit_desc", "dit_indirizzo", "dit_citta", "dit_cap", "dit_prov", "dit_piva", "dit_codfis" };

		private static readonly string JoinQuery = @"
		SELECT uteditte.*, dit_desc, dit_indirizzo, dit_citta, dit_cap, dit_prov, dit_piva, dit_codfis, img_data 
		FROM uteditte
		LEFT JOIN ditte ON utd_dit = dit_codice
		LEFT JOIN imgditte ON utd_dit = utd_dit AND utd_dit = img_codice AND img_formato = 1";

		private static readonly string CountJoinQuery = @"
		SELECT COUNT(*)
		FROM uteditte
		INNER JOIN ditte ON utd_dit = dit_codice";

		public UtentiDitteDb()
		{
			var utd_db = this;
			DbUtils.Initialize(ref utd_db);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}

		public static string GetCountJoinQuery()
		{
			return (CountJoinQuery);
		}

		public static bool Search(ref OdbcCommand cmd, int codute, int coddit, ref UtentiDitteDb utd, bool joined = false, bool writeLock = false)
		{
			if (utd != null) DbUtils.Initialize(ref utd);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codute, coddit, ref utd, writeLock);
				}
			}

			var found = false;
			string sql;

			if (joined)
				sql = JoinQuery + " WHERE utd_dit = ? AND utd_ute = ?";
			else
				sql = "SELECT * FROM uteditte WHERE utd_dit = ? AND utd_ute = ?";
			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
		
			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = coddit;
			cmd.Parameters.Add("codute", OdbcType.Int).Value = codute;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (utd != null) DbUtils.SqlRead(ref reader, ref utd, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}
	
		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref UtentiDitteDb utd, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref utd);

			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR || msg == DbMessage.DB_REWRITE)
			{
				var old = new UtentiDitteDb();
				if (!Search(ref cmd, utd.utd_ute, utd.utd_dit, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.utd_last_update != utd.utd_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				UtentiDb ute = null;
				if (!UtentiDb.Search(ref cmd, utd.utd_ute, ref ute)) throw new MCException(MCException.UtenteMsg, MCException.UtenteErr);

				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, utd.utd_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);
			}

			switch (msg)
			{
					case DbMessage.DB_INSERT:
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref utd, "uteditte", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref utd, joined);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex)) throw new MCException(MCException.DuplicateMsg, MCException.DuplicateErr);
							throw;
						}
					}
					break;

				case DbMessage.DB_UPDATE:
				case DbMessage.DB_REWRITE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref utd, "uteditte", "WHERE utd_dit = ? AND utd_ute = ?", ExcludeFields);
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = utd.utd_dit;
					cmd.Parameters.Add("codute", OdbcType.Int).Value = utd.utd_ute;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref utd, joined);
					break;

				case DbMessage.DB_DELETE:
				case DbMessage.DB_CLEAR:
					cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM uteditte WHERE utd_dit = ? AND utd_ute = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = utd.utd_dit;
					cmd.Parameters.Add("codute", OdbcType.Int).Value = utd.utd_ute;
					cmd.ExecuteNonQuery();
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref UtentiDitteDb utd, bool joined)
		{
			if (!Search(ref cmd, utd.utd_ute, utd.utd_dit, ref utd, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
