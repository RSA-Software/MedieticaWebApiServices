using System;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models.Anac
{
	public class BandiCigDb
	{
		public long id { get; set; }
		public string cig { get; set; }
		public string cig_accordo_quadro { get; set; }
		public string numero_gara { get; set; }
		public string oggetto_gara { get; set; }
		public double importo_complessivo_gara { get; set; }
		public long n_lotti_componenti { get; set; }
		public string oggetto_lotto { get; set; }
		public double importo_lotto { get; set; }
		public string oggetto_principale_contratto { get; set; }
		public string stato { get; set; }
		public string settore { get; set; }
		public string luogo_istat { get; set; }
		public string provincia { get; set; }
		public DateTime? data_pubblicazione { get; set; }
		public DateTime? data_scadenza_offerta { get; set; }
		public long cod_tipo_scelta_contraente { get; set; }
		public string tipo_scelta_contraente { get; set; }
		public long cod_modalita_realizzazione { get; set; }
		public string modalita_realizzazione { get; set; }
		public string codice_ausa { get; set; }
		public string cf_amministrazione_appaltante { get; set; }
		public string denominazione_amministrazione_appaltante { get; set; }
		public string sezione_regionale { get; set; }
		public string id_centro_costo { get; set; }
		public string denominazione_centro_costo { get; set; }
		public string anno_pubblicazione { get; set; }
		public string mese_pubblicazione { get; set; }
		public string cod_cpv { get; set; }
		public string descrizione_cpv { get; set; }
		public long flag_prevalente { get; set; }
		public long COD_MOTIVO_CANCELLAZIONE { get; set; }
		public string MOTIVO_CANCELLAZIONE { get; set; }
		public DateTime? DATA_CANCELLAZIONE { get; set; }
		public DateTime? DATA_ULTIMO_PERFEZIONAMENTO { get; set; }
		public long COD_MODALITA_INDIZIONE_SPECIALI { get; set; }
		public string MODALITA_INDIZIONE_SPECIALI { get; set; }
		public long COD_MODALITA_INDIZIONE_SERVIZI { get; set; }
		public string MODALITA_INDIZIONE_SERVIZI { get; set; }
		public double DURATA_PREVISTA { get; set; }
		public double COD_STRUMENTO_SVOLGIMENTO { get; set; }
		public string STRUMENTO_SVOLGIMENTO { get; set; }
		public bool FLAG_URGENZA { get; set; }
		public long COD_MOTIVO_URGENZA { get; set; }
		public string MOTIVO_URGENZA { get; set; }
		public bool FLAG_DELEGA { get; set; }
		public string FUNZIONI_DELEGATE { get; set; }
		public string CF_SA_DELEGANTE { get; set; }
		public string DENOMINAZIONE_SA_DELEGANTE { get; set; }
		public string CF_SA_DELEGATA { get; set; }
		public string DENOMINAZIONE_SA_DELEGATA { get; set; }
		public double IMPORTO_SICUREZZA { get; set; }
		public string TIPO_APPALTO_RISERVATO { get; set; }
		public string CUI_PROGRAMMA { get; set; }
		public bool FLAG_PREV_RIPETIZIONI { get; set; }
		public long COD_IPOTESI_COLLEGAMENTO { get; set; }
		public string IPOTESI_COLLEGAMENTO { get; set; }
		public string CIG_COLLEGAMENTO { get; set; }
		public long COD_ESITO { get; set; }
		public string ESITO { get; set; }
		public DateTime? DATA_COMUNICAZIONE_ESITO { get; set; }

		public BandiCigDb()
		{
			var ban_db = this;
			DbUtils.Initialize(ref ban_db);
		}
	}
}
