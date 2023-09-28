using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class ModelliDb
	{
		public int mod_dit { get; set; }
		public int mod_codice { get; set; }
		public string mod_desc { get; set; }
		public string mod_cod_for { get; set; }
		public int mod_ver { get; set; }
		public int mod_mar { get; set; }
		public int mod_tip { get; set; }
		public string mod_note { get; set; }
		public bool mod_verificato { get; set; }
		public bool mod_manuale_uso { get; set; }
		public bool mod_marchio_ce { get; set; }
		public bool mod_rispondenza_all_v { get; set; }
		public bool mod_formazione { get; set; }
		public bool mod_corso { get; set; }
		public int mod_user { get; set; }
		public DateTime? mod_created_at { get; set; }
		public DateTime? mod_last_update { get; set; }

		//
		// Campi Relazionati
		//
		public string mar_desc{ get; set; }
		public string tip_desc { get; set; }
		public string ver_desc { get; set; }
		public short ver_funzionamento_anni { get; set; }
		public short ver_integrita_anni { get; set; }
		public short ver_interna_anni { get; set; }
		public string img_data { get; set; }

		public List<ImgModelliDb> img_list { get; set; }
		public List<DocModelliDb> doc_public { get; set; }
		public List<DocModelliDb> doc_private { get; set; }
		public List<DocModelliDb> doc_admin { get; set; }
		public List<DocModelliDb> doc_reserved { get; set; }

		private static readonly List<string> ExcludeFields = new List<string>() { "mar_desc", "tip_desc", "ver_desc", "ver_funzionamento_anni", "ver_integrita_anni", "ver_interna_anni", "img_data", "img_list", "doc_public", "doc_private", "doc_admin", "doc_reserved" };

		private static readonly string JoinQuery = @"
		SELECT modelli.*, mar_desc, tip_desc, ver_desc, ver_funzionamento_anni, ver_integrita_anni, ver_interna_anni, img_data, NULL AS img_list, NULL AS doc_public, NULL AS doc_private, NULL AS doc_admin, NULL AS doc_reserved
		FROM modelli
		LEFT JOIN marchi ON mod_mar = mar_codice
		LEFT JOIN tipologie ON mod_tip = tip_codice
		LEFT JOIN verifiche ON mod_ver = ver_codice
		LEFT JOIN imgmodelli ON mod_dit = img_dit AND mod_codice = img_codice AND img_formato = 1";

		public ModelliDb()
		{
			var mod_db = this;
			DbUtils.Initialize(ref mod_db);
		}
		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static bool Search(ref OdbcCommand cmd, int codDit, int codice, ref ModelliDb mod, bool joined = false, bool writeLock = false)
		{
			if (mod != null) DbUtils.Initialize(ref mod);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codice, ref mod, writeLock);
				}
			}

			var found = false;
			string sql;
			if (!joined)
				sql = "SELECT * FROM modelli WHERE mod_dit = ? AND mod_codice = ?";
			else
				sql = GetJoinQuery() + " WHERE mod_dit = ? AND mod_codice = ?";
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (mod != null) DbUtils.SqlRead(ref reader, ref mod, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref ModelliDb mod, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref mod);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new ModelliDb();
				if (!Search(ref cmd, mod.mod_dit, mod.mod_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.mod_last_update != mod.mod_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(mod.mod_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({mod.mod_dit} - {mod.mod_codice}) : desc", MCException.CampoObbligatorioErr);

				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, mod.mod_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				VerificheDb ver = null;
				if (!VerificheDb.Search(ref cmd, mod.mod_ver, ref ver)) throw new MCException(MCException.VerificaMsg, MCException.VerificaErr);

				MarchiDb mar = null;
				if (!MarchiDb.Search(ref cmd, mod.mod_mar, ref mar)) throw new MCException(MCException.MarchioMsg, MCException.MarchioErr);
				
				TipologieDb tip = null;
				if (!TipologieDb.Search(ref cmd, mod.mod_tip, ref tip)) throw new MCException(MCException.TipologiaMsg, MCException.TipologiaErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref mod, "modelli");
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref mod, "modelli", "WHERE mod_dit = ? AND mod_codice = ?");
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = mod.mod_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = mod.mod_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({mod.mod_dit} - {mod.mod_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref mod, "modelli", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref mod, joined);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								mod.mod_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref mod, "modelli", "WHERE mod_dit = ? AND mod_codice = ?", ExcludeFields);
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = mod.mod_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = mod.mod_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref mod, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("SELECT COUNT(*) FROM mezzi WHERE mez_dit_mod = ? AND mez_mod = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = mod.mod_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mod.mod_codice;
						var num = Convert.ToInt32(cmd.ExecuteScalar());
						if (num > 0) throw new MCException(MCException.CancelMsg, MCException.CancelErr);

						var dmo_arr = new List<DocModelliDb>();
						cmd.CommandText = DbUtils.QueryAdapt("SELECT * from docmodelli WHERE dmo_dit = ? AND dmo_mod = ? FOR UPDATE NOWAIT");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = mod.mod_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mod.mod_codice;
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var dmo = new DocModelliDb();
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
						
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM modelli WHERE mod_dit = ? AND mod_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = mod.mod_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mod.mod_codice;
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

		public static void Reload(ref OdbcCommand cmd, ref ModelliDb mod, bool joined)
		{
			if (!Search(ref cmd, mod.mod_dit, mod.mod_codice, ref mod, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
