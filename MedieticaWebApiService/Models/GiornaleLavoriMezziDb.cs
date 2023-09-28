using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class GiornaleLavoriMezziDb
	{
		public long gme_codice { get; set; }
		public int gme_gio { get; set; }
		public int gme_dit { get; set; }
		public int gme_mez { get; set; }
		public int gme_mez_dit { get; set; }
		public DateTime? gme_created_at { get; set; }
		public DateTime? gme_last_update { get; set; }
		public int gme_utente { get; set; }

		//
		// Campi Relazionati
		//
		public string mez_desc { get; set; }
		public string mez_serial { get; set; }
		public string mez_targa { get; set; }
		public string dit_desc { get; set; }


		public string img_data { get; set; }

		private static readonly List<string> ExcludeFields = new List<string>() {"mez_desc", "mez_serial", "mez_targa", "dit_desc", "img_data"};

		private static readonly List<string> ExcludeInsertFields = new List<string>() { "gme_codice", "mez_desc", "mez_serial", "mez_targa", "dit_desc", "img_data" };

		private static readonly string JoinQuery = @"
		SELECT giornalemez.*, mez_desc, mez_serial, mez_targa, dit_desc,
		(CASE
			WHEN imz.img_data IS NOT NULL THEN imz.img_data
			ELSE imm.img_data
		END) AS img_data
		FROM giornalemez
		LEFT JOIN mezzi ON (gme_mez_dit = mez_dit AND gme_mez = mez_codice)
		LEFT JOIN ditte ON (gme_mez_dit = dit_codice)
		LEFT JOIN imgmezzi AS imz ON gme_mez_dit = imz.img_dit AND gme_mez = imz.img_codice AND imz.img_formato = 1
		LEFT JOIN imgmodelli AS imm ON mez_dit_mod = imm.img_dit AND mez_mod = imm.img_codice AND imm.img_formato = 1
		";

		public GiornaleLavoriMezziDb()
		{
			var gme_db = this;
			DbUtils.Initialize(ref gme_db);
		}
		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static bool Search(ref OdbcCommand cmd, long codice, ref GiornaleLavoriMezziDb mod, bool joined = false, bool writeLock = false)
		{
			if (mod != null) DbUtils.Initialize(ref mod);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref mod, writeLock);
				}
			}

			var found = false;
			string sql;
			if (!joined)
				sql = "SELECT * FROM giornalemez WHERE gme_codice = ?";
			else
				sql = GetJoinQuery() + " WHERE gme_codice = ?";
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;
			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (mod != null) DbUtils.SqlRead(ref reader, ref mod, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref GiornaleLavoriMezziDb gme, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref gme);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new GiornaleLavoriMezziDb();
				if (!Search(ref cmd, gme.gme_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.gme_last_update != gme.gme_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, gme.gme_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				if (!DitteDb.Search(ref cmd, gme.gme_mez_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				MezziDb mez = null;
				if (!MezziDb.Search(ref cmd, gme.gme_mez_dit, gme.gme_mez, ref mez)) throw new MCException(MCException.CantiereMsg, MCException.CantiereErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref gme, "giornalemez", null, ExcludeInsertFields);
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref gme, "giornalemez", "WHERE gme_codice = ?", ExcludeFields);
								cmd.Parameters.Add("codice", OdbcType.BigInt).Value = gme.gme_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({gme.gme_dit} - {gme.gme_mez} - {gme.gme_codice})", MCException.DuplicateErr);
								throw;
							}
						}
						else throw;
					}
					break;

				case DbMessage.DB_INSERT:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref gme, "giornalemez", null, ExcludeInsertFields);
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref gme, joined);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref gme, "giornalemez", "WHERE gme_codice = ?", ExcludeFields);
					cmd.Parameters.Add("codice", OdbcType.BigInt).Value = gme.gme_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref gme, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						/*
						cmd.CommandText = DbUtils.QueryAdapt("SELECT COUNT(*) FROM mezzi WHERE mez_dit_mod = ? AND mez_mod = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = mod.mod_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mod.mod_codice;
						var num = Convert.ToInt32(cmd.ExecuteScalar());
						if (num > 0) throw new MCException(MCException.CancelMsg, MCException.CancelErr);

						var dmo_arr = new List<DocGiornaleLavoriMezziDb>();
						cmd.CommandText = DbUtils.QueryAdapt("SELECT * from docmodelli WHERE dmo_dit = ? AND dmo_mod = ? FOR UPDATE NOWAIT");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = mod.mod_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mod.mod_codice;
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var dmo = new DocGiornaleLavoriMezziDb();
							DbUtils.SqlRead(ref reader, ref dmo);
							dmo_arr.Add(dmo);
						}
						reader.Close();

						var all_arr = new List<AllegatiDb>();
						foreach (var dmo in dmo_arr)
						{
							cmd.CommandText = DbUtils.QueryAdapt("SELECT * FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ? FOR UPDATE NOWAIT");
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = dmo.dmo_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dmo.dmo_codice;
							cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_MODELLI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								all_arr.Add(all);
							}
							reader.Close();
						}

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM imgmodelli WHERE img_dit = ? AND img_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = mod.mod_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mod.mod_codice;
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM videomodelli WHERE vmo_dit = ? AND vmo_mod = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = mod.mod_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mod.mod_codice;
						cmd.ExecuteNonQuery();
				
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM docmodelli WHERE dmo_dit = ? AND dmo_mod = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = mod.mod_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mod.mod_codice;
						cmd.ExecuteNonQuery();

						foreach (var dmo in dmo_arr)
						{
							cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ?");
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = dmo.dmo_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dmo.dmo_codice;
							cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_MODELLI;
							cmd.ExecuteNonQuery();
						}
						*/
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM giornalemez WHERE gme_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = gme.gme_codice;
						cmd.ExecuteNonQuery();

						/*
						foreach (var all in all_arr)
						{
							var upload_path = AllegatiDb.SetupPath(all.all_dit, all.all_type, all.all_doc);
							upload_path += $"/{all.all_local_fname}";
							try
							{
								File.Delete(upload_path);
							}
							catch (DirectoryNotFoundException)
							{
							}
							catch (IOException)
							{
							}
							catch (UnauthorizedAccessException)
							{
							}
						}
						*/

					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref GiornaleLavoriMezziDb gme, bool joined)
		{
			if (!Search(ref cmd, gme.gme_codice, ref gme, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
