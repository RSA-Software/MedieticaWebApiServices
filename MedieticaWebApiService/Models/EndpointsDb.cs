using System;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public enum EndpointsOperations
	{
		VIEW = 0,
		ADD = 1,
		UPDATE = 2,
		DELETE = 3,
		SPECIAL1 = 4,
		SPECIAL2 = 5,
		SPECIAL3 = 6
	}

	public enum Endpoints
	{
		GRUPPI_AZIENDE  = 10,
		
		DITTE = 20,
		DOCUMENTI_DITTE = 30,
		SCADENZE_DITTE = 40,
		IMMAGINI_DITTE = 50,
		SEDI_DITTE = 55,

		PERSONE_GIURIDICHE = 60,


		MARCHI = 60,


		TIPOLOGIE_MEZZI_ATTREZZATURE = 70,
		VERIFICHE_MEZZI_ATTREZZATURE = 80,
		MANSIONI = 85,
		MODELLI_MEZZI_ATTREZZATURE = 90,
		DOCUMENTI_MODELLI_MEZZI_ATTREZZATURE = 100,
		VIDEO_MODELLI_MEZZI_ATTREZZATURE = 110,
		IMMAGINI_MODELLI_MEZZI_ATTREZZATURE = 120,
		CHECK_LIST_DITTE = 130,
		CHECK_LIST_MEZZI = 140,
		CHECK_LIST_CANTIERI = 150,
		CHECK_LIST_DIPENDENTI = 160,
		MEZZI_ATTREZZATURE = 170,
		DOCUMENTI_MEZZI_ATTREZZATURE = 180,
		MANUTENZIONI_MEZZI_ATTREZZATURE = 190,
		SCADENZE_MEZZI_ATTREZZATURE = 200,
		VIDEO_MEZZI_ATTREZZATURE = 210,
		IMMAGINI_MEZZI_ATTREZZATURE = 220,
		UTENTI = 230,
		ASSOCIAZIONE_UTENTI_DITTE = 240,
		DIPENDENTI = 250,
		DOCUMENTI_DIPENDENTI = 260,
		CORSI_DIPENDENTI = 270,
		UNILAV_DIPENDENTI = 280,
		SCADENZE_DIPENDENTI = 290,
		IMMAGINI_DIPENDENTI = 300,
		VISITE_DIPENDENTI = 305,
		CANTIERI = 310,
		DOCUMENTI_CANTIERI = 320,
		SCADENZE_CANTIERI = 330,
		IMMAGINI_CANTIERI = 340,
		GIORNALE_LAVORI_CANTIERI = 350,
		CERTIFICATI_DI_PAGAMENTO = 360
	};



	public class EndpointsDb
	{
		public int end_codice { get; set; }
		public string end_desc { get; set; }
		public DateTime? end_created_at { get; set; }
		public DateTime? end_last_update { get; set; }

		public EndpointsDb()
		{
			var end_db = this;
			DbUtils.Initialize(ref end_db);
		}
		public static bool Search(ref OdbcCommand cmd, int codice, ref EndpointsDb end, bool writeLock = false)
		{
			if (end != null) DbUtils.Initialize(ref end);
			if (codice == 0) return (true);

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codice, ref end, writeLock);
				}
			}

			var found = false;
			var	sql = DbUtils.QueryAdapt("SELECT * FROM endpoints WHERE end_codice = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("@codice", OdbcType.Int).Value = codice;

			var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (end != null) DbUtils.SqlRead(ref reader, ref end);
				found = true;
			}
			reader.Close();
			return (found);
		}
	
		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref EndpointsDb end, ref object obj, bool joined = false)
		{
			DbUtils.Trim(ref end);

			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR || msg == DbMessage.DB_REWRITE)
			{
				var old = new EndpointsDb();
				if (!Search(ref cmd, end.end_codice, ref old, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.end_last_update != end.end_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}
			
			switch (msg)
			{
					case DbMessage.DB_INSERT:
					{
						var idx = 0;
						do
						{
							try
							{
								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref end, "endpoints");
								cmd.ExecuteNonQuery();
								Reload(ref cmd, ref end, joined);
							}
							catch (OdbcException ex)
							{
								if (DbUtils.IsDupKeyErr(ex))
								{
									idx++;
									end.end_codice++;
									continue;
								}
								throw;
							}
							break;
						} while (true && idx <= 10);
					}
					break;

				case DbMessage.DB_UPDATE:
				case DbMessage.DB_REWRITE:
					cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_UPDATE, ref end, "endpoints", "WHERE end_codice = ?");
					cmd.Parameters.Add("@numero", OdbcType.Int).Value = end.end_codice;
					cmd.ExecuteNonQuery();
					Reload(ref cmd, ref end, joined);
					break;

				case DbMessage.DB_DELETE:
				case DbMessage.DB_CLEAR:
					cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM endpoints WHERE end_codice = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("@codice", OdbcType.Int).Value = end.end_codice;
					cmd.ExecuteNonQuery();
					break;
			}
		}

		public static void Reload(ref OdbcCommand cmd, ref EndpointsDb end, bool joined)
		{
			if (!Search(ref cmd, end.end_codice, ref end, joined)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}

	}
}
