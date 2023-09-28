using System.Collections.Generic;
using System.Dynamic;

namespace MedieticaWebApiService.Helpers
{
	public class DefaultJson<T>
	{
		public long RecordsTotal { get; set; }
		public IList<T> Data { get; set; }
	}
	
	public class GenericJson
	{
		public long RecordsTotal { get; set; }
		public IList<ExpandoObject> Data { get; set; }
	}

	public class ErrorJson<T>
	{
		public string Description { get; set; }
		public IList<T> Errors { get; set; }
	}

}