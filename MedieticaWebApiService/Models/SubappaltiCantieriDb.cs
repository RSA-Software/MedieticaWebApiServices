using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class SubAppaltiCantieriDb
	{
		public long sub_codice { get; set; }
		public int sub_dit_app { get; set; }
		public int sub_can_app { get; set; }
		public int sub_dit_sub { get; set; }
		public int sub_can_sub { get; set; }
		public DateTime? sub_created_at { get; set; }
		public DateTime? sub_last_update { get; set; }

		//
		// Tabelle Relazionate
		//
		public string img_data { get; set; }
		public string dit_desc { get; set; }
		public string dit_piva { get; set; }
		public string dit_codfis { get; set; }
		public string can_desc { get; set; }

		private static readonly List<string> ExcludeFields = new List<string>() { "img_data", "dit_desc", "dit_piva", "dit_codfis", "can_desc" };
		private static readonly List<string> InsertExcludeFields = new List<string>() { "sub_codice", "img_data", "dit_desc", "dit_piva", "dit_codfis", "can_desc" };

		private static readonly string JoinQuery = @"
		SELECT subappalti.*, dit_desc, dit_piva, dit_codfis, can_desc, img_data 
		FROM subappalti
		LEFT JOIN ditte ON (sub_dit_sub = dit_codice)
		LEFT JOIN cantieri ON (sub_dit_sub = can_dit AND sub_can_sub = can_codice)
		LEFT JOIN imgditte ON sub_dit_sub = img_dit AND sub_dit_sub = img_codice AND img_formato = 1";

		private static readonly string CountJoinQuery = @"
		SELECT COUNT(*)
		FROM subappalti
		LEFT JOIN ditte ON (sub_dit_sub = dit_codice)
		LEFT JOIN cantieri ON (sub_dit_sub = can_dit AND sub_can_sub = can_codice)";

		public SubAppaltiCantieriDb()
		{
			var sub_db = this;
			DbUtils.Initialize(ref sub_db);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}

		public static string GetCountJoinQuery()
		{
			return (CountJoinQuery);
		}

		public static bool Search(ref OdbcCommand cmd, int codDitApp, int codCanApp, int codDitSub, int codCanSub, ref SubAppaltiCantieriDb mec, bool joined = false, bool writeLock = false)
		{
			if (mec != null) DbUtils.Initialize(ref mec);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDitApp, codCanApp, codDitSub, codCanSub, ref mec, writeLock);
				}
			}

			var found = false;
			string sql;

			if (joined)
				sql = JoinQuery + " WHERE sub_dit_app = ? AND sub_can_app = ? AND sub_dit_sub = ? AND sub_can_sub = ?";
			else
				sql = "SELECT * FROM subappalti sub_dit_app = ? AND sub_can_app = ? AND sub_dit_sub = ? AND sub_can_sub = ?";
			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
		
			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codditapp", OdbcType.Int).Value = codDitApp;
			cmd.Parameters.Add("codcanapp", OdbcType.Int).Value = codCanApp;
			cmd.Parameters.Add("codditsub", OdbcType.Int).Value = codDitSub;
			cmd.Parameters.Add("codcansub", OdbcType.Int).Value = codCanSub;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (mec != null) DbUtils.SqlRead(ref reader, ref mec, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static bool Search(ref OdbcCommand cmd, long codice, ref SubAppaltiCantieriDb mec, bool joined = false, bool writeLock = false)
		{
			if (mec != null) DbUtils.Initialize(ref mec);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					cmd = new OdbcCommand { Connection = connection };
					return Search(ref cmd, codice, ref mec, writeLock);
				}
			}

			var found = false;
			string sql;

			if (joined)
				sql = JoinQuery + " WHERE sub_codice = ?";
			else
				sql = "SELECT * FROM subappalti WHERE sub_codice = ?";
			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";

			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (mec != null) DbUtils.SqlRead(ref reader, ref mec, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}


		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref SubAppaltiCantieriDb sub, ref object obj, bool joined = false)
		{
			var can = new CantieriDb();
			var dit = new DitteDb();

			DbUtils.Trim(ref sub);
			
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR || msg == DbMessage.DB_REWRITE)
			{
				var old = new SubAppaltiCantieriDb();
				if (!Search(ref cmd, sub.sub_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.sub_last_update != sub.sub_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (sub.sub_dit_app == 0 || !DitteDb.Search(ref cmd, sub.sub_dit_app, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);
				if (sub.sub_can_app == 0 || !CantieriDb.Search(ref cmd, sub.sub_dit_app, sub.sub_can_app, ref can)) throw new MCException(MCException.CantiereMsg, MCException.CantiereErr);

				var yyy = new DitteDb();
				if (sub.sub_dit_sub == 0|| !DitteDb.Search(ref cmd, sub.sub_dit_sub, ref yyy)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				if (string.Compare(dit.dit_piva, yyy.dit_piva, StringComparison.CurrentCulture) == 0) throw new MCException(MCException.DittaPivaMsg, MCException.DittaPivaErr);
				if (string.Compare(dit.dit_codfis, yyy.dit_codfis, StringComparison.CurrentCulture) == 0) throw new MCException(MCException.DittaCodfisMsg, MCException.DittaCodfisErr);
				
				CantieriDb xxx = null;
				if (!CantieriDb.Search(ref cmd, sub.sub_dit_sub, sub.sub_can_sub, ref xxx)) throw new MCException(MCException.CantiereMsg, MCException.CantiereErr);
			}

			switch (msg)
			{
					case DbMessage.DB_INSERT:
					{
						//
						// Inseriamo il cantiere per la ditta subappaltatrice
						//
						do
						{
							try
							{
								can.can_dit = sub.sub_dit_sub;
								can.can_codice = 1;
								can.can_subappalto = true;
								cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(can_codice),0) AS codice FROM cantieri WHERE can_dit = ?");
								cmd.Parameters.Clear();
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = sub.sub_dit_sub;
								var reader = cmd.ExecuteReader();
								while (reader.Read())
								{
									can.can_codice = 1 + reader.GetInt32(reader.GetOrdinal("codice"));
								}
								reader.Close();

								can.can_impresa_aggiudicataria = dit.dit_desc;
								CantieriDb.Write(ref cmd, DbMessage.DB_INSERT, ref can, ref obj);
								CantieriDb.Reload(ref cmd, ref can, false);
							}
							catch (OdbcException ex)
							{
								if (DbUtils.IsDupKeyErr(ex))
								{
									can.can_codice++;
									continue;
								}
								throw;
							}
							break;
						} while (true);

						//
						// Inseriamo il subappalto
						//
						try
						{
							sub.sub_can_sub = can.can_codice;
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref sub, "subappalti", null, InsertExcludeFields);
							var rdr = cmd.ExecuteReader();
							while (rdr.Read())
							{
								DbUtils.SqlRead(ref rdr, ref sub, ExcludeFields);
							}
							rdr.Close();
							Reload(ref cmd, ref sub, joined);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex)) throw new MCException(MCException.DittaSubMsg, MCException.DittaSubErr);
							throw;
						}
					}
					break;

				case DbMessage.DB_UPDATE:
				case DbMessage.DB_REWRITE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref sub, "subappalti", "WHERE sub_codice = ?", ExcludeFields);
					cmd.Parameters.Add("coddit", OdbcType.BigInt).Value = sub.sub_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref sub, joined);
					break;

				case DbMessage.DB_DELETE:
				case DbMessage.DB_CLEAR:
					if (sub.sub_dit_sub != 0 && sub.sub_can_sub != 0 )
					{
						if (CantieriDb.Search(ref cmd, sub.sub_dit_sub, sub.sub_can_sub, ref can)) CantieriDb.Write(ref cmd, DbMessage.DB_DELETE, ref can, ref obj);
					}
					cmd.CommandText = "DELETE FROM subappalti WHERE sub_codice = ?";
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.BigInt).Value = sub.sub_codice;
					cmd.ExecuteNonQuery();
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref SubAppaltiCantieriDb sub, bool joined)
		{
			if (!Search(ref cmd, sub.sub_codice, ref sub, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
