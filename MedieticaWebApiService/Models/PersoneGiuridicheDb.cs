using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class PersoneGiuridicheDb
	{
		public long pgi_codice { get; set; }
		public string pgi_desc { get; set; }
		public DateTime? pgi_created_at { get; set; }

		public DateTime? pgi_last_update { get; set; }
		public int pgi_user { get; set; }

		public PersoneGiuridicheDb()
		{
			var pgi_db = this;
			DbUtils.Initialize(ref pgi_db);
		}

		private static readonly List<string> ExcludeFields = new List<string>() { "pgi_codice" };

		public static bool Search(ref OdbcCommand cmd, long codice, ref PersoneGiuridicheDb pgi, bool writeLock = false)
		{
			if (pgi != null) DbUtils.Initialize(ref pgi);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref pgi, writeLock);
				}
			}

			var found = false;

			var sql = DbUtils.QueryAdapt("SELECT * FROM persone_giuridiche WHERE pgi_codice = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.BigInt).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (pgi != null) DbUtils.SqlRead(ref reader, ref pgi);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref PersoneGiuridicheDb pgi, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref pgi);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new PersoneGiuridicheDb();
				if (!Search(ref cmd, pgi.pgi_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.pgi_last_update != pgi.pgi_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(pgi.pgi_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({pgi.pgi_codice}) : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref pgi, "persone_giuridiche", null, ExcludeFields);
							var reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								DbUtils.SqlRead(ref reader, ref pgi);
							}
							reader.Close();
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								pgi.pgi_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref pgi, "persone_giuridiche", "WHERE pgi_codice = ?");
					cmd.Parameters.Add("@codice", OdbcType.BigInt).Value = pgi.pgi_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref pgi);
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

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM persone_giuridiche WHERE pgi_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.BigInt).Value = pgi.pgi_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref PersoneGiuridicheDb pgi)
		{
			if (!Search(ref cmd, pgi.pgi_codice, ref pgi))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
