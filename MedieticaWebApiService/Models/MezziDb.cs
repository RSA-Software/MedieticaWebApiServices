using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class MezziDb
	{
		public int mez_dit { get; set; }
		public int mez_codice { get; set; }
		public int mez_dit_mod { get; set; }
		public int mez_mod { get; set; }
		public string mez_desc { get; set; }
		public string mez_note { get; set; }
		public DateTime? mez_data_imm { get; set; }
		public DateTime? mez_data_acq { get; set; }
		public DateTime? mez_data_dis { get; set; }
		public string mez_serial { get; set; }
		public string mez_targa { get; set; }
		public string mez_gps { get; set; }
		public string mez_telaio { get; set; }
		public bool mez_proprieta { get; set; }
		public short mez_type { get; set; }             // 0 : Mezzi Operativi  - 1 : Veicoli  - 2 : Attrezzature
		public string mez_cod_for { get; set; }
		public DateTime? mez_created_at { get; set; }
		public DateTime? mez_last_update { get; set; }
		
		//
		// Campi Relazionati
		//
		public string img_data { get; set; }
		
		public int mod_mar{ get; set; }
		public int mod_tip { get; set; }
		public int mod_ver { get; set; }
		public string mod_desc { get; set; }
		public string mod_cod_for { get; set; }
		public string mar_desc{ get; set; }
		public string tip_desc { get; set; }
		public string ver_desc { get; set; }
		public short ver_funzionamento_anni { get; set; }
		public short ver_integrita_anni { get; set; }
		public short ver_interna_anni { get; set; }

		public bool mod_manuale_uso { get; set; }
		public bool mod_marchio_ce { get; set; }
		public bool mod_rispondenza_all_v { get; set; }
		public bool mod_formazione { get; set; }
		public bool mod_corso { get; set; }
		public string dit_desc { get; set; }

		public List<ImgMezziDb> img_list { get; set; }
		public List<DocMezziDb> doc_list { get; set; }
		public List<ManutenzioniDb> man_list { get; set; }
		public List<VideoMezziDb> vid_list { get; set; }


		private static readonly List<string> ExcludeFields = new List<string>()
		{
			"img_data", "mod_mar", "mod_tip", "mod_ver", "mod_desc", "mod_cod_for", "tip_desc", "mar_desc", "ver_desc", 
			"ver_funzionamento_anni", "ver_integrita_anni", "ver_interna_anni", "img_list", "doc_list", "man_list", "vid_list",
			"mod_manuale_uso", "mod_marchio_ce", "mod_rispondenza_all_v", "mod_formazione", "mod_corso", "dit_desc"
		};

		private static readonly string JoinQuery = @"
		SELECT mezzi.*, mod_mar, mod_tip, mod_ver, mod_desc, mod_cod_for, tip_desc, mar_desc, ver_desc, ver_funzionamento_anni, ver_integrita_anni, ver_interna_anni, mod_manuale_uso, mod_marchio_ce, mod_rispondenza_all_v, 
		mod_formazione, mod_corso, dit_desc, NULL AS img_list, NULL AS doc_list, NULL AS man_list, NULL AS vid_list,
		(CASE
			WHEN imz.img_data IS NOT NULL THEN imz.img_data
			ELSE imm.img_data
		END) AS img_data
		FROM mezzi
		LEFT JOIN modelli ON mez_dit_mod = mod_dit AND mez_mod = mod_codice
		LEFT JOIN marchi ON mod_mar = mar_codice
		LEFT JOIN tipologie ON mod_tip = tip_codice
		LEFT JOIN verifiche ON mod_ver = ver_codice
		LEFT JOIN ditte ON mez_dit = dit_codice
		LEFT JOIN imgmezzi AS imz ON mez_dit = imz.img_dit AND mez_codice = imz.img_codice AND imz.img_formato = 1
		LEFT JOIN imgmodelli AS imm ON mez_dit_mod = imm.img_dit AND mez_mod = imm.img_codice AND imm.img_formato = 1";
		
		public MezziDb()
		{
			var mez_db = this;
			DbUtils.Initialize(ref mez_db);
			img_list = new List<ImgMezziDb>();
			doc_list = new List<DocMezziDb>();
			man_list = new List<ManutenzioniDb>();
			vid_list = new List<VideoMezziDb>();
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}


		public static bool Search(ref OdbcCommand cmd, int codDit, int codice, ref MezziDb mez, bool joined = false, bool writeLock = false)
		{
			if (mez != null) DbUtils.Initialize(ref mez);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codice, ref mez, joined, writeLock);
				}
			}

			var found = false;

			string sql;
			if (joined)
				sql = GetJoinQuery() + " WHERE mez_dit = ? AND mez_codice = ?";
			else
				sql = "SELECT * FROM mezzi WHERE mez_dit = ? AND mez_codice = ?";

			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql,1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (mez != null)
				{
					DbUtils.SqlRead(ref reader, ref mez, joined ? null : ExcludeFields);
					if (string.IsNullOrWhiteSpace(mez.mez_cod_for)) mez.mez_cod_for = mez.mod_cod_for;
				}
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref MezziDb mez, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref mez);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new MezziDb();
				if (!Search(ref cmd, mez.mez_dit, mez.mez_codice, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.mez_last_update != mez.mez_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			mez.mez_desc = mez.mez_desc.Trim();
			mez.mez_note = mez.mez_note.Trim();

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(mez.mez_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({mez.mez_dit} - {mez.mez_codice}) : desc", MCException.CampoObbligatorioErr);

				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, mez.mez_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				var mod = new ModelliDb();
				if (!ModelliDb.Search(ref cmd, mez.mez_dit_mod, mez.mez_mod, ref mod)) throw new MCException(MCException.ModelloMsg, MCException.ModelloErr);

				if (string.IsNullOrWhiteSpace(mez.mod_cod_for)) mez.mez_cod_for = mod.mod_cod_for;
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref mez, "mezzi", null, ExcludeFields);
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref mez, "mezzi", "WHERE mez_dit = ? AND mez_codice = ?", ExcludeFields);
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = mez.mez_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = mez.mez_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({mez.mez_dit} - {mez.mez_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref mez, "mezzi", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref mez, joined);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								mez.mez_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref mez, "mezzi", "WHERE mez_dit = ? AND mez_codice = ?", ExcludeFields);
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = mez.mez_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = mez.mez_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref mez, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						var dme_arr = new List<DocMezziDb>();
						cmd.CommandText = DbUtils.QueryAdapt("SELECT * from docmezzi WHERE dme_dit = ? AND dme_mez = ? FOR UPDATE NOWAIT");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = mez.mez_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mez.mez_codice;
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var dme = new DocMezziDb();
							DbUtils.SqlRead(ref reader, ref dme);
							dme_arr.Add(dme);
						}
						reader.Close();

						var mnt_arr = new List<ManutenzioniDb>();
						cmd.CommandText = DbUtils.QueryAdapt("SELECT * from manutenzioni WHERE mnt_dit = ? AND mnt_mez = ? FOR UPDATE NOWAIT");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = mez.mez_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mez.mez_codice;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var mnt = new ManutenzioniDb();
							DbUtils.SqlRead(ref reader, ref mnt);
							mnt_arr.Add(mnt);
						}
						reader.Close();

						var all_arr = new List<AllegatiDb>();
						foreach (var dme in dme_arr)
						{
							cmd.CommandText = DbUtils.QueryAdapt("SELECT * FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ? FOR UPDATE NOWAIT");
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = dme.dme_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dme.dme_codice;
							cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_MEZZI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								all_arr.Add(all);
							}
							reader.Close();
						}

						foreach (var mnt in mnt_arr)
						{
							cmd.CommandText = DbUtils.QueryAdapt("SELECT * FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ? FOR UPDATE NOWAIT");
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = mnt.mnt_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = mnt.mnt_codice;
							cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_MANUTENZIONE_MEZZI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								all_arr.Add(all);
							}
							reader.Close();
						}

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM imgmezzi WHERE img_dit = ? AND img_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = mez.mez_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mez.mez_codice;
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM videomezzi WHERE vme_dit = ? AND vme_mez = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = mez.mez_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mez.mez_codice;
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM docmezzi WHERE dme_dit = ? AND dme_mez = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = mez.mez_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mez.mez_codice;
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM manutenzioni WHERE mnt_dit = ? AND mnt_mez = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = mez.mez_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mez.mez_codice;
						cmd.ExecuteNonQuery();

						foreach (var dme in dme_arr)
						{
							cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ?");
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = dme.dme_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dme.dme_codice;
							cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_MEZZI;
							cmd.ExecuteNonQuery();
						}

						foreach (var mnt in mnt_arr)
						{
							cmd.CommandText = DbUtils.QueryAdapt("SELECT * FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ? FOR UPDATE NOWAIT");
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = mnt.mnt_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = mnt.mnt_codice;
							cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_MANUTENZIONE_MEZZI;
							cmd.ExecuteNonQuery();
						}

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM mezzi WHERE mez_dit = ? AND mez_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = mez.mez_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mez.mez_codice;
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

		public static void Reload(ref OdbcCommand cmd, ref MezziDb mez, bool joined)
		{
			if (!Search(ref cmd, mez.mez_dit, mez.mez_codice, ref mez, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
