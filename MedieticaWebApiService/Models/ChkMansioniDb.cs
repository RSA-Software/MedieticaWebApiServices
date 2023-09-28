using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class ChkMansioniDb
	{
		public int mac_man { get; set; }
		public int mac_chk { get; set; }
		public DateTime? mac_created_at { get; set; }
		public DateTime? mac_last_update { get; set; }
		public int mac_user { get; set; }

		//
		// Campi relazionati
		//
		public string chk_desc{ get; set; }
		public short chk_settore { get; set; }

		private static readonly List<string> ExcludeFields = new List<string>() { "chk_desc", "chk_settore" };

		private static readonly string JoinQuery = @"
		SELECT chkmansioni.*, chk_desc, chk_settore
		FROM chkmansioni
		INNER JOIN checklist ON mac_chk = chk_codice
		";
		
		public ChkMansioniDb()
		{
			var chk_db = this;
			DbUtils.Initialize(ref chk_db);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}


		public static bool Search(ref OdbcCommand cmd, int codMan, int codChk, ref ChkMansioniDb mac, bool joined = false, bool writeLock = false)
		{
			if (mac != null) DbUtils.Initialize(ref mac);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codMan, codChk, ref mac, joined, writeLock);
				}
			}

			var found = false;
			string sql;

			if (joined)
				sql = JoinQuery + " WHERE mac_man = ? AND mac_chk = ?";
			else
				sql = "SELECT * FROM chkmansioni WHERE mac_man = ? AND mac_chk = ?";
			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codman", OdbcType.Int).Value = codMan;
			cmd.Parameters.Add("codchk", OdbcType.Int).Value = codChk;
	
			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (mac != null) DbUtils.SqlRead(ref reader, ref mac, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref ChkMansioniDb mac, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref mac);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new ChkMansioniDb();
				if (!Search(ref cmd, mac.mac_man, mac.mac_chk, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.mac_last_update != mac.mac_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				CheckListDb chk = null;
				if (!CheckListDb.Search(ref cmd, mac.mac_chk, ref chk)) throw new MCException(MCException.CheckListMsg, MCException.CheckListErr);

				MansioniDb man = null;
				if (!MansioniDb.Search(ref cmd, mac.mac_man, ref man)) throw new MCException(MCException.MansioneMsg, MCException.MansioneErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref mac, "chkmansioni", null, ExcludeFields);
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref mac, "chkmansioni", "WHERE mac_man = ? AND mac_chk = ?", ExcludeFields);
								cmd.Parameters.Add("codman", OdbcType.Int).Value = mac.mac_man;
								cmd.Parameters.Add("codchk", OdbcType.Int).Value = mac.mac_chk;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({mac.mac_man} - {mac.mac_chk})", MCException.DuplicateErr);
								throw;
							}
						}
						else throw;
					}
					break;

				case DbMessage.DB_INSERT:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref mac, "chkmansioni", null, ExcludeFields);
						cmd.ExecuteNonQuery();
						Reload(ref cmd, ref mac, joined);
					}
					catch (OdbcException ex)
					{
						if (!DbUtils.IsDupKeyErr(ex)) throw;
						Reload(ref cmd, ref mac, joined);
					}
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref mac, "chkmansioni", "WHERE mac_man = ? AND mac_chk = ?", ExcludeFields);
					cmd.Parameters.Add("codman", OdbcType.Int).Value = mac.mac_man;
					cmd.Parameters.Add("codchk", OdbcType.Int).Value = mac.mac_chk;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref mac, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM chkmansioni WHERE mac_man = ? AND mac_chk = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codman", OdbcType.Int).Value = mac.mac_man;
						cmd.Parameters.Add("codchk", OdbcType.Int).Value = mac.mac_chk;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref ChkMansioniDb mac, bool joined)
		{
			if (!Search(ref cmd, mac.mac_man, mac.mac_chk, ref mac, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
