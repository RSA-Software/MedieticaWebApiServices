using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MedieticaWebApiService.Models;

namespace MedieticaWebApiService.ViewModel
{
	public class DipendentiCantieri
	{
		public int dip_dit { get; set; }
		public int dip_codice { get; set; }
		public string dip_cognome { get; set; }
		public string dip_nome { get; set; }
		public string dip_desc { get; set; }
		public string dip_codfis { get; set; }
		public int dip_enabled { get; set; }
		public string img_data { get; set; }
		public List<string> can_list { get; set; }

		public DipendentiCantieri()
		{
			dip_dit = 0;
			dip_codice = 0;
			dip_cognome = "";
			dip_nome = "";
			dip_desc = "";
			dip_codfis = "";
			dip_enabled = 0;
			img_data = "";
			can_list = new List<string>();
		}
	}
}
