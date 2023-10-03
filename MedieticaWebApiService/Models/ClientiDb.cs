using System;
using System.Collections.Generic;
using System.Data.Odbc;
using Chilkat;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class ClientiDb
	{
		public long cli_codice { get; set; }
		public string cli_rag_soc1 { get; set; }
		public string cli_rag_soc2 { get; set; }
		public string cli_desc { get; set; }
		public short cli_tipo { get; set; }
		public string cli_indirizzo { get; set; }
		public string cli_citta { get; set; }
		public string cli_cap { get; set; }
		public string cli_prov { get; set; }
		public string cli_piva { get; set; }
		public string cli_codfis { get; set; }
		public string cli_email { get; set; }
		public string cli_pec { get; set; }
		public string cli_tel1 { get; set; }
		public string cli_tel2 { get; set; }
		public string cli_cel { get; set; }
		public long cli_pgi { get; set; }
		public int cli_gru { get; set; }
		public long cli_tat { get; set; }
		public DateTime? cli_inizio_attivita{ get; set; }
		public string cli_ateco1 { get; set; }
		public string cli_ateco2 { get; set; }
		public string cli_interlocutore { get; set; }
		public string cli_int_funzione { get; set; }
		public string cli_int_telefono { get; set; }
		public string cli_int_email { get; set; }
		public DateTime? cli_data_nascita { get; set; }
		public string cli_luogo_nascita { get; set; }
		public string cli_prov_nascita { get; set; }
		public string cli_cap_nascita { get; set; }
		public long cli_att { get; set; }
		public string cli_note { get; set; }
		public DateTime? cli_created_at { get; set; }
		public DateTime? cli_last_update { get; set; }
		public int cli_user { get; set; }

		//
		// Campi Relazionati
		//
		public string img_data { get; set; }
		public List<ImgClientiDb> img_list { get; set; }
		
		private static readonly List<string> ExcludeFields = new List<string>() { "img_data", "img_list" };
		private static readonly List<string> ExcludeInsertFields = new List<string>() { "cli_codice", "img_data", "img_list" };

		private static readonly string JoinQuery = @"
		SELECT *, img_data, NULL AS img_list
		FROM ditte
		LEFT JOIN imgclienti ON cli_codice = img_dit AND cli_codice = img_codice AND img_formato = 1";

		private static readonly string CountQuery = @"
		SELECT COUNT(*) 
		FROM clienti";
		
		public ClientiDb()
		{
			var dit_db = this;
			DbUtils.Initialize(ref dit_db);
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


		public static bool Search(ref OdbcCommand cmd, long codice, ref ClientiDb cli, bool joined = false, bool writeLock = false)
		{
			if (cli != null) DbUtils.Initialize(ref cli);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref cli, joined, writeLock);
				}
			}

			var found = false;

			string sql;
			if (joined)
				sql = GetJoinQuery() + " WHERE cli_codice = ?";
			else
				sql = "SELECT * FROM clienti WHERE cli_codice = ?";

			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql,1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (cli != null) DbUtils.SqlRead(ref reader, ref cli, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref ClientiDb cli, ref int codute, bool joined = false)
		{
			DbUtils.Trim(ref cli);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new ClientiDb();
				if (!Search(ref cmd, cli.cli_codice, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.cli_last_update != cli.cli_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}
			
			cli.cli_desc = cli.cli_rag_soc1.Trim() + " " + cli.cli_rag_soc2.Trim();
			cli.cli_desc = cli.cli_desc.Trim();
			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(cli.cli_rag_soc1)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({cli.cli_codice}) : rag_soc1", MCException.CampoObbligatorioErr);
				if (string.IsNullOrWhiteSpace(cli.cli_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({cli.cli_codice}) : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref cli, "clienti", null, ExcludeInsertFields);
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								DbUtils.SqlRead(ref reader, ref cli);
							}
							reader.Close();
							if (joined)	Reload(ref cmd, ref cli, true);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								cli.cli_codice++;
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
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref cli, "clienti", "WHERE cli_codice = ?", ExcludeFields);
					cmd.Parameters.Add("@codice", OdbcType.BigInt).Value = cli.cli_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref cli, joined);
				}
				break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						/*
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
						*/
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM clienti WHERE cli_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.Int).Value = cli.cli_codice;
						cmd.ExecuteNonQuery();
					}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref ClientiDb cli, bool joined)
		{
			if (!Search(ref cmd, cli.cli_codice, ref cli, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
