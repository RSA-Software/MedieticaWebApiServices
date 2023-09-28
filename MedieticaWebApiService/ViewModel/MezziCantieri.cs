using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MedieticaWebApiService.Models;

namespace MedieticaWebApiService.ViewModel
{
	public class MezziCantieri
	{
		public int mez_dit { get; set; }
		public int mez_codice { get; set; }
		public string mez_desc { get; set; }
		public string mez_serial { get; set; }
		public string mez_targa { get; set; }
		public string mez_telaio { get; set; }
		public string tip_desc { get; set; }
		public string mar_desc { get; set; }
		public int mez_enabled { get; set; }
		public string img_data { get; set; }
		public List<string> can_list { get; set; }

		public MezziCantieri()
		{
			mez_dit = 0;
			mez_codice = 0;
			mez_desc = "";
			mez_serial = "";
			mez_targa = "";
			mez_telaio = "";
			mez_enabled = 0;
			tip_desc = "";
			mar_desc = "";
			img_data = "";
			can_list = new List<string>();
		}
	}
}
