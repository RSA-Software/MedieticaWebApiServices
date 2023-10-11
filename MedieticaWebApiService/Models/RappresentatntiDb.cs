using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class RappresentantiDb
	{
		public long rap_codice { get; set; }
		public long rap_cli { get; set; }
		public long rap_pot { get; set; }
		public long rap_car { get; set; }
		public string rap_cognome { get; set; }
		public string rap_nome { get; set; }
		public string rap_desc { get; set; }
		public DateTime? rap_data_nascita { get; set; }
		public string rap_luogo_nascita { get; set; }
		public string rap_prov_nascita { get; set; }
		public string rap_cap_nascita { get; set; }
		public string rap_codfis { get; set; }
		public bool rap_esposto { get; set; }
		public double rap_quota { get; set; }
		public DateTime? rap_scadenza { get; set; }
		public bool rap_fino_a_revoca { get; set; }
		public string rap_note { get; set; }
		public DateTime? rap_created_at { get; set; }
		public DateTime rap_last_update { get; set; }
		public int rap_user { get; set; }

		//
		// Joined fields
		//
		public string pot_desc { get; set; }
		public string car_desc { get; set; }


		public RappresentantiDb()
		{
			var rap_db = this;
			DbUtils.Initialize(ref rap_db);
		}

		private static readonly List<string> InsExcludeFields = new List<string>() { "pot_codice", "rap_desc", "pot_desc" };
		private static readonly List<string> ExcludeFields = new List<string>() { "rap_desc", "pot_desc" };

		private static readonly string JoinQuery = @"
		SELECT rappresentanti.*, pot_desc, car_desc
		FROM rappresentanti
		LEFT JOIN poteri ON rap_pot = pot_codice
		LEFT JOIN cariche ON rap_car = car_codice
		";

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}



		public static bool Search(ref OdbcCommand cmd, long codice, ref RappresentantiDb rap, bool joined = false,  bool writeLock = false)
		{
			if (rap != null) DbUtils.Initialize(ref rap);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref rap, joined, writeLock);
				}
			}

			var found = false;
			string sql;
			if (!joined)
				sql = "SELECT * FROM componenti WHERE rap_codice = ?";
			else
				sql = GetJoinQuery() + " WHERE rap_codice = ?";
			if (writeLock && !joined) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;
			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (rap != null) DbUtils.SqlRead(ref reader, ref rap, joined ? null : ExcludeFields );
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref RappresentantiDb rap, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref rap);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new RappresentantiDb();
				if (!Search(ref cmd, rap.rap_codice, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.rap_last_update != rap.rap_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(rap.rap_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({rap.rap_codice}) : desc", MCException.CampoObbligatorioErr);

				ClientiDb cli = null;
				if (rap.rap_cli == 0 || !ClientiDb.Search(ref cmd, rap.rap_cli, ref cli)) throw new MCException(MCException.ClientiMsg, MCException.ClientiErr);

				PoteriDb pot = null;
				if (!PoteriDb.Search(ref cmd, rap.rap_pot, ref pot)) throw new MCException(MCException.PoteriMsg, MCException.PoteriErr);

				CaricheDb car = null;
				if (!CaricheDb.Search(ref cmd, rap.rap_car, ref car)) throw new MCException(MCException.CaricheMsg, MCException.CaricheErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref rap, "rappresentanti", null, InsExcludeFields);
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								DbUtils.SqlRead(ref reader, ref rap, ExcludeFields);
							}
							reader.Close();
							if (joined) Reload(ref cmd, ref rap, true);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								rap.rap_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref rap, "rappresentanti", "WHERE rap_codice = ?", ExcludeFields);
					cmd.Parameters.Add("@codice", OdbcType.BigInt).Value = rap.rap_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref rap, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM rappresentanti WHERE rap_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = rap.rap_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref RappresentantiDb rap, bool joined)
		{
			if (!Search(ref cmd, rap.rap_codice, ref rap, joined))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
