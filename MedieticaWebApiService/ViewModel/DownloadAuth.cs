using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MedieticaWebApiService.Models;

namespace MedieticaWebApiService.ViewModel
{
	public class DownloadAuth
	{
		public short doc_tipo { get; set; }
		public int doc_dit { get; set; }
		public int doc_cod { get; set; }
		public string password{ get; set; }
		public bool auth{ get; set; }
		
		public DownloadAuth()
		{
			doc_tipo = 0;
			doc_dit = 0;
			doc_cod = 0;
			password = "";
			auth = false;
		}
	}
}
