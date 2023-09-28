using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public class ImgDitteDb
	{
		public int img_dit { get; set; }
		public int img_codice { get; set; }
		public short img_formato { get; set; }
		public short img_tipo { get; set; }
		public int img_bytes_size { get; set; }
		public DateTime? img_created_at { get; set; }
		public DateTime? img_last_update { get; set; }
		public string img_data { get; set; }

		private const int SmallSize = 512;
		private const short SwapGap = 20000;

		public ImgDitteDb()
		{
			var img_db = this;
			DbUtils.Initialize(ref img_db);
		}

		public static Image ByteArrayToImage(byte[] byteArrayIn)
		{
			if (byteArrayIn == null) return (null);

			var ms = new MemoryStream(byteArrayIn, 0, byteArrayIn.Length);
			ms.Write(byteArrayIn, 0, byteArrayIn.Length);
			return (Image.FromStream(ms, true));
		}

		public static byte[] ImageToByteArray(Bitmap imageIn, ImageFormat format)
		{
			var ms = new MemoryStream();
			imageIn.Save(ms, format);
			return ms.ToArray();
		}

		public static Bitmap ResizeImage(Image image, int width, int height)
		{
			Bitmap dest_image;

			var dest_rect = new Rectangle(0, 0, width, height);
			if (Equals(image.RawFormat, ImageFormat.Png))
				dest_image = new Bitmap(width, height, PixelFormat.Format32bppArgb);
			else
				dest_image = new Bitmap(width, height);

			dest_image.SetResolution(image.HorizontalResolution, image.VerticalResolution);
			using (var graphics = Graphics.FromImage(dest_image))
			{
				graphics.CompositingMode = CompositingMode.SourceCopy;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				using (var wrap_mode = new ImageAttributes())
				{
					wrap_mode.SetWrapMode(WrapMode.TileFlipXY);
					graphics.DrawImage(image, dest_rect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrap_mode);
				}
			}
			return dest_image;
		}

		public static bool Search(ref OdbcCommand cmd, int codDit, int codice, short formato, ref ImgDitteDb img, bool partial = false, bool writeLock = false)
		{
			if (img != null) DbUtils.Initialize(ref img);
			if (cmd == null)
			{
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					connection.Open();
					var command = new OdbcCommand { Connection = connection };
					return Search(ref command, codDit, codice, formato, ref img, partial, writeLock);
				}
			}

			string sql;
			if (partial)
				sql = DbUtils.QueryAdapt("SELECT img_dit, img_codice, img_formato, img_tipo, img_bytes_size, img_created_at, img_last_update FROM imgditte WHERE img_dit = ? AND img_codice = ? AND img_formato = ?", 1);
			else 
				sql = DbUtils.QueryAdapt("SELECT * FROM imgditte WHERE img_dit = ? AND img_codice = ? AND img_formato = ?", 1);
			if (writeLock) sql += " FOR UPDATE NOWAIT";
			cmd.CommandText = sql;
			cmd.Parameters.Clear();
			cmd.Parameters.Add("coddit", OdbcType.Int).Value = codDit;
			cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
			cmd.Parameters.Add("formato", OdbcType.SmallInt).Value = formato;

			List<string> esc_fields = null;
			if (partial) esc_fields = new List<string> { "img_data" };

			var reader = cmd.ExecuteReader();
			var found = reader.HasRows;
			while (img != null && reader.Read())
			{
				DbUtils.SqlRead(ref reader, ref img, esc_fields);
			}
			reader.Close();
			return (found);
		}

		public static void Write(ref OdbcCommand cmd, DbMessage msg, ref ImgDitteDb img, ref object obj, bool partial = true)
		{
			DbUtils.Trim(ref img);

			if (msg == DbMessage.DB_UPDATE || msg == DbMessage.DB_DELETE || msg == DbMessage.DB_CLEAR || msg == DbMessage.DB_REWRITE)
			{
				var old = new ImgDitteDb();
				if (!Search(ref cmd, img.img_dit, img.img_codice, img.img_formato, ref old, true, true)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
				if (old.img_last_update != img.img_last_update) throw new MCException(MCException.ModifiedMsg, MCException.ModifiedErr);
			}

			if (msg == DbMessage.DB_INSERT || msg == DbMessage.DB_UPDATE)
			{
				DitteDb dit = null;
				if (!DitteDb.Search(ref cmd, img.img_dit, ref dit)) throw new MCException(MCException.DittaMsg, MCException.DittaErr);
			}
			switch (msg)
			{
				case DbMessage.DB_INSERT:
					{
						do
						{
							try
							{
								//
								// Eliminiamo le immagini precedenti
								// 
								cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM imgditte WHERE img_dit = ? AND img_codice = ?");
								cmd.Parameters.Clear();
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = img.img_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = img.img_codice;
								cmd.ExecuteNonQuery();



								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref img, "imgditte");
								cmd.ExecuteNonQuery();

								//
								// Scriviamo la miniatura
								// 
								cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM imgditte WHERE img_dit = ? AND img_codice = ? AND img_formato = ?");
								cmd.Parameters.Clear();
								cmd.Parameters.Add("coddit", OdbcType.Int).Value = img.img_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = img.img_codice;
								cmd.Parameters.Add("formato", OdbcType.SmallInt).Value = img.img_formato + 1;
								cmd.ExecuteNonQuery();

								var image = (Bitmap)ByteArrayToImage(Convert.FromBase64String(img.img_data));
								if (image.Width > SmallSize || image.Height > SmallSize)
								{
									double mul;
									if (image.Width > image.Height)
										mul = SmallSize / (double)image.Width;
									else
										mul = SmallSize / (double)image.Height;
									image = ResizeImage(image, (int)(mul * image.Width), (int)(mul * image.Height));
								}

								var data = ImageToByteArray(image, img.img_tipo != 15 ? ImageFormat.Jpeg : ImageFormat.Png);
								var thu = new ImgDitteDb();
								thu.img_dit = img.img_dit;
								thu.img_codice = img.img_codice;
								thu.img_tipo = img.img_tipo;
								thu.img_formato = (short)(img.img_formato + 1);
								thu.img_bytes_size = data.Length;
								thu.img_data = Convert.ToBase64String(data);

								cmd.CommandText = DbUtils.SqlCommand(ref cmd, DbMessage.DB_INSERT, ref thu, "imgditte");
								cmd.ExecuteNonQuery();
							}
							catch (OdbcException ex)
							{
								if (DbUtils.IsDupKeyErr(ex))
								{
									img.img_formato += 2;
									continue;
								}
								throw;
							}
							break;
						} while (true);
					}
					break;

				case DbMessage.DB_DELETE:
				case DbMessage.DB_CLEAR:
				{
					cmd.CommandText = DbUtils.QueryAdapt("DELETE FROM imgditte WHERE img_dit = ? AND img_codice = ? AND img_formato IN (?, ?)");
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = img.img_dit;
					cmd.Parameters.Add("codice", OdbcType.Int).Value = img.img_codice;
					cmd.Parameters.Add("form1", OdbcType.SmallInt).Value = img.img_formato;
					cmd.Parameters.Add("form2", OdbcType.SmallInt).Value = (img.img_formato % 2) == 0 ? img.img_formato + 1 : img.img_formato - 1;
					cmd.ExecuteNonQuery();

					var arr = new List<short>();
					var sql = "SELECT img_formato FROM imgditte WHERE img_dit = ? AND img_codice = ? ORDER BY img_formato FOR UPDATE NOWAIT";
					cmd.CommandText = DbUtils.QueryAdapt(sql);
					cmd.Parameters.Clear();
					cmd.Parameters.Add("coddit", OdbcType.Int).Value = img.img_dit;
					cmd.Parameters.Add("coddip", OdbcType.Int).Value = img.img_codice;
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						arr.Add(reader.GetInt16(reader.GetOrdinal("img_formato")));
					}
					reader.Close();

					short last = 0;
					sql = DbUtils.QueryAdapt("UPDATE imgditte SET img_formato = ?, img_last_update = Now() WHERE img_dit = ? AND img_codice = ? AND img_formato = ?");
					foreach (var fmt in arr)
					{
						if ((fmt % 2) != (last % 2)) throw new MCException(MCException.ImgDisallineateMsg, MCException.ImgDisallineateErr);

						if (fmt != last)
						{
							cmd.CommandText = sql;
							cmd.Parameters.Clear();
							cmd.Parameters.Add("@form1", OdbcType.SmallInt).Value = last;
							cmd.Parameters.Add("coddit", OdbcType.Int).Value = img.img_dit;
							cmd.Parameters.Add("coddip", OdbcType.Int).Value = img.img_codice;
							cmd.Parameters.Add("@form2", OdbcType.SmallInt).Value = fmt;
							cmd.ExecuteNonQuery();
						}
						last++;
					}
				}
				break;

			}
		}

		public static void Swap(ref OdbcCommand cmd, DbMessage msg, ref ImagesSwap swa, ref IList<ImagesSwapList> arr, bool parial = false)
		{
			if (arr == null) return;

			//
			// Applichiamo il lock a tutti i record coinvolti
			//
			var first = true;
			var sql = "SELECT * FROM imgditte";
			var where = $" WHERE img_dit = {swa.img_dit} AND img_codice = {swa.img_codice} AND img_formato IN (";
			foreach (var swp in arr)
			{
				if (!first) where += ", ";
				where += $"{swp.img_formato}";
				if ((swp.img_formato % 2) == 0)
					where += $", {swp.img_formato + 1}";
				else
					where += $", {swp.img_formato - 1}";
				first = false;
			}
			where += ")";
			cmd.CommandText = DbUtils.QueryAdapt(sql + where);
			cmd.Parameters.Clear();
			cmd.ExecuteNonQuery();

			//
			// Riscriviamo i record con il formato in negativo
			//
			sql = DbUtils.QueryAdapt($"UPDATE imgditte SET img_formato = ? WHERE img_dit = {swa.img_dit} AND img_codice = {swa.img_codice} AND img_formato = ?");
			foreach (var swp in arr)
			{
				var old_img = swp.img_formato % 2 == 0 ? swp.img_formato : swp.img_formato - 1;
				var old_thu = swp.img_formato % 2 != 0 ? swp.img_formato : swp.img_formato + 1;

				cmd.CommandText = sql;
				cmd.Parameters.Clear();
				cmd.Parameters.Add("new_fmt", OdbcType.SmallInt).Value = -(old_img + SwapGap) ;
				cmd.Parameters.Add("old_fmt", OdbcType.SmallInt).Value = old_img;
				cmd.ExecuteNonQuery();

				cmd.CommandText = sql;
				cmd.Parameters.Clear();
				cmd.Parameters.Add("new_fmt", OdbcType.SmallInt).Value = - (old_thu + SwapGap);
				cmd.Parameters.Add("old_fmt", OdbcType.SmallInt).Value = old_thu;
				cmd.ExecuteNonQuery();
			}

			//
			// Riscriviamo i record nell'ordine desiderato
			//
			short last = 0;
			sql = DbUtils.QueryAdapt($"UPDATE imgditte SET img_formato = ?, img_last_update = Now() WHERE img_dit = {swa.img_dit} AND img_codice = {swa.img_codice} AND img_formato = ?");
			foreach (var swp in arr)
			{
				var old_img = swp.img_formato % 2 == 0 ? swp.img_formato : swp.img_formato - 1;
				var old_thu = swp.img_formato % 2 != 0 ? swp.img_formato : swp.img_formato + 1;

				do
				{
					try
					{
						cmd.CommandText = sql;
						cmd.Parameters.Clear();
						cmd.Parameters.Add("new_fmt", OdbcType.SmallInt).Value = last;
						cmd.Parameters.Add("ild_fmt", OdbcType.SmallInt).Value = - (old_img + SwapGap);
						cmd.ExecuteNonQuery();
					}
					catch (OdbcException ex)
					{
						if (DbUtils.IsDupKeyErr(ex))
						{
							last += 2;
							continue;
						}
						throw;
					}
					break;
				} while (true);

				last++;
				cmd.CommandText = DbUtils.QueryAdapt(sql);
				cmd.Parameters.Clear();
				cmd.Parameters.Add("new_fmt", OdbcType.SmallInt).Value = last;
				cmd.Parameters.Add("old_fmt", OdbcType.SmallInt).Value = -(old_thu + SwapGap);
				cmd.ExecuteNonQuery();

				last++;
			}
		}
		
		public static void Reload(ref OdbcCommand cmd, ref ImgDitteDb img, bool partial)
		{
			if (!Search(ref cmd, img.img_dit, img.img_codice, img.img_formato, ref img, partial)) throw new MCException(MCException.DeletedMsg, MCException.DeletedErr);
		}
	}
}
