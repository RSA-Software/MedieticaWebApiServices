using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class DipMansioniDb
	{
		public int dma_dit { get; set; }
		public int dma_dip { get; set; }
		public int dma_man { get; set; }
		public DateTime? dma_created_at { get; set; }
		public DateTime? dma_last_update { get; set; }
		public int dma_user { get; set; }

		//
		// Campi relazionati
		//
		public string man_desc { get; set; }
		public short man_rischio{ get; set; }

		private static readonly List<string> ExcludeFields = new List<string>() { "man_desc", "man_rischio" };

		private static readonly string JoinQuery = @"
		SELECT dipmansioni.*, man_desc, man_rischio
		FROM dipmansioni
		INNER JOIN mansioni ON dma_man = man_codice
		";
		
		public DipMansioniDb()
		{
			var dma_db = this;
			DbUtils.Initialize(ref dma_db);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}


		public static bool Search(ref OdbcCommand cmd, int codDit, int codDip, int codMan, ref DipMansioniDb dma, bool joined = false, bool writeLock = false)
		{
			if (dma != null) DbUtils.Initialize(ref dma);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command =  new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codDip, codMan, ref dma, joined, writeLock);
				}
			}

			var found = false;
			string sql;

			if (joined)
				sql = JoinQuery + " WHERE dma_dit = ? AND dma_dip = ? AND dma_man = ?";
			else
				sql = "SELECT * FROM dipmansioni WHERE dma_dit = ? AND dma_dip = ? AND dma_man = ?";
			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("coddip", OdbcType.Int).Value = codDip;
			cmd.Parameters.Add("codman", OdbcType.Int).Value = codMan;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (dma != null) DbUtils.SqlRead(ref reader, ref dma, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref DipMansioniDb dma, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref dma);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new DipMansioniDb();
				if (!Search(ref cmd, dma.dma_dit, dma.dma_dip, dma.dma_man, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.dma_last_update != dma.dma_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, dma.dma_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				DipendentiDb dip = null;
				if (!DipendentiDb.Search(ref cmd, dma.dma_dit, dma.dma_dip, ref dip)) throw new MCException(MCException.DipendenteMsg, MCException.DipendenteErr);

				MansioniDb man = null;
				if (!MansioniDb.Search(ref cmd, dma.dma_man, ref man)) throw new MCException(MCException.MansioneMsg, MCException.MansioneErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref dma, "dipmansioni", null, ExcludeFields);
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dma, "dipmansioni", "WHERE dma_dit = ? AND dma_dip = ? AND dma_man = ?", ExcludeFields);
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = dma.dma_dit;
								cmd.Parameters.Add("coddip", OdbcType.Int).Value = dma.dma_dip;
								cmd.Parameters.Add("codman", OdbcType.Int).Value = dma.dma_man;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({dma.dma_dit} - {dma.dma_dip} - {dma.dma_man})", MCException.DuplicateErr);
								throw;
							}
						}
						else throw;
					}
					break;

				case DbMessage.DB_INSERT:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref dma, "dipmansioni", null, ExcludeFields);
						cmd.ExecuteNonQuery();
						Reload(ref cmd, ref dma, joined);

						//
						// Leggiamo i documenti compilati generati automaticamente tramite la check list
						//
						List<int> chk_list = null;
						cmd.CommandText = DbUtils.QueryAdapt("SELECT doc_chk FROM documenti WHERE doc_dit = ? AND doc_dip = ? AND doc_chk <> 0");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dma.dma_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dma.dma_dip;
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var cod_chk = reader.GetInt32(0);
							if (chk_list == null) chk_list = new List<int>();
							chk_list.Add(cod_chk);
						}
						reader.Close();

						//
						// Leggiamo i documenti necessari dalla checklist legata alla mansione
						//
						var chk_arr = new List<CheckListDb>();
						cmd.CommandText = @"
						SELECT *
						FROM checklist
						WHERE chk_tipo = ? AND chk_codice IN(
							SELECT mac_chk
							FROM chkmansioni
							WHERE mac_man = ?
						)
						ORDER BY chk_codice";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)CheckListType.DIPENDENTI;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dma.dma_man;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var chk = new CheckListDb();
							DbUtils.SqlRead(ref reader, ref chk);
							chk_arr.Add(chk);
						}
						reader.Close();

						//
						// Inseriamo i documenti non presenti
						//
						cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(doc_codice),0) AS codice FROM documenti WHERE doc_dit = ? AND doc_dip = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dma.dma_dit;
						cmd.Parameters.Add("coddip", OdbcType.Int).Value = dma.dma_dip;
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
							doc.doc_dit = dma.dma_dit;
							doc.doc_dip = dma.dma_dip;
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
						if (!DbUtils.IsDupKeyErr(ex)) throw;
						Reload(ref cmd, ref dma, joined);
					}
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dma, "dipmansioni", "WHERE dma_dit = ? AND dma_dip = ? AND dma_man = ?", ExcludeFields);
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = dma.dma_dit;
					cmd.Parameters.Add("coddip", OdbcType.Int).Value = dma.dma_dip;
					cmd.Parameters.Add("codman", OdbcType.Int).Value = dma.dma_man;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref dma, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM dipmansioni WHERE dma_dit = ? AND dma_dip = ? AND dma_man = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dma.dma_dit;
						cmd.Parameters.Add("coddip", OdbcType.Int).Value = dma.dma_dip;
						cmd.Parameters.Add("codman", OdbcType.Int).Value = dma.dma_man;
						cmd.ExecuteNonQuery();

						//
						// Leggiamo la checklist della mansione da rimuovere
						//
						var chk_sel = new List<CheckListDb>();
						cmd.CommandText = @"
						SELECT *
						FROM checklist
						WHERE chk_tipo = ? AND chk_codice IN(
							SELECT mac_chk
							FROM chkmansioni
							WHERE mac_man = ?
						)
						ORDER BY chk_codice";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)CheckListType.DIPENDENTI;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dma.dma_man;
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var chk = new CheckListDb();
							DbUtils.SqlRead(ref reader, ref chk);
							chk_sel.Add(chk);
						}
						reader.Close();

						//
						// Leggiamo tutte le mansioni legate al dipendente
						//
						List<int> man_list = null;
						cmd.CommandText = @"
						SELECT dma_man
						FROM dipmansioni
						WHERE dma_dit = ? AND dma_dip = ? AND dma_man <> ?
						ORDER BY dma_man";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dma.dma_dit;
						cmd.Parameters.Add("coddip", OdbcType.Int).Value = dma.dma_dip;
						cmd.Parameters.Add("codman", OdbcType.Int).Value = dma.dma_man;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var cod_man = reader.GetInt32(0);
							if (man_list == null) man_list = new List<int>();
							man_list.Add(cod_man);
						}
						reader.Close();

						//
						// Leggiamo tutti i codici delle checklist legate alle mansioni del dipendente
						//
						var chk_lock = new List<CheckListDb>();
						if (man_list != null)
						{
							foreach (var man in man_list)
							{
								cmd.CommandText = @"
								SELECT *
								FROM checklist
								WHERE chk_tipo = ? AND chk_codice IN(
									SELECT mac_chk
									FROM chkmansioni
									WHERE mac_man = ?
								)
								ORDER BY chk_codice";
								cmd.Parameters.Clear();
								cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)CheckListType.DIPENDENTI;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = man;
								 reader = cmd.ExecuteReader();
								while (reader.Read())
								{
									var chk = new CheckListDb();
									DbUtils.SqlRead(ref reader, ref chk);
									chk_lock.Add(chk);
								}
								reader.Close();
							}
						}

						//
						// Cancelliamo i documenti non compilati legati alla checklist della mansione ma non presenti nella checklist bloccata
						//
						foreach (var chk in chk_sel)
						{
							var found = false;
							foreach (var lck in chk_lock)
							{
								if (chk.chk_codice == lck.chk_codice)
								{
									found = true;
									break;
								}
							}
							if (found) continue;

							cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM documenti WHERE doc_dit = ? AND doc_dip = ? AND doc_chk = ? AND doc_data IS NULL");
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = dma.dma_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dma.dma_dip;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = chk.chk_codice;
							cmd.ExecuteNonQuery();
						}
					}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref DipMansioniDb dma, bool joined)
		{
			if (!Search(ref cmd, dma.dma_dit, dma.dma_dip, dma.dma_man, ref dma, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
