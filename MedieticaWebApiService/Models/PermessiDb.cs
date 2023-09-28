using System;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{

	public class PermessiDb
	{
		public int per_usg { get; set; }
		public int per_end { get; set; }
		public bool per_view { get; set; }
		public bool per_add { get; set; }
		public bool per_update { get; set; }
		public bool per_delete { get; set; }
		public bool per_special1 { get; set; }
		public bool per_special2 { get; set; }
		public bool per_special3 { get; set; }
		public DateTime? per_created_at { get; set; }
		public DateTime? per_last_update { get; set; }

		public PermessiDb()
		{
			var per_db = this;
			DbUtils.Initialize(ref per_db);
		}
		public static bool Search(ref OdbcCommand cmd, int codUsg, int codEnd, ref PermessiDb per, bool writeLock = false)
		{
			if (per != null) DbUtils.Initialize(ref per);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codUsg, codEnd, ref per, writeLock);
				}
			}

			var found = false;
			var	sql = DbUtils.QueryAdapt("SELECT * FROM permessi WHERE per_usg = ? AND per_end = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("codusg", OdbcType.Int).Value = codUsg;
			cmd.Parameters.Add("codend", OdbcType.Int).Value = codEnd;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (per != null) DbUtils.SqlRead(ref reader, ref per);
				found = true;
			}
			reader.Close();
			return (found);
		}
	
		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref PermessiDb per, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref per);

			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR || msg == DbMessage.DB_REWRITE)
			{
				var old = new PermessiDb();
				if (!Search(ref cmd, per.per_usg, per.per_end, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.per_last_update != per.per_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				EndpointsDb end = null;
				if (!EndpointsDb.Search(ref cmd, per.per_end, ref end)) throw new MCException(MCException.EndpointMsg, MCException.EndpointErr);

				UtentiGruppiDb usg = null;
				if (!UtentiGruppiDb.Search(ref cmd, per.per_usg, ref usg)) throw new MCException(MCException.UtentiGruppoMsg, MCException.UtentiGruppoErr);
			}

			switch (msg)
			{
					case DbMessage.DB_INSERT:
					{
						try
						{
							cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref per, "permessi");
							cmd.ExecuteNonQuery();
							Reload(ref cmd, ref per, joined);
						}
						catch (OdbcException ex)
						{
							if (DbUtils.IsDupKeyErr(ex))
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref per, "permessi", "WHERE per_usg = ? AND per_end = ?");
								cmd.Parameters.Add("codusg", OdbcType.Int).Value = per.per_usg;
								cmd.Parameters.Add("codend", OdbcType.Int).Value = per.per_end;
								cmd.ExecuteNonQuery();
								Reload(ref cmd, ref per, joined);
							}
							else throw;
						}
					}
					break;

				case DbMessage.DB_UPDATE:
				case DbMessage.DB_REWRITE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref per, "permessi", "WHERE per_usg = ? AND per_end = ?");
					cmd.Parameters.Add("codusg", OdbcType.Int).Value = per.per_usg;
					cmd.Parameters.Add("codend", OdbcType.Int).Value = per.per_end;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref per, joined);
					break;

				case DbMessage.DB_DELETE:
				case DbMessage.DB_CLEAR:
					cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM permessi WHERE per_usg = ? AND per_end = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("codusg", OdbcType.Int).Value = per.per_usg;
					cmd.Parameters.Add("codend", OdbcType.Int).Value = per.per_end;
					cmd.ExecuteNonQuery();
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref PermessiDb per, bool joined)
		{
			if (!Search(ref cmd, per.per_usg, per.per_end, ref per, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
