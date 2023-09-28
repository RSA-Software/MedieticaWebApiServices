using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class DipendentiCantieriDb
	{
		public int dic_dit { get; set; }
		public int dic_can { get; set; }
		public int dic_dip { get; set; }
		public DateTime? dic_created_at { get; set; }
		public DateTime? dic_last_update { get; set; }

		//
		// Tabelle Relazionate
		//
		public string img_data { get; set; }
		public string can_desc { get; set; }
		public string dip_desc { get; set; }
	

		private static readonly List<string> ExcludeFields = new List<string>() { "img_data", "can_desc", "dip_desc" };

		private static readonly string JoinQuery = @"
		SELECT dipcantieri.*, can_desc, dip_desc, img_data 
		FROM dipcantieri
		LEFT JOIN cantieri ON (dic_dit = can_dit AND dic_can = can_codice)
		LEFT JOIN dipendenti ON (dic_dit = dip_dit AND dic_dip = dip_codice)
		LEFT JOIN imgdipendenti ON (dic_dit = img_dit AND dic_dip = img_codice AND img_formato = 1)";

		private static readonly string CountJoinQuery = @"
		SELECT COUNT(*)
		FROM dipcantieri
		LEFT JOIN cantieri ON (dic_dit = can_dit AND dic_can = can_codice)
		LEFT JOIN dipendenti ON (dic_dit = dip_dit AND dic_dip = dip_codice)";

		public DipendentiCantieriDb()
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

		public static bool Search(ref OdbcCommand cmd, int coddit, int codcan, int coddip, ref DipendentiCantieriDb dic, bool joined = false, bool writeLock = false)
		{
			if (dic != null) DbUtils.Initialize(ref dic);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, coddit, codcan, coddip, ref dic, writeLock);
				}
			}

			var found = false;
			string sql;

			if (joined)
				sql = JoinQuery + " WHERE dic_dit = ? AND dic_can = ? AND dic_dip = ?";
			else
				sql = "SELECT * FROM dipcantieri WHERE dic_dit = ? AND dic_can = ? AND dic_dip = ?";
			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
		
			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = coddit;
			cmd.Parameters.Add("codcan", OdbcType.Int).Value = codcan;
			cmd.Parameters.Add("coddip", OdbcType.Int).Value = coddip;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (dic != null) DbUtils.SqlRead(ref reader, ref dic, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}
	
		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref DipendentiCantieriDb dic, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref dic);

			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR || msg == DbMessage.DB_REWRITE)
			{
				var old = new DipendentiCantieriDb();
				if (!Search(ref cmd, dic.dic_dit, dic.dic_can, dic.dic_dip, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.dic_last_update != dic.dic_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, dic.dic_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				CantieriDb can = null;
				if (!CantieriDb.Search(ref cmd, dic.dic_dit, dic.dic_can, ref can)) throw new MCException(MCException.CantiereMsg, MCException.CantiereErr);

				DipendentiDb dip = null;
				if (!DipendentiDb.Search(ref cmd, dic.dic_dit, dic.dic_dip, ref dip)) throw new MCException(MCException.DipendenteMsg, MCException.DipendenteErr);
			}

			switch (msg)
			{
					case DbMessage.DB_INSERT:
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref dic, "dipcantieri", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref dic, joined);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								Reload(ref cmd, ref dic, joined);
								return;
							};
							throw;
						}
					}
					break;

				case DbMessage.DB_UPDATE:
				case DbMessage.DB_REWRITE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dic, "dipcantieri", "WHERE dic_dit = ? AND dic_can = ? AND dic_dip = ?", ExcludeFields);
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = dic.dic_dit;
					cmd.Parameters.Add("codcan", OdbcType.Int).Value = dic.dic_can;
					cmd.Parameters.Add("coddip", OdbcType.Int).Value = dic.dic_dip;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref dic, joined);
					break;

				case DbMessage.DB_DELETE:
				case DbMessage.DB_CLEAR:
					cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM dipcantieri WHERE dic_dit = ? AND dic_can = ? AND dic_dip = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = dic.dic_dit;
					cmd.Parameters.Add("codcan", OdbcType.Int).Value = dic.dic_can;
					cmd.Parameters.Add("coddip", OdbcType.Int).Value = dic.dic_dip;
					cmd.ExecuteNonQuery();
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref DipendentiCantieriDb dic, bool joined)
		{
			if (!Search(ref cmd, dic.dic_dit, dic.dic_can, dic.dic_dip, ref dic, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
