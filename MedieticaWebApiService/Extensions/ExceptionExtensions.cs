using System;

namespace MedieticaWebApiService.Extensions
{
	public static class ExceptionExtensions
	{
		public static int LineNumber(this Exception ex)
		{
			var line_number = 0;
			var line_search = ":line ";
			var index = ex.StackTrace.LastIndexOf(line_search, StringComparison.Ordinal);
			if (index != -1)
			{
				var line_number_text = ex.StackTrace.Substring(index + line_search.Length);
				if (int.TryParse(line_number_text, out line_number))
				{
				}
			}
			else
			{
				line_search = ":linea";
				index = ex.StackTrace.LastIndexOf(line_search, StringComparison.Ordinal);
				if (index != -1)
				{
					var line_number_text = ex.StackTrace.Substring(index + line_search.Length);
					if (int.TryParse(line_number_text, out line_number))
					{
					}
				}
			}
			return line_number;
		}
	}
}
