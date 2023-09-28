using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class DocDitteDb
	{
		public int dod_dit { get; set; }
		public int dod_codice { get; set; }
		public short dod_livello { get; set; }
		public short dod_settore { get; set; }
		public DateTime? dod_data { get; set; }
		public string dod_desc { get; set; }
		public string dod_url { get; set; }
		public DateTime? dod_data_rilascio { get; set; }
		public DateTime? dod_data_scadenza { get; set; }
		public short dod_scad_alert_before { get; set; }
		public short dod_scad_alert_after { get; set; }
		public DateTime? dod_created_at { get; set; }
		public DateTime? dod_last_update { get; set; }

		//
		// Campi Relazionati
		//
		public long allegati { get; set; }
		public string tdo_desc { get; set; }		// Tabella Tipo documenti da realizzare

		private static readonly List<string> ExcludeFields = new List<string>() { "allegati", "tdo_desc" };
		
		//
		// Attenzione alla tabella dei documenti va messa in JOIN con la tabella dei tipi di documenti 
		//
		private static readonly string JoinQuery = @"
		SELECT docditte.*, '' AS tdo_desc, (SELECT COUNT(*) FROM allegati WHERE all_dit = docditte.dod_dit AND all_type_dod = docditte.dod_codice) AS allegati
		FROM docditte";

		private static readonly string CountQuery = @"
		SELECT COUNT(*)
		FROM docditte";
		
		public DocDitteDb()
		{
			var dod_db = this;
			DbUtils.Initialize(ref dod_db);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}
		public static string GetCountQuery()
		{
			return (CountQuery);
		}

		public static string GetTableDescription()
		{
			return ("Documenti Ditte");
		}

		public static bool Search(ref OdbcCommand cmd, int codDit, int codice, ref DocDitteDb dod, bool joined = false, bool writeLock = false)
		{
			if (dod != null) DbUtils.Initialize(ref dod);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codice, ref dod, joined, writeLock);
				}
			}

			var found = false;
			string sql;
			if (joined)
				sql = JoinQuery + " WHERE dod_dit = ? AND dod_codice = ?";
			else
				sql = "SELECT * FROM docditte WHERE dod_dit = ? AND dod_codice = ?";
			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql,1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (dod != null) DbUtils.SqlRead(ref reader, ref dod, joined ? null : GetJoinExcludeFields());
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref DocDitteDb dod, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref dod);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new DocDitteDb();
				if (!Search(ref cmd, dod.dod_dit, dod.dod_codice, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.dod_last_update != dod.dod_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, dod.dod_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref dod, "docditte", null, ExcludeFields);
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dod, "docditte", "WHERE dod_dit = ? AND dod_codice = ?", ExcludeFields);
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = dod.dod_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = dod.dod_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({dod.dod_dit} - {dod.dod_codice})", MCException.DuplicateErr);
								throw;
							}
						}
						else throw;
					}
					break;

				case DbMessage.DB_ADD:
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref dod, "docditte", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref dod, joined);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								dod.dod_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dod, "docditte", "WHERE dod_dit = ? AND dod_codice = ?", ExcludeFields);
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = dod.dod_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = dod.dod_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref dod, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM docditte WHERE dod_dit = ? AND dod_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dod.dod_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dod.dod_codice;
						cmd.ExecuteNonQuery();
					}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref DocDitteDb dod, bool joined)
		{
			if (!Search(ref cmd, dod.dod_dit, dod.dod_codice, ref dod, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}
	}
}
