using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class DocMezziDb
	{
		public int dme_dit { get; set; }
		public int dme_codice { get; set; }
		public int dme_mez { get; set; }
		public short dme_livello { get; set; }
		public DateTime? dme_data { get; set; }
		public string dme_desc { get; set; }
		public string dme_url { get; set; }
		public DateTime? dme_data_rilascio { get; set; }
		public DateTime? dme_data_scadenza { get; set; }
		public short dme_scad_alert_before { get; set; }
		public short dme_scad_alert_after { get; set; }
		public short dme_tipo_doc { get; set; }					// 0 - Documenti Normali    1 - Documenti Industria 4.0
		public bool dme_protetto { get; set; }					// Se true richiede password al download da QrCode
		public DateTime? dme_created_at { get; set; }
		public DateTime? dme_last_update { get; set; }
		public int dme_user { get; set; }

		//
		// Lista Allegati
		//
		public List<AllegatiDb> all_list;

		private static readonly List<string> ExcludeFields = new List<string>()
		{
			"all_list"
		};


		public DocMezziDb()
		{
			var dme_db = this;
			DbUtils.Initialize(ref dme_db);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}


		public DocMezziDb(DocModelliDb dmo)
		{
			var dme_db = this;
			DbUtils.Initialize(ref dme_db);
			dme_db.dme_dit = dmo.dmo_dit;
			dme_db.dme_codice = dmo.dmo_codice;
			dme_db.dme_mez = dmo.dmo_mod;
			dme_db.dme_livello = dmo.dmo_livello;
			dme_db.dme_data = dmo.dmo_data;
			dme_db.dme_desc = dmo.dmo_desc;
			dme_db.dme_url = dmo.dmo_url;
			dme_db.dme_data_rilascio = dmo.dmo_data_rilascio;
			dme_db.dme_data_scadenza = dmo.dmo_data_scadenza;
			dme_db.dme_scad_alert_before = dmo.dmo_scad_alert_before;
			dme_db.dme_scad_alert_after = dmo.dmo_scad_alert_after;
			dme_db.dme_created_at = dmo.dmo_created_at;
			dme_db.dme_last_update = dmo.dmo_last_update;
		}

		public static bool Search(ref OdbcCommand cmd, int codDit, int codice, ref DocMezziDb dme, bool writeLock = false)
		{
			if (dme != null) DbUtils.Initialize(ref dme);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codice, ref dme, writeLock);
				}
			}

			var found = false;

			var sql = "SELECT * FROM docmezzi WHERE dme_dit = ? AND dme_codice = ?";

			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql,1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (dme != null) DbUtils.SqlRead(ref reader, ref dme, ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref DocMezziDb dme, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref dme);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new DocMezziDb();
				if (!Search(ref cmd, dme.dme_dit, dme.dme_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.dme_last_update != dme.dme_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(dme.dme_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({dme.dme_dit} - {dme.dme_codice}) : desc", MCException.CampoObbligatorioErr);

				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, dme.dme_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				MezziDb mez = null;
				if (!MezziDb.Search(ref cmd, dme.dme_dit, dme.dme_mez, ref mez)) throw new MCException(MCException.DipendenteMsg, MCException.DipendenteErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref dme, "docmezzi", null, ExcludeFields);
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dme, "docmezzi", "WHERE dme_dit = ? AND dme_codice = ?", ExcludeFields);
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = dme.dme_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = dme.dme_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({dme.dme_dit} - {dme.dme_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref dme, "docmezzi", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref dme);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								dme.dme_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dme, "docmezzi", "WHERE dme_dit = ? AND dme_codice = ?", ExcludeFields);
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = dme.dme_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = dme.dme_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref dme);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						// Leggiamo tutti gli allegati del modello
						var all_arr = new List<AllegatiDb>();
						cmd.CommandText = DbUtils.QueryAdapt("SELECT * FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ? FOR UPDATE NOWAIT");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dme.dme_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dme.dme_codice;
						cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_MEZZI;
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var all = new AllegatiDb();
							DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
							all_arr.Add(all);
						}
						reader.Close();
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dme.dme_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dme.dme_codice;
						cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_MEZZI;
						cmd.ExecuteNonQuery();
						
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM docmezzi WHERE dme_dit = ? AND dme_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dme.dme_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dme.dme_codice;
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
							catch
							{
								throw;
							}
						}
					}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref DocMezziDb dme)
		{
			if (!Search(ref cmd, dme.dme_dit, dme.dme_codice, ref dme)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
