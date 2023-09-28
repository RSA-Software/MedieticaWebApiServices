using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class CertificatiPagamentoDb
	{
		public int cpa_dit { get; set; }
		public int cpa_codice { get; set; }
		public int cpa_can { get; set; }
		public DateTime? cpa_data { get; set; }
		public string cpa_desc { get; set; }
		public int cpa_sub { get; set; }
		public short cpa_mese { get; set; }
		public double cpa_importo { get; set; }
		public bool cpa_firma_sub { get; set; }
		public bool cpa_firma_dir { get; set; }
		public bool cpa_firma_amm { get; set; }
		public string cpa_num_fat { get; set; }
		public DateTime? cpa_data_fat { get; set; }
		public DateTime? cpa_created_at { get; set; }
		public DateTime? cpa_last_update { get; set; }
		public int cpa_utente { get; set; }

		//
		// Campi Relazionati
		//
		public string sub_desc{ get; set; }
		public string sub_codfis { get; set; }

		private static readonly List<string> ExcludeFields = new List<string>() {"sub_desc", "sub_codfis"};


		private static readonly string JoinQuery = @"
		SELECT *, s.dit_desc AS sub_desc, s.dit_codfis AS sub_codfis
		FROM certificatipag
		LEFT JOIN ditte s ON cpa_sub = s.dit_codice
		";

		public CertificatiPagamentoDb()
		{
			var cpa_db = this;
			DbUtils.Initialize(ref cpa_db);
		}
		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static bool Search(ref OdbcCommand cmd, int dit, int codice, ref CertificatiPagamentoDb cpa, bool joined = false, bool writeLock = false)
		{
			if (cpa != null) DbUtils.Initialize(ref cpa);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, dit, codice, ref cpa, joined, writeLock);
				}
			}

			var found = false;
			string sql;
			if (!joined)
				sql = "SELECT * FROM certificatipag WHERE cpa_dit = ? AND cpa_codice = ?";
			else
				sql = GetJoinQuery() + " WHERE cpa_dit = ? AND cpa_codice = ?";
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("dit", OdbcType.Int).Value = dit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (cpa != null) DbUtils.SqlRead(ref reader, ref cpa, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();

			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref CertificatiPagamentoDb cpa, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref cpa);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new CertificatiPagamentoDb();
				if (!Search(ref cmd, cpa.cpa_dit, cpa.cpa_codice, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.cpa_last_update != cpa.cpa_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(cpa.cpa_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({cpa.cpa_dit} - {cpa.cpa_can} - {cpa.cpa_codice}) : desc", MCException.CampoObbligatorioErr);

				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, cpa.cpa_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				CantieriDb can = null;
				if (!CantieriDb.Search(ref cmd, cpa.cpa_dit, cpa.cpa_can, ref can)) throw new MCException(MCException.CantiereMsg, MCException.CantiereErr);

				if (!DitteDb.Search(ref cmd, cpa.cpa_sub, ref dit)) throw new MCException(MCException.SubappaltatoreMsg, MCException.SubappaltatoreErr);

			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref cpa, "certificatipag", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref cpa, joined);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								cpa.cpa_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref cpa, "certificatipag", "WHERE cpa_dit = ? AND cpa_codice = ?", ExcludeFields);
					cmd.Parameters.Add("dit", OdbcType.Int).Value = cpa.cpa_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = cpa.cpa_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref cpa, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						//
						// Leggiamo la lista degli allegati
						//
						var all_arr = new List<AllegatiDb>();
						cmd.CommandText = "SELECT * FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ?";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = cpa.cpa_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = cpa.cpa_codice;
						cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_CERTIFICATI_PAG;
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var all = new AllegatiDb();
							DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
							all_arr.Add(all);
						}
						reader.Close();
					
						//
						// Cancelliamo gli allegati
						//
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM allegati WHERE all_dit = ? AND all_doc = ? AND all_type = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = cpa.cpa_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = cpa.cpa_codice;
						cmd.Parameters.Add("type", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_CERTIFICATI_PAG;
						cmd.ExecuteNonQuery();

						//
						// Cancelliamo il certificato di pagamento
						//
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM certificatipag WHERE cpa_dit = ? AND cpa_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = cpa.cpa_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = cpa.cpa_codice;
						cmd.ExecuteNonQuery();

						//
						// Cancelliamo i files degli allegati
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

		public static void Reload(ref OdbcCommand cmd, ref CertificatiPagamentoDb cpa, bool joined)
		{
			if (!Search(ref cmd, cpa.cpa_dit, cpa.cpa_codice, ref cpa, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
