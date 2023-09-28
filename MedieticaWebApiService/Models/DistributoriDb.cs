using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{

	public class DistributoriDb
	{
		public int dis_codice { get; set; }
		public string dis_desc { get; set; }
		public int dis_disabled { get; set; }
		public DateTime? dis_created_at { get; set; }
		public DateTime? dis_last_update { get; set; }

		private static readonly string JoinQuery = @"
		SELECT img_data, aco_ana, ana_desc, mer_desc, aco_esistenza, ana_sell_price, ana_mer
		FROM artcounter
		LEFT JOIN artanag ON aco_ana = ana_codice
		LEFT JOIN catmerc ON ana_mer = mer_codice
		LEFT JOIN artimag ON aco_ana = img_codice AND img_formato = 1";

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}

		public DistributoriDb()
		{
			var dis_db = this;
			DbUtils.Initialize(ref dis_db);
		}
		public static string GetTableDescription()
		{
			return ("Distributori");
		}

		public static bool Search(ref OdbcCommand cmd, int codice, ref DistributoriDb dis, bool writeLock = false)
		{
			if (dis != null) DbUtils.Initialize(ref dis);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref dis, writeLock);
				}
			}

			var found = false;

			string sql = DbUtils.QueryAdapt("SELECT * FROM distributori WHERE dis_codice = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("@codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (dis!= null) DbUtils.SqlRead(ref reader, ref dis);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref DistributoriDb dis, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref dis);

			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR || msg == DbMessage.DB_REWRITE)
			{
				var old = new DistributoriDb();
				if (!Search(ref cmd, dis.dis_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.dis_last_update != dis.dis_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(dis.dis_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({dis.dis_codice}) : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
					case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref dis, "distributori");
							cmd.ExecuteNonQuery();
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								dis.dis_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_UPDATE:
				case DbMessage.DB_REWRITE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dis, "distributori", "WHERE dis_codice = ?");
					cmd.Parameters.Add("@codice", OdbcType.Int).Value = dis.dis_codice;
					cmd.ExecuteNonQuery();
					break;

				case DbMessage.DB_DELETE:
				case DbMessage.DB_CLEAR:
					cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM distributori WHERE dis_codice = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("@codice", OdbcType.Int).Value = dis.dis_codice;
					cmd.ExecuteNonQuery();
					break;
			}
		}

	}
}
