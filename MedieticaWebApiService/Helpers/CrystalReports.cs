using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;

namespace MedieticaWebApiService.Helpers
{
	public enum ParType : short
	{
		PE_VI_NUMBER = 0,
		PE_VI_DATE = 3,
		PE_VI_STRING = 4,
		PE_VI_TIME = 6,
		PE_VI_LONG = 10,
	}

	public enum PaperUser : short
	{
		PaperUser = 256
	}

	public class Parameters
	{
		public string name { get; set; }
		public string subreport { get; set; }
		public string type { get; set; }
		public Object pVal { get; set; }

		public Parameters()
		{
			name = "";
			subreport = "";
			type = "NUMBER";
			pVal = null;
		}
	}

	public class NumParameters : Parameters
	{
		public NumParameters(string nam, string rep, double value)
		{
			name = name;
			subreport = rep;
			type = "NUMBER";
			pVal = new Double();
			pVal = value;
		}
	}

	public class LongParameters : Parameters
	{
		public LongParameters(string nam, string rep, long value)
		{
			name = nam;
			subreport = rep;
			type = "LONG";
			pVal = new Int64();
			pVal = 0;
		}
	}

	public class StringParameters : Parameters
	{
		public StringParameters(string nam, string rep, string value)
		{
			name = nam;
			subreport = rep;
			type = "STRING";
			pVal = new String('a', 1);
			pVal = value;
		}
	}

	public class DateParameters : Parameters
	{
		public DateParameters(string nam, string rep, DateTime value)
		{
			name = nam;
			subreport = rep;
			type = "DATE";
			pVal = new DateTime(value.Year, value.Month, value.Day, 0, 0, 0);
		}

		public DateParameters(string nam, string rep, int year, int mon, int day)
		{
			name = nam;
			subreport = rep;
			type = "DATE";
			pVal = new DateTime(year, mon, day, 0, 0, 0);
		}
	}

	public class TimeParameters : Parameters
	{
		public DateTime? val { get; set; }

		public TimeParameters(string nam, string rep, int value)
		{
			var hh = value / 3600;
			var mm = (value - hh * 3600) / 60;
			var ss = value - hh * 3600 - mm * 60;
			name = nam;
			subreport = rep;
			type = "TIME";
			pVal = new DateTime(0, 0, 0, hh, mm, ss);
			val = new DateTime(0, 0, 0, hh, mm, ss);
		}
		public TimeParameters(string nam, string rep, int hh, int mm, int ss)
		{
			name = nam;
			subreport = rep;
			type = "TIME";
			pVal = new DateTime(0, 0, 0, hh, mm, ss);
			val = new DateTime(0, 0, 0, hh, mm, ss);
		}

		public TimeParameters(string nam, string rep, DateTime time)
		{
			name = nam;
			subreport = rep;
			type = "TIME";
			pVal = time;
			val = time;
		}

	}

	public class Sort
	{
		public SortFieldType type { get; set; }
		public string name { get; set; }
		public SortDirection direction { get; set; }

		public Sort(string nam, SortDirection dir)
		{
			type = SortFieldType.RecordSortField;
			name = nam;
			direction = dir;
		}
	}
	public class Selection
	{
		public string subreport { get; set; }
		public string select { get; set; }

		public Selection(string nam, string sel)
		{
			subreport = nam;
			select = sel;
		}
	}

	public class TableSwitch
	{
		public string old_table { get; set; }
		public string new_table { get; set; }

		public TableSwitch(string vecchia, string nuova)
		{
			old_table = vecchia;
			new_table = nuova;
		}
	}


	public class Crystal
	{
		public string title { get; set; }
		public string path { get; set; }
		public string dnsLessConnection { get; set; }
		public string dbName { get; set; }
		public string user { get; set; }
		public string pwd { get; set; }
		public string pdf_file { get; set; }
		public string pdf_path { get; set; }
		public short papersize { get; set; }
		public PaperOrientation orientation { get; set; }
		public int paper_length { get; set; }
		public int paper_width { get; set; }
		public bool collated { get; set; }

		private List<Selection> selList { get; set; }
		private List<TableSwitch> tblList { get; set; }
		private List<Parameters> parList { get; set; }
		private List<Sort> sortList { get; set; }

		private bool getpaper;
		private ReportDocument _m_report;

		public Crystal()
		{
			path = "";
			title = "";
			pdf_file = "";
			pdf_path = "";
			getpaper = false;
			papersize = (short)PaperSize.PaperA4;
			orientation = PaperOrientation.Portrait;
			paper_length = 0;
			paper_width = 0;
			collated = true;
			selList = null;
			tblList = null;
		}

		public void CryOpen(string name, string titolo, bool fullPath = false, bool psql = false)
		{
			path = "";
			title = titolo;
			papersize = (short)PaperSize.PaperA4;
			orientation = PaperOrientation.Portrait;

			path = fullPath ? name : DbUtils.GetStartupOptions().ReportPath + @"\" + name;
			if (!File.Exists(path)) throw new MCException(MCException.ReportNotFoundMsg, MCException.ReportNotFoundErr);

			pdf_file = name.Substring(0, name.Length - 4).ToLower();
			pdf_file += $"_{DateTime.Now:yyyyMMdd_hhmmss}.pdf";
		}

		public void CryClose()
		{
			_m_report.Close();
			_m_report.Dispose();
			_m_report = null;
		}

		public void CrySetOrientation(PaperOrientation orien)
		{
			orientation = orien;
		}

		public void CrySetPaper(short pap)
		{
			papersize = pap;
		}
		public void CrySetPaperSize(short width, short length)
		{
			papersize = (short)PaperUser.PaperUser;
			paper_width = width * 10;
			paper_length = length * 10;
		}

		public void CrySetPaperSizeDm(short width, short length)
		{
			papersize = (short)PaperUser.PaperUser;
			paper_width = width;
			paper_length = length;
		}
		public void CryCollated(bool col)
		{
			collated = col;
		}

		public void CryLongParam(string name, string report, long val)
		{
			var par = new LongParameters(name, report, val);
			if (parList == null) parList = new List<Parameters>();
			parList.Add(par);
		}
		public void CryDoubleParam(string name, string report, double val)
		{
			var par = new NumParameters(name, report, val);
			if (parList == null) parList = new List<Parameters>();
			parList.Add(par);
		}
		public void CryDateParam(string name, string report, DateTime val)
		{
			var par = new DateParameters(name, report, val);
			if (parList == null) parList = new List<Parameters>();
			parList.Add(par);
		}

		public void CryDateParam(string name, string report, int year, int mon, int day)
		{
			var par = new DateParameters(name, report, year, mon, day);
			if (parList == null) parList = new List<Parameters>();
			parList.Add(par);
		}
		public void CryTimeParam(string name, string report, DateTime val)
		{
			var par = new TimeParameters(name, report, val);
			if (parList == null) parList = new List<Parameters>();
			parList.Add(par);
		}

		public void CryTimeParam(string name, string report, int hh, int mm, int ss)
		{
			var par = new TimeParameters(name, report, hh, mm, ss);
			if (parList == null) parList = new List<Parameters>();
			parList.Add(par);
		}

		public void CryStringParam(string name, string report, string val)
		{
			var par = new StringParameters(name, report, val);
			if (parList == null) parList = new List<Parameters>();
			parList.Add(par);
		}

		public void CrySort(string name, SortDirection dir = SortDirection.AscendingOrder)
		{
			var sort = new Sort(name, dir);
			if (sortList == null) sortList = new List<Sort>();
			sortList.Add(sort);
		}

		public void CrySelect(string select, string subreport = "")
		{
			var sel = new Selection(subreport, select);
			if (selList == null) selList = new List<Selection>();
			selList.Add(sel);
		}


		public void CryEsegui()
		{
			double position = 0;
			var open = false;

			try
			{
				_m_report = new ReportDocument();
				_m_report.Load(path);
				open = true;

				position = 1;
				if (getpaper)
				{
					orientation = _m_report.PrintOptions.PaperOrientation;
					papersize = (short)_m_report.PrintOptions.PaperSize;
				}


				//_m_report.VerifyDatabase();

				//
				// Passiamo i parametri al report
				//
				position = 9;
				if (parList != null && parList.Count > 0)
				{
					foreach (var par in parList)
					{
						var found = false;
						if (string.IsNullOrWhiteSpace(par.subreport))
						{
							foreach (ParameterField pardef in _m_report.ParameterFields)
							{
								if (string.Compare(pardef.Name, par.name, StringComparison.CurrentCulture) == 0)
								{
									found = true;
									break;
								}
							}
							switch (par.type.ToUpper())
							{
								case "STRING":
									position = 9.1;
									_m_report.SetParameterValue(par.name, (string)par.pVal);
									break;

								case "NUMBER":
									position = 9.2;
									_m_report.SetParameterValue(par.name, (double)par.pVal);
									break;

								case "DATE":
									position = 9.3;
									_m_report.SetParameterValue(par.name, (DateTime)par.pVal);
									break;

								case "TIME":
									position = 9.4;
									_m_report.SetParameterValue(par.name, (DateTime)par.pVal);
									break;

								case "LONG":
									position = 9.5;
									_m_report.SetParameterValue(par.name, (int)par.pVal);
									break;
							}
						}
						else
						{
							foreach (ReportObject rep in _m_report.ReportDefinition.ReportObjects)
							{
								if (rep.Kind == ReportObjectKind.SubreportObject)
								{
									var sub = (SubreportObject)rep;
									if (sub.SubreportName == par.subreport)
									{
										var subreport = new ReportDocument();
										subreport.OpenSubreport(sub.SubreportName);

										foreach (ParameterField pardef in subreport.ParameterFields)
										{
											if (string.Compare(pardef.Name, par.name, StringComparison.CurrentCulture) == 0)
											{
												found = true;
												break;
											}
										}

										switch (par.type)
										{
											case "STRING":
												position = 9.6;
												subreport.SetParameterValue(par.name, (string)par.pVal);
												break;

											case "NUMBER":
												position = 9.7;
												subreport.SetParameterValue(par.name, (double)par.pVal);
												break;

											case "DATE":
												position = 9.8;
												subreport.SetParameterValue(par.name, (DateTime)par.pVal);
												break;

											case "TIME":
												position = 9.9;
												subreport.SetParameterValue(par.name, (DateTime)par.pVal);
												break;

											case "LONG":
												position = 9.10;
												subreport.SetParameterValue(par.name, (int)par.pVal);
												break;
										}
										subreport.Close();
										break;
									}
								}
							}
						}
						if (!found) throw new MCException($"Position = {position:0.#} - Parametro non trovato - {par.name}", 0);
					}
				}
				//
				// Fine Passaggio Parametri
				//


				//
				// Impostiamo la formula di selezione dei record
				//
				position = 7;
				if (selList != null && selList.Count > 0)
				{
					foreach (var sel in selList)
					{
						if (string.IsNullOrWhiteSpace(sel.subreport))
							_m_report.RecordSelectionFormula = sel.select;
						else
						{
							foreach (ReportObject rep in _m_report.ReportDefinition.ReportObjects)
							{
								if (rep.Kind == ReportObjectKind.SubreportObject)
								{
									var sub = (SubreportObject)rep;
									if (sub.SubreportName == sel.subreport)
									{

										var subreport = new ReportDocument();
										subreport.OpenSubreport(sub.SubreportName);
										subreport.RecordSelectionFormula = sel.select;
										subreport.Close();
										break;
									}
								}
							}
						}
					}
				}
				//
				// Fine Impostazione Selezione
				//

				//
				// Impostiamo l'ordinamento dei records
				//
				position = 4;
				if (sortList != null && sortList.Count > 0)
				{
					foreach (var sort in sortList)
					{
						if (sort.name.StartsWith("{")) sort.name = sort.name.Substring(1);
						if (sort.name.EndsWith("}")) sort.name = sort.name.Remove(sort.name.Length - 1, 1);
					}

					//
					//  Rimuoviamo tutti i Sort Field Presenti
					//
					position = 4.1;
					var get_ras_sorts = _m_report.DataDefinition.SortFields.GetType().GetMethod("get_RasSorts", BindingFlags.NonPublic | BindingFlags.Instance);
					if (get_ras_sorts == null) throw new MCException($"Position = {position:0.#} - Metodo non trovato - get_RasSorts", 0);

					position = 4.2;
					var ras_sorts = get_ras_sorts.Invoke(_m_report.DataDefinition.SortFields, Type.EmptyTypes);

					position = 4.3;
					var add_sort = ras_sorts.GetType().GetMethod("Add");
					if (add_sort == null) throw new MCException($"Position = {position:0.#} - Metodo non trovato - Add", 0);

					position = 4.4;
					var remove_sort = ras_sorts.GetType().GetMethod("Remove");
					if (remove_sort == null) throw new MCException($"Position = {position:0.#} - Metodo non trovato - Remove", 0);

					position = 4.4;
					var ras_assembly = get_ras_sorts.ReturnType.Assembly;
					var ci_ras_sort = ras_assembly.GetType("CrystalDecisions.ReportAppServer.DataDefModel.SortClass").GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, System.Type.EmptyTypes, null);
					if (ci_ras_sort == null) throw new MCException($"Position = {position:0.#} - Metodo non trovato - CrystalDecisions.ReportAppServer.DataDefModel.SortClass", 0);

					for (var idx = _m_report.DataDefinition.SortFields.Count - 1; idx >= 0; idx--)
					{
						var sort = _m_report.DataDefinition.SortFields[idx];
						if (sort.SortType == SortFieldType.RecordSortField)
						{
							remove_sort.Invoke(ras_sorts, new object[] { idx });
						}
					}

					//
					// Aggiungiamo i nuovi sort Filed dalle tabelle
					//
					// A T T E N Z I O N E : Qui dobbiamo controllare che il campo sia presente anche nei nomi dei parametri o delle formule
					position = 4.5;
					foreach (var sort in sortList)
					{
						DatabaseFieldDefinition field_def = null;
						foreach (Table table in _m_report.Database.Tables)
						{
							for (var xxx = 0; xxx < table.Fields.Count; xxx++)
							{
								var field = table.Fields[xxx];
								var field_str = table.Name + "." + field.Name;
								var field_par = "{" + field_str + "}";

								if (field_str == sort.name || field_par == sort.name)
								{
									field_def = table.Fields[xxx];
									break;
								}
							}
							if (field_def != null) break;
						}
						if (field_def == null) throw new MCException(MCException.SortFieldNotFoundMsg + $" ({sort.name})", MCException.SortFieldNotFoundErr);

						position = 4.51;
						var ras_sort = ci_ras_sort.Invoke(Type.EmptyTypes);
						var set_sort_field = ras_sort.GetType().GetMethod("set_SortField", BindingFlags.Public | BindingFlags.Instance);
						if (set_sort_field == null) throw new MCException($"Position = {position:0.#} - Metodo non trovato - set_SortField", 0);

						position = 4.52;
						var get_ras_field = field_def.GetType().GetMethod("get_RasField", BindingFlags.NonPublic | BindingFlags.Instance);
						if (get_ras_field == null) throw new MCException($"Position = {position:0.#} - Metodo non trovato - get_ras_field", 0);

						position = 4.53;
						var ras_field = get_ras_field.Invoke(field_def, Type.EmptyTypes);
						if (ras_field == null) throw new MCException($"Position = {position:0.#} - Metodo non trovato - ras_field", 0);

						set_sort_field.Invoke(ras_sort, new object[] { ras_field });
						add_sort.Invoke(ras_sorts, new object[] { ras_sort });
					}

					//
					// Impostiamo la direzione dell'ordinamento
					//
					position = 4.6;
					var idy = 0;
					for (var idx = 0; idx < _m_report.DataDefinition.SortFields.Count; idx++)
					{
						var sort = _m_report.DataDefinition.SortFields[idx];
						if (sort.SortType == SortFieldType.RecordSortField)
						{
							sort.SortDirection = sortList[idy].direction;
							idy++;
						}
					}
				}
				//
				// Fine Impostazione Ordinamento Records
				//


				position = 5;
				_m_report.PrintOptions.PaperSize = (PaperSize)papersize;

				position = 6;
				_m_report.PrintOptions.PaperOrientation = orientation;

				// 				position = 8;
				// 				_m_report.Refresh();

				// 
				position = 3;
				// 				try
				// 				{
				//_m_report.VerifyDatabase();
				// 				}
				// 				catch (Exception)
				// 				{
				// 				}





				// 				 				position = 9.11;
				// 				 				_m_report.Refresh();



				//
				// Impostiamo i parametri per l'accesso al database;
				//
				foreach (Table table in _m_report.Database.Tables)
				{
					var logon = table.LogOnInfo;
					logon.ConnectionInfo.ServerName = dnsLessConnection;
					logon.ConnectionInfo.DatabaseName = dbName;
					logon.ConnectionInfo.UserID = user;
					logon.ConnectionInfo.Password = pwd;
					table.ApplyLogOnInfo(logon);

					if (tblList != null && tblList.Count > 0)
					{
						foreach (var tbl in tblList)
						{
							if (tbl.old_table == table.Name) table.Location = tbl.new_table;
						}
					}

					var test = table.TestConnectivity();
					if (!test) throw new MCException($"Position = {position:0.#} - Table Connectivity Failed - {table.Name}", 0);
				}

				position = 2;
				foreach (ReportObject rep in _m_report.ReportDefinition.ReportObjects)
				{
					if (rep.Kind == ReportObjectKind.SubreportObject)
					{
						var sub = (SubreportObject)rep;
						var subreport = sub.OpenSubreport(sub.SubreportName);
						foreach (Table table in subreport.Database.Tables)
						{
							var logon = table.LogOnInfo;
							var crcon = logon.ConnectionInfo;

							crcon.ServerName = dnsLessConnection;
							crcon.DatabaseName = dbName;
							crcon.UserID = user;
							crcon.Password = pwd;
							logon.ConnectionInfo = crcon;
							table.ApplyLogOnInfo(logon);

							if (tblList != null && tblList.Count > 0)
							{
								foreach (var tbl in tblList)
								{
									if (tbl.old_table == table.Name) table.Location = tbl.new_table;
								}
							}
							var test = table.TestConnectivity();
							if (!test) throw new MCException($"Position = {position:0.#} - Table Connectivity Failed - {table.Name}", 0);
						}
						//subreport.VerifyDatabase();
						subreport.Close();
					}

				}

				/*
				foreach (ReportDocument sub in _m_report.Subreports)
				{
					string name = sub.Name;
					var subdoc = sub.OpenSubreport(name);

					foreach (Table table in subdoc.Database.Tables)
					{
						var logon = table.LogOnInfo;
						var crcon = logon.ConnectionInfo;

						crcon.ServerName = dnsLessConnection;
						crcon.DatabaseName = dbName;
						crcon.UserID = user;
						crcon.Password = pwd;
						logon.ConnectionInfo = crcon;
						
						table.ApplyLogOnInfo(logon);

						if (tblList != null && tblList.Count > 0)
						{
							foreach (var tbl in tblList)
							{
								if (tbl.old_table == table.Name) table.Location = tbl.new_table;
							}
						}
						var test = table.TestConnectivity();
						if (!test) throw new MCException($"Position = {position:0.#} - Table Connectivity Failed - {table.Name}", 0);
					}
//					sub.VerifyDatabase();
				}
				*/
				//
				// Fine Impostazione parametri per l'accesso al database
				//




				//
				// Esportiamo il file in formato pdf
				//
				position = 10;
				pdf_path = System.Reflection.Assembly.GetExecutingAssembly().Location;
				pdf_path = System.IO.Path.GetDirectoryName(pdf_path);
				if (pdf_path != null)
					pdf_path += @"/Pdf";
				else
					pdf_path = HostingEnvironment.MapPath("/") + @"/Pdf";

				position = 11;
				Directory.CreateDirectory(pdf_path);



				position = 12;
				var export_opts = new ExportOptions();
				var disk_opts = ExportOptions.CreateDiskFileDestinationOptions();
				export_opts.ExportFormatType = ExportFormatType.PortableDocFormat;
				export_opts.ExportDestinationType = ExportDestinationType.DiskFile;
				//				export_opts.ExportFormatOptions = pdfFormatOptions;

				disk_opts.DiskFileName = pdf_path + @"/" + pdf_file;
				export_opts.ExportDestinationOptions = disk_opts;
				_m_report.Export(export_opts);
			}
			catch (System.Exception ex)
			{
				if (open)
				{
					_m_report.Close();
					_m_report.Dispose();
				}
				Debug.WriteLine(ex.Message);
				throw new MCException($"Position = {position:0.#} - " + ex.Message, 0);
			}
			finally
			{
			}
		}

		public void CrySetDnsLessConnInfo(string server, string dbname, string userId, string password)
		{
			dnsLessConnection = server;
			dbName = dbname;
			user = userId;
			pwd = password;
		}

	}

}
