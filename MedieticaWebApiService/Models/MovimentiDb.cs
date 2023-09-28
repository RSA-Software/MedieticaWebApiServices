using System;
using System.Collections.Generic;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class MovimentiDb
	{
		public int mov_codice { get; set; }
		public double mov_qta { get; set; }
		public double mov_price { get; set; }
		public double mov_total { get; set; }
		public string mov_ana { get; set; }
		public int mov_dis { get; set; }
		public int mov_ute { get; set; }
		public DateTime? mov_created_at { get; set; }
		public DateTime? mov_last_update { get; set; }


		private static readonly string JoinQuery = @"
		SELECT *
		FROM movimenti
		LEFT JOIN artanag ON mov_ana = ana_codice
		LEFT JOIN utenti ON mov_ute = ute_codice
		LEFT JOIN distributori ON mov_dis = dis_codice
		";

		public static string GetJoinQuery()
		{
			return (JoinQuery);
		}

		public MovimentiDb()
		{
			var mov_db = this;
			DbUtils.Initialize(ref mov_db);
		}

		public static bool Search(ref OdbcCommand cmd, int codice, ref MovimentiDb mov, bool joined = false, bool writeLock = false)
		{
			if (mov != null) DbUtils.Initialize(ref mov);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref mov, joined, writeLock);
				}
			}

			var found = false;

			string sql;
			if (joined)
				sql = GetJoinQuery() + " WHERE mov_codice = ?";
			else
				sql = "SELECT * FROM movimenti WHERE mov_codice = ?";
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = DbUtils.QueryAdapt(sql, 1);
			cmd.Parameters.Clear();
			cmd.Parameters.Add("@codice", OdbcType.Int).Value = codice;
			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (mov != null) DbUtils.SqlRead(ref reader, ref mov);
				found = true;
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref MovimentiDb mov, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref mov);

			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR || msg == DbMessage.DB_REWRITE)
			{
				var old = new MovimentiDb();
				if (!Search(ref cmd, mov.mov_codice, ref old, false, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.mov_last_update != mov.mov_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_BULK_INS)
			{
				ArtAnagDb ana = null;
				DistributoriDb dis = null;
				UtentiDb ute = null;
				if (string.IsNullOrWhiteSpace(mov.mov_ana) || !ArtAnagDb.Search(ref cmd, mov.mov_ana, ref ana)) throw new MCException(MCException.ArticoloMsg, MCException.ArticoloErr);
				if (mov.mov_dis == 0 || !DistributoriDb.Search(ref cmd, mov.mov_dis, ref dis)) throw new MCException(MCException.DistributoreMsg, MCException.DistributoreErr);
				if (mov.mov_ute == 0 || !UtentiDb.Search(ref cmd, mov.mov_ute, ref ute)) throw new MCException(MCException.UtenteMsg, MCException.UtenteErr);
			}

			switch (msg)
			{
				case DbMessage.DB_INSERT:
					{
						do
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref mov, "movimenti", null);
								cmd.ExecuteNonQuery();
								Reload(ref cmd, ref mov, joined);
							}
							catch (OdbcException ex)
							{
								if (DbUtils.IsDupKeyErr(ex))
								{
									mov.mov_codice++;
									continue;
								}
								throw;
							}
							break;
						} while (true);
					}
					break;

				case DbMessage.DB_UPDATE:
				case DbMessage.DB_REWRITE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref mov, "movimenti", "WHERE mov_codice = ?");
					cmd.Parameters.Add("@codice", OdbcType.Int).Value = mov.mov_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref mov, joined);
					break;

				case DbMessage.DB_DELETE:
				case DbMessage.DB_CLEAR:
					cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM movimenti WHERE mov_codice = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("@codice", OdbcType.Int).Value = mov.mov_codice;
					cmd.ExecuteNonQuery();
					break;
			}
		}

		public static void WritePayment(ref OdbcCommand cmd, DbMessage msg, ref List<MovimentiDb> movimenti, ref object obj, bool joined = false)
		{
			var utente_id = movimenti[0].mov_ute;
			var utente = new UtentiDb();
			if (!UtentiDb.Search(ref cmd, utente_id, ref utente)) throw new MCException(MCException.NotFoundMsg, MCException.NotFoundErr);

			// Controlliamo se il credito è sufficiente solo nel caso di scarico dei prodotti (qta negativa)
			double totale = 0;
			foreach (var mov in movimenti)
            {
				ArtAnagDb ana = null;
				DistributoriDb dis = null;
				UtentiDb ute = null;

				if (string.IsNullOrWhiteSpace(mov.mov_ana) || !ArtAnagDb.Search(ref cmd, mov.mov_ana, ref ana)) throw new MCException(MCException.ArticoloMsg, MCException.ArticoloErr);
				if (mov.mov_dis == 0 || !DistributoriDb.Search(ref cmd, mov.mov_dis, ref dis)) throw new MCException(MCException.DistributoreMsg, MCException.DistributoreErr);
				if (mov.mov_ute == 0 || !UtentiDb.Search(ref cmd, mov.mov_ute, ref ute)) throw new MCException(MCException.UtenteMsg, MCException.UtenteErr);
				
				if (mov.mov_qta < 0) totale += Math.Abs(mov.mov_price * mov.mov_qta);
			}

			var last = 1;
			cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(dis_codice),0) FROM distributori");
			last += (int)cmd.ExecuteScalar();
			
			foreach (var mov in movimenti)
			{
				var value = mov;

				value.mov_codice = last;
				if (value.mov_qta < 0)
				{

					cmd.CommandText = DbUtils.QueryAdapt("UPDATE utenti SET ute_credito = ?, ute_last_update = Now() WHERE ute_codice = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("@valore", OdbcType.Double).Value = 0;
					cmd.Parameters.Add("@numero", OdbcType.Int).Value = utente.ute_codice;
					cmd.ExecuteNonQuery();
					UtentiDb.Reload(ref cmd, ref utente, joined);
				}
				do
				{
					try
					{
						cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref value, "movimenti", null);
						cmd.ExecuteNonQuery();
						Reload(ref cmd, ref value, joined);
						last = value.mov_codice + 1;
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							value.mov_codice++;
							continue;
						}
						throw;
					}
					break;
				} while (true);
			}
			
		}

		public static void Reload(ref OdbcCommand cmd, ref MovimentiDb mov, bool joined)
		{
			if (!Search(ref cmd, mov.mov_codice, ref mov, joined))
			{
				throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
			}
		}

	}
}
