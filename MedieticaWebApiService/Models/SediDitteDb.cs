using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class SediDitteDb
	{
		public int sed_dit { get; set; }
		public int sed_codice { get; set; }
		public string sed_indirizzo { get; set; }
		public string sed_citta { get; set; }
		public string sed_cap { get; set; }
		public string sed_prov { get; set; }
		public DateTime? sed_created_at { get; set; }
		public DateTime? sed_last_update { get; set; }
		public int sed_user { get; set; }


		public SediDitteDb()
		{
			var sed_db = this;
			DbUtils.Initialize(ref sed_db);
		}

		public static bool Search(ref OdbcCommand cmd, int dit, int codice, ref SediDitteDb sed, bool writeLock = false)
		{
			if (sed != null) DbUtils.Initialize(ref sed);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, dit, codice, ref sed, writeLock);
				}
			}

			var found = false;
			var sql = "SELECT * FROM sedditte WHERE sed_dit = ? AND sed_codice = ?";

			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = dit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (sed != null) DbUtils.SqlRead(ref reader, ref sed);
				found = true;
			}

			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref SediDitteDb sed, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref sed);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new SediDitteDb();
				if (!Search(ref cmd, sed.sed_dit, sed.sed_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.sed_last_update != sed.sed_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(sed.sed_indirizzo)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({sed.sed_dit} - {sed.sed_codice}) : Indirizzo", MCException.CampoObbligatorioErr);
				if (string.IsNullOrWhiteSpace(sed.sed_citta)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({sed.sed_dit} - {sed.sed_codice}) : Citta", MCException.CampoObbligatorioErr);

				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, sed.sed_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref sed, "sedditte");
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref sed, "sedditte", "WHERE sed_dit = ? AND sed_codice = ?");
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = sed.sed_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = sed.sed_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({sed.sed_dit} - {sed.sed_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref sed, "sedditte");
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref sed);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								sed.sed_codice++;
								continue;
							}

							throw;
						}

						break;
					} while (true);

					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref sed, "sedditte", "WHERE sed_dit = ? AND sed_codice = ?");
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = sed.sed_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = sed.sed_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref sed);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
				{
					cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM sedditte WHERE sed_dit = ? AND sed_codice = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = sed.sed_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = sed.sed_codice;
					cmd.ExecuteNonQuery();
				}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref SediDitteDb sed)
		{
			if (!Search(ref cmd, sed.sed_dit, sed.sed_codice, ref sed)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}
	}
}