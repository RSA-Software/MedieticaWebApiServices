using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class CantieriDb
	{
		public int can_dit { get; set; }
		public int can_codice { get; set; }
		public string can_desc { get; set; }
		public string can_indirizzo { get; set; }
		public string can_citta { get; set; }
		public string can_cap { get; set; }
		public string can_prov { get; set; }
		public DateTime? can_data_inizio { get; set; }
		public DateTime? can_data_fine { get; set; }
		public string can_note { get; set; }
		public string can_approvazione_progetto_esecutivo { get; set; }
		public string can_ente_appaltante { get; set; }
		public string can_ufficio_competente { get; set; }
		public string can_rup { get; set; }
		public string can_progettazione_esecutiva { get; set; }
		public string can_direttore_lavori { get; set; }
		public string can_coord_sicurezza_progettazione { get; set; }
		public string can_coord_sicurezza_esecutiva { get; set; }
		public double can_importo_finanziamento { get; set; }
		public double can_importo_lavori { get; set; }
		public double can_importo_base_asta { get; set; }
		public double can_oneri_sicurezza { get; set; }
		public double can_importo_contrattuale { get; set; }
		public string can_estremi_contratto { get; set; }
		public string can_notifica_preliminare { get; set; }
		public string can_direttore_tecnico { get; set; }
		public string can_responsabile_cantiere { get; set; }
		public string can_rspp { get; set; }
		public int can_durata_lavori { get; set; }
		public string can_imprese_subappaltatrici { get; set; }
		public string can_direttore_operativo { get; set; }
		public string can_ispettore_di_cantiere { get; set; }
		public string can_collaudo_statico { get; set; }
		public string can_collaudo_tecnico_amministrativo { get; set; }
		public string can_impresa_aggiudicataria { get; set; }
		public string can_cup { get; set; }
		public string can_cig { get; set; }
		public double can_latitudine { get; set; }
		public double can_longitudine { get; set; }
		public bool can_subappalto { get; set; }
		public bool can_deleted { get; set; }
		public DateTime? can_created_at { get; set; }
		public DateTime? can_last_update { get; set; }
		
		//
		// Campi Relazionati
		//
		public string dit_desc { get; set; }
		public string dit_piva { get; set; }
		public string dit_codfis { get; set; }

		public string img_data { get; set; }
		public List<ImgCantieriDb> img_list { get; set; }
		public List<DocCantieriDb> ammministrazione_list { get; set; }
		public List<DocCantieriDb> autorizzazioni_list { get; set; }
		public List<DocCantieriDb> sicurezza_list { get; set; }
		public List<DocCantieriDb> tecnico_list { get; set; }
		public List<DocCantieriDb> contabilita_list { get; set; }
		public List<DocCantieriDb> rifiuti_list { get; set; }
		public List<DocCantieriDb> fornitori_list { get; set; }

		private static readonly List<string> ExcludeFields = new List<string>() { "dit_desc", "dit_piva", "dit_codfis", "img_data", "img_list", "ammministrazione_list", "autorizzazioni_list", "sicurezza_list", "tecnico_list" };

		private static readonly string JoinQuery = @"
		SELECT cantieri.*, dit_desc, dit_piva, dit_codfis, img_data, NULL AS img_list, NULL AS ammministrazione_list, NULL AS autorizzazioni_list, NULL AS sicurezza_list, NULL AS tecnico_list
		FROM cantieri
		INNER JOIN ditte ON can_dit = dit_codice
		LEFT JOIN imgcantieri ON can_dit = img_dit AND can_codice = img_codice AND img_formato = 1";

		public CantieriDb()
		{
			var can_db = this;
			DbUtils.Initialize(ref can_db);
		}

		public static string GetTableDescription()
		{
			return ("Cantieri");
		}
		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}

		public static bool Search(ref OdbcCommand cmd, int codDit, int codice, ref CantieriDb can, bool joined = false,  bool writeLock = false)
		{
			if (can != null) DbUtils.Initialize(ref can);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					var ret = Search(ref command, codDit, codice, ref can, writeLock);
					return (ret);
				}
			}

			var found = false;

			string sql;
			if (joined)
				sql = JoinQuery + " WHERE can_dit = ? AND can_codice = ?";
			else
				sql = "SELECT * FROM cantieri WHERE can_dit = ? AND can_codice = ?";

			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (can != null) DbUtils.SqlRead(ref reader, ref can, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref CantieriDb can, ref object obj, bool joined = false)
		{
			var dit = new DitteDb();

			DbUtils.Trim(ref can);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new CantieriDb();
				if (!Search(ref cmd, can.can_dit, can.can_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.can_last_update != can.can_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(can.can_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({can.can_dit} - {can.can_codice}) : desc", MCException.CampoObbligatorioErr);
		
				if (!DitteDb.Search(ref cmd, can.can_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);
				if (dit.dit_subappaltatrice && !can.can_subappalto) throw new MCException(MCException.CantiereSubappaltatriceMsg, MCException.CantiereSubappaltatriceErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref can, "catieri", null, ExcludeFields);
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref can, "cantieri", "WHERE can_dit = ? AND can_codice = ?", ExcludeFields);
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = can.can_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = can.can_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({can.can_dit} - {can.can_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref can, "cantieri", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref can, joined);

							//
							// Inseriamo i documenti della check list
							//
							var chk_arr = new List<CheckListDb>();
							cmd.CommandText = "SELECT * FROM checklist WHERE chk_tipo = ? ORDER BY chk_codice";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)CheckListType.CANTIERI;
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var chk = new CheckListDb();
								DbUtils.SqlRead(ref reader, ref chk);
								chk_arr.Add(chk);

							}
							reader.Close();

							cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(dca_codice),0) AS codice FROM doccantieri WHERE dca_dit = ? AND dca_can = ?");
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = can.can_dit;
							cmd.Parameters.Add("codcan", OdbcType.Int).Value = can.can_codice;
							var last = (int)cmd.ExecuteScalar();
							foreach (var chk in chk_arr)
							{
								var dca = new DocCantieriDb();
								dca.dca_dit = can.can_dit;
								dca.dca_can = can.can_codice;
								dca.dca_codice = last + 1;
								dca.dca_desc = chk.chk_desc;
								dca.dca_settore = chk.chk_settore;
								DocCantieriDb.Write(ref cmd, DbMessage.DB_ADD, ref dca, ref obj);
								last = dca.dca_codice;
							}
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								can.can_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref can, "cantieri", "WHERE can_dit = ? AND can_codice = ?", ExcludeFields);
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = can.can_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = can.can_codice;
					cmd.ExecuteNonQuery();

					//
					// Estraiamo la lista dei subappalti
					//
					if (msg == DbMessage.DB_UPDATE)
					{
						var sub_arr = new List<SubAppaltiCantieriDb>();
						cmd.CommandText = "SELECT * FROM subappalti WHERE sub_dit_app = ? AND sub_can_app = ?";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = can.can_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = can.can_codice;
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var cxx = new SubAppaltiCantieriDb();
							DbUtils.SqlRead(ref reader, ref cxx, SubAppaltiCantieriDb.GetJoinExcludeFields());
							if (cxx.sub_dit_sub != 0 && cxx.sub_can_sub != 0) sub_arr.Add(cxx);
						}
						reader.Close();

						foreach (var sub in sub_arr)
						{
							var csu = new CantieriDb();
							if (Search(ref cmd, can.can_dit, can.can_codice, ref csu))
							{
								csu.can_dit = sub.sub_dit_sub;
								csu.can_codice = sub.sub_can_sub;
								csu.can_impresa_aggiudicataria = dit.dit_desc;
								csu.can_subappalto = true;
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref csu, "cantieri", "WHERE can_dit = ? AND can_codice = ?", ExcludeFields);
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = csu.can_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = csu.can_codice;
								cmd.ExecuteNonQuery();
							}
						}
					}
					Reload(ref cmd, ref can, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						//
						// Cancelliamo la lista dei cantieri dei subappalti
						//
						var sub_arr = new List<SubAppaltiCantieriDb>();
						cmd.CommandText = "SELECT * FROM subappalti WHERE sub_dit_app = ? AND sub_can_app = ?";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = can.can_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = can.can_codice;
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var sub = new SubAppaltiCantieriDb();
							DbUtils.SqlRead(ref reader, ref sub, SubAppaltiCantieriDb.GetJoinExcludeFields());
							 sub_arr.Add(sub);
						}
						reader.Close();
						foreach (var sub in sub_arr)
						{
							var csu = new CantieriDb();
							if (Search(ref cmd, sub.sub_dit_sub, sub.sub_can_sub, ref csu)) Write(ref cmd, DbMessage.DB_DELETE, ref csu, ref obj);
						}

						//
						// Cancelliamo subbappalti
						//
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM subappalti WHERE sub_dit_app = ? AND sub_can_app = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = can.can_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = can.can_codice;
						cmd.ExecuteNonQuery();

						//
						// Cancelliamo il giornale dei lavori
						//
						var gio_arr = new List<GiornaleLavoriDb>();
						cmd.CommandText = "SELECT * FROM giornalelav WHERE gio_dit = ? AND gio_can = ? ORDER BY gio_codice";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = can.can_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = can.can_codice;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var gio = new GiornaleLavoriDb();
							DbUtils.SqlRead(ref reader, ref gio, GiornaleLavoriDb.GetJoinExcludeFields());
							gio_arr.Add(gio);
						}
						reader.Close();
						foreach (var gxx in gio_arr)
						{
							var gio = new GiornaleLavoriDb();
							if (GiornaleLavoriDb.Search(ref cmd, gxx.gio_dit, gxx.gio_codice, ref gio)) GiornaleLavoriDb.Write(ref cmd, DbMessage.DB_DELETE, ref gio, ref obj);
						}

						//
						// Cancelliamo i Certificati di Pagamento
						//
						var cpa_arr = new List<CertificatiPagamentoDb>();
						cmd.CommandText = "SELECT * FROM certificatipag WHERE cpa_dit = ? AND cpa_can = ? ORDER BY cpa_codice";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = can.can_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = can.can_codice;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var cpa = new CertificatiPagamentoDb();
							DbUtils.SqlRead(ref reader, ref cpa, CertificatiPagamentoDb.GetJoinExcludeFields());
							cpa_arr.Add(cpa);
						}
						reader.Close();
						foreach (var cxx in cpa_arr)
						{
							var cpa = new CertificatiPagamentoDb();
							if (CertificatiPagamentoDb.Search(ref cmd, cxx.cpa_dit, cxx.cpa_codice, ref cpa)) CertificatiPagamentoDb.Write(ref cmd, DbMessage.DB_DELETE, ref cpa, ref obj);
						}

						//
						// Leggiamo la lista dei documenti
						//
						var dca_arr = new List<DocCantieriDb>();
						cmd.CommandText = DbUtils.QueryAdapt("SELECT * from doccantieri WHERE dca_dit = ? AND dca_can = ? FOR UPDATE NOWAIT");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = can.can_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = can.can_codice;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var dca = new DocCantieriDb();
							DbUtils.SqlRead(ref reader, ref dca);
							dca_arr.Add(dca);
						}
						reader.Close();

						var all_arr = new List<AllegatiDb>();
						foreach (var dca in dca_arr)
						{
							cmd.CommandText = DbUtils.QueryAdapt("SELECT * FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ? FOR UPDATE NOWAIT");
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = dca.dca_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dca.dca_codice;
							cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_CANTIERI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								all_arr.Add(all);
							}
							reader.Close();
						}
						
						//
						// Cancelliamo le immagini
						//
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM imgcantieri WHERE img_dit = ? AND img_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = can.can_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = can.can_codice;
						cmd.ExecuteNonQuery();

						//
						// Cancelliamo i documenti
						//
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM doccantieri WHERE dca_dit = ? AND dca_can = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = can.can_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = can.can_codice;
						cmd.ExecuteNonQuery();

						foreach (var dca in dca_arr)
						{
							cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ?");
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = dca.dca_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dca.dca_codice;
							cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_CANTIERI;
							cmd.ExecuteNonQuery();
						}

						//
						// Cancelliamo l'elenco dei mezzi impiegati
						//
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM mezcantieri WHERE mec_dit = ? AND mec_can = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = can.can_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = can.can_codice;
						cmd.ExecuteNonQuery();

						//
						// Cancelliamo l'elenco dei dipendenti impiegati
						//
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM dipcantieri WHERE dic_dit = ? AND dic_can = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = can.can_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = can.can_codice;
						cmd.ExecuteNonQuery();

						//
						// Cancelliamo le scadenze
						//
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM scacantieri WHERE scc_dit = ? AND scc_can = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = can.can_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = can.can_codice;
						cmd.ExecuteNonQuery();

						//
						// Cancelliamo il cantiere
						//
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM cantieri WHERE can_dit = ? AND can_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = can.can_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = can.can_codice;
						cmd.ExecuteNonQuery();

						//
						// Rimuoviamo i files degli allegati
						//
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

		public static void Reload(ref OdbcCommand cmd, ref CantieriDb can, bool joined)
		{
			if (!Search(ref cmd, can.can_dit, can.can_codice, ref can, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
