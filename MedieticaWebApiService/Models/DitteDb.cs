using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class DitteDb
	{
		public int dit_codice { get; set; }
		public int dit_gru { get; set; }
		public string dit_rag_soc1 { get; set; }
		public string dit_rag_soc2 { get; set; }
		public string dit_desc { get; set; }
		public string dit_indirizzo { get; set; }
		public string dit_citta { get; set; }
		public string dit_cap { get; set; }
		public string dit_note { get; set; }
		public string dit_prov { get; set; }
		public string dit_piva { get; set; }
		public string dit_codfis { get; set; }
		public int	dit_riv { get; set; }
		public string dit_email { get; set; }
		public string dit_pec { get; set; }
		public string dit_tel1 { get; set; }
		public string dit_tel2 { get; set; }
		public string dit_cel { get; set; }
		public string dit_matricola_inps { get; set; }
		public bool dit_reseller { get; set; }
		public bool dit_subappaltatrice { get; set; }
		public DateTime? dit_created_at { get; set; }
		public DateTime? dit_last_update { get; set; }

		//
		// Campi Relazionati
		//
		public string img_data { get; set; }
		public List<ImgDitteDb> img_list { get; set; }
		
		private static readonly List<string> ExcludeFields = new List<string>() { "img_data", "img_list" };

		private static readonly string JoinQuery = @"
		SELECT *, img_data, NULL AS img_list
		FROM ditte
		LEFT JOIN imgditte ON dit_codice = img_dit AND dit_codice = img_codice AND img_formato = 1";

		private static readonly string CountQuery = @"
		SELECT COUNT(*) 
		FROM ditte";
		
		public DitteDb()
		{
			var dit_db = this;
			DbUtils.Initialize(ref dit_db);
		}

		public static string GetTableDescription()
		{
			return ("Ditte");
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}
		public static string GetCountQuery()
		{
			return (CountQuery);
		}


		public static bool Search(ref OdbcCommand cmd, int codice, ref DitteDb dit, bool joined = false, bool writeLock = false)
		{
			if (dit != null) DbUtils.Initialize(ref dit);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref dit, joined, writeLock);
				}
			}

			var found = false;

			string sql;
			if (joined)
				sql = GetJoinQuery() + " WHERE dit_codice = ?";
			else
				sql = "SELECT * FROM ditte WHERE dit_codice = ?";

			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql,1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (dit != null) DbUtils.SqlRead(ref reader, ref dit, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref DitteDb dit, ref int codute, bool joined = false)
		{
			DbUtils.Trim(ref dit);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new DitteDb();
				if (!Search(ref cmd, dit.dit_codice, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.dit_last_update != dit.dit_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}
			
			dit.dit_desc = dit.dit_rag_soc1.Trim() + " " + dit.dit_rag_soc2.Trim();
			dit.dit_desc = dit.dit_desc.Trim();
			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(dit.dit_rag_soc1)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({dit.dit_codice}) : rag_soc1", MCException.CampoObbligatorioErr);
				if (string.IsNullOrWhiteSpace(dit.dit_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({dit.dit_codice}) : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref dit, "ditte", null, ExcludeFields);
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dit, "ditte", "WHERE dit_codice = ?", ExcludeFields);
								cmd.Parameters.Add("codice", OdbcType.Int).Value = dit.dit_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({dit.dit_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref dit, "ditte", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref dit, joined);

							object obj = null;
							if (codute != 0)
							{
								var utd = new UtentiDitteDb();
								utd.utd_dit = dit.dit_codice;
								utd.utd_ute = codute;
								UtentiDitteDb.Write(ref cmd, DbMessage.DB_INSERT, ref utd, ref obj);
							}

							//
							// Creiamo/aggiorniamo la sede principale
							//
							if (!string.IsNullOrEmpty(dit.dit_citta) && !string.IsNullOrWhiteSpace(dit.dit_indirizzo))
							{
								var sedmsg = DbMessage.DB_INSERT;
								var sed = new SediDitteDb();
								if (SediDitteDb.Search(ref cmd, dit.dit_codice, 1, ref sed)) sedmsg = DbMessage.DB_UPDATE;
								sed.sed_dit = dit.dit_codice;
								sed.sed_codice = 1;
								sed.sed_indirizzo = dit.dit_indirizzo;
								sed.sed_citta = dit.dit_citta;
								sed.sed_cap = dit.dit_cap;
								sed.sed_prov = dit.dit_prov;
								SediDitteDb.Write(ref cmd, sedmsg, ref sed, ref obj);
							}

						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								dit.dit_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
				{
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dit, "ditte", "WHERE dit_codice = ?", ExcludeFields);
					cmd.Parameters.Add("@codice", OdbcType.Int).Value = dit.dit_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref dit, joined);

						//
						// Creiamo/aggiorniamo la sede principale
						//
						if (!string.IsNullOrEmpty(dit.dit_citta) && !string.IsNullOrWhiteSpace(dit.dit_indirizzo))
						{
							object obj = null;
							var sedmsg = DbMessage.DB_INSERT;
							var sed = new SediDitteDb();
							if (SediDitteDb.Search(ref cmd, dit.dit_codice, 1, ref sed)) sedmsg = DbMessage.DB_UPDATE;
							sed.sed_dit = dit.dit_codice;
							sed.sed_codice = 1;
							sed.sed_indirizzo = dit.dit_indirizzo;
							sed.sed_citta = dit.dit_citta;
							sed.sed_cap = dit.dit_cap;
							sed.sed_prov = dit.dit_prov;
							SediDitteDb.Write(ref cmd, sedmsg, ref sed, ref obj);
						}
				}
				break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM allegati WHERE all_dit = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dit.dit_codice;
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM imgditte WHERE img_dit = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dit.dit_codice; 
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM docditte WHERE dod_dit = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dit.dit_codice;
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM sedditte WHERE sed_dit = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dit.dit_codice;
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM ditte WHERE dit_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dit.dit_codice;
						cmd.ExecuteNonQuery();
					}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref DitteDb dit, bool joined)
		{
			if (!Search(ref cmd,dit.dit_codice, ref dit, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
