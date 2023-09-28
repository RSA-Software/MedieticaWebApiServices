using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.ViewModel
{
	public class DistributoriArt
	{
		public string aco_ana { get; set; }
		public string ana_desc { get; set; }
		public string mer_desc { get; set; }
		public double aco_esistenza { get; set; }
		public double ana_sell_price { get; set; }
		public int ana_mer { get; set; }
		public string img_data { get; set; }

		public DistributoriArt()
		{
			var dsa = this;
			DbUtils.Initialize(ref dsa);
		}
	}
}
