using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedieticaWebApiService.Extensions
{
	public static class NumericExtensions
	{
		public const double Epsilon = 0.0000000000000000001;

		public enum DecPlaces
		{
			MAX_DECIMAL_PLACES = 5,
			TOT_DECIMAL_PLACES = 2,
			SCO_DECIMAL_PLACES = 2,
			QTA_DECIMAL_PLACES = 3,
		}

		public static double MyFloor(this double num, short dec)
		{
			for (var idx = 1; idx <= dec; idx++)
			{
				num = num * 10;
			}
			num = Math.Floor(num);
			for (var idx = 1; idx <= dec; idx++)
			{
				num = num / 10;
			}
			return (num);
		}

		public static float MyFloor(this float num, short dec)
		{
			double val = num;
			for (var idx = 1; idx <= dec; idx++)
			{
				val = val * 10;
			}
			val = Math.Floor(val);
			for (var idx = 1; idx <= dec; idx++)
			{
				val = val / 10;
			}
			return ((float)val);
		}

		public static double MyCeil(this double num, short dec)
		{
			for (var idx = 1; idx <= dec; idx++)
			{
				num = num * 10;
			}
			num = Math.Ceiling(num);
			for (var idx = 1; idx <= dec; idx++)
			{
				num = num / 10;
			}
			return (num);
		}
		public static float MyCeil(this float num, short dec)
		{
			double val = num;
			for (var idx = 1; idx <= dec; idx++)
			{
				val = val * 10;
			}
			val = Math.Ceiling(val);
			for (var idx = 1; idx <= dec; idx++)
			{
				val = val / 10;
			}
			return ((float)val);
		}

		public static bool TestIfZero(this double val, DecPlaces dec)
		{
			return(TestIfZero(val, (int)dec));
		}

		public static bool TestIfZero(this double val, int dec)
		{
			if (dec > 8) dec = 8;
			var absval = Math.Abs(val);
			if (absval > 1) return (false);
			switch (dec)
			{
				case 0:
					if (absval < 0.9999999999999999999)
					{
						val = 0.0;
						return (true);
					}
					break;

				case 1:
					if (absval < 0.0999999999999999999)
					{
						val = 0.0;
						return (true);
					}
					break;

				case 2:
					if (absval < 0.0099999999999999999)
					{
						val = 0.0;
						return (true);
					}
					break;

				case 3:
					if (absval < 0.0009999999999999999)
					{
						val = 0.0;
						return (true);
					}
					break;

				case 4:
					if (absval < 0.0000999999999999999)
					{
						val = 0.0;
						return (true);
					}
					break;

				case 5:
					if (absval < 0.0000099999999999999)
					{
						val = 0.0;
						return (true);
					}
					break;

				case 6:
					if (absval < 0.0000009999999999999)
					{
						val = 0.0;
						return (true);
					}
					break;

				case 7:
					if (absval < 0.0000000999999999999)
					{
						val = 0.0;
						return (true);
					}
					break;

				case 8:
					if (absval < 0.0000000099999999999)
					{
						val = 0.0;
						return (true);
					}
					break;
			}

			var str = $"{val:0.00000000}";
			var buf = str.Substring(0, str.Length - (8 - dec));

			const string str1 = "0.0000000000";
			const string str2 = "-0.0000000000";
			if (buf != str1.Substring(0, dec + 2) && buf != str2.Substring(0, dec + 3))
			{
				val = Convert.ToDouble(buf);
				return (false);
			}
			return (true);
		}

		public static bool TestIfZero(this float val, DecPlaces dec)
		{
			return (TestIfZero(val, (int)dec));
		}

		public static bool TestIfZero(this float val, int dec)
		{
			if (dec > 8) dec = 8;
			var absval = Math.Abs(val);
			if (absval > 1) return (false);
			switch (dec)
			{
				case 0:
					if (absval < 0.9999999999999999999)
					{
						val = 0;
						return (true);
					}
					break;

				case 1:
					if (absval < 0.0999999999999999999)
					{
						val = 0;
						return (true);
					}
					break;

				case 2:
					if (absval < 0.0099999999999999999)
					{
						val = 0;
						return (true);
					}
					break;

				case 3:
					if (absval < 0.0009999999999999999)
					{
						val = 0;
						return (true);
					}
					break;

				case 4:
					if (absval < 0.0000999999999999999)
					{
						val = 0;
						return (true);
					}
					break;

				case 5:
					if (absval < 0.0000099999999999999)
					{
						val = 0;
						return (true);
					}
					break;

				case 6:
					if (absval < 0.0000009999999999999)
					{
						val = 0;
						return (true);
					}
					break;

				case 7:
					if (absval < 0.0000000999999999999)
					{
						val = 0;
						return (true);
					}
					break;

				case 8:
					if (absval < 0.0000000099999999999)
					{
						val = 0;
						return (true);
					}
					break;
			}

			var str = $"{val:0.00000000}";
			var buf = str.Substring(0, str.Length - (8 - dec));

			const string str1 = "0.0000000000";
			const string str2 = "-0.0000000000";
			if (buf != str1.Substring(0, dec + 2) && buf != str2.Substring(0, dec + 3))
			{
				val = Convert.ToSingle(buf);
				return (false);
			}
			return (true);
		}

		public static string CifreToLettere(this double numero)
		{
			var numlettere = new string[]
			{
				"","uno","due","tre","quattro","cinque","sei",
				"sette","otto","nove","dieci","undici","dodici",
				"tredici","quattordici","quindici","sedici","diciassette",
				"diciotto","diciannove","venti","trenta","quaranta",
				"cinquanta","sessanta","settanta","ottanta","novanta"
			};

			var str = "";
			var espon = 3;

			while (espon > -1)
			{
				var fraz1 = (int)Math.Floor(numero / Math.Pow(1000.0, espon));
				if (fraz1 > 0)
				{
					var fraz2 = fraz1;
					int fraz3;
					if ((fraz2 >= 100) && (fraz2 <= 999))
					{
						fraz3 = fraz2 / 100;
						fraz2 = fraz2 - fraz3 * 100;
						if (fraz3 == 1)
							str += "cento";
						else
						{
							str += numlettere[fraz3];
							str += "cento";
						}
					}

					if (fraz2 >= 21 && fraz2 <= 99)
					{
						fraz3 = fraz2 / 10;
						str += numlettere[fraz3 + 18];
						fraz2 = fraz2 - fraz3 * 10;
						if (fraz2 == 1 || fraz2 == 8) str = str.Substring(0, str.Length - 1);
						str += numlettere[fraz2];
					}
					else
					{
						if (fraz2 >= 0 && fraz2 <= 20) str += numlettere[fraz2];
					}

					switch (espon)
					{
						case 3:
							if (fraz1 == 1)
							{
								str = str.Substring(0, str.Length - 3);
								str += "unmiliardo";
							}
							else str += "miliardi";
							break;

						case 2:
							if (fraz1 == 1)
							{
								str = str.Substring(0, str.Length - 3);
								str += "unmilione";
							}
							else str += "milioni";
							break;

						case 1:
							if (fraz1 == 1)
							{
								str = str.Substring(0, str.Length - 3);
								str += "mille";
							}
							else str += "mila";
							break;
					}
				}
				numero = numero - fraz1 * Math.Pow(1000.0, espon);
				espon--;
			}

			if (string.IsNullOrWhiteSpace(str)) str = "zero";

			return (str);
		}
		
	}
}
