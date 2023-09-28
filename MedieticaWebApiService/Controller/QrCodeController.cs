using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using MedieticaWebApiService.Extensions;
using MedieticaWebApiService.Helpers;
using MedieticaWebApiService.Models;

namespace MedieticaWebApiService.Controller
{
	[EnableCors("*", "*", "*")]

	public class QrCodeController : ApiController
	{
		[HttpGet]
		[Route("api/qrcode/mezzi/{ditta}/{codice}")]
		public DefaultJson<MezziDb> Mezzi(int ditta, int codice)
		{
			try
			{
				var user_level = (short)UserLevel.PUBLIC;
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<MezziDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var mez = new MezziDb();
					if (MezziDb.Search(ref cmd, ditta, codice, ref mez, true))
					{
						mez.img_list = new List<ImgMezziDb>();
						mez.doc_list = new List<DocMezziDb>();
						mez.man_list = new List<ManutenzioniDb>();
						mez.vid_list = new List<VideoMezziDb>();
						
						//
						// Leggiamo le immagini del mezzo
						//
						cmd.CommandText = @"
						SELECT * 
						FROM imgmezzi 
						WHERE img_dit = ? AND img_codice = ? AND Mod(img_formato, 2) <> 0
						ORDER BY img_formato";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var img = new ImgMezziDb();
							DbUtils.SqlRead(ref reader, ref img, ImgMezziDb.GetJoinExcludeFields());
							mez.img_list.Add(img);
						}
						reader.Close();

						//
						// Leggiamo le immagini del modello
						//
						cmd.CommandText = @"
						SELECT * 
						FROM imgmodelli 
						WHERE img_dit = ? AND img_codice = ? AND Mod(img_formato, 2) <> 0
						ORDER BY img_formato";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = mez.mez_dit_mod;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mez.mez_mod;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var img = new ImgModelliDb();
							DbUtils.SqlRead(ref reader, ref img);
							if (string.IsNullOrWhiteSpace(mez.img_data) && img.img_formato == 1)
							{
								mez.img_data = img.img_data;
							}
							var imj = new ImgMezziDb(img);
							mez.img_list.Add(imj);
						}
						reader.Close();

						//
						// Leggiamo i documenti del mezzo ed i relativi allegati
						//
						cmd.CommandText = @"
						SELECT * 
						FROM docmezzi
						WHERE dme_dit = ? AND dme_mez = ? AND dme_livello <= ?
						ORDER BY dme_codice";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
						cmd.Parameters.Add("level", OdbcType.SmallInt).Value = user_level;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var dme = new DocMezziDb();
							DbUtils.SqlRead(ref reader, ref dme);
							dme.all_list = new List<AllegatiDb>();
							mez.doc_list.Add(dme);
						}
						reader.Close();

						foreach (var dme in mez.doc_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = dme.dme_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dme.dme_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_MEZZI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								dme.all_list.Add(all);
							}
							reader.Close();
						}



						//
						// Leggiamo i documenti del modello e inseriamo nella lista quelli vaildi per il mezzo
						//
						var man_found = false;
						List<DocModelliDb> dmo_list = null;
						cmd.CommandText = @"
						SELECT * 
						FROM docmodelli
						LEFT JOIN  modserial ON dmo_dit = mse_dit AND dmo_codice = mse_dmo
						WHERE dmo_dit = ? AND dmo_mod = ? AND dmo_livello <= ?
						ORDER BY mse_serial_start DESC, dmo_codice";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = mez.mez_dit_mod;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mez.mez_mod;
						cmd.Parameters.Add("level", OdbcType.SmallInt).Value = user_level;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var dmo = new DocModelliDb();
							var mse = new ModSerialDb();
							DbUtils.SqlRead(ref reader, ref dmo);
							DbUtils.SqlRead(ref reader, ref mse);

							//
							// Inseriamo i documenti che non hanno specifiche su codice e matricole
							//
							if (mse.mse_dit == 0 && mse.mse_dmo == 0 && mse.mse_codice == 0)
							{
								if (dmo_list == null)
								{
									dmo_list = new List<DocModelliDb>();
									dmo_list.Add(dmo);
								}
								else
								{
									var dx = dmo_list.Find(x => (x.dmo_dit == dmo.dmo_dit && x.dmo_mod == dmo.dmo_mod && x.dmo_codice == dmo.dmo_codice));
									if (dx == null)
									{
										dmo_list.Add(dmo);
									}
								}
							}
							else
							{
								if (!man_found)
								{
									if (string.Compare(mse.mse_cod_for, mez.mez_cod_for, StringComparison.CurrentCultureIgnoreCase) == 0 || string.IsNullOrWhiteSpace(mez.mez_cod_for))
									{
										var valid = false;
										if (string.Compare(mez.mez_serial, mse.mse_serial_start, StringComparison.CurrentCultureIgnoreCase) >= 0)
										{
											if (!string.IsNullOrWhiteSpace(mse.mse_serial_stop))
											{
												if (string.Compare(mez.mez_serial, mse.mse_serial_stop, StringComparison.CurrentCultureIgnoreCase) <= 0) valid = true;
											}
											else valid = true;
										}
										if (valid)
										{
											man_found = true;
											if (dmo_list == null)
											{
												dmo_list = new List<DocModelliDb>();
												dmo_list.Add(dmo);
											}
											else
											{
												var dx = dmo_list.Find(x => (x.dmo_dit == dmo.dmo_dit && x.dmo_mod == dmo.dmo_mod && x.dmo_codice == dmo.dmo_codice));
												if (dx == null)
												{
													dmo_list.Add(dmo);
												}
											}

										}
									}
								}
							}
						}
						reader.Close();

						//
						// Aggiungiamo i documenti selezionati del modello alla lista dei documenti del mezzo
						//
						if (dmo_list != null)
						{
							foreach (var dmo in dmo_list)
							{
								var dme = new DocMezziDb(dmo);
								mez.doc_list.Add(dme);
							}
						}
						

			
						/*
						 * Inizio Vecchio Codice
						 *
						 */

						//
						// Leggiamo i documenti del modello ed i relativi allegati
						//
					/*	cmd.CommandText = @"
						SELECT * 
						FROM docmodelli
						WHERE dmo_dit = ? AND dmo_mod = ? AND dmo_livello <= ?
						ORDER BY dmo_codice";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = mez.mez_dit_mod;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mez.mez_mod;
						cmd.Parameters.Add("level", OdbcType.SmallInt).Value = user_level;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var dmo = new DocModelliDb();
							DbUtils.SqlRead(ref reader, ref dmo);
							var dme = new DocMezziDb(dmo);
							mez.doc_list.Add(dme);
						}
						reader.Close();*/
						
						/*
						 * Fine vecchio codice
						 *
						 */


						foreach (var dme in mez.doc_list)
						{
							if (dme.all_list == null)
							{
								dme.all_list = new List<AllegatiDb>();
								cmd.CommandText = @"
								SELECT * 
								FROM allegati
								WHERE all_dit = ? AND all_doc = ? AND all_type = ?
								ORDER BY all_idx";
								cmd.Parameters.Clear();
								cmd.Parameters.Add("ditta", OdbcType.Int).Value = dme.dme_dit;
								cmd.Parameters.Add("codice", OdbcType.Int).Value = dme.dme_codice;
								cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_MODELLI;
								reader = cmd.ExecuteReader();
								while (reader.Read())
								{
									var all = new AllegatiDb();
									DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
									dme.all_list.Add(all);
								}
								reader.Close();
							}
						}
						
						//
						// Leggiamo la lista delle manutenzioni
						//
						cmd.CommandText = @"
						SELECT * 
						FROM manutenzioni
						WHERE mnt_dit = ? AND mnt_mez = ?
						ORDER BY mnt_data DESC";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var man = new ManutenzioniDb();
							DbUtils.SqlRead(ref reader, ref man, ManutenzioniDb.GetJoinExcludeFields());
							man.all_list = new List<AllegatiDb>();
							mez.man_list.Add(man);
						}
						reader.Close();

						//
						// Leggiamo gli allegati delle manutenzioni
						//
						foreach (var mnt in mez.man_list)
						{
							mnt.all_list = new List<AllegatiDb>();
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = mnt.mnt_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = mnt.mnt_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_MANUTENZIONE_MEZZI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								mnt.all_list.Add(all);
							}
							reader.Close();
						}

						//
						// Leggiamo la lista dei video del mezzo
						//
						cmd.CommandText = @"
						SELECT * 
						FROM videomezzi
						WHERE vme_dit = ? AND vme_mez = ?
						ORDER BY vme_data DESC";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var vme = new VideoMezziDb();
							DbUtils.SqlRead(ref reader, ref vme);
							mez.vid_list.Add(vme);
						}
						reader.Close();

						//
						// Leggiamo la lista dei video del modello
						//
						cmd.CommandText = @"
						SELECT * 
						FROM videomodelli
						WHERE vmo_dit = ? AND vmo_mod = ?
						ORDER BY vmo_data DESC";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = mez.mez_dit_mod;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = mez.mez_mod;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var vmo = new VideoModelliDb();
							DbUtils.SqlRead(ref reader, ref vmo);
							var vme = new VideoMezziDb(vmo);
							mez.vid_list.Add(vme);
						}
						reader.Close();
						

						if (json.Data == null) json.Data = new List<MezziDb>();
						json.Data.Add(mez);
						json.RecordsTotal++;
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
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}

		[HttpGet]
		[Route("api/qrcode/dipendenti/{ditta}/{codfis}")]
		public DefaultJson<DipendentiDb> Dipendenti(int ditta, string codfis)
		{
			if (codfis.SqlDangerCheck()) throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, "Danger codfis value"));

			try
			{
				var user_level = (short)UserLevel.PUBLIC;
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<DipendentiDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var dip = new DipendentiDb();
					if (DipendentiDb.Search(ref cmd, ditta, codfis, ref dip, true))
					{
						dip.img_list = new List<ImgDipendentiDb>();

						dip.unilav_list = new List<DocDipendentiDb>();
						dip.ammministrazione_list = new List<DocDipendentiDb>();
						dip.autorizzazioni_list = new List<DocDipendentiDb>();
						dip.sicurezza_list = new List<DocDipendentiDb>();
						dip.tecnico_list = new List<DocDipendentiDb>();
						dip.corsi_list = new List<DocDipendentiDb>();

						//
						// Leggiamo le immagini del dipendente
						//
						cmd.CommandText = @"
						SELECT * 
						FROM imgdipendenti 
						WHERE img_dit = ? AND img_codice = ? AND Mod(img_formato, 2) <> 0
						ORDER BY img_formato";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dip.dip_codice;
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var img = new ImgDipendentiDb();
							DbUtils.SqlRead(ref reader, ref img);
							dip.img_list.Add(img);
						}
						reader.Close();


						//
						// Leggiamo i documenti del lavoratore ed i relativi allegati
						//
						cmd.CommandText = @"
						SELECT * 
						FROM documenti
						WHERE doc_dit = ? AND doc_dip = ? AND doc_livello <= ? AND doc_data IS NOT NULL
						ORDER BY doc_data DESC, doc_codice DESC";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = dip.dip_codice;
						cmd.Parameters.Add("level", OdbcType.SmallInt).Value = user_level;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var doc = new DocDipendentiDb();
							DbUtils.SqlRead(ref reader, ref doc);
							doc.all_list = new List<AllegatiDb>();

							switch (doc.doc_settore)
							{
								case (short)CheckListSettore.AMMINISTRAZIONE:
									dip.ammministrazione_list.Add(doc);
									break;

								case (short)CheckListSettore.AUTORIZZAZIONI:
									dip.autorizzazioni_list.Add(doc);
									break;

								case (short)CheckListSettore.SICUREZZA:
									dip.sicurezza_list.Add(doc);
									break;

								case (short)CheckListSettore.TECNICO:
									dip.tecnico_list.Add(doc);
									break;

								case (short)CheckListSettore.UNILAV:
									dip.unilav_list.Add(doc);
									break;

								case (short)CheckListSettore.CORSI:
									dip.corsi_list.Add(doc);
									break;
							}
						}
						reader.Close();

						foreach (var doc in dip.ammministrazione_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = doc.doc_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = doc.doc_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_DIPENDENTI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								doc.all_list.Add(all);
							}
							reader.Close();
						}

						foreach (var doc in dip.autorizzazioni_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = doc.doc_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = doc.doc_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_DIPENDENTI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								doc.all_list.Add(all);
							}
							reader.Close();
						}

						foreach (var doc in dip.sicurezza_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = doc.doc_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = doc.doc_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_DIPENDENTI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								doc.all_list.Add(all);
							}
							reader.Close();
						}

						foreach (var doc in dip.tecnico_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = doc.doc_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = doc.doc_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_DIPENDENTI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								doc.all_list.Add(all);
							}
							reader.Close();
						}

						foreach (var doc in dip.unilav_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = doc.doc_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = doc.doc_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_DIPENDENTI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								doc.all_list.Add(all);
							}
							reader.Close();
						}

						foreach (var doc in dip.corsi_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = doc.doc_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = doc.doc_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_DIPENDENTI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								doc.all_list.Add(all);
							}
							reader.Close();
						}

						if (json.Data == null) json.Data = new List<DipendentiDb>();
						json.Data.Add(dip);
						json.RecordsTotal++;
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
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}

		[HttpGet]
		[Route("api/qrcode/cantieri/{ditta}/{codice}")]
		public DefaultJson<CantieriDb> Cantieri(int ditta, int codice)
		{
			try
			{
				var user_level = (short)UserLevel.PUBLIC;
				using (var connection = new OdbcConnection(DbUtils.GetConnectionString()))
				{
					var json = new DefaultJson<CantieriDb>();

					connection.Open();
					var cmd = new OdbcCommand { Connection = connection };

					var can = new CantieriDb();
					if (CantieriDb.Search(ref cmd, ditta, codice, ref can, true))
					{
						can.img_list = new List<ImgCantieriDb>();

						can.ammministrazione_list = new List<DocCantieriDb>();
						can.autorizzazioni_list = new List<DocCantieriDb>();
						can.sicurezza_list = new List<DocCantieriDb>();
						can.tecnico_list = new List<DocCantieriDb>();
						can.contabilita_list = new List<DocCantieriDb>();
						can.rifiuti_list = new List<DocCantieriDb>();
						can.fornitori_list = new List<DocCantieriDb>();

						//
						// Leggiamo le immagini del cantiere
						//
						cmd.CommandText = @"
						SELECT * 
						FROM imgcantieri 
						WHERE img_dit = ? AND img_codice = ? AND Mod(img_formato, 2) <> 0
						ORDER BY img_formato";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
						var reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var img = new ImgCantieriDb();
							DbUtils.SqlRead(ref reader, ref img);
							can.img_list.Add(img);
						}
						reader.Close();

						//
						// Leggiamo i documenti della ditta ed i relativi allegati
						//
						cmd.CommandText = @"
						SELECT * 
						FROM docditte
						WHERE dod_dit = ? AND dod_livello <= ? AND dod_data IS NOT NULL
						ORDER BY dod_codice";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
						cmd.Parameters.Add("level", OdbcType.SmallInt).Value = user_level;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var dod = new DocDitteDb();
							DbUtils.SqlRead(ref reader, ref dod, DocDitteDb.GetJoinExcludeFields());

							var dca = new DocCantieriDb();
							dca.all_list = new List<AllegatiDb>();

							dca.dca_dit = dod.dod_dit;
							dca.dca_codice = dod.dod_codice;
							dca.dca_can = 0;
							dca.dca_livello = dod.dod_livello;
							dca.dca_settore = dod.dod_settore;
							dca.dca_data = dod.dod_data;
							dca.dca_desc = dod.dod_desc;
							dca.dca_url = dod.dod_url;
							dca.dca_data_rilascio = dod.dod_data_rilascio;
							dca.dca_data_scadenza = dod.dod_data_scadenza;
							dca.dca_scad_alert_before = dod.dod_scad_alert_before;
							dca.dca_scad_alert_after = dod.dod_scad_alert_after;
							dca.dca_created_at = dod.dod_created_at;
							dca.dca_last_update = dod.dod_last_update;

							switch (dca.dca_settore)
							{
								case (short)CheckListSettore.AMMINISTRAZIONE:
									can.ammministrazione_list.Add(dca);
									break;

								case (short)CheckListSettore.AUTORIZZAZIONI:
									can.autorizzazioni_list.Add(dca);
									break;

								case (short)CheckListSettore.SICUREZZA:
									can.sicurezza_list.Add(dca);
									break;

								case (short)CheckListSettore.TECNICO:
									can.tecnico_list.Add(dca);
									break;

								case (short)CheckListSettore.CONTABILITA:
									can.contabilita_list.Add(dca);
									break;

								case (short)CheckListSettore.RIFIUTI:
									can.rifiuti_list.Add(dca);
									break;

								case (short)CheckListSettore.FORNITORI:
									can.fornitori_list.Add(dca);
									break;
							}
						}
						reader.Close();

						foreach (var dca in can.ammministrazione_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = dca.dca_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dca.dca_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_DITTE;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								dca.all_list.Add(all);
							}
							reader.Close();
						}

						foreach (var dca in can.autorizzazioni_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = dca.dca_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dca.dca_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_DITTE;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								dca.all_list.Add(all);
							}
							reader.Close();
						}

						foreach (var dca in can.sicurezza_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = dca.dca_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dca.dca_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_DITTE;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								dca.all_list.Add(all);
							}
							reader.Close();
						}

						foreach (var dca in can.tecnico_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = dca.dca_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dca.dca_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_DITTE;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								dca.all_list.Add(all);
							}
							reader.Close();
						}
						
						foreach (var dca in can.contabilita_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = dca.dca_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dca.dca_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_DITTE;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								dca.all_list.Add(all);
							}
							reader.Close();
						}
						
						foreach (var dca in can.rifiuti_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = dca.dca_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dca.dca_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_DITTE;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								dca.all_list.Add(all);
							}
							reader.Close();
						}
						
						foreach (var dca in can.fornitori_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = dca.dca_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dca.dca_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_DITTE;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								dca.all_list.Add(all);
							}
							reader.Close();
						}

						//
						// Leggiamo i documenti del cantiere ed i relativi allegati
						//
						cmd.CommandText = @"
						SELECT * 
						FROM doccantieri
						WHERE dca_dit = ? AND dca_can = ? AND dca_livello <= ? AND dca_data IS NOT NULL
						ORDER BY dca_codice";
						cmd.Parameters.Clear();
						cmd.Parameters.Add("ditta", OdbcType.Int).Value = ditta;
						cmd.Parameters.Add("codice", OdbcType.Int).Value = codice;
						cmd.Parameters.Add("level", OdbcType.SmallInt).Value = user_level;
						reader = cmd.ExecuteReader();
						while (reader.Read())
						{
							var dca = new DocCantieriDb();
							DbUtils.SqlRead(ref reader, ref dca);
							dca.all_list = new List<AllegatiDb>();

							switch (dca.dca_settore)
							{
								case (short)CheckListSettore.AMMINISTRAZIONE:
									can.ammministrazione_list.Add(dca);
									break;

								case (short)CheckListSettore.AUTORIZZAZIONI:
									can.autorizzazioni_list.Add(dca);
									break;

								case (short)CheckListSettore.SICUREZZA:
									can.sicurezza_list.Add(dca);
									break;

								case (short)CheckListSettore.TECNICO:
									can.tecnico_list.Add(dca);
									break;

								case (short)CheckListSettore.CONTABILITA:
									can.contabilita_list.Add(dca);
									break;

								case (short)CheckListSettore.RIFIUTI:
									can.rifiuti_list.Add(dca);
									break;

								case (short)CheckListSettore.FORNITORI:
									can.fornitori_list.Add(dca);
									break;
							}
						}
						reader.Close();

						foreach (var dca in can.ammministrazione_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = dca.dca_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dca.dca_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_CANTIERI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								dca.all_list.Add(all);
							}
							reader.Close();
						}

						foreach (var dca in can.autorizzazioni_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = dca.dca_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dca.dca_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_CANTIERI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								dca.all_list.Add(all);
							}
							reader.Close();
						}

						foreach (var dca in can.sicurezza_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = dca.dca_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dca.dca_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_CANTIERI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								dca.all_list.Add(all);
							}
							reader.Close();
						}

						foreach (var dca in can.tecnico_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = dca.dca_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dca.dca_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_CANTIERI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								dca.all_list.Add(all);
							}
							reader.Close();
						}

						foreach (var dca in can.contabilita_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = dca.dca_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dca.dca_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_CANTIERI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								dca.all_list.Add(all);
							}
							reader.Close();
						}

						foreach (var dca in can.rifiuti_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = dca.dca_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dca.dca_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_CANTIERI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								dca.all_list.Add(all);
							}
							reader.Close();
						}

						foreach (var dca in can.fornitori_list)
						{
							cmd.CommandText = @"
							SELECT * 
							FROM allegati
							WHERE all_dit = ? AND all_doc = ? AND all_type = ?
							ORDER BY all_idx";
							cmd.Parameters.Clear();
							cmd.Parameters.Add("ditta", OdbcType.Int).Value = dca.dca_dit;
							cmd.Parameters.Add("codice", OdbcType.Int).Value = dca.dca_codice;
							cmd.Parameters.Add("tipo", OdbcType.SmallInt).Value = (short)AllegatiTipo.ALLEGATI_TYPE_CANTIERI;
							reader = cmd.ExecuteReader();
							while (reader.Read())
							{
								var all = new AllegatiDb();
								DbUtils.SqlRead(ref reader, ref all, AllegatiDb.GetExcludeFields());
								dca.all_list.Add(all);
							}
							reader.Close();
						}

						if (json.Data == null) json.Data = new List<CantieriDb>();
						json.Data.Add(can);
						json.RecordsTotal++;
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
			catch (Exception ex)
			{
				var res = new McResponse(ExceptionsType.GENERIC_EXCEPTION, 0, ex.Message, ex.StackTrace);
				throw new HttpResponseException(Request.CreateResponse<McResponse>(HttpStatusCode.InternalServerError, res));
			}
		}

	}
}
