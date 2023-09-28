using System;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public enum CheckListType
	{
		DIPENDENTI = 0,
		CANTIERI = 1,
		DITTE = 2,
		MEZZI = 3,
	}

	public enum CheckListSettore
	{
		AMMINISTRAZIONE = 0,
		AUTORIZZAZIONI = 1,
		SICUREZZA = 2,
		TECNICO = 3,
		UNILAV = 4,
		CORSI = 5,
		CONTABILITA = 6,
		RIFIUTI = 7,
		FORNITORI = 8
	}


	public class CheckListDb
	{
		public int chk_codice { get; set; }
		public short chk_tipo { get; set; }
		public short chk_settore { get; set; }
		public string chk_desc { get; set; }
		public DateTime? chk_created_at { get; set; }
		public DateTime? chk_last_update { get; set; }

		public CheckListDb()
		{
			var chk_db = this;
			DbUtils.Initialize(ref chk_db);
		}

		public static bool Search(ref OdbcCommand cmd, int codice, ref CheckListDb chk, bool writeLock = false)
		{
			if (chk != null) DbUtils.Initialize(ref chk);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref chk, writeLock);
				}
			}

			var found = false;

			var sql = DbUtils.QueryAdapt("SELECT * FROM checklist WHERE chk_codice = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (chk != null) DbUtils.SqlRead(ref reader, ref chk);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref CheckListDb chk, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref chk);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new CheckListDb();
				if (!Search(ref cmd, chk.chk_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.chk_last_update != chk.chk_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				if (string.IsNullOrWhiteSpace(chk.chk_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({chk.chk_codice}) : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref chk, "checklist");
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref chk, "checklist", "WHERE chk_codice = ?");
								cmd.Parameters.Add("@numero", OdbcType.Int).Value = chk.chk_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({chk.chk_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref chk, "checklist");
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref chk);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								chk.chk_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref chk, "checklist", "WHERE chk_codice = ?");
					cmd.Parameters.Add("@codice", OdbcType.Int).Value = chk.chk_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref chk);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM checklist WHERE chk_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.Int).Value = chk.chk_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref CheckListDb chk)
		{
			if (!Search(ref cmd, chk.chk_codice, ref chk)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}
	}
}
