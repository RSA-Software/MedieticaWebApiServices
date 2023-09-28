using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class ModSerialDb
	{
		public int mse_dit { get; set; }
		public int mse_dmo { get; set; }
		public int mse_codice { get; set; }
		public string mse_cod_for { get; set; }
		public string mse_serial_start { get; set; }
		public string mse_serial_stop { get; set; }
		public DateTime? mse_created_at { get; set; }
		public DateTime? mse_last_update { get; set; }
		public int mse_user { get; set; }
		
		public ModSerialDb()
		{
			var mse_db = this;
			DbUtils.Initialize(ref mse_db);
		}


		public static bool Search(ref OdbcCommand cmd, int codDit, int codDmo, int codice, ref ModSerialDb mse, bool writeLock = false)
		{
			if (mse != null) DbUtils.Initialize(ref mse);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codDmo, codice, ref mse, writeLock);
				}
			}

			var found = false;
			var sql = "SELECT * FROM modserial WHERE mse_dit = ? AND mse_dmo = ? AND mse_codice = ?";

			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql,1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("coddmo", OdbcType.Int).Value = codDmo;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (mse != null) DbUtils.SqlRead(ref reader, ref mse);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref ModSerialDb mse, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref mse);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new ModSerialDb();
				if (!Search(ref cmd, mse.mse_dit, mse.mse_dmo, mse.mse_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.mse_last_update != mse.mse_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, mse.mse_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				DocModelliDb dmo = null;
				if (!DocModelliDb.Search(ref cmd, mse.mse_dit, mse.mse_dmo, ref dmo)) throw new MCException(MCException.DocModelloMsg, MCException.DocModelloErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref mse, "modserial");
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref mse);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								mse.mse_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref mse, "modserial", "WHERE mse_dit = ? AND mse_dmo = ? AND mse_codice = ?");
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = mse.mse_dit;
					cmd.Parameters.Add("coddmo", OdbcType.Int).Value = mse.mse_dmo;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = mse.mse_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref mse);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM modserial WHERE mse_dit = ? AND mse_dmo = ? AND mse_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = mse.mse_dit;
						cmd.Parameters.Add("coddmo", OdbcType.Int).Value = mse.mse_dmo;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mse.mse_codice;
						cmd.ExecuteNonQuery();
					}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref ModSerialDb mse)
		{
			if (!Search(ref cmd, mse.mse_dit, mse.mse_dmo, mse.mse_codice, ref mse)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
