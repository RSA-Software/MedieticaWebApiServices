using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace MedieticaWebApiService.Helpers
{
	public enum ExceptionsType : short
	{
		MC_EXCEPTION = 0,
		ODBC_EXCEPTION = 1,
		GENERIC_EXCEPTION = 2,
	}
	public class McResponse
	{
		public ExceptionsType type { get; set; }
		public string type_desc { get; set; }
		public int error { get; set; }
		public string message { get; set; }
		public string stackTrace { get; set; }
		public List<string> alert_list { get; set; }
		public List<string> block_list { get; set; }
		public List<string> user_list { get; set; }

		public McResponse()
		{
			type = ExceptionsType.GENERIC_EXCEPTION;
			type_desc = "GENERIC EXCEPTION";
			error = 0;
			message = "";
			stackTrace = "";
			alert_list = null;
			block_list = null;
			user_list = null;
		}

		public McResponse(ExceptionsType tp, int err, string msg, string stack = "", List<string> alert = null, List<string> block = null, List<string> user = null)
		{
			type = tp;
			switch (tp)
			{
				case ExceptionsType.MC_EXCEPTION:
					type_desc = "MC EXCEPTION";
					break;

				case ExceptionsType.ODBC_EXCEPTION:
					type_desc = "ODBC EXCEPTION";
					break;

				default:
					type_desc = "GENERIC EXCEPTION";
					break;
			}
			error = err;
			message = msg;
			stackTrace = stack;
			alert_list = alert;
			block_list = block;
			user_list = user;
		}

	}

	public class MCException : DbException
	{
		public static readonly string LockedMsg = "Il record è bloccato da un altro nodo della rete.";
		public static readonly string DeletedMsg = "Il record è stato eliminato da un altro nodo della rete.";
		public static readonly string DuplicateMsg = "In archivio esiste già un record con lo stesso codice.";
		public static readonly string ModifiedMsg = "Il record è stato modificato da un altro nodo della rete.";
		public static readonly string AbortedMsg = "Operazione Annullata dall' Utente.";
		public static readonly string CancelMsg = "Non è possibile eliminare il record.";
		public static readonly string NotFoundMsg = "Record non trovato in archivio.";

		public static readonly string CatMercMsg = "Il codice della Categoria Merceologica non è valido o disponibile.";
		public static readonly string InvalidHostMsg = "Nome host non valido";
		public static readonly string InvalidDbMsg = "Nome database non valido";
		public static readonly string InvalidPortMsg = "Numero porta non valido";
		public static readonly string InvalidUserMsg = "Nome utente non valido";
		public static readonly string InvalidPasswordMsg = "Password non valida";
		public static readonly string FieldNotFoundMsg = "Nome di campo non trovato :";
		public static readonly string DeadLockDetectedMsg = "E' stato riscontrato un problema di DeadLock";
		public static readonly string CampoObbligatorioMsg = "Dati campo obbligatorio assenti";
		public static readonly string ChilkatInvalidMsg = "Chilkat Unlock code Invalid!";
		public static readonly string ImgDisallineateMsg = "Disallineamento tra Immagini e Miniature!";
		public static readonly string IoExceptionMsg = "Errore di Input - Output";
		public static readonly string ArticoloMsg = "Il codice dell' Articolo non è valido o disponibile.";
		public static readonly string DistributoreMsg = "Il codice del Distributore non è valido o disponibile.";
		public static readonly string UtenteMsg = "Il codice dell'Utente non è valido o disponibile.";
		public static readonly string InsufficientCreditMsg = "Credito non sufficiente";

		public static readonly string DittaMsg = "Il codice della Ditta non è valido o disponibile.";
		public static readonly string MansioneMsg = "Il codice della Mansione non è valido o disponibile.";
		public static readonly string DipendenteMsg = "Il codice del Dipendente non è valido o disponibile.";
		public static readonly string NormaMsg = "Il codice della Norma non è valido o disponibile.";
		public static readonly string CantiereMsg = "Il codice del Cantiere non è valido o disponibile.";
		public static readonly string DocumentiMsg = "Il codice del Documento non è valido o disponibile.";
		public static readonly string MarchioMsg = "Il codice del Marchio non è valido o disponibile.";
		public static readonly string TipologiaMsg = "Il codice della Tipologia non è valido o disponibile.";
		public static readonly string VerificaMsg = "Il codice della Verifica non è valido o disponibile.";
		public static readonly string ModelloMsg = "Il codice del Modello non è valido o disponibile.";
		public static readonly string GruppoMsg = "Il codice del Gruppo non è valido o disponibile.";
		public static readonly string MezzoMsg = "Il codice del Mezzo non è valido o disponibile.";
		public static readonly string AllegatiPathMsg = "Path per gli allegati non impostata o non valida";
		public static readonly string SmtpServerInvalidMsg = "Server SMTP per invio email non impostato";
		public static readonly string SmtpUserInvalidMsg = "User Server SMTP per invio email non impostato";
		public static readonly string SmtpPasswordInvalidMsg = "Password Server SMTP per invio email non impostata";
		public static readonly string SmtpPortInvalidMsg = "Porta Server SMTP per invio email non impostata";
		public static readonly string ToListInvalidMsg = "Non è stato impostato alcun utente a cui inviare il messaggio!";
		public static readonly string SendMailMsg = "Invio email fallito!";
		public static readonly string UtentiGruppoMsg = "Il codice dell Gruppo Utenti non è valido o disponibile.";
		public static readonly string EndpointMsg = "Il codice dell' Endpoint non è valido o disponibile.";
		public static readonly string UserTypeMsg = "L' account Utente è fgià presente con un tipo diverso.";
		public static readonly string DittaSubMsg = "La Ditta è già presente come subappaltatrice per il cantiere";
		public static readonly string DittaPivaMsg = "La partita Iva della ditta subappaltatrice non può essere uaguale alla partita Iva della ditta appaltante";
		public static readonly string DittaCodfisMsg = "Il codice fiscale della ditta subappaltatrice non può essere uaguale al codice fiscale della ditta appaltante";
		public static readonly string MethodNotAllowedMsg = "L'utente non il permesso per l'operazione richiesta!";
		public static readonly string CantiereSubappaltatriceMsg = "Le ditte subappaltatrici non possono inserire cantieri!";
		public static readonly string GiornaliMsg = "Il codice del Giornale di Lavori non è valido o disponibile.";
		public static readonly string ReportNotFoundMsg = "Il report richiesto non esiste.";
		public static readonly string SortFieldNotFoundMsg = "Nome campo di ordinamento non trovato";
		public static readonly string SubappaltatoreMsg = "Il codice della Ditta SubAppaltatrice non è valido o disponibile.";
		public static readonly string CertificatiPagamentoMsg = "Il codice del Certificato di Pagamento non è valido o disponibile.";
		public static readonly string DocModelloMsg = "Il codice del documento del modello non è valido o disponibile.";
		public static readonly string SedeMsg = "Il codice della Sede non è valido o disponibile.";
		public static readonly string CheckListMsg = "Il codice della CheckList non è valido o disponibile.";
		public static readonly string ClientiMsg = "Il codice del Cliente non è valido o disponibile.";
		public static readonly string AttivitaMsg = "Il codice dell' Attività non è valido o disponibile.";
		public static readonly string PoteriMsg = "Il codice del Potere non è valido o disponibile.";
		public static readonly string CaricheMsg = "Il codice della Carica non è valido o disponibile.";

		public static readonly int NoErr = 0;
		public static readonly int LockedErr = -10;
		public static readonly int DeletedErr = -11;
		public static readonly int DuplicateErr = -12;
		public static readonly int ModifiedErr = -13;
		public static readonly int AbortedErr = -14;
		public static readonly int CancelErr = -15;
		public static readonly int NotFoundErr = -16;

		public static readonly int NotInitializedPropertyErr = -20;
		public static readonly int NotFoundPropertyErr = -21;
		public static readonly int CatMercErr = -22;
		public static readonly int InvalidHostErr = -23;
		public static readonly int InvalidDbErr = -24;
		public static readonly int InvalidPortErr = -25;
		public static readonly int InvalidUserErr = -26;
		public static readonly int InvalidPasswordErr = -27;
		public static readonly int FieldNotFoundErr = -28;
		public static readonly int DeadLockDetecteErr = -29;
		public static readonly int IoExceptionerr = -30;
		public static readonly int CampoObbligatorioErr = -90;
		public static readonly int ChilkatInvalidErr = -91;
		public static readonly int ImgDisallineateErr = -92;
		public static readonly int ArticoloErr = -93;
		public static readonly int DistributoreErr = -94;
		public static readonly int UtenteErr = -95;
		public static readonly int InsufficientCreditErr = -96;

		public static readonly int DittaErr = -100;
		public static readonly int MansioneErr = -101;
		public static readonly int DipendenteErr = -102;
		public static readonly int NormaErr = -103;
		public static readonly int CantiereErr = -104;
		public static readonly int DocumentiErr = -105;
		public static readonly int MarchioErr = -106;
		public static readonly int TipologiaErr = -107;
		public static readonly int VerificaErr = -108;
		public static readonly int ModelloErr = -109;
		public static readonly int GruppoErr = -110;
		public static readonly int MezzoErr = -111;
		public static readonly int AllegatiPathErr = -112;
		public static readonly int SmtpServerInvalidErr = -123;
		public static readonly int SmtpUserInvalidErr = -124;
		public static readonly int SmtpPasswordInvalidErr = -125;
		public static readonly int SmtpPortInvalidErr = -126;
		public static readonly int ToListInvalidErr = -127;
		public static readonly int SendMailErr = -128;
		public static readonly int UtentiGruppoErr = -129;
		public static readonly int EndpointErr = -130;
		public static readonly int UserTypeErr = -131;
		public static readonly int DittaSubErr = -132;
		public static readonly int DittaPivaErr = -133;
		public static readonly int DittaCodfisErr = -134;
		public static readonly int MethodNotAllowedErr = -135;
		public static readonly int CantiereSubappaltatriceErr = -136;
		public static readonly int GiornaliErr = -137;
		public static readonly int ReportNotFoundErr = -138;
		public static readonly int SortFieldNotFoundErr = -139;
		public static readonly int SubappaltatoreErr = -140;
		public static readonly int CertificatiPagamentoErr = -141;
		public static readonly int DocModelloErr = -142;
		public static readonly int SedeErr = -143;
		public static readonly int CheckListErr = -144;
		public static readonly int ClientiErr = -145;
		public static readonly int AttivitaErr = -146;
		public static readonly int PoteriErr = -147;
		public static readonly int CaricheErr = -148;

		private readonly int _error;
		private readonly string _stackTrace;

		public MCException()
		{
			_error = 0;
			_stackTrace = "";
		}

		public MCException(string message, int err, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string path = null, [CallerMemberName] string caller = null)
			: base(message, err)
		{
			_error = err;
			_stackTrace = $"Linea : {lineNumber} - File : {path}  - Metodo : {caller}";
		}

		public MCException(string message, int err, Exception inner)
			: base(message, inner)
		{
			_error = err;
			_stackTrace = "";
		}

		public int GetError()
		{
			return _error;
		}
		public string GetStackTrace()
		{
			return _stackTrace;
		}

	}
}
