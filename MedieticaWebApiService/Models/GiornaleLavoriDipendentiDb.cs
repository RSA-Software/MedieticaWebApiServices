using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class GiornaleLavoriDipendentiDb
	{
		public long gdi_codice { get; set; }
		public int gdi_gio { get; set; }
		public int gdi_dit { get; set; }
		public int gdi_dip { get; set; }
		public int gdi_dip_dit { get; set; }
		public DateTime? gdi_created_at { get; set; }
		public DateTime? gdi_last_update { get; set; }
		public int gdi_utente { get; set; }

		//
		// Campi Relazionati
		//
		public string dip_desc { get; set; }
		public string dip_codfis { get; set; }
		public string dit_desc { get; set; }
		public string img_data { get; set; }

		private static readonly List<string> ExcludeFields = new List<string>() {"dip_desc", "dip_codfis", "dit_desc", "img_data"};

		private static readonly List<string> ExcludeInsertFields = new List<string>() { "gdi_codice", "dip_desc", "dip_codfis", "dit_desc", "img_data" };

		private static readonly string JoinQuery = @"
		SELECT giornaledip.*, dip_desc, dip_codfis, dit_desc, img_data
		FROM giornaledip
		LEFT JOIN dipendenti ON (gdi_dip_dit = dip_dit AND gdi_dip = dip_codice)
		LEFT JOIN ditte ON (gdi_dip_dit = dit_codice)
		LEFT JOIN imgdipendenti ON gdi_dip_dit = img_dit AND gdi_dip = img_codice AND img_formato = 1
		";

		public GiornaleLavoriDipendentiDb()
		{
			var gdi_db = this;
			DbUtils.Initialize(ref gdi_db);
		}
		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static bool Search(ref OdbcCommand cmd, long codice, ref GiornaleLavoriDipendentiDb gdi, bool joined = false, bool writeLock = false)
		{
			if (gdi != null) DbUtils.Initialize(ref gdi);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref gdi, joined, writeLock);
				}
			}

			var found = false;
			string sql;
			if (!joined)
				sql = "SELECT * FROM giornaledip WHERE gdi_codice = ?";
			else
				sql = GetJoinQuery() + " WHERE gdi_codice = ?";
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;
			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (gdi != null) DbUtils.SqlRead(ref reader, ref gdi, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref GiornaleLavoriDipendentiDb gdi, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref gdi);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new GiornaleLavoriDipendentiDb();
				if (!Search(ref cmd, gdi.gdi_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.gdi_last_update != gdi.gdi_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, gdi.gdi_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				if (!DitteDb.Search(ref cmd, gdi.gdi_dip_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				DipendentiDb dip = null;
				if (!DipendentiDb.Search(ref cmd, gdi.gdi_dit, gdi.gdi_dip, ref dip)) throw new MCException(MCException.DipendenteMsg, MCException.DipendenteErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref gdi, "giornaledip", null, ExcludeInsertFields);
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref gdi, "giornaledip", "WHERE gdi_codice = ?", ExcludeFields);
								cmd.Parameters.Add("codice", OdbcType.BigInt).Value = gdi.gdi_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({gdi.gdi_dit} - {gdi.gdi_dip} - {gdi.gdi_codice})", MCException.DuplicateErr);
								throw;
							}
						}
						else throw;
					}
					break;

				case DbMessage.DB_INSERT:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref gdi, "giornaledip", null, ExcludeInsertFields);
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref gdi, joined);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref gdi, "giornaledip", "WHERE gdi_codice = ?", ExcludeFields);
					cmd.Parameters.Add("codice", OdbcType.BigInt).Value = gdi.gdi_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref gdi, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM giornaledip WHERE gdi_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = gdi.gdi_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref GiornaleLavoriDipendentiDb gdi, bool joined)
		{
			if (!Search(ref cmd, gdi.gdi_codice, ref gdi, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
