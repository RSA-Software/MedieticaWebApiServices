using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class MarchiDb
	{
		public int mar_codice { get; set; }
		public string mar_desc { get; set; }
		public string mar_url { get; set; }
		public string mar_note { get; set; }
		public DateTime? mar_created_at { get; set; }
		public DateTime? mar_last_update { get; set; }

		public string img_data { get; set; }
		public List<ImgMarchiDb> img_list { get; set; }

		private static readonly List<string> ExcludeFields = new List<string>() { "img_data", "img_list" };

		private static readonly string JoinQuery = @"
		SELECT marchi.*, img_data, NULL AS img_list
		FROM marchi
		LEFT JOIN imgmarchi ON img_dit = 0 AND mar_codice = img_codice AND img_formato = 1";
		
		public MarchiDb()
		{
			var mar_db = this;
			DbUtils.Initialize(ref mar_db);
		}

		public static List<string> GetJoinExcludeFields()
		{
			return (ExcludeFields);
		}

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}


		public static bool Search(ref OdbcCommand cmd, int codice, ref MarchiDb mar, bool joined = false, bool writeLock = false)
		{
			if (mar != null) DbUtils.Initialize(ref mar);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref mar, joined, writeLock);
				}
			}

			var found = false;

			string sql;
			if (joined)
				sql = JoinQuery + " WHERE mar_codice = ?";
			else
				sql = "SELECT * FROM marchi WHERE mar_codice = ?";
			if (writeLock && !joined ) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (mar != null) DbUtils.SqlRead(ref reader, ref mar, joined ? null :ExcludeFields);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref MarchiDb mar, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref mar);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new MarchiDb();
				if (!Search(ref cmd, mar.mar_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.mar_last_update != mar.mar_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(mar.mar_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({mar.mar_codice}) : desc", MCException.CampoObbligatorioErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref mar, "marchi", null, ExcludeFields);
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref mar, "marchi", "WHERE mar_codice = ?", ExcludeFields);
								cmd.Parameters.Add("codice", OdbcType.Int).Value = mar.mar_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({mar.mar_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref mar, "marchi", null, ExcludeFields);
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref mar, joined);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								mar.mar_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref mar, "marchi", "WHERE mar_codice = ?", ExcludeFields);
					cmd.Parameters.Add("codice", OdbcType.Int).Value = mar.mar_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref mar, joined);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("SELECT COUNT(*) FROM modelli WHERE mod_mar = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mar.mar_codice;
						var num = Convert.ToInt32(cmd.ExecuteScalar());
						if (num > 0) throw new MCException(MCException.CancelMsg, MCException.CancelErr);

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM imgmarchi WHERE img_dit = ? AND img_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = 0;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mar.mar_codice;
						cmd.ExecuteNonQuery();

						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM marchi WHERE mar_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mar.mar_codice;
						cmd.ExecuteNonQuery();
					}
					break;

			}
		}

		public static void Reload(ref OdbcCommand cmd, ref MarchiDb mar, bool joied)
		{
			if (!Search(ref cmd, mar.mar_codice, ref mar, joied)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
