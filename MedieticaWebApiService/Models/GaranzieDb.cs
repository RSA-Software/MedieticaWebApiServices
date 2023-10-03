using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class GaranzieDb
	{
		public long gar_codice { get; set; }
		public string gar_desc { get; set; }
		public DateTime? gar_created_at { get; set; }

		public DateTime? gar_last_update { get; set; }
		public int gar_user { get; set; }

		public GaranzieDb()
		{
			var gar_db = this;
			DbUtils.Initialize(ref gar_db);
		}

		private static readonly List<string> ExcludeFields = new List<string>() { "gar_codice" };

		public static bool Search(ref OdbcCommand cmd, long codice, ref GaranzieDb gar, bool writeLock = false)
		{
			if (gar != null) DbUtils.Initialize(ref gar);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref gar, writeLock);
				}
			}

			var found = false;

			var sql = DbUtils.QueryAdapt("SELECT * FROM garanzie WHERE gar_codice = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (gar != null) DbUtils.SqlRead(ref reader, ref gar);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref GaranzieDb gar, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref gar);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new GaranzieDb();
				if (!Search(ref cmd, gar.gar_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.gar_last_update != gar.gar_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(gar.gar_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({gar.gar_codice}) : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref gar, "garanzie", null, ExcludeFields);
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								DbUtils.SqlRead(ref reader, ref gar);
							}
							reader.Close();
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								gar.gar_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref gar, "garanzie", "WHERE gar_codice = ?");
					cmd.Parameters.Add("@codice", OdbcType.BigInt).Value = gar.gar_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref gar);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
/*
						cmd.CommandText = DbUtils.QueryAdapt("SELECT COUNT(*) FROM ditte WHERE dit_gru = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.Int).Value = gru.gru_codice;
						var num = Convert.ToInt32(cmd.ExecuteScalar());
						if (num > 0) throw new MCException(MCException.CancelMsg, MCException.CancelErr);
*/

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM garanzie WHERE gar_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = gar.gar_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref GaranzieDb gar)
		{
			if (!Search(ref cmd, gar.gar_codice, ref gar))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
