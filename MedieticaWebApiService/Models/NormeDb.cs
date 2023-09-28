using System;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class NormeDb
	{
		public int nor_dit { get; set; }
		public int nor_codice { get; set; }
		public string nor_desc { get; set; }
		public DateTime? nor_created_at { get; set; }
		public DateTime? nor_last_update { get; set; }

		public NormeDb()
		{
			var nor_db = this;
			DbUtils.Initialize(ref nor_db);
		}

		public static string GetTableDescription()
		{
			return ("Norme");
		}


		public static bool Search(ref OdbcCommand cmd, int codDit, int codice, ref NormeDb nor, bool writeLock = false)
		{
			if (nor != null) DbUtils.Initialize(ref nor);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codice, ref nor, writeLock);
				}
			}

			var found = false;

			var sql = DbUtils.QueryAdapt("SELECT * FROM norme WHERE nor_dit = ? AND nor_codice = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (nor != null) DbUtils.SqlRead(ref reader, ref nor);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref NormeDb nor, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref nor);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new NormeDb();
				if (!Search(ref cmd, nor.nor_dit, nor.nor_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.nor_last_update != nor.nor_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(nor.nor_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({nor.nor_dit} - {nor.nor_codice}) : desc", MCException.CampoObbligatorioErr);

				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, nor.nor_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref nor, "norme");
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref nor, "norme", "WHERE nor_dit ? ? AND nor_codice = ?");
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = nor.nor_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = nor.nor_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({nor.nor_dit} - {nor.nor_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref nor, "norme");
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref nor);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								nor.nor_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref nor, "norme", "WHERE nor_dit = ? AND nor_codice = ?");
					cmd.Parameters.Add("@codice", OdbcType.Int).Value = nor.nor_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref nor);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("SELECT COUNT(*) FROM documenti WHERE doc_dit = ? AND doc_nor = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = nor.nor_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = nor.nor_codice;
						var num = Convert.ToInt32(cmd.ExecuteScalar());
						if (num > 0) throw new MCException(MCException.CancelMsg, MCException.CancelErr);

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM norme WHERE nor_dit = ? AND nor_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = nor.nor_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = nor.nor_codice;
						cmd.ExecuteNonQuery();
					}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref NormeDb nor)
		{
			if (!Search(ref cmd, nor.nor_dit, nor.nor_codice, ref nor))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
