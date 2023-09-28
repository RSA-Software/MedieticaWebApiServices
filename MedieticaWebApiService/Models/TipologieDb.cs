using System;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class TipologieDb
	{
		public int tip_codice { get; set; }
		public string tip_desc { get; set; }
		public DateTime? tip_created_at { get; set; }
		public DateTime? tip_last_update { get; set; }

		public TipologieDb()
		{
			var tip_db = this;
			DbUtils.Initialize(ref tip_db);
		}

		public static string GetTableDescription()
		{
			return ("Mansioni");
		}
		
		public static bool Search(ref OdbcCommand cmd, int codice, ref TipologieDb tip, bool writeLock = false)
		{
			if (tip != null) DbUtils.Initialize(ref tip);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref tip, writeLock);
				}
			}

			var found = false;

			var sql = DbUtils.QueryAdapt("SELECT * FROM tipologie WHERE tip_codice = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (tip != null) DbUtils.SqlRead(ref reader, ref tip);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref TipologieDb tip, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref tip);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new TipologieDb();
				if (!Search(ref cmd, tip.tip_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.tip_last_update != tip.tip_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(tip.tip_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({tip.tip_codice}) : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref tip, "tipologie");
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref tip, "tipologie", "WHERE tip_codice = ?");
								cmd.Parameters.Add("codice", OdbcType.Int).Value = tip.tip_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({tip.tip_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref tip, "tipologie");
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref tip);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								tip.tip_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref tip, "tipologie", "WHERE tip_codice = ?");
					cmd.Parameters.Add("codice", OdbcType.Int).Value = tip.tip_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref tip);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("SELECT COUNT(*) FROM modelli WHERE mod_tip = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.Int).Value = tip.tip_codice;
						var num = Convert.ToInt32(cmd.ExecuteScalar());
						if (num > 0) throw new MCException(MCException.CancelMsg, MCException.CancelErr);

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM tipologie WHERE tip_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.Int).Value = tip.tip_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref TipologieDb tip)
		{
			if (!Search(ref cmd, tip.tip_codice, ref tip)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
