using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class ManutenzioniDb
	{
		public int mnt_dit { get; set; }
		public int mnt_codice { get; set; }
		public int mnt_mez { get; set; }
		public short mnt_tipo { get; set; }
		public DateTime? mnt_data { get; set; }
		public string mnt_desc { get; set; }
		public DateTime? mnt_created_at { get; set; }
		public DateTime? mnt_last_update { get; set; }

		//
		// Lista Allegati
		//
		public DateTime? next_data { get; set; }
		public List<AllegatiDb> all_list;

		private static readonly List<string> ExcludeFields = new List<string>()
		{
			"next_data",
			"all_list"
		};

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public ManutenzioniDb()
		{
			var mnt_db = this;
			DbUtils.Initialize(ref mnt_db);
		}

		public static bool Search(ref OdbcCommand cmd, int codDit, int codice, ref ManutenzioniDb mnt, bool writeLock = false)
		{
			if (mnt != null) DbUtils.Initialize(ref mnt);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codice, ref mnt, writeLock);
				}
			}

			var found = false;

			var sql = "SELECT *, NULL AS next_data FROM manutenzioni WHERE mnt_dit = ? AND mnt_codice = ?";

			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql,1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (mnt != null) DbUtils.SqlRead(ref reader, ref mnt, ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref ManutenzioniDb mnt, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref mnt);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new ManutenzioniDb();
				if (!Search(ref cmd, mnt.mnt_dit, mnt.mnt_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.mnt_last_update != mnt.mnt_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(mnt.mnt_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({mnt.mnt_dit} - {mnt.mnt_codice}) : desc", MCException.CampoObbligatorioErr);

				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, mnt.mnt_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				MezziDb mez = null;
				if (!MezziDb.Search(ref cmd, mnt.mnt_dit, mnt.mnt_mez, ref mez)) throw new MCException(MCException.DipendenteMsg, MCException.DipendenteErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref mnt, "manutenzioni", null, ExcludeFields);
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref mnt, "manutenzioni", "WHERE mnt_dit = ? AND mnt_codice = ?", ExcludeFields);
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = mnt.mnt_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = mnt.mnt_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({mnt.mnt_dit} - {mnt.mnt_codice})", MCException.DuplicateErr);
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
							var next_data = mnt.next_data;

							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref mnt, "manutenzioni", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref mnt);

							//
							// Creiamo la successiva scadenza
							//
							if (next_data != null && next_data > mnt.mnt_data)
							{
								var scm = new ScaMezziDb();
								cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(scm_codice),0) AS codice FROM scamezzi WHERE scm_dit = ?");
								cmd.Parameters.Clear();
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = mnt.mnt_dit;
								scm.scm_codice = 1 + (int)cmd.ExecuteScalar();
								scm.scm_desc = mnt.mnt_desc;
								scm.scm_dit = mnt.mnt_dit;
								scm.scm_mez = mnt.mnt_mez;
								scm.scm_data = next_data;
								scm.scm_scad_alert_before = 15;
								ScaMezziDb.Write(ref cmd, DbMessage.DB_INSERT, ref scm, ref obj);
							}
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								mnt.mnt_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref mnt, "manutenzioni", "WHERE mnt_dit = ? AND mnt_codice = ?", ExcludeFields);
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = mnt.mnt_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = mnt.mnt_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref mnt);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						// Leggiamo tutti gli allegati della manutenzione
						var all_arr = new List<AllegatiDb>();
						cmd.CommandText = DbUtils.QueryAdapt("SELECT * FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ? FOR UPDATE NOWAIT");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = mnt.mnt_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mnt.mnt_codice;
						cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_MANUTENZIONE_MEZZI;
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
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = mnt.mnt_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mnt.mnt_codice;
						cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_MANUTENZIONE_MEZZI;
						cmd.ExecuteNonQuery();
						
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM manutenzioni WHERE mnt_dit = ? AND mnt_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = mnt.mnt_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mnt.mnt_codice;
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

		public static void Reload(ref OdbcCommand cmd, ref ManutenzioniDb mnt)
		{
			if (!Search(ref cmd, mnt.mnt_dit, mnt.mnt_codice, ref mnt))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
