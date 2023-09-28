using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedieticaWebApiService.ViewModel
{
	public class Reports
	{
		public string rep_name { get; set; }
		public bool rep_email_sent { get; set; }
		public string rep_data { get; set; }

		public Reports()
		{
			rep_name = "";
			rep_data = null;
			rep_email_sent = false;
		}
	}
}
