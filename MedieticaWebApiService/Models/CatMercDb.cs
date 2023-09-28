using System;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class CatMercDb
	{
		public int mer_codice { get; set; }
		public string mer_desc { get; set; }
		public DateTime? mer_created_at { get; set; }

		public DateTime? mer_last_update { get; set; }

		public CatMercDb()
		{
			var catmerc_db = this;
			DbUtils.Initialize(ref catmerc_db);
		}

		public static string GetTableDescription()
		{
			return ("Categorie Merceologiche");
		}


		public static bool Search(ref OdbcCommand cmd, int codice, ref CatMercDb mer, bool writeLock = false)
		{
			if (mer != null) DbUtils.Initialize(ref mer);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref mer, writeLock);
				}
			}

			var found = false;

			var sql = DbUtils.QueryAdapt("SELECT * FROM catmerc WHERE mer_codice = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("@codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (mer != null) DbUtils.SqlRead(ref reader, ref mer);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref CatMercDb mer, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref mer);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new CatMercDb();
				if (!Search(ref cmd, mer.mer_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.mer_last_update != mer.mer_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(mer.mer_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({mer.mer_codice}) : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref mer, "catmerc");
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref mer, "catmerc", "WHERE mer_codice = ?");
								cmd.Parameters.Add("@numero", OdbcType.Int).Value = mer.mer_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({mer.mer_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref mer, "catmerc");
							cmd.ExecuteNonQuery();
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								mer.mer_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref mer, "catmerc", "WHERE mer_codice = ?");
					cmd.Parameters.Add("@codice", OdbcType.Int).Value = mer.mer_codice;
					cmd.ExecuteNonQuery();
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("SELECT COUNT(*) FROM artanag WHERE ana_mer = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("@codice", OdbcType.Int).Value = mer.mer_codice;
						var num = Convert.ToInt32(cmd.ExecuteScalar());
						if (num > 0) throw new MCException(MCException.CancelMsg, MCException.CancelErr);

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM catmerc WHERE mer_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("@codice", OdbcType.Int).Value = mer.mer_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

	}
}
