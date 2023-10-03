using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Hosting;
using MedieticaWebApiService.Helpers;
using MedieticaWebApiService.Models;


namespace MedieticaWebApiService.Controller
{
	[EnableCors("*", "*", "*")]
	public class ImgClientiController : ApiController
	{
		[HttpGet]
		[Route("api/imgclienti/getthumbnails/{ditta}/{codice}")]
		public DefaultJson<ImgClientiDb> GetThumbnails(int ditta, int codice)
		{
			var json = new DefaultJson<ImgClientiDb>();
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					cmd.CommandText = "SELECT * FROM imgclienti WHERE img_dit = ? AND img_codice = ? AND Mod(img_formato, 2) <> 0 ORDER BY img_formato, img_codice";
					cmd.Parameters.Clear();
					cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var img = new ImgClientiDb();
						DbUtils.SqlRead(ref reader, ref img);
						if (json.Data == null) json.Data = new List<ImgClientiDb>();
						json.Data.Add(img);
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
		[Route("api/imgclienti/get/{ditta}/{codice}/{formato}")]
		public DefaultJson<ImgClientiDb> Get(int ditta, int codice, short formato)
		{
			var json = new DefaultJson<ImgClientiDb>();
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var img = new ImgClientiDb();
					if (ImgClientiDb.Search(ref cmd, ditta, codice, formato, ref img))
					{
						if (json.Data == null) json.Data = new List<ImgClientiDb>();
						json.Data.Add(img);
						json.RecordsTotal++;
					}

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
			if (json.RecordsTotal == 0)
			{
				throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Record non trovato : " + formato + " " + codice));
			}
			return (json);
		}



		[HttpGet]
		[Route("api/imgclienti/getimage/{ditta}/{codice}/{formato}")]
		public DefaultJson<ImagesJson> GetImage(int ditta, int codice, short formato)
		{
			int max_image_size = 600;

			var json = new DefaultJson<ImagesJson>();
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					var img = new ImgClientiDb();
					if (ImgClientiDb.Search(ref cmd, ditta, codice, formato, ref img))
					{
						var image = ImgClientiDb.ByteArrayToImage(Convert.FromBase64String(img.img_data));

						var bmp_w = image.Width;
						var bmp_h = image.Height;
						if ((bmp_w > max_image_size) || (bmp_h > max_image_size))
						{
							var zoom_w = max_image_size / (double)bmp_w;
							var zoom_h = max_image_size / (double)bmp_h;
							var zoom = zoom_w < zoom_h ? zoom_w : zoom_h;
							bmp_h = (int)(bmp_h * zoom);
							bmp_w = (int)(bmp_w * zoom);
							var bitmap = ImgClientiDb.ResizeImage(image, bmp_w, bmp_h);
							img.img_data = Convert.ToBase64String(ImgClientiDb.ImageToByteArray(bitmap, image.RawFormat));
						}

						var imj = new ImagesJson(img);
						imj.img_data = img.img_data;
						if (json.Data == null) json.Data = new List<ImagesJson>();
						json.Data.Add(imj);
						json.RecordsTotal++;
					}
					connection.Close();
				}
			}
			catch (MCException ex)
			{
				throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message));
			}
			catch (OdbcException ex)
			{
				throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message + " " + ex.StackTrace));
			}
			catch (Exception ex)
			{
				throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message + " " + ex.StackTrace));
			}
			if (json.Data == null) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));
			return (json);
		}


		[HttpPost]
		[Route("api/imgclienti/post/{ditta}/{codice}")]
		public async Task<DefaultJson<ImgClientiDb>> Post(int ditta, int codice)
		{
			var json = new DefaultJson<ImgClientiDb>();
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var upload_path = System.Reflection.Assembly.GetExecutingAssembly().Location;
					upload_path = Path.GetDirectoryName(upload_path);
					if (upload_path != null)
						upload_path += @"/Uploads";
					else
						upload_path = HostingEnvironment.MapPath("/") + @"/Uploads";
					Directory.CreateDirectory(upload_path);
					upload_path += @"/Images";
					Directory.CreateDirectory(upload_path);

					var provider = new MultipartFormDataStreamProvider(upload_path);
					await Request.Content.ReadAsMultipartAsync(provider);

					cmd.CommandText = DbUtils.QueryAdapt("SELECT COALESCE(CAST(ABS(MAX(img_formato)) AS INT), 0) AS codice FROM imgclienti WHERE img_dit = ? AND img_codice = ? AND Mod(img_formato, 2) = 0");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
					var last = (int)cmd.ExecuteScalar();
					if (last != 0) last += 2;

					//
					// Files
					//
					foreach (MultipartFileData file in provider.FileData)
					{
						var filename = file.Headers.ContentDisposition.FileName;
						if (filename[0] == '"') filename = filename.Substring(1, filename.Length - 1);
						if (filename[filename.Length - 1] == '"') filename = filename.Substring(0, filename.Length - 1);

						var ext = Path.GetExtension(filename);

						var img = new ImgClientiDb();
						var data = File.ReadAllBytes(file.LocalFileName);

						img.img_dit = ditta;
						img.img_codice = codice;
						img.img_tipo = (short)(string.Compare(ext.ToLower(), ".png", StringComparison.CurrentCulture) == 0 ? 15 : 4);
						img.img_formato = (short)last;
						img.img_bytes_size = data.Length;
						img.img_data = Convert.ToBase64String(data);

						File.Delete(file.LocalFileName);

						object obj = null;
						DbUtils.SqlWrite(ref cmd, ImgClientiDb.Write, DbMessage.DB_INSERT, ref img, ref obj, true);

						if (!ImgClientiDb.Search(ref cmd, img.img_dit, img.img_codice, (short)(img.img_formato + 1), ref img)) throw new MCException(MCException.NotFoundMsg, MCException.NotFoundErr);

						last = img.img_formato + 2;
						if (json.Data == null) json.Data = new List<ImgClientiDb>();
						json.Data.Add(img);
						json.RecordsTotal++;
					}
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

		
		[HttpPut]
		[Route("api/imgclienti/swap/{ditta}/{codice}")]
		public DefaultJson<ImgClientiDb> Swap(int ditta, int codice, [FromBody] DefaultJson<ImagesSwapList> value)
		{
			DefaultJson<ImgClientiDb> json;
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };
					
					var swa = new ImagesSwap(ditta, codice);
	
					var arr = value.Data;
					DbUtils.SqlWrite(ref cmd, ImgClientiDb.Swap, DbMessage.DB_INSERT, ref swa, ref arr);
					json = GetThumbnails(ditta, codice);
					
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


		[HttpDelete]
		[Route("api/imgclienti/delete/{ditta}/{codice}/{formato}")]
		public DefaultJson<ImgClientiDb> Delete(int ditta, int codice, short formato)
		{
			
			try
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var img = new ImgClientiDb();
					if (!ImgClientiDb.Search(ref cmd, ditta, codice, formato, ref img)) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, "Risorsa non trovata"));

					object objx = null;
					DbUtils.SqlWrite(ref cmd, ImgClientiDb.Write, DbMessage.DB_DELETE, ref img, ref objx);
					var json = GetThumbnails(ditta, codice);

					connection.Close();
					return (json);
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
