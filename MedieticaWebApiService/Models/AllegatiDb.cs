using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	internal enum AllegatiTipo : short
	{
		ALLEGATI_TYPE_DIPENDENTI = 0,
		ALLEGATI_TYPE_CANTIERI = 1,
		ALLEGATI_TYPE_DITTE = 2,
		ALLEGATI_TYPE_MEZZI = 3,
		ALLEGATI_TYPE_MANUTENZIONE_MEZZI = 4,
		ALLEGATI_TYPE_MODELLI = 5,
		ALLEGATI_TYPE_GIORNALE = 6,
		ALLEGATI_TYPE_CERTIFICATI_PAG = 7,
		ALLEGATI_TYPE_VISITE_MEDICHE = 8,
	}

	public class AllegatiDb
	{
		public int all_dit { get; set; }
		public short all_type { get; set; }
		public int all_doc { get; set; }
		public int all_idx { get; set; }
		public string all_desc { get; set; }
		public string all_fname { get; set; }
		public string all_local_fname { get; set; }
		public long all_bytes_size { get; set; }
		public DateTime? all_date_time { get; set; }
		public DateTime? all_created_at { get; set; }
		public DateTime? all_last_update { get; set; }
		
		//
		// Contenuto allegato
		//
		public byte[] all_data { get; set; }

		public AllegatiDb()
		{
			var all_db = this;
			DbUtils.Initialize(ref all_db);
		}

		private static readonly List<string> ExcludeFields = new List<string>() { "all_data" };

		public static List<string> GetExcludeFields()
		{
			return (ExcludeFields);
		}

		public static string SetupPath(int ditta, short tipo, int doc)
		{
			var upload_path = DbUtils.GetStartupOptions().DocPath.Trim();
			if (string.IsNullOrWhiteSpace(upload_path)) throw new MCException(MCException.AllegatiPathMsg, MCException.AllegatiPathErr);

			if (!upload_path.EndsWith("/") && !upload_path.EndsWith("\\")) upload_path += "/";
			Directory.CreateDirectory(upload_path);

			upload_path += $"Ditta_{ditta:00000}";
			Directory.CreateDirectory(upload_path);

			switch (tipo)
			{
				case (short)AllegatiTipo.ALLEGATI_TYPE_DIPENDENTI:
					upload_path += "/Dipendenti";
					break;

				case (short)AllegatiTipo.ALLEGATI_TYPE_CANTIERI:
					upload_path += "/Cantieri";
					break;

				case (short)AllegatiTipo.ALLEGATI_TYPE_DITTE:
					upload_path += "/Ditte";
					break;

				case (short)AllegatiTipo.ALLEGATI_TYPE_MEZZI:
					upload_path += "/Mezzi";
					break;

				case (short)AllegatiTipo.ALLEGATI_TYPE_MANUTENZIONE_MEZZI:
					upload_path += "/ManutenzioneMezzi";
					break;

				case (short)AllegatiTipo.ALLEGATI_TYPE_MODELLI:
					upload_path += "/Modelli";
					break;

				case (short)AllegatiTipo.ALLEGATI_TYPE_GIORNALE:
					upload_path += "/GiornaliLavoro";
					break;

				case (short)AllegatiTipo.ALLEGATI_TYPE_CERTIFICATI_PAG:
					upload_path += "/CertificatiPagamento";
					break;

				case (short)AllegatiTipo.ALLEGATI_TYPE_VISITE_MEDICHE:
					upload_path += "/VisiteMediche";
					break;

				default:
					upload_path += "/Vari";
					break;
			}
			Directory.CreateDirectory(upload_path);

			upload_path += $"/{doc:00000}";
			Directory.CreateDirectory(upload_path);

			return (upload_path);
		}

		public static bool Search(ref OdbcCommand cmd, int codDit, short type, int codice, int index, ref AllegatiDb all, bool writeLock = false)
		{
			if (all != null) DbUtils.Initialize(ref all);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					var ret = Search(ref command, codDit, type, codice, index, ref all, writeLock);
					connection.Close();
					return (ret);
				}
			}

			var	sql = "SELECT * FROM allegati WHERE all_dit = ? AND all_type = ? AND all_doc = ? AND all_idx = ?";
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("type", OdbcType.SmallInt).Value = type;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
			cmd.Parameters.Add("index", OdbcType.Int).Value = index;
			var reader = cmd.ExecuteReader();
			var found = reader.HasRows;
			while (all != null && reader.Read())
			{
				DbUtils.SqlRead(ref reader, ref all, ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref AllegatiDb all, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref all);

			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR || msg == DbMessage.DB_REWRITE)
			{
				var old = new AllegatiDb();
				if (!Search(ref cmd, all.all_dit, all.all_type, all.all_doc, all.all_idx, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.all_last_update != all.all_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}


			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, all.all_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);


				switch (all.all_type)
				{
					case (short)AllegatiTipo.ALLEGATI_TYPE_DIPENDENTI:
						{
							DocDipendentiDb doc = null;
							if (!DocDipendentiDb.Search(ref cmd, all.all_dit, all.all_doc, ref doc)) throw new MCException(MCException.DocumentiMsg, MCException.DocumentiErr);
						}
						break;
					case (short)AllegatiTipo.ALLEGATI_TYPE_CANTIERI:
						{
							DocCantieriDb doc = null;
							if (!DocCantieriDb.Search(ref cmd, all.all_dit, all.all_doc, ref doc)) throw new MCException(MCException.DocumentiMsg, MCException.DocumentiErr);
						}
						break;
					case (short)AllegatiTipo.ALLEGATI_TYPE_DITTE:
						{
							DocDitteDb doc = null;
							if (!DocDitteDb.Search(ref cmd, all.all_dit, all.all_doc, ref doc)) throw new MCException(MCException.DocumentiMsg, MCException.DocumentiErr);
						}
						break;
					case (short)AllegatiTipo.ALLEGATI_TYPE_MEZZI:
						{
							DocMezziDb doc = null;
							if (!DocMezziDb.Search(ref cmd, all.all_dit, all.all_doc, ref doc)) throw new MCException(MCException.DocumentiMsg, MCException.DocumentiErr);
						}
						break;

					case (short)AllegatiTipo.ALLEGATI_TYPE_MANUTENZIONE_MEZZI:
						{
							//DocMezziDb doc = null;
							//if (!DocMezziDb.Search(cmd, all.all_dit, all.all_doc, ref doc)) throw new MCException(MCException.DocumentiMsg, MCException.DocumentiErr);
						}
						break;
					case (short)AllegatiTipo.ALLEGATI_TYPE_MODELLI:
						{
							DocModelliDb doc = null;
							if (!DocModelliDb.Search(ref cmd, all.all_dit, all.all_doc, ref doc)) throw new MCException(MCException.DocumentiMsg, MCException.DocumentiErr);
						}
						break;
					case (short)AllegatiTipo.ALLEGATI_TYPE_GIORNALE:
						{
							GiornaleLavoriDb gio = null;
							if (!GiornaleLavoriDb.Search(ref cmd, all.all_dit, all.all_doc, ref gio)) throw new MCException(MCException.GiornaliMsg, MCException.GiornaliErr);
						}
						break;
					case (short)AllegatiTipo.ALLEGATI_TYPE_CERTIFICATI_PAG:
						{
							CertificatiPagamentoDb cpa = null;
							if (!CertificatiPagamentoDb.Search(ref cmd, all.all_dit, all.all_doc, ref cpa)) throw new MCException(MCException.GiornaliMsg, MCException.GiornaliErr);
						}
						break;
				}

			}

			if (all.all_data.Length != 0) all.all_bytes_size = all.all_data.Length;

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref all, "allegati", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref all);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								all.all_idx++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_UPDATE:
				case DbMessage.DB_REWRITE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref all, "allegati", "WHERE all_dit = ? AND all_type = ? AND all_doc = ? AND all_idx = ?", all.all_data.Length != 0 ? null : ExcludeFields);
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = all.all_dit;
					cmd.Parameters.Add("type", OdbcType.SmallInt).Value = all.all_type;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = all.all_doc;
					cmd.Parameters.Add("index", OdbcType.Int).Value = all.all_idx;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref all);
					break;

				case DbMessage.DB_DELETE:
				case DbMessage.DB_CLEAR:
					cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM allegati WHERE all_dit = ? AND all_type = ? AND all_doc = ? AND all_idx = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = all.all_dit;
					cmd.Parameters.Add("type", OdbcType.SmallInt).Value = all.all_type;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = all.all_doc;
					cmd.Parameters.Add("index", OdbcType.Int).Value = all.all_idx;
					cmd.ExecuteNonQuery();
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
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref AllegatiDb all)
		{
			if (!Search(ref cmd, all.all_dit, all.all_type, all.all_doc, all.all_idx, ref all)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}
	}

}
