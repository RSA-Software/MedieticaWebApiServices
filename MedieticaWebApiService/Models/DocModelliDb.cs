using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class DocModelliDb
	{
		public int dmo_dit { get; set; }
		public int dmo_codice { get; set; }
		public int dmo_mod { get; set; }
		public short dmo_livello { get; set; }
		public DateTime? dmo_data { get; set; }
		public string dmo_desc { get; set; }
		public string dmo_url { get; set; }
		public DateTime? dmo_data_rilascio { get; set; }
		public DateTime? dmo_data_scadenza { get; set; }
		public short dmo_scad_alert_before { get; set; }
		public short dmo_scad_alert_after { get; set; }
		public DateTime? dmo_created_at { get; set; }
		public DateTime? dmo_last_update { get; set; }
		public int dmo_user { get; set; }

		//
		// Campi relazionati
		//
		public List<ModSerialDb> mse_list { get; set; }

		private static readonly List<string> ExcludeFields = new List<string>() { "mse_list" };
		

		public DocModelliDb()
		{
			var dmo_db = this;
			DbUtils.Initialize(ref dmo_db);
			mse_list = new List<ModSerialDb>();
		}
		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}


		public static bool Search(ref OdbcCommand cmd, int codDit, int codice, ref DocModelliDb dmo, bool joined = false, bool writeLock = false)
		{
			if (dmo != null) DbUtils.Initialize(ref dmo);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codice, ref dmo, joined, writeLock);
				}
			}

			var found = false;

			var sql = "SELECT * FROM docmodelli WHERE dmo_dit = ? AND dmo_codice = ?";

			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql,1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (dmo != null) DbUtils.SqlRead(ref reader, ref dmo, ExcludeFields);
				found = true;
			}
			reader.Close();

			if (dmo != null && found && joined)
			{
				if (dmo.mse_list == null) dmo.mse_list = new List<ModSerialDb>();
				cmd.CommandText = "SELECT * FROM modserial WHERE mse_dit = ? AND mse_dmo = ? ORDER BY mse_dit, mse_dmo, mse_codice";
				cmd.Parameters.Clear();
				cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
				cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

				reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					var mse = new ModSerialDb();
					DbUtils.SqlRead(ref reader, ref mse);
					if (dmo.mse_list == null) dmo.mse_list = new List<ModSerialDb>();
					dmo.mse_list.Add(mse);
				}
				reader.Close();
			}

			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref DocModelliDb dmo, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref dmo);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new DocModelliDb();
				if (!Search(ref cmd, dmo.dmo_dit, dmo.dmo_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.dmo_last_update != dmo.dmo_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(dmo.dmo_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({dmo.dmo_dit} - {dmo.dmo_codice}) : desc", MCException.CampoObbligatorioErr);

				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, dmo.dmo_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				ModelliDb mod = null;
				if (!ModelliDb.Search(ref cmd, dmo.dmo_dit, dmo.dmo_mod, ref mod)) throw new MCException(MCException.DipendenteMsg, MCException.DipendenteErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref dmo, "docmodelli", null, ExcludeFields);
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dmo, "docmodelli", "WHERE dmo_dit = ? AND dmo_codice = ?", ExcludeFields);
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = dmo.dmo_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = dmo.dmo_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({dmo.dmo_dit} - {dmo.dmo_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref dmo, "docmodelli", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref dmo, joined);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								dmo.dmo_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dmo, "docmodelli", "WHERE dmo_dit = ? AND dmo_codice = ?", ExcludeFields);
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = dmo.dmo_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = dmo.dmo_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref dmo, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						// Leggiamo tutti gli allegati del mezzo
						var all_arr = new List<AllegatiDb>();
						cmd.CommandText = DbUtils.QueryAdapt("SELECT * FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ? FOR UPDATE NOWAIT");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dmo.dmo_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dmo.dmo_codice;
						cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_MODELLI;
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
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dmo.dmo_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dmo.dmo_codice;
						cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_MODELLI;
						cmd.ExecuteNonQuery();

						//
						// Cancelliamo le immagini del modello
						//
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM imgmodelli WHERE img_dit = ? AND img_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dmo.dmo_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dmo.dmo_codice;
						cmd.ExecuteNonQuery();

						//
						// Cancelliamo i video del modello
						//
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM videomodelli WHERE vmo_dit = ? AND vmo_mod = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dmo.dmo_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dmo.dmo_codice;
						cmd.ExecuteNonQuery();

						//
						// Cancelliamo i legamo tra modello e seriali
						//
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM modserial WHERE mse_dit = ? AND mse_dmo = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dmo.dmo_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dmo.dmo_codice;
						cmd.ExecuteNonQuery();
						
						//
						// Cancelliamo il documento
						//
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM docmodelli WHERE dmo_dit = ? AND dmo_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dmo.dmo_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dmo.dmo_codice;
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

		public static void Reload(ref OdbcCommand cmd, ref DocModelliDb dmo, bool joined)
		{
			if (!Search(ref cmd, dmo.dmo_dit, dmo.dmo_codice, ref dmo, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
