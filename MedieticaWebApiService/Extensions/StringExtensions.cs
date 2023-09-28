
namespace MedieticaWebApiService.Extensions
{
	public static class StringExtensions
	{
		public static bool AllDigits(this string str)
		{
			foreach (var c in str)
			{
				if (c < '0' || c > '9')
					return false;
			}

			return true;
		}

		public static string SqlQuote(this string str, bool start_jolly = false, bool end_jolly = false, bool quotes = true)
		{
			str = str.Replace("\\", "\\\\");
			str = str.Replace('*', '%');
			str = str.Replace('?', '_');
			str = str.Replace("'", "''");
			str = str.Replace("\b", "\\b");
			str = str.Replace("\n", "\\n");
			str = str.Replace("\x00", "\\0");
			str = str.Replace("\r", "\\r");
			str = str.Replace("\t", "\\t");
			str = str.Replace("\u001A", "\\Z");

			var dest = "";

			if (quotes) dest = "'";
			if (start_jolly && !str.Contains("%") && !str.Contains("_"))
			{
				dest += "%";
			}

			dest += str;
			if (end_jolly && !str.Contains("%") && !str.Contains("_"))
			{
				dest += "%";
			}

			if (quotes) dest += "'";

			return (dest);
		}
		
		public static bool SqlDangerCheck(this string str)
		{
			var sql = str.ToUpper();

			if (sql.Contains("DROP ")) return true;
			if (sql.Contains("DELETE ")) return true;
			if (sql.Contains("INSERT ")) return true;
			if (sql.Contains("UPDATE ")) return true;

			return false;
		}


		public static bool IsValidEmail(this string email)
		{
			try
			{
				var addr = new System.Net.Mail.MailAddress(email);
				return addr.Address == email;
			}
			catch
			{
				return false;
			}
		}

	}
}
