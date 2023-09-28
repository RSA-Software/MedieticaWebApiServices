using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class MansioniDb
	{
		public int man_codice { get; set; }
		public string man_desc { get; set; }
		public short man_rischio { get; set; }
		public DateTime? man_created_at { get; set; }
		public DateTime? man_last_update { get; set; }
		public int man_user { get; set; }

		public List<ChkMansioniDb> mac_list { get; set; }
		
		private static readonly List<string> ExcludeFields = new List<string>() { "mac_list" };

		private static readonly string JoinQuery = @"
		SELECT mansioni.*, NULL AS mac_list
		FROM mansioni
		";
		
		public MansioniDb()
		{
			var man_db = this;
			DbUtils.Initialize(ref man_db);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}



		public static bool Search(ref OdbcCommand cmd, int codice, ref MansioniDb man, bool joined = false, bool writeLock = false)
		{
			if (man != null) DbUtils.Initialize(ref man);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref man, joined, writeLock);
				}
			}

			var found = false;
			string sql;
			if (joined)
				sql = GetJoinQuery() + " WHERE man_codice = ?";
			else
				sql = "SELECT * FROM mansioni WHERE man_codice = ?";
			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (man != null) DbUtils.SqlRead(ref reader, ref man, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();

			if (joined && found && man != null)
			{
				man.mac_list = new List<ChkMansioniDb>();
				cmd.CommandText = ChkMansioniDb.GetJoinQuery() + " WHERE mac_man = ? ORDER BY mac_man, mac_chk";
				cmd.Parameters.Clear();
				cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
				reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					var mac = new ChkMansioniDb();
					DbUtils.SqlRead(ref reader, ref mac);
					man.mac_list.Add(mac);
				}
				reader.Close();
			}
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref MansioniDb man, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref man);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new MansioniDb();
				if (!Search(ref cmd, man.man_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.man_last_update != man.man_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(man.man_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" {man.man_codice} : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref man, "mansioni");
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref man, "mansioni", "WHERE man_dit ? ? AND man_codice = ?");
								cmd.Parameters.Add("codice", OdbcType.Int).Value = man.man_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" {man.man_codice}", MCException.DuplicateErr);
								throw;
							}
						}
						else throw;
					}
					break;

				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref man, "mansioni", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref man, joined);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								man.man_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref man, "mansioni", "WHERE man_codice = ?", ExcludeFields);
					cmd.Parameters.Add("@codice", OdbcType.Int).Value = man.man_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref man, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM chkmansioni WHERE mac_man = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.Int).Value = man.man_codice;
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM mansioni WHERE man_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.Int).Value = man.man_codice;
						cmd.ExecuteNonQuery();
					}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref MansioniDb man, bool joined)
		{
			if (!Search(ref cmd, man.man_codice, ref man, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
