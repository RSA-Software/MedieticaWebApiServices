using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{

	public class ArtAnagDb
	{
		public string ana_codice { get; set; }
		public string ana_desc { get; set; }
		public string ana_ingredienti { get; set; }
		public DateTime? ana_created_at { get; set; }
		public DateTime? ana_last_update { get; set; }
		public double ana_sell_price { get; set; }
		public double ana_purchase_price { get; set; }
		public int ana_mer { get; set; }

		public string img_data { get; set; }
		public string mer_desc { get; set; }
		

		private static readonly List<string> ExcludeFields = new List<string>() { "img_data", "mer_desc" };

		private static readonly string JoinQuery = @"
		SELECT ana_codice,
				ana_desc,
				ana_ingredienti, 
				ana_sell_price, 
				ana_purchase_price,
				ana_mer,
				ana_created_at,
				ana_last_update,
				img_data,
				mer_desc
		FROM artanag
		LEFT JOIN catmerc ON ana_mer = mer_codice
		LEFT JOIN artimag ON ana_codice = img_codice AND img_formato = 1";

		public ArtAnagDb()
		{
			var ana_db = this;
			DbUtils.Initialize(ref ana_db);
		}
		public static List<string> GetJoinExcludeFields(bool all = true)
		{
			return(ExcludeFields);
		}

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}


		public static bool Search(ref OdbcCommand cmd, string codice, ref ArtAnagDb ana, bool joined = false, bool writeLock = false)
		{
			if (ana != null) DbUtils.Initialize(ref ana);
			if (codice == "") return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref ana, joined, writeLock);
				}
			}

			var found = false;

			string sql;
			if (joined)
				sql = GetJoinQuery() + " WHERE ana_codice = ?";
			else
				sql = DbUtils.QueryAdapt(@"SELECT * FROM artanag WHERE ana_codice = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("@codice", OdbcType.VarChar).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (ana != null) DbUtils.SqlRead(ref reader, ref ana, joined ? null : ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref ArtAnagDb ana, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref ana);

			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR || msg == DbMessage.DB_REWRITE)
			{
				var old = new ArtAnagDb();
				if (!Search(ref cmd, ana.ana_codice, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.ana_last_update != ana.ana_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg != DbMessage.DB_DELETE)
			{
				
			}
			
			switch (msg)
			{
					case DbMessage.DB_INSERT:
					{
						var idx = 0;
						do
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref ana, "artanag", null, ExcludeFields);
								cmd.ExecuteNonQuery();
								Reload(ref cmd, ref ana, joined);
							}
							catch (OdbcException ex)
							{
								if (DbUtils.IsDupKeyErr(ex))
								{
									idx++;
									//ana.ana_codice++;
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
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref ana, "artanag", "WHERE ana_codice = ?", ExcludeFields);
					cmd.Parameters.Add("@codice", OdbcType.VarChar).Value = ana.ana_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref ana, joined);
					break;

				case DbMessage.DB_DELETE:
				case DbMessage.DB_CLEAR:
					cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM artanag WHERE ana_codice = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("@codice", OdbcType.VarChar).Value = ana.ana_codice;
					cmd.ExecuteNonQuery();
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref ArtAnagDb ana, bool joined)
		{
			if (!Search(ref cmd, ana.ana_codice, ref ana, joined))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
