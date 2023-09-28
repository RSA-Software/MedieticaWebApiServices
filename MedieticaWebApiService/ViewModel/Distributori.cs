using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MedieticaWebApiService.Models;

namespace MedieticaWebApiService.ViewModel
{
	public class Distributori {
		public DistributoriDb dis { get; set; }
		public List<DistributoriArt> articoli { get; set; }

		public Distributori()
		{
			dis = new DistributoriDb();
			articoli = null;
		}
	}
}
