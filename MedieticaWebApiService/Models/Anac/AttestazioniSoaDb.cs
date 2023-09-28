using System;
using System.Data.Odbc;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models.Anac
{
	public class AttestazioniSoaDb
	{
		public string cf_soa { get; set; }
		public string denom_soa { get; set; }
		public string num_protocollo_autorizzazione { get; set; }
		public DateTime? data_autorizzazione { get; set; }
		public string num_attestazione { get; set; }
		public string regolamento { get; set; }
		public DateTime? data_emissione { get; set; }
		public int anno_emissione { get; set; }
		public DateTime? data_rilascio_originaria { get; set; }
		public DateTime? data_scadenza_verifica { get; set; }
		public DateTime? data_scadenza_finale { get; set; }
		public string  fase_attestato { get; set; }
		public DateTime? alla_data_del { get; set; }
		public string cf_impresa { get; set; }
		public string denom_impresa { get; set; }
		public string numAttPrecedente { get; set; }
		public string enteRilcertQualita { get; set; }
		public DateTime? certificazioneDiQualitaScadenza { get; set; }
		public DateTime? data_effettuazione_verifica { get; set; }
		public long cod_categoria { get; set; }
		public string categoria { get; set; }
		public string desc_categoria { get; set; }
		public string classifica { get; set; }

		public AttestazioniSoaDb()
		{
			var soa_db = this;
			DbUtils.Initialize(ref soa_db);
		}
	}
}
