using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class DocDipendentiDb
	{
		public int doc_dit { get; set; }
		public int doc_codice { get; set; }
		public int doc_dip { get; set; }
		public short doc_livello { get; set; }
		public short doc_settore { get; set; }
		public short doc_ore { get; set; }
		public DateTime? doc_data { get; set; }
		public string doc_desc { get; set; }
		public string doc_url { get; set; }
		public DateTime? doc_data_rilascio { get; set; }
		public DateTime? doc_data_scadenza { get; set; }
		public short doc_scad_alert_before { get; set; }
		public short doc_scad_alert_after { get; set; }
		public string doc_rif_normativo { get; set; }
		public string doc_certificatore { get; set; }
		public int doc_chk { get; set; }
		public DateTime? doc_created_at { get; set; }
		public DateTime? doc_last_update { get; set; }

		//
		// Lista Allegati
		//
		public List<AllegatiDb> all_list;

		private static readonly List<string> ExcludeFields = new List<string>()
		{
			"all_list"
		};

		public DocDipendentiDb()
		{
			var doc_db = this;
			DbUtils.Initialize(ref doc_db);
		}

		public static string GetTableDescription()
		{
			return ("Documenti Dipendenti");
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static bool Search(ref OdbcCommand cmd, int codDit, int codice, ref DocDipendentiDb doc, bool writeLock = false)
		{
			if (doc != null) DbUtils.Initialize(ref doc);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codice, ref doc, writeLock);
				}
			}

			var found = false;

			var	sql = "SELECT * FROM documenti WHERE doc_dit = ? AND doc_codice = ?";

			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql,1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (doc != null) DbUtils.SqlRead(ref reader, ref doc);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref DocDipendentiDb doc, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref doc);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new DocDipendentiDb();
				if (!Search(ref cmd, doc.doc_dit, doc.doc_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.doc_last_update != doc.doc_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, doc.doc_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				DipendentiDb dip = null;
				if (!DipendentiDb.Search(ref cmd, doc.doc_dit, doc.doc_dip, ref dip)) throw new MCException(MCException.DipendenteMsg, MCException.DipendenteErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref doc, "documenti", null, ExcludeFields);
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref doc, "documenti", "WHERE doc_dit = ? AND doc_codice = ?", ExcludeFields);
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = doc.doc_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = doc.doc_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({doc.doc_dit} - {doc.doc_codice})", MCException.DuplicateErr);
								throw;
							}
						}
						else throw;
					}
					break;

				case DbMessage.DB_ADD:
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref doc, "documenti", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref doc);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								doc.doc_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref doc, "documenti", "WHERE doc_dit = ? AND doc_codice = ?", ExcludeFields);
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = doc.doc_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = doc.doc_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref doc);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM documenti WHERE doc_dit = ? AND doc_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = doc.doc_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = doc.doc_codice;
						cmd.ExecuteNonQuery();
					}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref DocDipendentiDb doc)
		{
			if (!Search(ref cmd, doc.doc_dit, doc.doc_codice, ref doc)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}
	}
}
