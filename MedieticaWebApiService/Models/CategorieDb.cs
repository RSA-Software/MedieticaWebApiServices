using System;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class CategorieDb
	{
		public int cat_dit { get; set; }
		public int cat_codice { get; set; }
		public string cat_desc { get; set; }
		public string cat_title { get; set; }
		public short cat_data { get; set; }
		public short cat_ore { get; set; }
		public short cat_corso { get; set; }
		public short cat_norma { get; set; }
		public short cat_data_rilascio { get; set; }
		public short cat_date_scadenza { get; set; }
		public short cat_custom { get; set; }
		public string cat_custom_name { get; set; }
		public DateTime? cat_created_at { get; set; }
		public DateTime? cat_last_update { get; set; }

		public CategorieDb()
		{
			var catmerc_db = this;
			DbUtils.Initialize(ref catmerc_db);
		}

		public static string GetTableDescription()
		{
			return ("Categorie");
		}


		public static bool Search(ref OdbcCommand cmd, int codDit, int codice, ref CategorieDb cat, bool writeLock = false)
		{
			if (cat != null) DbUtils.Initialize(ref cat);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codice, ref cat, writeLock);
				}
			}

			var found = false;

			var sql = DbUtils.QueryAdapt("SELECT * FROM categorie WHERE cat_dit = ? AND cat_codice = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (cat != null) DbUtils.SqlRead(ref reader, ref cat);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref CategorieDb cat, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref cat);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new CategorieDb();
				if (!Search(ref cmd, cat.cat_dit, cat.cat_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.cat_last_update != cat.cat_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(cat.cat_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({cat.cat_dit} - {cat.cat_codice}) : desc", MCException.CampoObbligatorioErr);

				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, cat.cat_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref cat, "categorie");
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref cat, "categorie", "WHERE cat_dit = ? AND cat_codice = ?");
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = cat.cat_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = cat.cat_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({cat.cat_dit} - {cat.cat_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref cat, "categorie");
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref cat);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								cat.cat_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref cat, "categorie", "WHERE cat_dit = ? AND cat_codice = ?");
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = cat.cat_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = cat.cat_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref cat);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("SELECT COUNT(*) FROM documenti WHERE doc_dit = ? AND doc_cat = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = cat.cat_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = cat.cat_codice;
						var num = Convert.ToInt32(cmd.ExecuteScalar());
						if (num > 0) throw new MCException(MCException.CancelMsg, MCException.CancelErr);

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM categorie WHERE cat_dit = ? AND cat_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = cat.cat_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = cat.cat_codice;
						cmd.ExecuteNonQuery();
					}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref CategorieDb cat)
		{
			if (!Search(ref cmd, cat.cat_dit, cat.cat_codice, ref cat))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
