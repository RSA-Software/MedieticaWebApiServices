using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class DipSediDb
	{
		public int dse_dit { get; set; }
		public int dse_dip { get; set; }
		public int dse_sed { get; set; }
		public DateTime? dse_created_at { get; set; }
		public DateTime? dse_last_update { get; set; }
		public int dse_user { get; set; }

		//
		// Campi relazionati
		//
		public string sed_indirizzo { get; set; }
		public string sed_citta { get; set; }
		public string sed_prov { get; set; }
		public string sed_cap { get; set; }
		
		private static readonly List<string> ExcludeFields = new List<string>() { "sed_indirizzo", "sed_citta", "sed_prov", "sed_cap" };

		private static readonly string JoinQuery = @"
		SELECT dipsedi.*, sed_indirizzo, sed_citta, sed_prov, sed_cap
		FROM dipsedi
		INNER JOIN sedditte ON (dse_dit = sed_dit AND dse_sed = sed_codice)
		";
		
		public DipSediDb()
		{
			var dma_db = this;
			DbUtils.Initialize(ref dma_db);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}


		public static bool Search(ref OdbcCommand cmd, int codDit, int codDip, int codSed, ref DipSediDb dse, bool joined = false, bool writeLock = false)
		{
			if (dse != null) DbUtils.Initialize(ref dse);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codDip, codSed, ref dse, joined, writeLock);
				}
			}

			var found = false;
			string sql;

			if (joined)
				sql = JoinQuery + " WHERE dse_dit = ? AND dse_dip = ? AND dse_sed = ?";
			else
				sql = "SELECT * FROM dipsedi WHERE dse_dit = ? AND dse_dip = ? AND dse_sed = ?";
			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("coddip", OdbcType.Int).Value = codDip;
			cmd.Parameters.Add("codman", OdbcType.Int).Value = codSed;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (dse != null) DbUtils.SqlRead(ref reader, ref dse, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref DipSediDb dse, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref dse);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new DipSediDb();
				if (!Search(ref cmd, dse.dse_dit, dse.dse_dip, dse.dse_sed, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.dse_last_update != dse.dse_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, dse.dse_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				DipendentiDb dip = null;
				if (!DipendentiDb.Search(ref cmd, dse.dse_dit, dse.dse_dip, ref dip)) throw new MCException(MCException.DipendenteMsg, MCException.DipendenteErr);

				SediDitteDb sed = null;
				if (!SediDitteDb.Search(ref cmd, dse.dse_dit, dse.dse_sed, ref sed)) throw new MCException(MCException.SedeMsg, MCException.SedeErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref dse, "dipsedi", null, ExcludeFields);
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dse, "dipsedi", "WHERE dse_dit = ? AND dse_dip = ? AND dse_sed = ?", ExcludeFields);
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = dse.dse_dit;
								cmd.Parameters.Add("coddip", OdbcType.Int).Value = dse.dse_dip;
								cmd.Parameters.Add("codman", OdbcType.Int).Value = dse.dse_sed;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({dse.dse_dit} - {dse.dse_dip} - {dse.dse_sed})", MCException.DuplicateErr);
								throw;
							}
						}
						else throw;
					}
					break;

				case DbMessage.DB_INSERT:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref dse, "dipsedi", null, ExcludeFields);
						cmd.ExecuteNonQuery();
						Reload(ref cmd, ref dse, joined);
					}
					catch (OdbcException ex)
					{
						if (!DbUtils.IsDupKeyErr(ex)) throw;
						Reload(ref cmd, ref dse, joined);
					}
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dse, "dipsedi", "WHERE dse_dit = ? AND dse_dip = ? AND dse_man = ?", ExcludeFields);
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = dse.dse_dit;
					cmd.Parameters.Add("coddip", OdbcType.Int).Value = dse.dse_dip;
					cmd.Parameters.Add("codman", OdbcType.Int).Value = dse.dse_sed;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref dse, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM dipsedi WHERE dse_dit = ? AND dse_dip = ? AND dse_sed = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dse.dse_dit;
						cmd.Parameters.Add("coddip", OdbcType.Int).Value = dse.dse_dip;
						cmd.Parameters.Add("codman", OdbcType.Int).Value = dse.dse_sed;
						cmd.ExecuteNonQuery();
					}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref DipSediDb dse, bool joined)
		{
			if (!Search(ref cmd, dse.dse_dit, dse.dse_dip, dse.dse_sed, ref dse, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
