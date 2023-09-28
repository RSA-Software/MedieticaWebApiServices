using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using MedieticaWebApiService.Helpers;
using MedieticaWebApiService.Models;

namespace MedieticaWebApiService.Controller
{
	[EnableCors("*", "*", "*")]

	public class AllegatiController : ApiController
	{
		[HttpGet]
		[Route("api/allegati/{ditta}/{tipo}/{doc}")]
		public DefaultJson<AllegatiDb> Blank(int ditta, short tipo, int doc)
		{
			var json = new DefaultJson<AllegatiDb>();
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(all_idx), -1) AS codice FROM allegati WHERE all_dit = ? AND all_type = ? AND all_doc = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("type", OdbcType.SmallInt).Value = tipo;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = doc;
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var all = new AllegatiDb();
						all.all_dit = ditta;
						all.all_type = tipo;
						all.all_doc = doc;
						all.all_idx = 1 + reader.GetInt32(reader.GetOrdinal("codice"));
						if (json.Data == null) json.Data = new List<AllegatiDb>();
						json.Data.Add(all);
						json.RecordsTotal++;
					}
					reader.Close();
					connection.Close();
				}
			}
			catch (MCException ex)
			{
				var res = new McResponse(ExceptionsType.MC_EXCEPTION, ex.GetError(), ex.Message, ex.GetStackTrace());
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			catch (OdbcException ex)
			{
				var err = 0;
				if (ex.Errors.Count > 0) err = ex.Errors[0].NativeError;
				var res = new McResponse(ExceptionsType.ODBC_EXCEPTION, err, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			return (json);
		}

		[HttpGet]
		[Route("api/allegati/get/{ditta}/{tipo}/{doc}")]
		[Route("api/allegati/get/{ditta}/{tipo}/{doc}/{partial}")]
		public DefaultJson<AllegatiDb> Get(int ditta, short tipo, int doc, bool partial = true)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var json = new DefaultJson<AllegatiDb>();
					cmd.CommandText = DbUtils.QueryAdapt("SELECT * FROM allegati WHERE all_dit = ? AND all_type = ? AND all_doc = ? ORDER BY all_idx");
					cmd.Parameters.Clear();
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("type", OdbcType.SmallInt).Value = tipo;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = doc;
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var all = new AllegatiDb();
						DbUtils.Initialize(ref all);
						DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());

						if (!partial)
						{
							var upload_path = AllegatiDb.SetupPath(ditta, tipo, doc);
							upload_path += $"/{all.all_local_fname}";
							all.all_data = File.ReadAllBytes(upload_path);
						}

						if (json.Data == null) json.Data = new List<AllegatiDb>();
						json.Data.Add(all);
						json.RecordsTotal++;
					}

					reader.Close();
					connection.Close();
					return (json);
				}
			}
			catch (MCException ex)
			{
				var res = new McResponse(ExceptionsType.MC_EXCEPTION, ex.GetError(), ex.Message, ex.GetStackTrace());
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			catch (OdbcException ex)
			{
				var err = 0;
				if (ex.Errors.Count > 0) err = ex.Errors[0].NativeError;
				var res = new McResponse(ExceptionsType.ODBC_EXCEPTION, err, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			catch (HttpResponseException)
			{
				throw;
			}
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}

		[HttpGet]
		[Route("api/allegati/getbyid/{ditta}/{tipo}/{doc}/{index}")]
		[Route("api/allegati/getbyid/{ditta}/{tipo}/{doc}/{index}/{partial}")]
		public DefaultJson<AllegatiDb> GetById(int ditta, short tipo, int doc, int index, bool partial = true)
		{
			var json = new DefaultJson<AllegatiDb>();
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					cmd.CommandText = DbUtils.QueryAdapt("SELECT * FROM allegati WHERE all_dit = ? AND all_type = ? AND all_doc = ? AND all_idx = ?");
					cmd.Parameters.Clear();
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("type", OdbcType.SmallInt).Value = tipo;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = doc;
					cmd.Parameters.Add("index", OdbcType.Int).Value = index;
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var all = new AllegatiDb();
						DbUtils.Initialize(ref all);
						DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());

						if (!partial)
						{
							var upload_path = AllegatiDb.SetupPath(ditta, tipo, doc);
							upload_path += $"/{all.all_local_fname}";
							all.all_data = File.ReadAllBytes(upload_path);
						}

						if (json.Data == null) json.Data = new List<AllegatiDb>();
						json.Data.Add(all);
						json.RecordsTotal++;
					}
					reader.Close();
					connection.Close();
				}
			}
			catch (MCException ex)
			{
				var res = new McResponse(ExceptionsType.MC_EXCEPTION, ex.GetError(), ex.Message, ex.GetStackTrace());
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			catch (OdbcException ex)
			{
				var err = 0;
				if (ex.Errors.Count > 0) err = ex.Errors[0].NativeError;
				var res = new McResponse(ExceptionsType.ODBC_EXCEPTION, err, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			catch (HttpResponseException)
			{
				throw;
			}
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			return (json);
		}

		[HttpPost]
		[Route("api/allegati/post/{ditta}/{tipo}/{doc}")]
		public async Task<DefaultJson<AllegatiDb>> Post(int ditta, short tipo, int doc)
		{
			if (doc == 0) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid input value"));
			
			try
			{
				var upload_path = AllegatiDb.SetupPath(ditta, tipo, doc);
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var json = new DefaultJson<AllegatiDb>();
					
					var provider = new MultipartFormDataStreamProvider(upload_path);
					await Request.Content.ReadAsMultipartAsync(provider);

					//
					// Files
					//
					if (provider.FileData != null)
					{
						var filename = provider.FileData[0].Headers.ContentDisposition.FileName;
						if (filename[0] == '"') filename = filename.Substring(1, filename.Length - 1);
						if (filename[filename.Length - 1] == '"') filename = filename.Substring(0, filename.Length - 1);
						var old_local_path = upload_path + "/" + filename;

						using (FileStream write_stream = File.Open(old_local_path, FileMode.OpenOrCreate))
						{
							write_stream.Seek(0, SeekOrigin.End);
							foreach (MultipartFileData file in provider.FileData)
							{
								using (FileStream stream = File.OpenRead(file.LocalFileName))
								{
									byte[] buffer = new Byte[1024*5];
									int bytes_read;
									while ((bytes_read = stream.Read(buffer, 0, 1024 * 5)) > 0)
									{
										write_stream.Write(buffer, 0, bytes_read);
									}
								}
								File.Delete(file.LocalFileName);
							}
						}

						if (provider.FormData.Count == 0 || Convert.ToInt32(provider.FormData["total-chunk"]) -1 == Convert.ToInt32(provider.FormData["chunk-index"]))
						{
							var local_file = $"{doc:00000}_{DateTime.Now:yyyyMMdd_HHmmss}_{filename}";
							var local_path = upload_path + "/" + local_file;
							File.Move(old_local_path, local_path);

							cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(MAX(all_idx), -1) AS codice FROM allegati WHERE all_dit = ? AND all_type = ? AND all_doc = ?");
							cmd.Parameters.Clear();
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = ditta;
							cmd.Parameters.Add("type", OdbcType.SmallInt).Value = tipo;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = doc;
							var last = (int)cmd.ExecuteScalar();


							var fi = new FileInfo(local_path);
							var all = new AllegatiDb();
							all.all_dit = ditta;
							all.all_type = tipo;
							all.all_doc = doc;
							all.all_idx = last + 1;
							all.all_fname = filename;
							all.all_local_fname = local_file;
							if (provider.FileData[0].Headers.LastModified != null)
								all.all_date_time = provider.FileData[0].Headers.LastModified.Value.UtcDateTime;
							else if (provider.FileData[0].Headers.ContentDisposition.ModificationDate != null)
								all.all_date_time = provider.FileData[0].Headers.ContentDisposition.ModificationDate.Value.UtcDateTime;
							else if (provider.FileData[0].Headers.ContentDisposition.CreationDate != null)
								all.all_date_time = provider.FileData[0].Headers.ContentDisposition.CreationDate.Value.UtcDateTime;
							else
								all.all_date_time = DateTime.Now;
							all.all_bytes_size = fi.Length;

							object obj = null;
							DbUtils.SqlWrite(ref cmd, AllegatiDb.Write, DbMessage.DB_INSERT, ref all, ref obj, true);
							if (json.Data == null) json.Data = new List<AllegatiDb>();
							json.Data.Add(all);
							json.RecordsTotal++;
						}
					}

					connection.Close();
					return (json);
				}
			}
			catch (MCException ex)
			{
				var res = new McResponse(ExceptionsType.MC_EXCEPTION, ex.GetError(), ex.Message, ex.GetStackTrace());
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			catch (OdbcException ex)
			{
				var err = 0;
				if (ex.Errors.Count > 0) err = ex.Errors[0].NativeError;
				var res = new McResponse(ExceptionsType.ODBC_EXCEPTION, err, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			catch (HttpResponseException)
			{
				throw;
			}
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			
		}

		[HttpPut]
		[Route("api/allegati/put/{ditta}/{tipo}/{doc}/{index}/{partial}")]
		public DefaultJson<AllegatiDb> Put(int ditta, short tipo, int doc, int index, bool partial, [FromBody] DefaultJson<AllegatiDb> value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non corrispondente"));
			if (value.Data.Count != 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non valido"));

			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var json = new DefaultJson<AllegatiDb>();

					var all = value.Data[0];
					if (all.all_dit != ditta) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Ditta allegato non corrisponde"));
					if (all.all_type != tipo) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Tipo non corrisponde"));
					if (all.all_doc != doc) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Doc non corrisponde"));
					if (all.all_idx != index) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Idx non corrisponde"));

					object obj = null;
					if (all.all_data.Length != 0) all.all_data = Convert.FromBase64String(Encoding.Default.GetString(all.all_data));
					DbUtils.SqlWrite(ref cmd, AllegatiDb.Write, DbMessage.DB_UPDATE, ref all, ref obj, partial);
					if (json.Data == null) json.Data = new List<AllegatiDb>();
					json.Data.Add(all);
					json.RecordsTotal++;

					connection.Close();
					return (json);
				}
			}
			catch (MCException ex)
			{
				var res = new McResponse(ExceptionsType.MC_EXCEPTION, ex.GetError(), ex.Message, ex.GetStackTrace());
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			catch (OdbcException ex)
			{
				var err = 0;
				if (ex.Errors.Count > 0) err = ex.Errors[0].NativeError;
				var res = new McResponse(ExceptionsType.ODBC_EXCEPTION, err, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			catch (HttpResponseException)
			{
				throw;
			}
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}

		[HttpPatch]
		[Route("api/allegati/patch/{ditta}/{tipo}/{doc}/{index}/{partial}")]
		public DefaultJson<AllegatiDb> Patch(int ditta, short tipo, int doc, int index, bool partial, [FromBody] GenericJson value)
		{
			if (value == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null input value"));
			if (value.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Null Data value"));
			if (value.Data.Count != value.RecordsTotal) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non corrispondente"));
			if (value.Data.Count != 1) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Il numero di record non valido"));
			
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var json = new DefaultJson<AllegatiDb>();

					var all = new AllegatiDb();
					if (!AllegatiDb.Search(ref cmd, ditta, tipo, doc, index, ref all)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));
					DbUtils.MapField(ref all, value.Data[0]);
					if (all.all_dit != ditta) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Ditta allegato non corrisponde"));
					if (all.all_type != tipo) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Tipo non corrisponde"));
					if (all.all_doc != doc) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Doc non corrisponde"));
					if (all.all_idx != index) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Idx non corrisponde"));

					object objx = null;
					if (all.all_data != null) all.all_data = Convert.FromBase64String(Encoding.Default.GetString(all.all_data));
					DbUtils.SqlWrite(ref cmd, AllegatiDb.Write, DbMessage.DB_UPDATE, ref all, ref objx, partial);
					if (json.Data == null) json.Data = new List<AllegatiDb>();
					json.Data.Add(all);
					json.RecordsTotal++;

					connection.Close();
					return (json);
				}
			}
			catch (MCException ex)
			{
				var res = new McResponse(ExceptionsType.MC_EXCEPTION, ex.GetError(), ex.Message, ex.GetStackTrace());
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			catch (OdbcException ex)
			{
				var err = 0;
				if (ex.Errors.Count > 0) err = ex.Errors[0].NativeError;
				var res = new McResponse(ExceptionsType.ODBC_EXCEPTION, err, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}

		[HttpDelete]
		[Route("api/allegati/delete/{ditta}/{tipo}/{doc}/{index}")]
		public void Delete(int ditta, short tipo, int doc, int index)
		{
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var val = new AllegatiDb();
					if (!AllegatiDb.Search(ref cmd, ditta, tipo, doc, index, ref val)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object objx = null;
					DbUtils.SqlWrite(ref cmd, AllegatiDb.Write, DbMessage.DB_DELETE, ref val, ref objx);
					connection.Close();

					var upload_path = AllegatiDb.SetupPath(ditta, tipo, doc);
					upload_path += $"/{val.all_local_fname}";
					try
					{
						File.Delete(upload_path);
					}
					catch (DirectoryNotFoundException)
					{
					}
					catch (IOException)
					{
					}
					catch (UnauthorizedAccessException)
					{
					}
					catch
					{
						throw;
					}
				}
			}
			catch (MCException ex)
			{
				if (ex.ErrorCode == MCException.CancelErr)
				{
					var res = new McResponse(ExceptionsType.MC_EXCEPTION, ex.GetError(), ex.Message, ex.GetStackTrace());
					throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.Forbidden, res));
				}
				else
				{
					var res = new McResponse(ExceptionsType.MC_EXCEPTION, ex.GetError(), ex.Message, ex.GetStackTrace());
					throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
				}
			}
			catch (OdbcException ex)
			{
				var err = 0;
				if (ex.Errors.Count > 0) err = ex.Errors[0].NativeError;
				var res = new McResponse(ExceptionsType.ODBC_EXCEPTION, err, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}

	}
}
