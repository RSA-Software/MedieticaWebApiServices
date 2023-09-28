using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public enum CryptMethod : short
	{
		PBES = 0,
		SHA256 = 1,
	}

	public enum UserLevel
	{
		NOAUTH = -1,
		PUBLIC	= 0,
		PRIVATE = 1,
		ADMIN = 2,
		SENSIBLE = 3,
		RESELLER = 4,
		SUPERADMIN = 5,
	}
	public enum UserType
	{
		NORMAL = 0,				// Accesso Normale
		DIPENDENTE = 1,			// Accesso Dipendenti
		SPECIAL = 2,		// Accesso Committenti -  CSE  - Direttore Lavori
	}


	public class ChangePassword
	{
		public string new_password { get; set; }

		public ChangePassword()
		{
			new_password = "";
		}
	}


	public class UtentiDb
	{
		public int ute_codice { get; set; }
		public string ute_rag_soc1 { get; set; }
		public string ute_rag_soc2 { get; set; }
		public string ute_desc { get; set; }
		public string ute_indirizzo { get; set; }
		public string ute_cap { get; set; }
		public string ute_prov { get; set; }
		public string ute_citta { get; set; }
		public string ute_email { get; set; }
		public string ute_tel { get; set; }
		public string ute_cel { get; set; }
		public DateTime? ute_created_at { get; set; }
		public DateTime? ute_last_update { get; set; }
		public string ute_password { get; set; }
		public int ute_level { get; set; }
		public int ute_type { get; set; }

		public string img_data { get; set; }
		public List<ImgUtentiDb> img_list { get; set; }
		public List<UteUsgDb> utg_list { get; set; }
		

		private static readonly List<string> ExcludeFields = new List<string>() { "img_data", "img_list", "utg_list" };

		private static readonly string JoinQuery = @"
		SELECT utenti.*, img_data, NULL AS img_list, NULL AS utg_list
		FROM utenti
		LEFT JOIN imgutenti ON (ute_codice = img_codice AND img_formato = 1 AND img_dit = 0)";

		public UtentiDb()
		{
			var ute_db = this;
			DbUtils.Initialize(ref ute_db);
		}
		public static List<string> GetJoinExcludeFields(bool all = true)
		{
			return(ExcludeFields);
		}

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}

		public static void LoadPermessi(ref OdbcCommand cmd, int codice, ref List<PermessiDb> perList)
		{
			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					LoadPermessi(ref command , codice, ref perList);
					return;
				}
			}

			//
			// Leggiamo gli endpoints
			//
			if (perList == null) perList = new List<PermessiDb>();
			cmd.CommandText = "SELECT * FROM endpoints ORDER BY end_codice";
			cmd.Parameters.Clear();
			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				var end = new EndpointsDb();
				DbUtils.SqlRead(ref reader, ref end);

				var per = new PermessiDb();
				per.per_usg = codice;
				per.per_end = end.end_codice;
				per.per_view = false;
				per.per_add = false;
				per.per_update = false;
				per.per_delete = false;
				per.per_special1 = false;
				per.per_special2 = false;
				per.per_special3 = false;
				perList.Add(per);
			}
			reader.Close();

			//
			// Leggiamo i Gruppi
			//
			cmd.CommandText = "SELECT * FROM permessi WHERE per_usg IN (SELECT utg_usg FROM uteusg WHERE utg_ute = ?) ORDER BY per_usg, per_end";
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codute", OdbcType.Int).Value = codice;
			reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				var per = new PermessiDb();

				DbUtils.SqlRead(ref reader, ref per);
				per.per_usg = codice;

				var found = false;
				foreach (var p in perList)
				{
					if (p.per_usg == per.per_usg && p.per_end == per.per_end)
					{
						found = true;
						p.per_view = p.per_view || per.per_view;
						p.per_add = p.per_add || per.per_add;
						p.per_update = p.per_update || per.per_update;
						p.per_delete = p.per_delete || per.per_delete;
						p.per_special1 = p.per_special1 || per.per_special1;
						p.per_special2 = p.per_special2 || per.per_special2;
						p.per_special3 = p.per_special3 || per.per_special3;
						break;
					}
				}
				if (!found) perList.Add(per);
			}
			reader.Close();
		}


		public static bool Search(ref OdbcCommand cmd, int codice, ref UtentiDb ute, bool joined = false, bool writeLock = false)
		{
			if (ute != null) DbUtils.Initialize(ref ute);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					cmd = new OdbcCommand { Connection = connection };
					return Search(ref cmd, codice, ref ute, joined, writeLock);
				}
			}

			var found = false;

			string sql;
			if (joined)
				sql = GetJoinQuery() + " WHERE ute_codice = ?";
			else
			{
				sql = DbUtils.QueryAdapt("SELECT * FROM utenti WHERE ute_codice = ?", 1);
				if (writeLock) sql += " FOR UPDATE NOWAIT";
			}
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("@codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (ute != null) DbUtils.SqlRead(ref reader, ref ute, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();

			if (ute != null && joined)
			{
				cmd.CommandText = UteUsgDb.GetJoinQuery() + " WHERE utg_ute = ? ORDER BY utg_created_at";
				cmd.Parameters.Clear();
				cmd.Parameters.Add("@codice", OdbcType.Int).Value = codice;
				reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					var utg = new UteUsgDb();
					DbUtils.SqlRead(ref reader, ref utg);
					if (ute.utg_list == null) ute.utg_list = new List<UteUsgDb>();
					ute.utg_list.Add(utg);
				}
				reader.Close();
			}

			return (found);
		}
		public static bool Search(ref OdbcCommand cmd, string user, string password, ref UtentiDb ute, bool joined = false)
		{
			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, user, password, ref ute, joined);
				}
			}

			if (ute != null)
			{
				DbUtils.Initialize(ref ute);
			}

			string sql;
			if (joined)
				sql = GetJoinQuery() + " WHERE ute_userid = ? AND ute_password = ?";
			else
			{
				sql = DbUtils.QueryAdapt("SELECT * FROM admin.utenti1 WHERE ute_userid = ? AND ute_password = ?", 1);
			}
			cmd.CommandText = sql;
			
			cmd.Parameters.Clear();
			cmd.Parameters.Add("@user", OdbcType.VarChar).Value = user;
			cmd.Parameters.Add("@password", OdbcType.VarChar).Value = password;

			var reader = cmd.ExecuteReader();
			var found = reader.HasRows;
			while (ute != null && reader.Read())
			{
				DbUtils.SqlRead(ref reader, ref ute, joined ? null : ExcludeFields);
			}
			reader.Close();

			if (ute != null && joined)
			{
				cmd.CommandText = UteUsgDb.GetJoinQuery() + " WHERE utg_ute = ? ORDER BY utg_created_at";
				cmd.Parameters.Clear();
				cmd.Parameters.Add("@codice", OdbcType.Int).Value = ute.ute_codice;
				reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					var utg = new UteUsgDb();
					DbUtils.SqlRead(ref reader, ref utg);
					if (ute.utg_list == null) ute.utg_list = new List<UteUsgDb>();
					ute.utg_list.Add(utg);
				}
				reader.Close();
			}

			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref UtentiDb ute, ref int obj, bool joined = false)
		{
			DbUtils.Trim(ref ute);

			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR || msg == DbMessage.DB_REWRITE)
			{
				var old = new UtentiDb();
				if (!Search(ref cmd, ute.ute_codice, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.ute_last_update != ute.ute_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}
			
			switch (msg)
			{
					case DbMessage.DB_INSERT:
					{
						var idx = 0;
						var password = ute.ute_password;
						if (string.IsNullOrWhiteSpace(password)) password = System.Web.Security.Membership.GeneratePassword(10, 2);
						do
						{
							try
							{
								ute.ute_password = password;
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref ute, "utenti", null, ExcludeFields);
								cmd.ExecuteNonQuery();

								object obxj = null;

								//
								// Abbiniamo l'utente al gruppo di default
								//
								var utg = new UteUsgDb();
								utg.utg_ute = ute.ute_codice;
								utg.utg_usg = 1;
								UteUsgDb.Write(ref cmd, DbMessage.DB_INSERT, ref utg, ref obxj);

								if (obj != 0)
								{
									var utd = new UtentiDitteDb();
									utd.utd_dit = obj;
									utd.utd_ute = ute.ute_codice;
									UtentiDitteDb.Write(ref cmd, DbMessage.DB_INSERT, ref utd, ref obxj);
								}
								
								Reload(ref cmd, ref ute, joined);

								try
								{
									//
									// Leggiamo il template
									//
									if (!string.IsNullOrWhiteSpace(DbUtils.GetStartupOptions().EmailTemplate))
									{
										var path = $"{DbUtils.GetStartupOptions().EmailTemplate.Trim()}/email_signature.html";
										var message = File.ReadAllText(path);
										
										var recipients = new Recipients();
										//recipients.email_adress = "capizz.filippo.rsa@gmail.com";
										recipients.email_adress = ute.ute_email.Trim().ToLower();
										recipients.friendly_name = ute.ute_desc;
										var msg_body = $@"
<h1>E' stato creato il tuo account utente </h1>
<table cellpadding='0' cellspacing='10' class='table__StyledTable - sc - 1avdl6r - 0 kAbRZI' style='vertical - align: -webkit - baseline - middle; font - size: medium; font - family: Arial;' >
	<tbody>
		<tr>
			<td style='width:40%;'>Utente</td>
			<td style='margin-left:10%;'><b> {ute.ute_email} </b></td>
		</tr>
		<tr>
			<td style='width:40;'>Password</td>
			<td style='margin-left:10%;'><b> {password} </b></td>
		</tr>
		<tr>
			<td height='15'></td>
		</tr>
	<tbody>
</table>
<a href = 'https://www.docxplorer.it'>Accedi a DocXplorer</a>";
										message = message.Replace("~tag~", msg_body);
										var email = new EmailMessages();
										email.subject = "Creazione Account Utente DocXplorer";
										email.message = message;
										email.from = "Maria Franca Giordano <mfgiordano@rsaweb.com>";
										email.to = new List<Recipients>();
										email.to.Add(recipients);
										EmailMessages.Send(email);
									}
								}
								catch (Exception)
								{

								}
							}
							catch (OdbcException ex)
							{
								if (DbUtils.IsDupKeyErr(ex))
								{
									idx++;
									ute.ute_codice++;
									continue;
								}
								throw;
							}
							break;
						} while (true && idx <= 10);
					}
					break;

				case DbMessage.DB_UPDATE:
				case DbMessage.DB_REWRITE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref ute, "utenti", "WHERE ute_codice = ?", ExcludeFields);
					cmd.Parameters.Add("@numero", OdbcType.Int).Value = ute.ute_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref ute, joined);
					break;

				case DbMessage.DB_DELETE:
				case DbMessage.DB_CLEAR:
					if (ute.ute_codice == 1) throw new MCException(MCException.CancelMsg, MCException.CancelErr);

					cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM uteusg WHERE utg_ute = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("@codice", OdbcType.Int).Value = ute.ute_codice;
					cmd.ExecuteNonQuery();

					cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM utenti WHERE ute_codice = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("@codice", OdbcType.Int).Value = ute.ute_codice;
					cmd.ExecuteNonQuery();
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref UtentiDb ute, bool joined)
		{
			if (!Search(ref cmd, ute.ute_codice, ref ute, joined))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
