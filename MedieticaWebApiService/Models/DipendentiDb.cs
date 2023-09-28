using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	enum DipCategoria : short
	{
		OPERAIO = 0,
		IMPIEGATO = 1,
		QUADRO = 2,
		DIRIGENTE = 3
	}

	public class DipendentiDb
	{
		public int dip_dit { get; set; }
		public int dip_codice { get; set; }
		public string dip_cognome { get; set; }
		public string dip_nome { get; set; }
		public string dip_desc { get; set; }
		public string dip_indirizzo { get; set; }
		public string dip_citta { get; set; }
		public string dip_cap { get; set; }
		public string dip_prov { get; set; }
		public string dip_citta_nas { get; set; }
		public string dip_cap_nas { get; set; }
		public string dip_prov_nas { get; set; }
		public DateTime? dip_data_nas { get; set; }
		public string dip_codfis { get; set; }
		public string dip_tel { get; set; }
		public string dip_cell1 { get; set; }
		public string dip_cell2 { get; set; }
		public string dip_email { get; set; }
		public string dip_pec { get; set; }
		public DateTime? dip_data_assunzione { get; set; }
		public DateTime? dip_data_fine_rapporto { get; set; }
		public DateTime? dip_scad_permesso_sog { get; set; }
		public string dip_note { get; set; }
		public short dip_categoria{ get; set; }
		public string dip_inquadramento{ get; set; }
		public short dip_tipo_contratto { get; set; }
		public bool dip_deleted { get; set; }
		public DateTime? dip_created_at { get; set; }
		public DateTime? dip_last_update { get; set; }
		public int dip_user { get; set; }

		//
		// Campi Relazionati
		//
		public string dit_desc { get; set; }
		public string dit_piva { get; set; }
		public string dit_codfis { get; set; }
		public string img_data { get; set; }
		
		public List<DipMansioniDb> man_list { get; set; }
		public List<DipSediDb> sed_list { get; set; }
		public List<ImgDipendentiDb> img_list { get; set; }
		public List<DocDipendentiDb> unilav_list { get; set; }
		public List<DocDipendentiDb> ammministrazione_list { get; set; }
		public List<DocDipendentiDb> autorizzazioni_list { get; set; }
		public List<DocDipendentiDb> sicurezza_list { get; set; }
		public List<DocDipendentiDb> tecnico_list { get; set; }
		public List<DocDipendentiDb> corsi_list { get; set; }


		private static readonly List<string> ExcludeFields = new List<string>() { "dit_desc", "dit_piva", "dit_codfis", "img_data", "img_list" , "man_list", "sed_list", "unilav_list", "ammministrazione_list", "autorizzzazioni_list", "sicurezza_list", "tecnico_list", "corsi_list" };

		private static readonly string JoinQuery = @"
		SELECT dipendenti.*, dit_desc, dit_piva, dit_codfis, img_data, NULL AS img_list, NULL AS man_list, NULL AS unilav_list, NULL AS ammministrazione_list, NULL AS autorizzzazioni_list, NULL AS sicurezza_list, NULL AS tecnico_list, NULL AS corsi_list
		FROM dipendenti
		INNER JOIN ditte ON dip_dit = dit_codice
		LEFT JOIN imgdipendenti ON dip_dit = img_dit AND dip_codice = img_codice AND img_formato = 1";
		
		public DipendentiDb()
		{
			var dip_db = this;
			DbUtils.Initialize(ref dip_db);
		}

		public static string GetTableDescription()
		{
			return ("Dipendenti");
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}


		public static bool Search(ref OdbcCommand cmd, int codDit, int codice, ref DipendentiDb dip, bool joined = false, bool writeLock = false)
		{
			if (dip != null) DbUtils.Initialize(ref dip);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codice, ref dip, joined, writeLock);
				}
			}

			var found = false;

			string sql;
			if (joined)
				sql = GetJoinQuery() + " WHERE dip_dit = ? AND dip_codice = ?";
			else
				sql = "SELECT * FROM dipendenti WHERE dip_dit = ? AND dip_codice = ?";

			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql,1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (dip != null) DbUtils.SqlRead(ref reader, ref dip, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();

			if (joined && found && dip != null)
			{
				dip.man_list = new List<DipMansioniDb>();
				cmd.CommandText = DipMansioniDb.GetJoinQuery() + " WHERE dma_dit = ? AND dma_dip = ? ORDER BY dma_dit, dma_dip, dma_man";
				cmd.Parameters.Clear();
				cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
				cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
				reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					var man = new DipMansioniDb();
					DbUtils.SqlRead(ref reader, ref man);
					dip.man_list.Add(man);
				}
				reader.Close();

				dip.sed_list = new List<DipSediDb>();
				cmd.CommandText = DipSediDb.GetJoinQuery() + " WHERE dse_dit = ? AND dse_dip = ? ORDER BY dse_dit, dse_dip, dse_sed";
				cmd.Parameters.Clear();
				cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
				cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
				reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					var sed = new DipSediDb();
					DbUtils.SqlRead(ref reader, ref sed);
					dip.sed_list.Add(sed);
				}
				reader.Close();
			}
			return (found);
		}

		public static bool Search(ref OdbcCommand cmd, int codDit, string codfis, ref DipendentiDb dip, bool joined = false, bool writeLock = false)
		{
			var codice = codfis.Trim().ToUpper();
			if (dip != null) DbUtils.Initialize(ref dip);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					cmd = new OdbcCommand { Connection = connection };
					return Search(ref cmd, codDit, codice, ref dip, joined, writeLock);
				}
			}

			var found = false;

			string sql;
			if (joined)
				sql = GetJoinQuery() + " WHERE dip_dit = ? AND dip_codfis = ?";
			else
				sql = "SELECT * FROM dipendenti WHERE dip_dit = ? AND dip_codfis = ?";

			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codfis", OdbcType.VarChar).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (dip != null) DbUtils.SqlRead(ref reader, ref dip, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref DipendentiDb dip, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref dip);
			dip.dip_codfis = dip.dip_codfis.ToUpper();
			dip.dip_email = dip.dip_email.ToLower();
			dip.dip_pec = dip.dip_pec.ToLower();

			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new DipendentiDb();
				if (!Search(ref cmd, dip.dip_dit, dip.dip_codice, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.dip_last_update != dip.dip_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			dip.dip_desc = dip.dip_cognome.Trim() + " " + dip.dip_nome.Trim();
			dip.dip_desc = dip.dip_desc.Trim();
			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(dip.dip_cognome)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({dip.dip_dit} - {dip.dip_codice}) : cognome", MCException.CampoObbligatorioErr);
				if (string.IsNullOrWhiteSpace(dip.dip_nome)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({dip.dip_dit} - {dip.dip_codice}) : nome", MCException.CampoObbligatorioErr);
				if (string.IsNullOrWhiteSpace(dip.dip_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({dip.dip_dit} - {dip.dip_codice}) : desc", MCException.CampoObbligatorioErr);

				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, dip.dip_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref dip, "dipendenti", null, ExcludeFields);
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dip, "dipendenti", "WHERE dip_dit = ? AND dip_codice = ?", ExcludeFields);
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = dip.dip_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({dip.dip_dit} - {dip.dip_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref dip, "dipendenti", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref dip, joined);
							
							//
							// Inseriamo i documenti della check list
							//
							var chk_arr = new List<CheckListDb>();
							cmd.CommandText = @"
							SELECT *
							FROM checklist
							WHERE chk_tipo = ? AND chk_codice IN (
								SELECT DISTINCT mac_chk
								FROM chkmansioni
								WHERE mac_man IN(
									SELECT dma_man
									from dipmansioni
									WHERE dma_dit = ? AND dma_dip = ?
								)
							)
							ORDER BY chk_codice";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)CheckListType.DIPENDENTI;
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
							cmd.Parameters.Add("coddip", OdbcType.Int).Value = dip.dip_codice;
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var chk = new CheckListDb();
								DbUtils.SqlRead(ref reader, ref chk);
								chk_arr.Add(chk);

							}
							reader.Close();

							cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(doc_codice),0) AS codice FROM documenti WHERE doc_dit = ? AND doc_dip = ?");
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
							cmd.Parameters.Add("coddip", OdbcType.Int).Value = dip.dip_codice;
							var last = (int)cmd.ExecuteScalar();
							foreach (var chk in chk_arr)
							{
								var doc = new DocDipendentiDb();
								doc.doc_dit = dip.dip_dit;
								doc.doc_dip = dip.dip_codice;
								doc.doc_codice = last + 1;
								doc.doc_desc = chk.chk_desc;
								doc.doc_settore = chk.chk_settore;
								doc.doc_chk = chk.chk_codice;
								DocDipendentiDb.Write(ref cmd, DbMessage.DB_ADD, ref doc, ref obj);
								last = doc.doc_codice;
							}
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								dip.dip_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_UPDATE:
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dip, "dipendenti", "WHERE dip_dit = ? AND dip_codice = ?", ExcludeFields);
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dip.dip_codice;
						cmd.ExecuteNonQuery();
						Reload(ref cmd, ref dip, joined);
					}
					break;

				case DbMessage.DB_REWRITE:
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dip, "dipendenti", "WHERE dip_dit = ? AND dip_codice = ?", ExcludeFields);
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dip.dip_codice;
						cmd.ExecuteNonQuery();
						Reload(ref cmd, ref dip, joined);

						//
						// Rimuoviamo i documenti non compilati generati automaticamente tramite la check list
						//
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM documenti WHERE doc_dit = ? AND doc_dip = ? AND doc_chk <> 0 AND doc_data IS NULL");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dip.dip_codice;
						cmd.ExecuteNonQuery();

						//
						// Leggiamo i documenti compilati generati automaticamente tramite la check list
						//
						List<int> chk_list = null;
						cmd.CommandText = DbUtils.QueryAdapt("SELECT doc_chk FROM documenti WHERE doc_dit = ? AND doc_dip = ? AND doc_chk <> 0 AND doc_data IS NOT NULL");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dip.dip_codice;
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var cod_chk = reader.GetInt32(0);
							if (chk_list == null) chk_list = new List<int>();
							chk_list.Add(cod_chk);
						}
						reader.Close();

						//
						// Inseriamo i documenti della check list
						//
						var chk_arr = new List<CheckListDb>();
						cmd.CommandText = @"
							SELECT *
							FROM checklist
							WHERE chk_tipo = ? AND chk_codice IN (
								SELECT DISTINCT mac_chk
								FROM chkmansioni
								WHERE mac_man IN(
									SELECT dma_man
									from dipmansioni
									WHERE dma_dit = ? AND dma_dip = ?
								)
							)
							ORDER BY chk_codice";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)CheckListType.DIPENDENTI;
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
						cmd.Parameters.Add("coddip", OdbcType.Int).Value = dip.dip_codice;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var chk = new CheckListDb();
							DbUtils.SqlRead(ref reader, ref chk);
							chk_arr.Add(chk);
						}
						reader.Close();

						cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(doc_codice),0) AS codice FROM documenti WHERE doc_dit = ? AND doc_dip = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
						cmd.Parameters.Add("coddip", OdbcType.Int).Value = dip.dip_codice;
						var last = (int)cmd.ExecuteScalar();
						foreach (var chk in chk_arr)
						{
							if (chk_list != null)
							{
								var found = false;
								foreach (var x in chk_list)
								{
									if (x == chk.chk_codice)
									{
										found = true;
										break;
									}
								}
								if (found) continue;
							}
							var doc = new DocDipendentiDb();
							doc.doc_dit = dip.dip_dit;
							doc.doc_dip = dip.dip_codice;
							doc.doc_codice = last + 1;
							doc.doc_desc = chk.chk_desc;
							doc.doc_settore = chk.chk_settore;
							doc.doc_chk = chk.chk_codice;
							DocDipendentiDb.Write(ref cmd, DbMessage.DB_ADD, ref doc, ref obj);
							last = doc.doc_codice;
						}
					}
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						var doc_arr = new List<DocDipendentiDb>();
						cmd.CommandText = DbUtils.QueryAdapt("SELECT * from documenti WHERE doc_dit = ? AND doc_dip = ? FOR UPDATE NOWAIT");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dip.dip_codice;
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var doc = new DocDipendentiDb();
							DbUtils.SqlRead(ref reader, ref doc);
							doc_arr.Add(doc);
						}
						reader.Close();

						var all_arr = new List<AllegatiDb>();
						foreach (var doc in doc_arr)
						{
							cmd.CommandText = DbUtils.QueryAdapt("SELECT * FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ? FOR UPDATE NOWAIT");
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = doc.doc_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = doc.doc_codice;
							cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_DIPENDENTI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								all_arr.Add(all);
							}
							reader.Close();
						}

						var vis_arr = new List<DipVisiteDb>();
						cmd.CommandText = DbUtils.QueryAdapt("SELECT * from dipvisite WHERE dvi_dit = ? AND dvi_dip = ? FOR UPDATE NOWAIT");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dip.dip_codice;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var dvi = new DipVisiteDb();
							DbUtils.SqlRead(ref reader, ref dvi, DipVisiteDb.GetJoinExcludeFields());
							vis_arr.Add(dvi);
						}
						reader.Close();

						foreach (var dvi in vis_arr)
						{
							cmd.CommandText = DbUtils.QueryAdapt("SELECT * FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ? FOR UPDATE NOWAIT");
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = dvi.dvi_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dvi.dvi_codice;
							cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_VISITE_MEDICHE;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								all_arr.Add(all);
							}
							reader.Close();
						}

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM imgdipendenti WHERE img_dit = ? AND img_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dip.dip_codice;
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM documenti WHERE doc_dit = ? AND doc_dip = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dip.dip_codice;
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM dipvisite WHERE dvi_dit = ? AND dvi_dip = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dip.dip_codice;
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM scadipendenti WHERE scp_dit = ? AND scp_dip = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dip.dip_codice;
						cmd.ExecuteNonQuery();
						
						foreach (var doc in doc_arr)
						{
							cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ?");
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = doc.doc_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = doc.doc_codice;
							cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_DIPENDENTI;
							cmd.ExecuteNonQuery();
						}

						foreach (var dvi in vis_arr)
						{
							cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ?");
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = dvi.dvi_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dvi.dvi_codice;
							cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_VISITE_MEDICHE;
							cmd.ExecuteNonQuery();
						}

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM dipmansioni WHERE dma_dit = ? AND dma_dip = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dip.dip_codice;
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM dipsedi WHERE dse_dit = ? AND dse_dip = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dip.dip_codice;
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM dipendenti WHERE dip_dit = ? AND dip_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dip.dip_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dip.dip_codice;
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

		public static void Reload(ref OdbcCommand cmd, ref DipendentiDb dip, bool joined)
		{
			if (!Search(ref cmd, dip.dip_dit, dip.dip_codice, ref dip, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
