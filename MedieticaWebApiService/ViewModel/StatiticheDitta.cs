using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MedieticaWebApiService.Models;

namespace MedieticaWebApiService.ViewModel
{
	public class DocumentiDaCompilare
	{
		public int tipo { get; set; }
		public int codice { get; set; }
		public string titolo { get; set; }
		public string matricola { get; set; }
		public string desc { get; set; }
		public int dit_codice { get; set; }
		public string dit_desc { get; set; }

		public DocumentiDaCompilare()
		{
			tipo = 0;
			codice = 0;
			titolo = "";
			matricola = "";
			desc = "";
			dit_codice = 0;
			dit_desc = "";
		}
	}


	public class StatisticheDitta
	{
		public long cantieri_attivi { get; set; }
		public long cantieri_cessati { get; set; }
		public long dipendenti_attivi { get; set; }
		public long dipendenti_cessati { get; set; }
		public long mezzi_attivi { get; set; }
		public long mezzi_cessati { get; set; }
		public long subappalti_attivi { get; set; }
		public long subappalti_cessati { get; set; }
		public List<DocumentiDaCompilare> doc_list;

		public StatisticheDitta()
		{
			cantieri_attivi = 0;
			cantieri_cessati = 0;
			dipendenti_attivi = 0;
			dipendenti_cessati = 0;
			mezzi_attivi = 0;
			mezzi_cessati = 0;
			subappalti_attivi = 0;
			subappalti_cessati = 0;
			doc_list = new List<DocumentiDaCompilare>();
		}
	}
}
