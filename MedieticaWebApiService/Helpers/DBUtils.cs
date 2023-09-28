using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web.Http;
using MedieticaeWebApi.Filters;
using MedieticaWebApiService.Models;

namespace MedieticaWebApiService.Helpers
{
	public enum DbMessage : short
	{
		DB_INSERT = 0,
		DB_UPDATE = 1,
		DB_DELETE = 2,
		DB_REWRITE = 3,
		DB_CLEAR = 4,
		DB_ADD = 5,
		DB_INS = 6,
		DB_PAYMENT = 7,
		DB_ADD_ROW = 8,
		DB_BULK_INS = 9,
	};

	public enum DbType
	{
		FAIRCOM_DB = 0,
		POSTGRESQL_DB = 1,
		MYSQL_DB = 2,
		MARIA_DB = 3
	};

	
	public delegate void WriteDel<T1, T2>(ref OdbcCommand cmd, DbMessage msg, ref T1 rec, ref T2 obj, bool joined);

	public class DbUtils
	{
		private static readonly int _retryTime = 500;
		private static readonly int _retryNum = 150;

		// Utile per il debug a trovare il campo in cui fallisce l'accoppiamento con i parametri (mettere un numero  grande)
		public static int max_field = 1000000;

		public static bool IsDupKeyErr(OdbcException ex)
		{
			switch (GetStartupOptions().DbType)
			{
				case (int)DbType.FAIRCOM_DB:
					if (int.Parse(ex.Errors[0].SQLState) == 2) return (true);
					break;

				case (int)DbType.POSTGRESQL_DB:
					if (int.Parse(ex.Errors[0].SQLState) == 23505) return (true);		// Da Verificare
					break;

				case (int)DbType.MYSQL_DB:
				case (int)DbType.MARIA_DB:
					if (int.Parse(ex.Errors[0].SQLState) == 1061) return (true);		// Da Verificare
					if (int.Parse(ex.Errors[0].SQLState) == 1062) return (true);		// Da Verificare
					break;
			}
			return (false);
		}
		public static bool IsDatabaseNotExist(OdbcException ex)
		{
			switch (GetStartupOptions().DbType)
			{
				case (int)DbType.FAIRCOM_DB:
					if (ex.Errors[0].NativeError == -21094) return (true);
					if (ex.Errors[0].NativeError == -17012) return (true);
					break;

				case (int)DbType.POSTGRESQL_DB:
					if (ex.Errors[0].SQLState.Equals("08001")) return (true);
					break;

				case (int)DbType.MYSQL_DB:
				case (int)DbType.MARIA_DB:
					if (int.Parse(ex.Errors[0].SQLState) == 1049) return (true);        // Da Verificare
					break;
			}
			return (false);
		}

		public static bool IsLockErr(OdbcException ex)
		{
			switch (GetStartupOptions().DbType)
			{
				case (int)DbType.FAIRCOM_DB:
					if (ex.Errors[0].SQLState.Equals("00042")) return(true);
					if (ex.Errors[0].SQLState.Equals("00057")) return(true);
					if (ex.Errors[0].SQLState.Equals("00086")) return(true);
					break;

				case (int)DbType.POSTGRESQL_DB:
					if (ex.Errors[0].SQLState.StartsWith("55P03")) return (true);
					if (ex.Errors[0].SQLState.StartsWith("40P01")) return (true);
					if (ex.Errors[0].SQLState.StartsWith("40001")) return (true);
					break;

				case (int)DbType.MYSQL_DB:
				case (int)DbType.MARIA_DB:
					if (int.Parse(ex.Errors[0].SQLState) == 1015) return (true);
					if (int.Parse(ex.Errors[0].SQLState) == 1099) return (true);
					if (int.Parse(ex.Errors[0].SQLState) == 1205) return (true);
					if (int.Parse(ex.Errors[0].SQLState) == 1206) return (true);
					if (int.Parse(ex.Errors[0].SQLState) == 1207) return (true);
					if (int.Parse(ex.Errors[0].SQLState) == 1213) return (true);
					if (int.Parse(ex.Errors[0].SQLState) == 1223) return (true);
					if (int.Parse(ex.Errors[0].SQLState) == 1428) return (true);
					if (int.Parse(ex.Errors[0].SQLState) == 1614) return (true);
					if (int.Parse(ex.Errors[0].SQLState) == 1689) return (true);
					if (int.Parse(ex.Errors[0].SQLState) == 3058) return (true);
					if (int.Parse(ex.Errors[0].SQLState) == 3177) return (true);
					if (int.Parse(ex.Errors[0].SQLState) == 3572) return (true);
					break;
			}
			return (false);
		}

		public static string QueryAdapt(string query, long limit = 0, long offset = 0)
		{
			while (query.Length > 0)
			{
				if (query.Substring(0, 1) == "\n" || query.Substring(0, 1) == "\r" || query.Substring(0, 1) == "\t")
				{
					query = query.Remove(0, 1);
					continue;
				}
				break;
			}

			query = query.Replace("\t=", " =");
			query = query.Replace("\t", "");

			if ((limit <= 0) && (offset <= 0))
			{
				if (GetStartupOptions().DbType != (int)DbType.FAIRCOM_DB) query = query.Replace("admin.", "");
				return (query);
			}

			if (GetStartupOptions().DbType == (int)DbType.FAIRCOM_DB)
			{
				string top = "";
				string skip = "";
				string str = "";

				if (limit > 0) top = $" TOP {limit}";
				if (offset > 0) skip = $" SKIP {offset}";
				if (!string.IsNullOrWhiteSpace(top)) str += top;
				if (!string.IsNullOrWhiteSpace(skip)) str += skip;
				if (!string.IsNullOrWhiteSpace(str))
				{
					var sel = query.Substring(0, 15).ToUpper();
					if (sel == "SELECT DISTINCT")
					{
						query = query.Insert(15, str);
					}
					else
					{
						sel = query.Substring(0, 6).ToUpper();
						if (sel == "SELECT")
						{
							query = query.Insert(6, str);
						}
					}
				}
			}
			else
			{
				string top = "";
				string skip = "";
				string str = "";

				if (limit > 0) top =  $" LIMIT {limit}";
				if (offset > 0) skip = $" OFFSET {offset}";
				if (!string.IsNullOrWhiteSpace(top)) str += top;
				if (!string.IsNullOrWhiteSpace(skip)) str += skip;
				query += str;
				query = query.Replace("admin.", "");
			}
			return query;
		}
		

		public static Startup GetStartupOptions()
		{
#if SERVICE
#if SELF_HOSTING
			return (Program.startup_options);
#else
			return (WebApiConfig.startup_options);
#endif  // SELF_HOSTING
#else
			return App.startup_options;
#endif
		}
		


		public static UtentiDb GetUtente()
		{
#if SERVICE
#if SELF_HOSTING
			return (Program.ute);
#else
			return (WebApiConfig.ute);
#endif  // SELF_HOSTING
#else
			return App.ute;
#endif
		}


		public static bool FlagNoSync()
		{
#if SERVICE
#if SELF_HOSTING
			return (Program.flag_nosync);
#else
			return (WebApiConfig.flag_nosync);
#endif  // SELF_HOSTING
#else
			return App.flag_nosync;
#endif
		}
		public static bool FlagService()
		{
#if SERVICE
#if SELF_HOSTING
			return (Program.flag_service);
#else
			return (WebApiConfig.flag_service);
#endif  // SELF_HOSTING
#else
			return App.flag_service;
#endif
		}
		public static bool FlagModify()
		{
#if SERVICE
#if SELF_HOSTING
			return (Program.flag_modify);
#else
			return (WebApiConfig.flag_modify);
#endif  // SELF_HOSTING
#else
			return App.flag_modify;
#endif
		}

		public static string GetConnectionString(int anno = 0, bool nodb = false)
		{
			var strcon = "";
			var options = GetStartupOptions();

			switch (GetStartupOptions().DbType)
			{
				case (int)DbType.FAIRCOM_DB:
					if (nodb)
						strcon = $"DRIVER=c-treeACE ODBC Driver;HOST={options.Host};SERVICE={options.DbPort};QUERY_TIMEOUT=18000";
					else
						strcon = $"DRIVER=c-treeACE ODBC Driver;HOST={options.Host};DATABASE={options.Archivio};SERVICE={options.DbPort};UID={options.User};PWD={options.Password};QUERY_TIMEOUT=18000" ;
					break;

				case (int)DbType.POSTGRESQL_DB:
					if (nodb)
						strcon = $"Driver={{PostgreSQL ANSI}}; Server={options.Host}; Port={options.DbPort}; UID={options.User}; PWD={options.Password}; QUERY_TIMEOUT=18000; MaxVarcharSize=512; BoolsAsChar=0";
					else
						strcon = $"Driver={{PostgreSQL ANSI}}; Server={options.Host}; Database={options.Archivio}; Port={options.DbPort}; UID={options.User}; PWD={options.Password}; QUERY_TIMEOUT=18000; MaxVarcharSize=512; BoolsAsChar=0";
					break;

				case (int)DbType.MYSQL_DB:
					if (nodb)
						strcon = $"Driver=MySQL ODBC 8.0 ANSI Driver; Server={options.Host}; Port={options.DbPort};UID={options.User}; PWD={options.Password}; QUERY_TIMEOUT=18000";
					else
						strcon = $"Driver=MySQL ODBC 8.0 ANSI Driver; Server={options.Host}; Database={options.Archivio}; Port={options.DbPort}; UID={options.User}; PWD={options.Password}; QUERY_TIMEOUT=18000";
					break;

				case (int)DbType.MARIA_DB:
					if (nodb)
						strcon = $"Driver=MySQL ODBC 8.0 ANSI Driver; Server={options.Host}; Port={options.DbPort}; UID={options.User}; PWD={options.Password} ; QUERY_TIMEOUT=18000";
					else
						strcon = $"Driver=MySQL ODBC 8.0 ANSI Driver; Server={options.Host}; Database={options.Archivio}; Port={options.DbPort}; UID={options.User}; PWD={options.Password}; QUERY_TIMEOUT=18000";
					break;
			}
			return (strcon);
		}

		public static string GetDatabase()
		{
			var database = "";
			var options = GetStartupOptions();
			switch (GetStartupOptions().DbType)
			{
				case (int)DbType.FAIRCOM_DB:
						database = options.Archivio;
					break;

				case (int)DbType.POSTGRESQL_DB:
				case (int)DbType.MYSQL_DB:
				case (int)DbType.MARIA_DB:
					database = options.Archivio;
					break;
			}
			return database;
		}
		

		public static void Initialize<T>(ref T obj)
		{
			if (obj == null) return;
			foreach (var prop in obj.GetType().GetProperties())
			{
				var name = prop.Name;
				var type = prop.PropertyType.GetTypeInfo().Name;
				switch (type)
				{
					case "Char":
						prop.SetValue(obj, '0');
						break;

					case "Byte":
						prop.SetValue(obj, (byte)0);
						break;

					case "String":
						prop.SetValue(obj, "");
						break;

					case "Int16":
						prop.SetValue(obj, (short)0);
						break;
					case "Int32":
						prop.SetValue(obj, 0);
						break;
					case "Int64":
						prop.SetValue(obj, 0);
						break;
					case "Single":
						prop.SetValue(obj, 0);
						break;
					case "Double":
						prop.SetValue(obj, 0);
						break;
					case "Decimal":
						prop.SetValue(obj, 0m);
						break;
					case "Boolean":
						prop.SetValue(obj, false);
						break;
					case "DateTime":
						prop.SetValue(obj, new DateTime());
						break;
					case "TimeSpan":
						prop.SetValue(obj, new TimeSpan());
						break;
					case "Nullable`1":
						prop.SetValue(obj, null);
						break;

					case "Byte[]":
						prop.SetValue(obj, new byte[0]);
						break;

					case "IList`1":
						prop.SetValue(obj, null);
						break;

					case "List`1":
						prop.SetValue(obj, null);
						break;

					case "HttpStatusCode":
						prop.SetValue(obj, HttpStatusCode.OK);
						break;

					case "MemberTypes":
						break; 

					case "ArtDescDb":
					case "ArtUbicaDb":
					case "GenScadenze":
						prop.SetValue(obj, null);
						break;

					default:
						var message = $"Proprietà non inizializzata : {name}    Type : {type}";
						throw new MCException(message, MCException.NotInitializedPropertyErr);
				}
			}
		}

		//
		// Permette l'estrazione delle copie nome_campo - valore da un record per inserirli
		// nella  lista di GenericJson com ExpandObject
		public static void Extract<T>(T obj, ref IDictionary<string, object> row, string prefix = null)
		{
			foreach (var prop in obj.GetType().GetProperties())
			{
				string name;
				if (prefix != null)
					name = prefix + "_" + prop.Name;
				else
					name = prop.Name;
				row.Add(name, prop.GetValue(obj));
			}
		}

		public static void MapField<T>(ref T obj, IDictionary<string, object> row)
		{
			foreach (var y in row)
			{
				foreach (var prop in obj.GetType().GetProperties())
				{
					if (y.Key != prop.Name) continue;

					var type = prop.PropertyType.GetTypeInfo().Name;
					switch (type)
					{
						case "Char":
							prop.SetValue(obj, Convert.ToChar(y.Value));
							break;

						case "Byte":
							prop.SetValue(obj, Convert.ToByte(y.Value));
							break;

						case "String":
							prop.SetValue(obj, Convert.ToString(y.Value));
							break;

						case "Int16":
							prop.SetValue(obj, Convert.ToInt16(y.Value));
							break;

						case "Int32":
							prop.SetValue(obj, Convert.ToInt32(y.Value));
							break;

						case "Int64":
							prop.SetValue(obj, Convert.ToInt64(y.Value));
							break;

						case "Single":
							prop.SetValue(obj, Convert.ToSingle(y.Value));
							break;

						case "Double":
							prop.SetValue(obj, Convert.ToDouble(y.Value));
							break;

						case "Decimal":
							prop.SetValue(obj, Convert.ToDecimal(y.Value));
							break;

						case "Boolean":
							prop.SetValue(obj, Convert.ToBoolean(y.Value));
							break;

						case "DateTime":
							prop.SetValue(obj, Convert.ToDateTime(y.Value));
							break;

						case "TimeSpan":
							prop.SetValue(obj, TimeSpan.Parse(y.Value.ToString()));
							break;

						case "Nullable`1":
							if (y.Value != null)
							{
								if (y.GetType().Name == "TimeSpan")
									prop.SetValue(obj, TimeSpan.Parse(y.Value.ToString()));
								else
									prop.SetValue(obj, Convert.ToDateTime(y.Value));
							}
							else prop.SetValue(obj, null);
							break;

						default:
							var message = $"Proprietà non inizializzata : {prop.Name}    Type : {type}";
							throw new MCException(message, MCException.NotInitializedPropertyErr);
					}
					break;
				}
			}
		}

		public static void Trim<T>(ref T obj)
		{
			foreach (var prop in obj.GetType().GetProperties())
			{
				if (prop.PropertyType.GetTypeInfo().Name != "String") continue;
				var str = (string)prop.GetValue(obj);
				prop.SetValue(obj, str != null ? str.Trim() : "");
			}
		}

		public static void ToUpper<T>(ref T obj)
		{
			foreach (var prop in obj.GetType().GetProperties())
			{
				if (prop.PropertyType.GetTypeInfo().Name != "String") continue;
				var str = (string)prop.GetValue(obj);
				prop.SetValue(obj, str.ToUpper());
			}
		}

		public static void ToLower<T>(ref T obj)
		{
			foreach (var prop in obj.GetType().GetProperties())
			{
				if (prop.PropertyType.GetTypeInfo().Name != "String") continue;
				var str = (string)prop.GetValue(obj);
				prop.SetValue(obj, str.ToLower());
			}
		}

		public static void SqlRead<T>(ref OdbcDataReader reader, ref T obj, List<string> exclude = null)
		{
			if (obj == null) return;

			string last_prop = "";

			foreach (var prop in obj.GetType().GetProperties())
			{

				try
				{
					string name = prop.Name;
					last_prop = prop.Name;

					if (exclude != null)
					{
						var found = false;
						foreach (var field_name in exclude)
						{
							if (string.Equals(field_name, prop.Name))
							{
								found = true;
								break;
							}
						}

						if (found) continue;
					}

					var type_obj = prop.PropertyType.GetTypeInfo().Name;
					if (type_obj == "List`1" || type_obj == "IList`1")
					{
						try
						{
							var num = reader.GetOrdinal(name);
						}
						catch
						{
							continue;
						}
					}
					
					var type_db = reader.GetFieldType(reader.GetOrdinal(name));
					switch (type_obj)
					{
						case "Byte":
							prop.SetValue(obj, reader.GetByte(reader.GetOrdinal(name)));
							break;

						case "Char":
							prop.SetValue(obj, reader.GetChar(reader.GetOrdinal(name)));
							break;

						case "String":
							if (type_db != null && type_db.Name == "Byte[]")
							{
								if (!reader.IsDBNull(reader.GetOrdinal(name)))
									prop.SetValue(obj, Convert.ToBase64String((byte[]) reader.GetValue(reader.GetOrdinal(name))));
								else
									prop.SetValue(obj, null);
							}
							else
							{
								if (!reader.IsDBNull(reader.GetOrdinal(name)))
									prop.SetValue(obj, reader.GetString(reader.GetOrdinal(name)).Trim());
								else
									prop.SetValue(obj, "");
							}
							break;

						case "Int16":
							if (!reader.IsDBNull(reader.GetOrdinal(name)))
								prop.SetValue(obj, reader.GetInt16(reader.GetOrdinal(name)));
							else
								prop.SetValue(obj, (short)0);
							break;

						case "Int32":
							if (!reader.IsDBNull(reader.GetOrdinal(name)))
								prop.SetValue(obj, reader.GetInt32(reader.GetOrdinal(name)));
							else
								prop.SetValue(obj, 0);
							break;

						case "Int64":
							if (!reader.IsDBNull(reader.GetOrdinal(name)))
								prop.SetValue(obj, reader.GetInt64(reader.GetOrdinal(name)));
							else
								prop.SetValue(obj, 0L);
							break;

						case "Single":
							if (!reader.IsDBNull(reader.GetOrdinal(name)))
								prop.SetValue(obj, reader.GetFloat(reader.GetOrdinal(name)));
							else
								prop.SetValue(obj, (float)0);
							break;

						case "Double":
							if (!reader.IsDBNull(reader.GetOrdinal(name)))
								prop.SetValue(obj, reader.GetDouble(reader.GetOrdinal(name)));
							else
								prop.SetValue(obj, (float)0.0);
							break;

						case "Decimal":
							if (!reader.IsDBNull(reader.GetOrdinal(name)))
								prop.SetValue(obj, reader.GetDecimal(reader.GetOrdinal(name)));
							else
								prop.SetValue(obj, new decimal(0));
							break;

						case "Boolean":
							try
							{
								if (type_db.Name == "Int16")
									prop.SetValue(obj, reader.GetInt16(reader.GetOrdinal(name)) != 0 ? true : false);
								else
									prop.SetValue(obj, reader.GetBoolean(reader.GetOrdinal(name)));
							}
							catch
							{
								if (reader.IsDBNull(reader.GetOrdinal(name)))
									prop.SetValue(obj, false);
								else 
									throw;
							}
							break;

						case "DateTime":
							prop.SetValue(obj, reader.GetDateTime(reader.GetOrdinal(name)));
							break;

						case "TimeSpan":
							prop.SetValue(obj, reader.GetTime(reader.GetOrdinal(name)));
							break;

						case "Byte[]":
							if (!reader.IsDBNull(reader.GetOrdinal(name)))
								prop.SetValue(obj, reader.GetValue(reader.GetOrdinal(name)));
							else
								prop.SetValue(obj, null);
							break;

						case "Nullable`1":
							if (type_db != null && type_db.Name == "TimeSpan")
								prop.SetValue(obj, reader.IsDBNull(reader.GetOrdinal(name)) ? (TimeSpan?)null : reader.GetTime(reader.GetOrdinal(name)));
							else
								prop.SetValue(obj, reader.IsDBNull(reader.GetOrdinal(name)) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal(name)));
							break;

						case "IList`1":
							prop.SetValue(obj, null);
							break;

						case "List`1":
							prop.SetValue(obj, null);
							break;

						case "ArtDescDb":
						case "ArtUbicaDb":
						case "GenScadenze":
							prop.SetValue(obj, null);
							break;
							
						default:
							var message = $"Proprietà non inizializzata: {name}    Type : {type_obj}";
							throw new MCException(message, MCException.NotInitializedPropertyErr);
					}

				}
				catch (OdbcException)
				{
					throw;
				}
				catch (Exception ex)
				{
					throw new MCException(MCException.FieldNotFoundMsg + " " + ex.Message + " (" + last_prop + ")", MCException.FieldNotFoundErr);
				}
			}
		}


		public static string SqlCommand<T>(ref OdbcCommand cmd, DbMessage msg, ref T obj, string table, string where = null, List<string> exclude = null)
		{
			string sql;
			
			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_INS || msg == DbMessage.DB_ADD || msg == DbMessage.DB_BULK_INS || msg == DbMessage.DB_ADD_ROW)
			{
				sql = "INSERT INTO " + table + " (";

				//var num_field = 0;
				//var num_param = 0;
				//var num_func = 0;

				var idx = 0;

				var first = true;
				foreach (var prop in obj.GetType().GetProperties())
				{
					if (!prop.Name.EndsWith("last_update") && !prop.Name.EndsWith("created_at") && (prop.GetValue(obj) == null)) continue;
					var name= prop.Name;

					if (exclude != null)
					{
						var found = false;
						foreach (var field_name in exclude)
						{
							if (string.Equals(field_name, prop.Name))
							{
								found = true;
								break;
							}
						}

						if (found) continue;
					}

					if (!first) sql += ", ";
					sql += name;
					first = false;
					//num_field++;

					idx++;
					if (idx > max_field)
					{
						break;
					}
				}

				sql += ") VALUES (";
				first = true;
				idx = 0;
				foreach (var prop in obj.GetType().GetProperties())
				{
					if (!prop.Name.EndsWith("last_update") && !prop.Name.EndsWith("created_at") && (prop.GetValue(obj) == null)) continue;

					if (exclude != null)
					{
						var found = false;
						foreach (var field_name in exclude)
						{
							if (string.Equals(field_name, prop.Name))
							{
								found = true;
								break;
							}
						}

						if (found) continue;
					}

					if (!first) sql += ", ";
					if (prop.Name.EndsWith("last_update") || prop.Name.EndsWith("created_at"))
					{
						sql += "Now()";
						//num_func++;
					}
					else
					{
						sql += "?";
						//num_param++;
					}
					first = false;

					idx++;
					if (idx > max_field) break;
				}
				sql += ")";
			}
			else
			{
				sql = "UPDATE " + table + " SET ";
				var first = true;
				foreach (var prop in obj.GetType().GetProperties())
				{
					if (prop.Name.EndsWith("created_at")) continue;
					if (!prop.Name.EndsWith("last_update") && prop.GetValue(obj) == null) continue;
					var name = prop.Name;

					if (exclude != null)
					{
						var found = false;
						foreach (var field_name in exclude)
						{
							if (string.Equals(field_name, prop.Name))
							{
								found = true;
								break;
							}
						}
						if (found) continue;
					}

					if (!first) sql += ", ";
					sql += name;
					if (prop.Name.EndsWith("last_update"))
						sql += " = Now()";
					else
						sql += " = ?";
					first = false;
				}
				if (where != null) sql += " " + where;
			}

			if (DbUtils.GetStartupOptions().DbType == (int)DbType.POSTGRESQL_DB && msg != DbMessage.DB_BULK_INS) sql += " RETURNING *";

			sql = QueryAdapt(sql);
			SqlParameter(ref cmd, ref obj, msg, exclude);
			return (sql);
		}

		private static OdbcType GetParamType(PropertyInfo prop)
		{

			var type = prop.PropertyType.GetTypeInfo().Name;
			var fullname = prop.PropertyType.GetTypeInfo().FullName;
			switch (type)
			{
				case "Byte": return (OdbcType.TinyInt);
				case "String": return (OdbcType.VarChar);
				case "Int16": return (OdbcType.SmallInt);
				case "Int32": return (OdbcType.Int);
				case "Int64": return (OdbcType.BigInt);
				case "Single": return (OdbcType.Real);
				case "Double": return (OdbcType.Double);
				case "Decimal": return (OdbcType.Decimal);
				case "Boolean": return (OdbcType.Bit);
				case "DateTime": return (OdbcType.DateTime);
				case "TimeSpan": return (OdbcType.Time);
				case "Nullable`1":
					if (fullname != null && fullname.Contains("DateTime"))
						return (OdbcType.DateTime);
					else
						return (OdbcType.Time);

				case "Byte[]":
					return (OdbcType.VarBinary);

				default:
					var message = $"Proprietà non trovata : {prop.Name}    Type : {type}";
					throw new MCException(message, MCException.NotFoundPropertyErr);
			}
		}

		public static void SqlParameter<T>(ref OdbcCommand cmd, ref T obj, DbMessage msg, List<string> exclude = null)
		{
			cmd.Parameters.Clear();
			var idx = 0;
			foreach (var prop in obj.GetType().GetProperties())
			{
				if (prop.GetValue(obj) == null) continue;

				if (exclude != null)
				{
					var found = false;
					foreach (var field_name in exclude)
					{
						if (string.Equals(field_name, prop.Name))
						{
							found = true;
							break;
						}
					}
					if (found) continue;
				}

				var name = "@" + prop.Name;
				if (prop.Name.EndsWith("last_update")) continue;
				if (prop.Name.EndsWith("created_at")) continue;
				if (prop.Name.Equals("user"))
					cmd.Parameters.Add(name, OdbcType.VarChar).Value = GetUtente() != null ? GetUtente().ute_desc : GetStartupOptions().User;
				else
				{
					if (string.Compare(prop.Name,"img_data", StringComparison.CurrentCulture) == 0)
						cmd.Parameters.Add(name, OdbcType.VarBinary).Value = Convert.FromBase64String((string)prop.GetValue(obj));
					else if (prop.Name.EndsWith("_rtf"))
						cmd.Parameters.Add(name, OdbcType.Text).Value = prop.GetValue(obj);
					else
						cmd.Parameters.Add(name, GetParamType(prop)).Value = prop.GetValue(obj);
				}

				idx++;
				if (idx > max_field) break;
			}
		}

		public static void SqlSetIsolationLebvel(OdbcCommand cmd, IsolationLevel level)
		{
			var trn = cmd.Connection.BeginTransaction(level);
			trn.Commit();
		}
		
		/*
		public static void SqlWrite<T1, T2>(WriteDel<T1, T2> call, DbMessage msg, ref T1 rec, ref T2 obj, bool joined = false)
		{
			if (call == null) throw new ArgumentNullException(nameof(call));

			//
			// Apriamo la connessione al Database
			//
			var retry = 0;
			OdbcTransaction transaction = null;
			OdbcCommand cmd = null;
			using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
			{
				do
				{
					try
					{
						if (connection.State == System.Data.ConnectionState.Closed ||
						    connection.State == System.Data.ConnectionState.Broken)
						{
							connection.Open();
						}


						transaction = connection.BeginTransaction(System.Data.IsolationLevel.RepeatableRead);
						cmd = new OdbcCommand
						{
							Connection = connection,
							Transaction = transaction
						};

						//
						// Chiamata solo per Faircom
						//
						if (GetStartupOptions().DbType == (int)DbType.FAIRCOM_DB)
						{
							cmd.CommandText = "call fc_set_blockinglock( 0 )";
							cmd.Parameters.Clear();
							cmd.ExecuteNonQuery();
						}

						//
						// Chiamata solo per Postgresql
						//
						if (GetStartupOptions().DbType == (int)DbType.POSTGRESQL_DB)
						{
							cmd.CommandText = "SET lock_timeout TO '50'"; // 50 ms
							cmd.Parameters.Clear();
							cmd.ExecuteNonQuery();
						}
						call(ref cmd, msg, ref rec, ref obj, joined);
						transaction.Commit();
						connection.Close();
						return;
					}
					catch (OdbcException ex)
					{
						try
						{
							transaction?.Rollback();
						}
						catch
						{
							// ignored
						}
						if (!IsLockErr(ex)) throw;
						Thread.Sleep(_retryTime);
					}
					catch (MCException ex)
					{
						try
						{
							transaction?.Rollback();
						}
						catch
						{
							// ignored
						}

						if (ex.GetError() == MCException.LockedErr)
						{
							cmd?.Dispose();
							transaction?.Dispose();
							Thread.Sleep(_retryTime);
						}
						else
						{
							cmd?.Dispose();
							transaction?.Dispose();
							throw;
						}
					}
					catch
					{
						try
						{
							transaction?.Rollback();
						}
						catch
						{
							// ignored
						}
						cmd?.Dispose();
						transaction?.Dispose();
						throw;
					}
					retry++;
				} while (retry < _retryNum);

				connection.Close();
			}
			if (retry >= _retryNum) throw new MCException(MCException.DeadLockDetectedMsg, MCException.DeadLockDetecteErr);
		}
		*/

		public static void SqlWrite<T1, T2>(ref OdbcCommand cmd, WriteDel<T1, T2> call, DbMessage msg, ref T1 rec, ref T2 obj, bool joined = false)
		{
			if (call == null) throw new ArgumentNullException(nameof(call));

			//
			// Apriamo la connessione al Database
			//
			var connection = cmd.Connection;
			var retry = 0;
			OdbcTransaction transaction = null;

			//
			// Chiamata solo per Postgresql
			//
			if (GetStartupOptions().DbType == (int)DbType.POSTGRESQL_DB)
			{
				cmd.CommandText = "SET lock_timeout TO '50'"; // 50 ms
				cmd.Parameters.Clear();
				cmd.ExecuteNonQuery();
			}

			do
			{
				try
				{
						
					transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
					cmd.Transaction = transaction; 
					call(ref cmd, msg, ref rec, ref obj, joined);
					transaction.Commit();
					cmd.Transaction = null;
					SqlSetIsolationLebvel(cmd, IsolationLevel.ReadCommitted);
					return;
				}
				catch (OdbcException ex)
				{
					try
					{
						transaction?.Rollback();
					}
					catch
					{
							// ignored
					}
					cmd.Transaction = null;
					SqlSetIsolationLebvel(cmd, IsolationLevel.ReadCommitted);
					if (!IsLockErr(ex)) throw;
					Thread.Sleep(_retryTime);
				}
				catch (MCException ex)
				{
					try
					{
						transaction?.Rollback();
					}
					catch
					{
						// ignored
					}

					cmd.Transaction = null;
					SqlSetIsolationLebvel(cmd, IsolationLevel.ReadCommitted);
					if (ex.GetError() == MCException.LockedErr)
						Thread.Sleep(_retryTime);
					else
						throw;
				}
				catch
				{
					try
					{
						transaction?.Rollback();
					}
					catch
					{
						// ignored
					}
					cmd.Transaction = null;
					SqlSetIsolationLebvel(cmd, IsolationLevel.ReadCommitted);
					throw;
				}
				retry++;
			} while (retry < _retryNum);

			if (retry >= _retryNum) throw new MCException(MCException.DeadLockDetectedMsg, MCException.DeadLockDetecteErr);
		}

		public static string GetUnlockCode()
		{
			return ("BEWASR.CB1032026_PVchMWYK5Rkk");
		}


		public static short GetTokenLevel(HttpRequestMessage request)
		{
			AuthenticationHeaderValue authorization = request.Headers.Authorization;
			if (authorization == null) return ((short)UserLevel.NOAUTH);

			if (authorization.Scheme != "Basic" && authorization.Scheme != "OAuth") return ((short)UserLevel.NOAUTH);

			if (authorization.Scheme == "Basic") return ((short)UserLevel.SUPERADMIN);


			if (authorization.Scheme == "OAuth")
			{
				var token = "";
				var start = authorization.Parameter.IndexOf("oauth_token=", StringComparison.Ordinal);
				var end = authorization.Parameter.Length;
				if (start >= 0)
				{
					start += 13;
					while (start < end)
					{
						if (authorization.Parameter[start] == '"') break;
						token += authorization.Parameter[start];
						start++;
					}
				}

				if (string.IsNullOrEmpty(token)) return ((short)UserLevel.NOAUTH);


				var glob = new Chilkat.Global();
				if (!glob.UnlockBundle(DbUtils.GetUnlockCode())) return ((short)UserLevel.NOAUTH);

				var jwt = new Chilkat.Jwt();
				if (!jwt.VerifyJwt(token, "FWX322HYYMOKHGRT1560OPN67XZATR27")) return ((short)UserLevel.NOAUTH);

				var leeway = 60;
				if (!jwt.IsTimeValid(token, leeway)) return ((short)UserLevel.NOAUTH);
				var payload = jwt.GetPayload(token);
				var json = new Chilkat.JsonObject();
				if (!json.Load(payload)) return ((short)UserLevel.NOAUTH);

				var iss = json.StringOf("iss").Trim();
				var aud = json.StringOf("aud").Trim();
				var level = json.StringOf("level").Trim();
				if (string.Compare(iss, "https://www.rsaweb.com", StringComparison.CurrentCulture) != 0) return ((short)UserLevel.NOAUTH);
				if (string.Compare(aud, request.Headers.Host, StringComparison.CurrentCulture) != 0) return ((short)UserLevel.NOAUTH);

				if (!string.IsNullOrWhiteSpace(level))
				{
					try
					{
						return(short.Parse(level));
					}
					catch (Exception)
					{
						return ((short)UserLevel.NOAUTH);
					}
				}
			}

			return ((short)UserLevel.NOAUTH);
		}


		public static int GetTokenUser(HttpRequestMessage request)
		{
			AuthenticationHeaderValue authorization = request.Headers.Authorization;
			if (authorization == null) return 0;
			if (authorization.Scheme != "Basic" && authorization.Scheme != "OAuth") return 0;
			if (authorization.Scheme == "Basic") return 0;
			
			if (authorization.Scheme == "OAuth")
			{
				var token = "";
				var start = authorization.Parameter.IndexOf("oauth_token=", StringComparison.Ordinal);
				var end = authorization.Parameter.Length;
				if (start >= 0)
				{
					start += 13;
					while (start < end)
					{
						if (authorization.Parameter[start] == '"') break;
						token += authorization.Parameter[start];
						start++;
					}
				}

				if (string.IsNullOrEmpty(token)) return 0;

				var glob = new Chilkat.Global();
				if (!glob.UnlockBundle(DbUtils.GetUnlockCode())) return 0;

				var jwt = new Chilkat.Jwt();
				if (!jwt.VerifyJwt(token, "FWX322HYYMOKHGRT1560OPN67XZATR27")) return 0;

				var leeway = 60;
				if (!jwt.IsTimeValid(token, leeway)) return 0;
				var payload = jwt.GetPayload(token);
				var json = new Chilkat.JsonObject();
				if (!json.Load(payload)) return 0;

				var iss = json.StringOf("iss").Trim();
				var aud = json.StringOf("aud").Trim();
				var user = json.StringOf("user").Trim();
				if (string.Compare(iss, "https://www.rsaweb.com", StringComparison.CurrentCulture) != 0) return 0;
				if (string.Compare(aud, request.Headers.Host, StringComparison.CurrentCulture) != 0) return 0;

				if (!string.IsNullOrWhiteSpace(user))
				{
					try
					{
						return (int.Parse(user));
					}
					catch (Exception)
					{
						return 0;
					}
				}
			}

			return 0;
		}

		public static string GetTokenDitte(HttpRequestMessage request)
		{
			AuthenticationHeaderValue authorization = request.Headers.Authorization;
			if (authorization == null) return "";
			if (authorization.Scheme != "Basic" && authorization.Scheme != "OAuth") return "";
			if (authorization.Scheme == "Basic") return "";

			if (authorization.Scheme == "OAuth")
			{
				var token = "";
				var start = authorization.Parameter.IndexOf("oauth_token=", StringComparison.Ordinal);
				var end = authorization.Parameter.Length;
				if (start >= 0)
				{
					start += 13;
					while (start < end)
					{
						if (authorization.Parameter[start] == '"') break;
						token += authorization.Parameter[start];
						start++;
					}
				}

				if (string.IsNullOrEmpty(token)) return "";

				var glob = new Chilkat.Global();
				if (!glob.UnlockBundle(DbUtils.GetUnlockCode())) return "";

				var jwt = new Chilkat.Jwt();
				if (!jwt.VerifyJwt(token, "FWX322HYYMOKHGRT1560OPN67XZATR27")) return "";

				var leeway = 60;
				if (!jwt.IsTimeValid(token, leeway)) return "";
				var payload = jwt.GetPayload(token);
				var json = new Chilkat.JsonObject();
				if (!json.Load(payload)) return "";

				var iss = json.StringOf("iss").Trim();
				var aud = json.StringOf("aud").Trim();
				var ditte = json.StringOf("ditte").Trim();
				if (string.Compare(iss, "https://www.rsaweb.com", StringComparison.CurrentCulture) != 0) return "";
				if (string.Compare(aud, request.Headers.Host, StringComparison.CurrentCulture) != 0) return "";

				return (ditte);
			}

			return "";
		}

		public static void CheckAuthorization(OdbcCommand cmd, HttpRequestMessage request, int ditta, Endpoints edp, EndpointsOperations ope)
		{
			var authorization = request.Headers.Authorization;
			if (authorization == null) throw new HttpResponseException(request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg));
#if DEBUG
			if (authorization.Scheme == "Basic") return;
#endif
			if (authorization.Scheme != "OAuth") throw new HttpResponseException(request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg));

			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					cmd = new OdbcCommand { Connection = connection };
					CheckAuthorization(cmd, request, ditta, edp, ope);
					return;
				}
			}

			var token = "";
			var start = authorization.Parameter.IndexOf("oauth_token=", StringComparison.Ordinal);
			var end = authorization.Parameter.Length;
			if (start >= 0)
			{
				start += 13;
				while (start < end)
				{
					if (authorization.Parameter[start] == '"') break;
					token += authorization.Parameter[start];
					start++;
				}
			}

			if (string.IsNullOrEmpty(token)) throw new HttpResponseException(request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg));
			var glob = new Chilkat.Global();
			if (!glob.UnlockBundle(DbUtils.GetUnlockCode())) throw new HttpResponseException(request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg));

			var jwt = new Chilkat.Jwt();
			if (!jwt.VerifyJwt(token, "FWX322HYYMOKHGRT1560OPN67XZATR27")) throw new HttpResponseException(request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg));

			var leeway = 60;
			if (!jwt.IsTimeValid(token, leeway)) throw new HttpResponseException(request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg));
			var payload = jwt.GetPayload(token);
			var json = new Chilkat.JsonObject();
			if (!json.Load(payload)) throw new HttpResponseException(request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg));

			var iss = json.StringOf("iss").Trim();
			var aud = json.StringOf("aud").Trim();
			var usergroups = json.StringOf("gruppi").Trim();
			var userditte = json.StringOf("ditte").Trim();
			int[] dit_arr = Array.ConvertAll(userditte.Split(','), int.Parse);

			if (string.Compare(iss, "https://www.rsaweb.com", StringComparison.CurrentCulture) != 0) throw new HttpResponseException(request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg));
			if (string.Compare(aud, request.Headers.Host, StringComparison.CurrentCulture) != 0) throw new HttpResponseException(request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg));

			if (string.IsNullOrWhiteSpace(usergroups)) throw new HttpResponseException(request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg));

		/*	if (ditta != 0)
			{
				var found = false;
				if (dit_arr.Length > 0)
				{
					for (var idx = 0; idx < dit_arr.Length; idx++)
					{
						if (dit_arr[idx] == ditta)
						{
							found = true;
							break;
						}
					}
				}

				if (!found) throw new HttpResponseException(request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg));
			}*/

			var sql = $@"
			SELECT COUNT(*) FROM permessi
			WHERE per_usg IN ({usergroups}) AND per_end = ?";

			switch (ope)
			{
				case EndpointsOperations.VIEW:
					sql += " AND per_view <> 0";
					break;

				case EndpointsOperations.ADD:
					sql += " AND per_add <> 0";
					break;

				case EndpointsOperations.UPDATE:
					sql += " AND per_update <> 0";
					break;

				case EndpointsOperations.DELETE:
					sql += " AND per_delete <> 0";
					break;

				case EndpointsOperations.SPECIAL1:
					sql += " AND per_special1 <> 0";
					break;

				case EndpointsOperations.SPECIAL2:
					sql += " AND per_special2 <> 0";
					break;

				case EndpointsOperations.SPECIAL3:
					sql += " AND per_special3 <> 0";
					break;

				default:
					throw new HttpResponseException(request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg));
			}
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("endpoint", OdbcType.Int).Value = (int)edp;
			var num = (long)cmd.ExecuteScalar();
			if (num == 0) throw new HttpResponseException(request.CreateResponse(HttpStatusCode.MethodNotAllowed, MCException.MethodNotAllowedMsg));
		}
	}
}
