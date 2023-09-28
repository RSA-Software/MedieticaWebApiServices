using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public enum UserCantieriSettore : short
	{
		AMMINISTRATIVI = 0,
		TECNICI = 1,
		SICUREZZA = 2,
		CONTABILITA = 3,
		RIFIUTI = 4
	}
	
	public class DocCantieriDb
	{
		public int dca_dit { get; set; }
		public int dca_codice { get; set; }
		public int dca_can { get; set; }
		public short dca_livello { get; set; }
		public short dca_settore { get; set; }
		public DateTime? dca_data { get; set; }
		public string dca_desc { get; set; }
		public string dca_url { get; set; }
		public DateTime? dca_data_rilascio { get; set; }
		public DateTime? dca_data_scadenza { get; set; }
		public short dca_scad_alert_before { get; set; }
		public short dca_scad_alert_after { get; set; }
		public DateTime? dca_created_at { get; set; }
		public DateTime? dca_last_update { get; set; }

		//
		// Lista Allegati
		//
		public List<AllegatiDb> all_list;

		private static readonly List<string> ExcludeFields = new List<string>()
		{
			"all_list"
		};

		public DocCantieriDb()
		{
			var dca_db = this;
			DbUtils.Initialize(ref dca_db);
		}

		public static string GetTableDescription()
		{
			return ("Documenti Cantieri");
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static bool Search(ref OdbcCommand cmd, int codDit, int codice, ref DocCantieriDb dca,  bool writeLock = false)
		{
			if (dca != null) DbUtils.Initialize(ref dca);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codice, ref dca, writeLock);
				}
			}

			var found = false;
			var	sql = "SELECT * FROM doccantieri WHERE dca_dit = ? AND dca_codice = ?";

			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql,1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (dca != null) DbUtils.SqlRead(ref reader, ref dca);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref DocCantieriDb dca, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref dca);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new DocCantieriDb();
				if (!Search(ref cmd, dca.dca_dit, dca.dca_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.dca_last_update != dca.dca_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, dca.dca_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				CantieriDb can = null;
				if (!CantieriDb.Search(ref cmd, dca.dca_dit, dca.dca_can, ref can)) throw new MCException(MCException.CantiereMsg, MCException.CantiereErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref dca, "doccantieri", null, ExcludeFields);
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dca, "doccantieri", "WHERE dca_dit = ? AND dca_codice = ?", ExcludeFields);
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = dca.dca_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = dca.dca_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({dca.dca_dit} - {dca.dca_codice})", MCException.DuplicateErr);
								throw;
							}
						}
						else throw;
					}
					break;

				case DbMessage.DB_ADD:
				case DbMessage.DB_INSERT:
					do
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref dca, "doccantieri", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref dca);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								dca.dca_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref dca, "doccantieri", "WHERE dca_dit = ? AND dca_codice = ?", ExcludeFields);
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = dca.dca_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = dca.dca_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref dca);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM doccantieri WHERE dca_dit = ? AND dca_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = dca.dca_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dca.dca_codice;
						cmd.ExecuteNonQuery();
					}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref DocCantieriDb dca)
		{
			if (!Search(ref cmd, dca.dca_dit, dca.dca_codice, ref dca)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}
	}
}
