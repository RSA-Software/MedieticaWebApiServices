using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class MezziCantieriDb
	{
		public int mec_dit { get; set; }
		public int mec_can { get; set; }
		public int mec_mez { get; set; }
		public DateTime? mec_created_at { get; set; }
		public DateTime? mec_last_update { get; set; }

		//
		// Tabelle Relazionate
		//
		public string img_data { get; set; }
		public string can_desc { get; set; }
		public string mez_desc { get; set; }
	

		private static readonly List<string> ExcludeFields = new List<string>() { "img_data", "can_desc", "mez_desc" };

		private static readonly string JoinQuery = @"
		SELECT mezcantieri.*, can_desc, mez_desc, img_data 
		FROM mezcantieri
		LEFT JOIN cantieri ON (mec_dit = can_dit AND mec_can = can_codice)
		LEFT JOIN mezzi ON (mec_dit = mez_dit AND mec_mez = mez_codice)
		LEFT JOIN imgmezzi ON mec_dit = 0 AND mec_mez = img_codice AND img_formato = 1";

		private static readonly string CountJoinQuery = @"
		SELECT COUNT(*)
		FROM mezcantieri
		LEFT JOIN cantieri ON (mec_dit = can_dit AND mec_can = can_codice)
		LEFT JOIN mezzi ON (mec_dit = mez_dit AND mec_mez = mez_codice)";

		public MezziCantieriDb()
		{
			var mec_db = this;
			DbUtils.Initialize(ref mec_db);
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

		public static bool Search(ref OdbcCommand cmd, int coddit, int codcan, int codmez, ref MezziCantieriDb mec, bool joined = false, bool writeLock = false)
		{
			if (mec != null) DbUtils.Initialize(ref mec);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, coddit, codcan, codmez, ref mec, writeLock);
				}
			}

			var found = false;
			string sql;

			if (joined)
				sql = JoinQuery + " WHERE mec_dit = ? AND mec_can = ? AND mec_mez = ?";
			else
				sql = "SELECT * FROM mezcantieri WHERE mec_dit = ? AND mec_can = ? AND mec_mez = ?";
			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
		
			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = coddit;
			cmd.Parameters.Add("codcan", OdbcType.Int).Value = codcan;
			cmd.Parameters.Add("codmez", OdbcType.Int).Value = codmez;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (mec != null) DbUtils.SqlRead(ref reader, ref mec, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}
	
		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref MezziCantieriDb mec, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref mec);

			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR || msg == DbMessage.DB_REWRITE)
			{
				var old = new MezziCantieriDb();
				if (!Search(ref cmd, mec.mec_dit, mec.mec_can, mec.mec_mez, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.mec_last_update != mec.mec_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, mec.mec_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				CantieriDb can = null;
				if (!CantieriDb.Search(ref cmd, mec.mec_dit, mec.mec_can, ref can)) throw new MCException(MCException.CantiereMsg, MCException.CantiereErr);

				MezziDb mez = null;
				if (!MezziDb.Search(ref cmd, mec.mec_dit, mec.mec_mez, ref mez)) throw new MCException(MCException.MezzoMsg, MCException.MezzoErr);
			}

			switch (msg)
			{
					case DbMessage.DB_INSERT:
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref mec, "mezcantieri", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref mec, joined);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								Reload(ref cmd, ref mec, joined);
								return;
							};
							throw;
						}
					}
					break;

				case DbMessage.DB_UPDATE:
				case DbMessage.DB_REWRITE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref mec, "mezcantieri", "WHERE mec_dit = ? AND mec_can = ? AND mec_mez = ?", ExcludeFields);
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = mec.mec_dit;
					cmd.Parameters.Add("codcan", OdbcType.Int).Value = mec.mec_can;
					cmd.Parameters.Add("codmez", OdbcType.Int).Value = mec.mec_mez;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref mec, joined);
					break;

				case DbMessage.DB_DELETE:
				case DbMessage.DB_CLEAR:
					cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM mezcantieri WHERE mec_dit = ? AND mec_can = ? AND mec_mez = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = mec.mec_dit;
					cmd.Parameters.Add("codcan", OdbcType.Int).Value = mec.mec_can;
					cmd.Parameters.Add("codmez", OdbcType.Int).Value = mec.mec_mez;
					cmd.ExecuteNonQuery();
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref MezziCantieriDb mec, bool joined)
		{
			if (!Search(ref cmd, mec.mec_dit, mec.mec_can, mec.mec_mez, ref mec, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
