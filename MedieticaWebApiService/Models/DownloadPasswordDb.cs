using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class DownloadPasswordDb
	{
		public long pwd_codice { get; set; }
		public int pwd_dit { get; set; }
		public string pwd_password { get; set; }
		public DateTime? pwd_scadenza { get; set; }
		public DateTime? pwd_created_at { get; set; }
		public DateTime? pwd_last_update { get; set; }
		public int pwd_utente { get; set; }

		private static readonly List<string> ExcludeInsertFields = new List<string>() { "pwd_codice" };

		public DownloadPasswordDb()
		{
			var pwd_db = this;
			DbUtils.Initialize(ref pwd_db);
		}

		public static bool Search(ref OdbcCommand cmd, long codice, ref DownloadPasswordDb pwd, bool writeLock = false)
		{
			if (pwd != null) DbUtils.Initialize(ref pwd);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref pwd, writeLock);
				}
			}

			var found = false;
			var sql = "SELECT * FROM downloadpwd WHERE pwd_codice = ?";
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;
			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (pwd != null) DbUtils.SqlRead(ref reader, ref pwd);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref DownloadPasswordDb pwd, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref pwd);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new DownloadPasswordDb();
				if (!Search(ref cmd, pwd.pwd_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.pwd_last_update != pwd.pwd_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, pwd.pwd_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref pwd, "downloadpwd", null, ExcludeInsertFields);
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						DbUtils.SqlRead(ref reader, ref pwd);
					}
					reader.Close(); 
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref pwd, "downloadpwd", "WHERE pwd_codice = ?");
					cmd.Parameters.Add("codice", OdbcType.BigInt).Value = pwd.pwd_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref pwd);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM downloadpwd WHERE pwd_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = pwd.pwd_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref DownloadPasswordDb gdi)
		{
			if (!Search(ref cmd, gdi.pwd_codice, ref gdi)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
