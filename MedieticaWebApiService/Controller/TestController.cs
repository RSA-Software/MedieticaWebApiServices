using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Http;
using System.Web.Http.Cors;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Controller
{
	[EnableCors("*", "*", "*")]
	public class TestController : ApiController
	{
		[EnableCors("*", "*", "*")]

		// GET: api/Banche
		public string Get()
		{
			return ($"MedieticaWebApiService\nVer. 2022 B01.00\n\nCopyright(c) - Marco Medietica");
		}
	}
}
