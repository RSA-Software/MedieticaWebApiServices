using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MedieticaWebApiService.Models;

namespace MedieticaWebApiService.ViewModel
{
	public class Permessi
	{
		public int per_end { get; set; }
		public string end_desc { get; set; }
		public bool per_view { get; set; }
		public bool per_add { get; set; }
		public bool per_update { get; set; }
		public bool per_delete { get; set; }
		public bool per_special1 { get; set; }
		public bool per_special2 { get; set; }
		public bool per_special3 { get; set; }
		
		public Permessi()
		{
			per_end = 0;
			end_desc = "";
			per_view = false;
			per_add = false;
			per_update = false;
			per_delete = false;
			per_special1 = false;
			per_special2 = false;
			per_special3 = false;
		}
	}
}
