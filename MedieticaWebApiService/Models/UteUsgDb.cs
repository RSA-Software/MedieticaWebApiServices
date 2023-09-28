using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{

	public class UteUsgDb
	{
		public int utg_ute { get; set; }
		public int utg_usg { get; set; }
		public DateTime? utg_created_at { get; set; }
		public DateTime? utg_last_update { get; set; }

		//
		// Campi relazionati
		//
		public string usg_desc{ get; set; }

		private static readonly List<string> ExcludeFields = new List<string>() { "usg_desc" };

		private static readonly string JoinQuery = @"
		SELECT uteusg.*, usg_desc
		FROM uteusg
		LEFT JOIN usergroups ON utg_usg = usg_codice";


		public UteUsgDb()
		{
			var utg_db = this;
			DbUtils.Initialize(ref utg_db);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}


		public static bool Search(ref OdbcCommand cmd, int codUte, int codUsg, ref UteUsgDb utg, bool joined = false, bool writeLock = false)
		{
			if (utg != null) DbUtils.Initialize(ref utg);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codUte, codUsg, ref utg, joined, writeLock);
				}
			}

			string sql;
			var found = false;
			if (joined)
				sql = JoinQuery + " WHERE utg_ute = ? AND utg_usg = ?";
			else
				sql = "SELECT * FROM uteusg WHERE utg_ute = ? AND utg_usg = ?";
			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";

			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codute", OdbcType.Int).Value = codUte;
			cmd.Parameters.Add("codusg", OdbcType.Int).Value = codUsg;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (utg != null) DbUtils.SqlRead(ref reader, ref utg, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}
	
		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref UteUsgDb utg, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref utg);

			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR || msg == DbMessage.DB_REWRITE)
			{
				var old = new UteUsgDb();
				if (!Search(ref cmd, utg.utg_ute, utg.utg_usg, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.utg_last_update != utg.utg_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				UtentiDb ute = null;
				if (!UtentiDb.Search(ref cmd, utg.utg_ute, ref ute)) throw new MCException(MCException.UtenteMsg, MCException.UtenteErr);

				UtentiGruppiDb usg = null;
				if (!UtentiGruppiDb.Search(ref cmd, utg.utg_usg, ref usg)) throw new MCException(MCException.UtentiGruppoMsg, MCException.UtentiGruppoErr);
			}
			switch (msg)
			{
					case DbMessage.DB_INSERT:
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref utg, "uteusg", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref utg, joined);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref utg, "uteusg", "WHERE utg_ute = ? AND utg_usg = ?", ExcludeFields);
								cmd.Parameters.Add("codute", OdbcType.Int).Value = utg.utg_ute;
								cmd.Parameters.Add("codusg", OdbcType.Int).Value = utg.utg_usg;
								cmd.ExecuteNonQuery();
								Reload(ref cmd, ref utg, joined);
							}
							else throw;
						}
					}
					break;

				case DbMessage.DB_UPDATE:
				case DbMessage.DB_REWRITE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref utg, "uteusg", "WHERE utg_ute = ? AND utg_usg = ?", ExcludeFields);
					cmd.Parameters.Add("codute", OdbcType.Int).Value = utg.utg_ute;
					cmd.Parameters.Add("codusg", OdbcType.Int).Value = utg.utg_usg;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref utg, joined);
					break;

				case DbMessage.DB_DELETE:
				case DbMessage.DB_CLEAR:
					cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM uteusg WHERE utg_ute = ? AND utg_usg = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("codute", OdbcType.Int).Value = utg.utg_ute;
					cmd.Parameters.Add("codusg", OdbcType.Int).Value = utg.utg_usg;
					cmd.ExecuteNonQuery();
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref UteUsgDb utg, bool joined)
		{
			if (!Search(ref cmd, utg.utg_ute, utg.utg_usg, ref utg, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
