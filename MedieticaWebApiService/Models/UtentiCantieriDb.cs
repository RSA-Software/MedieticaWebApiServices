using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Models;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public enum UserCantieriType : short
	{
		RUP = 0,		// Accesso Committenti
		CSE = 1,				// Accesso CSE
		DL = 2,					// Accesso Direttore Lavori
	}

	public class UtentiCantieriDb
	{
		public long usc_codice { get; set; }
		public int usc_dit { get; set; }
		public int usc_can { get; set; }
		public short usc_tipo { get; set; }
		public string usc_rag_soc1 { get; set; }
		public string usc_rag_soc2 { get; set; }
		public string usc_desc { get; set; }
		public string usc_email { get; set; }
		public string usc_pec { get; set; }
		public DateTime? usc_created_at { get; set; }
		public DateTime? usc_last_update { get; set; }

		public UtentiCantieriDb()
		{
			var usc_db = this;
			DbUtils.Initialize(ref usc_db);
		}

		private static readonly List<string> ExcludeFields = new List<string>() { "usc_codice" };

		public static bool Search(ref OdbcCommand cmd, long codice, ref UtentiCantieriDb usc, bool writeLock = false)
		{
			if (usc != null) DbUtils.Initialize(ref usc);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref usc, writeLock);
				}
			}
			var found = false;

			var sql = DbUtils.QueryAdapt("SELECT * FROM usrcantieri WHERE usc_codice = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";

			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (usc != null) DbUtils.SqlRead(ref reader, ref usc);
				found = true;
			}
			reader.Close();
			return (found);
		}
		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref UtentiCantieriDb usc, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref usc);
			usc.usc_desc = (usc.usc_rag_soc1 + " " + usc.usc_rag_soc2).Trim();

			var old = new UtentiCantieriDb();
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR || msg == DbMessage.DB_REWRITE)
			{
				if (!Search(ref cmd, usc.usc_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.usc_last_update != usc.usc_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(usc.usc_rag_soc1)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({usc.usc_dit} - {usc.usc_codice}) : Cognome", MCException.CampoObbligatorioErr);
				if (string.IsNullOrWhiteSpace(usc.usc_rag_soc2)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({usc.usc_dit} - {usc.usc_codice}) : Nome", MCException.CampoObbligatorioErr);

				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, usc.usc_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				CantieriDb can = null;
				if (!CantieriDb.Search(ref cmd, usc.usc_dit, usc.usc_can, ref can)) throw new MCException(MCException.CantiereMsg, MCException.CantiereErr);
			}

			switch (msg)
			{
					case DbMessage.DB_INSERT:
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref usc, "usrcantieri", null, ExcludeFields);
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							DbUtils.SqlRead(ref reader, ref usc);
						}
						reader.Close();

						if (!string.IsNullOrWhiteSpace(usc.usc_email))
						{
							cmd.CommandText = DbUtils.QueryAdapt("SELECT * FROM utenti WHERE ute_email = ?", 1);
							cmd.Parameters.Clear();
							cmd.Parameters.Add("email", OdbcType.VarChar).Value = usc.usc_email;

							var found = false;
							var ute = new UtentiDb();
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								found = true;
								DbUtils.SqlRead(ref reader, ref ute, UtentiDb.GetJoinExcludeFields());
							}

							reader.Close();
							if (found)
							{
								if (ute.ute_type != (int)UserType.SPECIAL) throw new MCException(MCException.UserTypeMsg, MCException.UserTypeErr);
							}
							else
							{
								ute = new UtentiDb();
								ute.ute_codice = 1;
								cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(ute_codice),0) AS codice FROM utenti");
								reader = cmd.ExecuteReader();
								while (reader.Read())
								{
									ute.ute_codice += reader.GetInt32(reader.GetOrdinal("codice"));
								}
								reader.Close();

								int val = 0;
								var password = System.Web.Security.Membership.GeneratePassword(10, 2);
								ute.ute_rag_soc1 = usc.usc_rag_soc1;
								ute.ute_rag_soc2 = usc.usc_rag_soc2;
								ute.ute_desc = usc.usc_desc;
								ute.ute_email = usc.usc_email;
								ute.ute_type = (int)UserType.SPECIAL;
								ute.ute_level = (int)UserLevel.PRIVATE;
								ute.ute_password = password;
								UtentiDb.Write(ref cmd, DbMessage.DB_INSERT, ref ute, ref val);
							}

							//
							// Colleghiamo l'account alla ditta
							//
							var utd = new UtentiDitteDb();
							utd.utd_dit = usc.usc_dit;
							utd.utd_ute = ute.ute_codice;
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref utd, "uteditte", null, UtentiDitteDb.GetJoinExcludeFields());
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException ex)
							{
								if (!DbUtils.IsDupKeyErr(ex)) throw;
							}
						}
					}
					break;

				case DbMessage.DB_UPDATE:
				case DbMessage.DB_REWRITE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref usc, "usrcantieri", "WHERE usc_codice = ?");
					cmd.Parameters.Add("codice", OdbcType.BigInt).Value = usc.usc_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref usc);
					break;

				case DbMessage.DB_DELETE:
				case DbMessage.DB_CLEAR:
					cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM usrcantieri WHERE usc_codice = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("codice", OdbcType.BigInt).Value = usc.usc_codice;
					cmd.ExecuteNonQuery();
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref UtentiCantieriDb usc)
		{
			if (!Search(ref cmd, usc.usc_codice, ref usc))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
