using System;
using System.Collections.Generic;
using System.Data.Odbc;
using Chilkat;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class FornitoriDb
	{
		public long for_codice { get; set; }
		public string for_rag_soc1 { get; set; }
		public string for_rag_soc2 { get; set; }
		public string for_desc { get; set; }
		public string for_indirizzo { get; set; }
		public string for_citta { get; set; }
		public string for_cap { get; set; }
		public string for_note { get; set; }
		public string for_prov { get; set; }
		public string for_piva { get; set; }
		public string for_codfis { get; set; }
		public string for_email { get; set; }
		public string for_pec { get; set; }
		public string for_tel1 { get; set; }
		public string for_tel2 { get; set; }
		public string for_cel { get; set; }
		public DateTime? for_created_at { get; set; }
		public DateTime? for_last_update { get; set; }
		public int for_user { get; set; }

		//
		// Campi Relazionati
		//
		public string img_data { get; set; }
		public List<ImgFornitoriDb> img_list { get; set; }

		private static readonly List<string> ExcludeFields = new List<string>() { "img_data", "img_list" };
		private static readonly List<string> ExcludeInsertFields = new List<string>() { "for_codice", "img_data", "img_list" };

		private static readonly string JoinQuery = @"
		SELECT *, img_data, NULL AS img_list
		FROM fornitori
		LEFT JOIN imgfornitori ON for_codice = img_dit AND for_codice = img_codice AND img_formato = 1";

		private static readonly string CountQuery = @"
		SELECT COUNT(*) 
		FROM fornitori";
		
		public FornitoriDb()
		{
			var dit_db = this;
			DbUtils.Initialize(ref dit_db);
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


		public static bool Search(ref OdbcCommand cmd, long codice, ref FornitoriDb forn, bool joined = false, bool writeLock = false)
		{
			if (forn != null) DbUtils.Initialize(ref forn);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref forn, joined, writeLock);
				}
			}

			var found = false;

			string sql;
			if (joined)
				sql = GetJoinQuery() + " WHERE for_codice = ?";
			else
				sql = "SELECT * FROM fornitori WHERE for_codice = ?";

			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql,1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (forn != null) DbUtils.SqlRead(ref reader, ref forn, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref FornitoriDb forn, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref forn);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new FornitoriDb();
				if (!Search(ref cmd, forn.for_codice, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.for_last_update != forn.for_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}
			
			forn.for_desc = (forn.for_rag_soc1.Trim() + " " + forn.for_rag_soc2.Trim()).Trim();
			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(forn.for_rag_soc1)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({forn.for_codice}) : rag_soc1", MCException.CampoObbligatorioErr);
				if (string.IsNullOrWhiteSpace(forn.for_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({forn.for_codice}) : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref forn, "fornitori", null, ExcludeInsertFields);
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								DbUtils.SqlRead(ref reader, ref forn, ExcludeFields);
							}
							reader.Close();
							if (joined)	Reload(ref cmd, ref forn, true);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								forn.for_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
				{
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref forn, "fornitori", "WHERE for_codice = ?", ExcludeFields);
					cmd.Parameters.Add("@codice", OdbcType.BigInt).Value = forn.for_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref forn, joined);
				}
				break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						/*
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM allegati WHERE all_dit = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dit.dit_codice;
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM imgditte WHERE img_dit = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dit.dit_codice; 
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM docditte WHERE dod_dit = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dit.dit_codice;
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM sedditte WHERE sed_dit = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dit.dit_codice;
						cmd.ExecuteNonQuery();
						*/
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM fornitori WHERE for_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = forn.for_codice;
						cmd.ExecuteNonQuery();
					}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref FornitoriDb forn, bool joined)
		{
			if (!Search(ref cmd, forn.for_codice, ref forn, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
