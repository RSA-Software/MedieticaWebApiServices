using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class GiornaleLavoriDb
	{
		public int gio_dit { get; set; }
		public int gio_codice { get; set; }
		public int gio_can { get; set; }
		public DateTime? gio_data { get; set; }
		public string gio_desc { get; set; }
		public string gio_osservazioni { get; set; }
		public short gio_meteo { get; set; }
		public float gio_temperatura_min { get; set; }
		public float gio_temperatura_max { get; set; }
		public DateTime? gio_created_at { get; set; }
		public DateTime? gio_last_update { get; set; }
		public int gio_utente { get; set; }

		//
		// Campi Relazionati
		//
		public List<GiornaleLavoriMezziDb> mez_list { get; set; }
		public List<GiornaleLavoriDipendentiDb> dip_list { get; set; }
		public List<ImgGiornaliLavoriDb> img_list { get; set; }

		private static readonly List<string> ExcludeFields = new List<string>() {"mez_list", "dip_list", "img_list"};


		private static readonly string JoinQuery = @"
		SELECT *
		FROM giornalelav";

		public GiornaleLavoriDb()
		{
			var mod_db = this;
			DbUtils.Initialize(ref mod_db);
			mez_list = new List<GiornaleLavoriMezziDb>();
			dip_list = new List<GiornaleLavoriDipendentiDb>();
		}
		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static bool Search(ref OdbcCommand cmd, int dit, int codice, ref GiornaleLavoriDb gio, bool joined = false, bool writeLock = false)
		{
			if (gio != null) DbUtils.Initialize(ref gio);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, dit, codice, ref gio, joined, writeLock);
				}
			}

			var found = false;
			string sql;
			if (!joined)
				sql = "SELECT * FROM giornalelav WHERE gio_dit = ? AND gio_codice = ?";
			else
				sql = GetJoinQuery() + " WHERE gio_dit = ? AND gio_codice = ?";
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("dit", OdbcType.Int).Value = dit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (gio != null) DbUtils.SqlRead(ref reader, ref gio, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();

			if (joined && gio != null && found)
			{
				if (gio.dip_list == null) gio.dip_list = new List<GiornaleLavoriDipendentiDb>();
				if (gio.mez_list == null) gio.mez_list = new List<GiornaleLavoriMezziDb>();
				if (gio.img_list == null) gio.img_list = new List<ImgGiornaliLavoriDb>();

				sql = GiornaleLavoriMezziDb.GetJoinQuery() + " WHERE gme_dit = ? AND gme_gio = ? ORDER BY gme_gio, gme_codice";
				cmd.CommandText = sql;
				cmd.Parameters.Clear();
				cmd.Parameters.Add("dit", OdbcType.Int).Value = dit;
				cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
				reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					var gme = new GiornaleLavoriMezziDb();
					DbUtils.SqlRead(ref reader, ref gme);
					gio.mez_list.Add(gme);
				}
				reader.Close();

				sql = GiornaleLavoriDipendentiDb.GetJoinQuery() + " WHERE gdi_dit = ? AND gdi_gio = ? ORDER BY gdi_gio, gdi_codice";
				cmd.CommandText = sql;
				cmd.Parameters.Clear();
				cmd.Parameters.Add("dit", OdbcType.Int).Value = dit;
				cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
				reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					var gdi = new GiornaleLavoriDipendentiDb();
					DbUtils.SqlRead(ref reader, ref gdi);
					gio.dip_list.Add(gdi);
				}
				reader.Close();

				sql = @"
				SELECT * 
				FROM giornaleimg 
				WHERE img_dit = ? AND img_codice = ? AND Mod(img_formato, 2) <> 0
				ORDER BY img_formato";
				cmd.CommandText = sql;
				cmd.Parameters.Clear();
				cmd.Parameters.Add("ditta", OdbcType.Int).Value = dit;
				cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
				reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					var img = new ImgGiornaliLavoriDb();
					DbUtils.SqlRead(ref reader, ref img);
					gio.img_list.Add(img);
				}
				reader.Close();
			}
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref GiornaleLavoriDb gio, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref gio);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new GiornaleLavoriDb();
				if (!Search(ref cmd, gio.gio_dit, gio.gio_codice, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.gio_last_update != gio.gio_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(gio.gio_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({gio.gio_dit} - {gio.gio_can} - {gio.gio_codice}) : desc", MCException.CampoObbligatorioErr);

				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, gio.gio_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				CantieriDb can = null;
				if (!CantieriDb.Search(ref cmd, gio.gio_dit, gio.gio_can, ref can)) throw new MCException(MCException.CantiereMsg, MCException.CantiereErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref gio, "giornalelav", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref gio, joined);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								gio.gio_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref gio, "giornalelav", "WHERE gio_dit = ? AND gio_codice = ?", ExcludeFields);
					cmd.Parameters.Add("dit", OdbcType.Int).Value = gio.gio_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = gio.gio_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref gio, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						var all_arr = new List<AllegatiDb>();
						cmd.CommandText = "SELECT * FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ?";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = gio.gio_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = gio.gio_codice;
						cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_GIORNALE;
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var all = new AllegatiDb();
							DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
							all_arr.Add(all);
						}
						reader.Close();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM giornalemez WHERE gme_dit = ? AND gme_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = gio.gio_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = gio.gio_codice;
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM giornaledip WHERE gdi_dit = ? AND gdi_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = gio.gio_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = gio.gio_codice;
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM giornaleimg WHERE img_dit = ? AND img_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = gio.gio_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = gio.gio_codice;
						cmd.ExecuteNonQuery();
							
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = gio.gio_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = gio.gio_codice;
						cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_GIORNALE;
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM giornalelav WHERE gio_dit = ? AND gio_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = gio.gio_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = gio.gio_codice;
						cmd.ExecuteNonQuery();

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
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref GiornaleLavoriDb gio, bool joined)
		{
			if (!Search(ref cmd, gio.gio_dit, gio.gio_codice, ref gio, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
