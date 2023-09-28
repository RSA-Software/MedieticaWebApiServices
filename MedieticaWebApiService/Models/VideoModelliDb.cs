using System;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class VideoModelliDb
	{
		public int vmo_dit { get; set; }
		public int vmo_codice { get; set; }
		public int vmo_mod { get; set; }
		public short vmo_livello { get; set; }
		public DateTime? vmo_data { get; set; }
		public string vmo_desc { get; set; }
		public string vmo_url { get; set; }
		public DateTime? vmo_created_at { get; set; }
		public DateTime? vmo_last_update { get; set; }

		public VideoModelliDb()
		{
			var vmo_db = this;
			DbUtils.Initialize(ref vmo_db);
		}


		public static bool Search(ref OdbcCommand cmd, int codDit, int codice, ref VideoModelliDb vmo, bool writeLock = false)
		{
			if (vmo != null) DbUtils.Initialize(ref vmo);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codice, ref vmo, writeLock);
				}
			}

			var found = false;

			var sql = "SELECT * FROM videomodelli WHERE vmo_dit = ? AND vmo_codice = ?";

			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql,1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (vmo != null) DbUtils.SqlRead(ref reader, ref vmo);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref VideoModelliDb vmo, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref vmo);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new VideoModelliDb();
				if (!Search(ref cmd, vmo.vmo_dit, vmo.vmo_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.vmo_last_update != vmo.vmo_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(vmo.vmo_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({vmo.vmo_dit} - {vmo.vmo_codice}) : desc", MCException.CampoObbligatorioErr);

				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, vmo.vmo_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				ModelliDb mod = null;
				if (!ModelliDb.Search(ref cmd, vmo.vmo_dit, vmo.vmo_mod, ref mod)) throw new MCException(MCException.ModelloMsg, MCException.ModelloErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref vmo, "videomodelli");
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref vmo, "videomodelli", "WHERE vmo_dit = ? AND vmo_codice = ?");
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = vmo.vmo_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = vmo.vmo_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({vmo.vmo_dit} - {vmo.vmo_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref vmo, "videomodelli");
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref vmo);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								vmo.vmo_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref vmo, "videomodelli", "WHERE vmo_dit = ? AND vmo_codice = ?");
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = vmo.vmo_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = vmo.vmo_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref vmo);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM videomodelli WHERE vmo_dit = ? AND vmo_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = vmo.vmo_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = vmo.vmo_codice;
						cmd.ExecuteNonQuery();
					}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref VideoModelliDb vmo)
		{
			if (!Search(ref cmd, vmo.vmo_dit, vmo.vmo_codice, ref vmo)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
