using System;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class VideoMezziDb
	{
		public int vme_dit { get; set; }
		public int vme_codice { get; set; }
		public int vme_mez { get; set; }
		public short vme_livello { get; set; }
		public DateTime? vme_data { get; set; }
		public string vme_desc { get; set; }
		public string vme_url { get; set; }
		public DateTime? vme_created_at { get; set; }
		public DateTime? vme_last_update { get; set; }

		public VideoMezziDb()
		{
			var vme_db = this;
			DbUtils.Initialize(ref vme_db);
		}

		public VideoMezziDb(VideoModelliDb vmo)
		{
			var vme_db = this;
			DbUtils.Initialize(ref vme_db);
			vme_db.vme_dit = vmo.vmo_dit;
			vme_db.vme_codice = vmo.vmo_codice;
			vme_db.vme_mez = vmo.vmo_mod;
			vme_db.vme_livello = vmo.vmo_livello;
			vme_db.vme_data = vmo.vmo_data;
			vme_db.vme_desc = vmo.vmo_desc;
			vme_db.vme_url = vmo.vmo_url;
			vme_db.vme_created_at = vmo.vmo_created_at;
			vme_db.vme_last_update = vmo.vmo_last_update;
		}

		public static bool Search(ref OdbcCommand cmd, int codDit, int codice, ref VideoMezziDb vme, bool writeLock = false)
		{
			if (vme != null) DbUtils.Initialize(ref vme);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codice, ref vme, writeLock);
				}
			}

			var found = false;

			var sql = "SELECT * FROM videomezzi WHERE vme_dit = ? AND vme_codice = ?";

			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql,1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (vme != null) DbUtils.SqlRead(ref reader, ref vme);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref VideoMezziDb vme, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref vme);
			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_REWRITE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR)
			{
				var old = new VideoMezziDb();
				if (!Search(ref cmd, vme.vme_dit, vme.vme_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.vme_last_update != vme.vme_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				if (string.IsNullOrWhiteSpace(vme.vme_desc)) throw new MCException(MCException.CampoObbligatorioMsg + $" ({vme.vme_dit} - {vme.vme_codice}) : desc", MCException.CampoObbligatorioErr);

				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, vme.vme_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);

				MezziDb mez = null;
				if (!MezziDb.Search(ref cmd, vme.vme_dit, vme.vme_mez, ref mez)) throw new MCException(MCException.MezzoMsg, MCException.MezzoErr);
			}

			switch (msg)
			{
				case DbMessage.DB_BULK_INS:
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref vme, "videomezzi");
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref vme, "videomezzi", "WHERE vme_dit = ? AND vme_codice = ?");
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = vme.vme_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = vme.vme_codice;
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException e)
							{
								if (DbUtils.IsDupKeyErr(e)) throw new MCException(MCException.DuplicateMsg + $" ({vme.vme_dit} - {vme.vme_codice})", MCException.DuplicateErr);
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
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref vme, "videomezzi");
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref vme);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								vme.vme_codice++;
								continue;
							}
							throw;
						}
						break;
					} while (true);
					break;

				case DbMessage.DB_REWRITE:
				case DbMessage.DB_UPDATE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref vme, "videomezzi", "WHERE vme_dit = ? AND vme_codice = ?");
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = vme.vme_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = vme.vme_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref vme);
					break;

				case DbMessage.DB_CLEAR:
				case DbMessage.DB_DELETE:
					{
						cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM videomezzi WHERE vme_dit = ? AND vme_codice = ?");
						cmd.Parameters.Clear();
						cmd.Parameters.Add("coddit", OdbcType.Int).Value = vme.vme_dit;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = vme.vme_codice;
						cmd.ExecuteNonQuery();
					}
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref VideoMezziDb vme)
		{
			if (!Search(ref cmd, vme.vme_dit, vme.vme_codice, ref vme)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
