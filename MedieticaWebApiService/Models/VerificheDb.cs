using System;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class VerificheDb
	{
		public int ver_codice { get; set; }
		public string ver_desc { get; set; }
		public short ver_funzionamento_anni { get; set; }
		public short ver_integrita_anni { get; set; }
		public short ver_interna_anni { get; set; }
		public DateTime? ver_created_at { get; set; }
		public DateTime? ver_last_update { get; set; }

		public VerificheDb()
		{
			var ver_db = this;
			DbUtils.Initialize(ref ver_db);
		}

		public static bool Search(ref OdbcCommand cmd, int codice, ref VerificheDb ver, bool writeLock = false)
		{
			if (ver != null) DbUtils.Initialize(ref ver);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref ver, writeLock);
				}
			}

			var found = false;

			var sql = DbUtils.QueryAdapt("SELECT * FROM verifiche WHERE ver_codice = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (ver != null) DbUtils.SqlRead(ref reader, ref ver);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref VerificheDb ver, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref ver);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new VerificheDb();
				if (!Search(ref cmd, ver.ver_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.ver_last_update != ver.ver_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(ver.ver_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({ver.ver_codice}) : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref ver, "verifiche");
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref ver, "verifiche", "WHERE ver_codice = ?");
								cmd.Parameters.Add("@numero", OdbcType.Int).Value = ver.ver_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({ver.ver_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref ver, "verifiche");
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref ver);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								ver.ver_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref ver, "verifiche", "WHERE ver_codice = ?");
					cmd.Parameters.Add("@codice", OdbcType.Int).Value = ver.ver_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref ver);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("SELECT COUNT(*) FROM modelli WHERE mod_ver = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.Int).Value = ver.ver_codice;
						var num = Convert.ToInt32(cmd.ExecuteScalar());
						if (num > 0) throw new MCException(MCException.CancelMsg, MCException.CancelErr);

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM verifiche WHERE ver_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.Int).Value = ver.ver_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref VerificheDb ver)
		{
			if (!Search(ref cmd, ver.ver_codice, ref ver)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
