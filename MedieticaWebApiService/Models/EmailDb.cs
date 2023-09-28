using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Net;
using System.Text;
using System.Web.Http;
using Chilkat;
using MedieticaWebApiService.Helpers;

namespace MedieticaWebApiService.Models
{
	public enum MailPriority : short
	{
		MAIL_PRIORITY_NULL  = 0,
		MAIL_PRIORITY_HIGH  = 1,
		MAIL_PRIORITY_NORMAL = 3,
		MAIL_PRIORITY_LOW  = 4,
	}

	public class Recipients
	{
		public string friendly_name{ get; set; }
		public string email_adress { get; set; }

		public Recipients()
		{
			friendly_name = "";
			email_adress = "";
		}
	}

	public class Attachment
	{
		public string file_name { get; set; }
		public string file_data{ get; set; }
		public string content_type{ get; set; }

		public Attachment()
		{
			file_name = "";
			file_data = "";
			content_type = "";
		}
	}

	
	public class EmailMessages
	{
		public string subject { get; set; }
		public string message { get; set; }
		public string from { get; set; }
		public string reply_to { get; set; }
		public MailPriority priority { get; set; }
		public List<Recipients> to { get; set; }
		public List<Recipients> cc { get; set; }
		public List<Recipients> bcc { get; set; }
		public List<Attachment> attachments { get; set; }

		public EmailMessages()
		{
			subject = "";
			message = "";
			from = "";
			reply_to = "";
			priority = MailPriority.MAIL_PRIORITY_NORMAL;
			to = null;
			cc = null;
			bcc = null;
			attachments = null;
		}

		public static void Send(EmailMessages msg)
		{
			//
			// Controlliamo la corretta impostazione dei dati del server SMTP
			//
			if (string.IsNullOrEmpty(DbUtils.GetStartupOptions().SmtpServer)) throw new MCException(MCException.SmtpServerInvalidMsg, MCException.SmtpServerInvalidErr);
			if (string.IsNullOrEmpty(DbUtils.GetStartupOptions().SmtpUser)) throw new MCException(MCException.SmtpUserInvalidMsg, MCException.SmtpUserInvalidErr);
			if (string.IsNullOrEmpty(DbUtils.GetStartupOptions().SmtpUser)) throw new MCException(MCException.SmtpPasswordInvalidMsg, MCException.SmtpPasswordInvalidErr);
			if (DbUtils.GetStartupOptions().SmtpPort == 0) throw new MCException(MCException.SmtpPortInvalidMsg, MCException.SmtpPortInvalidErr);

			if (msg.to == null) throw new MCException(MCException.ToListInvalidMsg, MCException.ToListInvalidErr);
			
			var glob = new Global();
			if (!glob.UnlockBundle(DbUtils.GetUnlockCode())) throw new MCException(MCException.ChilkatInvalidMsg, MCException.ChilkatInvalidErr);

			var mailman = new MailMan();
			var mht = new Mht();
			var email = new Email();

			mht.UseCids = true;
			mht.EmbedImages = true;
			
			var mime = mht.HtmlToEML(msg.message);
			if (!email.SetFromMimeText(mime)) throw new MCException(MCException.SendMailMsg + " " + email.LastErrorText, MCException.SendMailErr); ;
			email.Subject = msg.subject.Trim();

			email.From = msg.from;
			foreach (var val in msg.to)
			{
				if (!email.AddTo(val.friendly_name.Trim(), val.email_adress.Trim())) throw new MCException(MCException.SendMailMsg + " " + email.LastErrorText, MCException.SendMailErr); ;
			}
			if (msg.cc != null)
			{
				foreach (var val in msg.cc)
				{
					if (!email.AddCC(val.friendly_name.Trim(), val.email_adress.Trim())) throw new MCException(MCException.SendMailMsg + " " + email.LastErrorText, MCException.SendMailErr); ;
				}
			}
			if (msg.bcc != null)
			{
				foreach (var val in msg.bcc)
				{
					if (!email.AddBcc(val.friendly_name.Trim(), val.email_adress.Trim())) throw new MCException(MCException.SendMailMsg + " " + email.LastErrorText, MCException.SendMailErr); ;
				}
			}
			
			if (msg.attachments != null)
			{
				foreach (var val in msg.attachments)
				{
					if (string.IsNullOrWhiteSpace(val.file_name)) continue;
					if (string.IsNullOrWhiteSpace(val.file_data)) continue;

					if (string.IsNullOrWhiteSpace(val.content_type))
					{
						if (!email.AddDataAttachment(val.file_name, Convert.FromBase64String(val.file_data))) throw new MCException(MCException.SendMailMsg + " " + email.LastErrorText, MCException.SendMailErr);
					}
					else
					{
						if (!email.AddDataAttachment2(val.file_name, Convert.FromBase64String(val.file_data), val.content_type)) throw new MCException(MCException.SendMailMsg + " " + email.LastErrorText, MCException.SendMailErr);
					}
				}
			}
			
			switch (msg.priority)
			{
				case MailPriority.MAIL_PRIORITY_NORMAL:
					email.AddHeaderField("X-Priority","3");
					break;

				case MailPriority.MAIL_PRIORITY_LOW:
					email.AddHeaderField("X-Priority", "4");
					break;

				case MailPriority.MAIL_PRIORITY_HIGH:
					email.AddHeaderField("X-Priority", "1");
					break;

				default:
					email.AddHeaderField("X-Priority", "3");
					break;
			}

			mailman.SmtpHost = DbUtils.GetStartupOptions().SmtpServer.Trim();
			mailman.SmtpUsername = DbUtils.GetStartupOptions().SmtpUser.Trim();
			mailman.SmtpPassword = DbUtils.GetStartupOptions().SmtpPassword.Trim();
			mailman.SmtpPort = DbUtils.GetStartupOptions().SmtpPort;
			mailman.SmtpSsl = DbUtils.GetStartupOptions().SmtpSsl;

			if (!mailman.SendEmail(email)) throw new MCException(MCException.SendMailMsg + " " + mailman.LastErrorText, MCException.SendMailErr);
		}
	}
}
