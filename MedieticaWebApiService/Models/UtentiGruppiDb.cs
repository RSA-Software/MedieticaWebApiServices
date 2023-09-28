using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{

	public class UtentiGruppiDb
	{
		public int usg_codice { get; set; }
		public string usg_desc { get; set; }
		public DateTime? usg_created_at { get; set; }
		public DateTime? usg_last_update { get; set; }

		public UtentiGruppiDb()
		{
			var usg_db = this;
			DbUtils.Initialize(ref usg_db);
		}
		public static bool Search(ref OdbcCommand cmd, int codice, ref UtentiGruppiDb usg, bool writeLock = false)
		{
			if (usg != null) DbUtils.Initialize(ref usg);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref usg, writeLock);
				}
			}

			var found = false;
			var	sql = DbUtils.QueryAdapt("SELECT * FROM usergroups WHERE usg_codice = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("@codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (usg != null) DbUtils.SqlRead(ref reader, ref usg);
				found = true;
			}
			reader.Close();
			return (found);
		}
	
		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref UtentiGruppiDb usg, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref usg);

			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR || msg == DbMessage.DB_REWRITE)
			{
				var old = new UtentiGruppiDb();
				if (!Search(ref cmd, usg.usg_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.usg_last_update != usg.usg_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}
			
			switch (msg)
			{
					case DbMessage.DB_INSERT:
					{
						var idx = 0;
						do
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref usg, "usergroups");
								cmd.ExecuteNonQuery();
								Reload(ref cmd, ref usg, joined);
							}
							catch (OdbcException ex)
							{
								if (DbUtils.IsDupKeyErr(ex))
								{
									idx++;
									usg.usg_codice++;
									continue;
								}
								throw;
							}
							break;
						} while (true && idx <= 10);
					}
					break;

				case DbMessage.DB_UPDATE:
				case DbMessage.DB_REWRITE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref usg, "usergroups", "WHERE usg_codice = ?");
					cmd.Parameters.Add("@numero", OdbcType.Int).Value = usg.usg_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref usg, joined);
					break;

				case DbMessage.DB_DELETE:
				case DbMessage.DB_CLEAR:
					if (usg.usg_codice == 1) throw new MCException(MCException.CancelMsg, MCException.CancelErr);

					cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM usergroups WHERE usg_codice = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("@codice", OdbcType.Int).Value = usg.usg_codice;
					cmd.ExecuteNonQuery();
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref UtentiGruppiDb usg, bool joined)
		{
			if (!Search(ref cmd, usg.usg_codice, ref usg, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
