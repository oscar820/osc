using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace QTool
{

	public static class QMailTool
    {
	
		public static void Send(QMailAccount account, string toAddres, string title, string messageInfo, params string[] files)
		{
			_=SendAsync(account, toAddres, title, messageInfo, files);
		}
		
		public static async Task SendAsync(QMailAccount account, string toAddres, string title, string messageInfo ,params string[] files)
        {
            SmtpClient client = null;
			client = new SmtpClient(account.smtpServer);
			client.Credentials = new System.Net.NetworkCredential(account.account, account.password);
            client.EnableSsl = true;
			var message = new MailMessage();
			message.From = new MailAddress(account.account);
			message.To.Add(toAddres);
			message.IsBodyHtml = true;
			message.BodyEncoding = System.Text.Encoding.UTF8;
			message.Subject = title;
			message.Body = messageInfo;
			foreach (var filePath in files)
			{
				message.Attachments.Add(new Attachment(filePath));
			}
			await Send(client, message);
        }
        private static async Task Send(SmtpClient stmpClient, MailMessage message)
        {
            var task= stmpClient.SendMailAsync(message);
            await task;
            if (task.Exception != null)
            {
                Debug.LogError("发送邮件失败【"+message.Subject+"】:"+task.Exception);
            }
            else
            {
                Debug.Log("发送邮件成功 "+message.Subject+" \n"+message.Body);
            }
        }
		static async Task<string[]> CommondCheckReadLine(this StreamWriter writer,string command,StreamReader reader)
		{
			await writer.WriteLineAsync(command);
			return await reader.CheckReadLine(command);
		}
		static async Task<string[]> CheckReadLine(this StreamReader reader,string checkFlag)
		{
			var info = await reader.ReadLineAsync();
			if (info == null)
			{
				await reader.ReadLineAsync();
			}
			if (info != null && info.StartsWith("+OK"))
			{
				var infos = info.Split(' ');
				return infos; 
			}
			else
			{
				Debug.LogError(checkFlag + "读取出错 " + info);
				throw new Exception(checkFlag+"读取出错 " +info);
			}
		}
		static async Task<QMailInfo> ReceiveEmail(StreamWriter writer, StreamReader reader, long index, long countIndex = -1)
		{
			string Id = "";
			if (index == countIndex)
			{
				Id = (await writer.CommondCheckReadLine("UIDL " + index, reader))[2];
			}
			await writer.WriteLineAsync("RETR " + index);
			Debug.Log("读取第 " + index + "/" + countIndex + " 封邮件 大小：" + int.Parse((await reader.CheckReadLine("RETR " + index))[1]).ToSizeString());
			var info = "";
			string result = null;
			while (( result = await reader.ReadLineAsync()) != ".")
			{
				info += result + "\n"; 
			}
			var mail = new QMailInfo(info,index,Id);
			Debug.Log("邮件 " +mail);
			return mail;
		}
		public static async Task FreshEmails(QMailAccount account, Action<QMailInfo> callBack, QMailInfo lastMail)
		{
			using (TcpClient clientSocket = new TcpClient())
			{
				clientSocket.Connect(account.popServer, 995);
				//建立SSL连接
				using (SslStream stream = new SslStream(clientSocket.GetStream(), false, (object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors errors) => { return true; }))
				{

					stream.AuthenticateAsClient(account.popServer);
					using (StreamReader reader = new StreamReader(stream, Encoding.Default, true))
					{
						using (StreamWriter writer = new StreamWriter(stream))
						{
							writer.AutoFlush = true;

							try
							{
								await reader.CheckReadLine("SSL连接");
								await writer.CommondCheckReadLine("USER " + account.account, reader);
								await writer.CommondCheckReadLine("PASS " + account.password, reader);
								var infos = await writer.CommondCheckReadLine("STAT", reader);
								var mailCount = long.Parse(infos[1]);
								Debug.Log("邮件总数：" + mailCount + " 总大小：" + long.Parse(infos[2]).ToSizeString());



								long startIndex = 1;
								if (!string.IsNullOrWhiteSpace(lastMail?.Id))
								{
									Debug.Log("上一封邮件：" + lastMail.Date);
									if (await writer.IdCheck(lastMail.Index, lastMail.Id, reader))
									{
										startIndex = lastMail.Index + 1;
									}
									else
									{
										for (long i = lastMail.Index - 1; i >= 1; i--)
										{
											if (await writer.IdCheck(lastMail.Index, lastMail.Id, reader))
											{
												startIndex = lastMail.Index + 1;
												break;
											}
										}
									}
								}
								for (long i = startIndex; i <= mailCount; i++)
								{
									var mail = await ReceiveEmail(writer, reader, i, mailCount);
									try
									{
										callBack(mail);
									}
									catch (Exception e)
									{
										Debug.LogError("读取邮件出错：" + mail.Subject + "\n" + e);
									}
								}
								Debug.Log("接收邮件完成");
							}
							catch (Exception e)
							{

								Debug.LogError("邮件读取出错：" + e);
							}
							clientSocket.Close();
						}
					}
				}
			}
		}
		public static async Task<bool> IdCheck(this StreamWriter writer ,long index,string Id, StreamReader reader)
		{
			return (await writer.CommondCheckReadLine("UIDL " + index, reader))[2] == Id;
		}
	}
	
	[System.Serializable]
	public class QMailAccount
	{
		public string account;
		public string password;
		public string popServer;
		public string smtpServer;
		public void Init()
		{
			this.popServer = string.IsNullOrEmpty(this.popServer) ? GetServer(account, "pop.") : this.popServer;
			this.smtpServer = string.IsNullOrEmpty(this.smtpServer) ? GetServer(account, "smtp.") : this.smtpServer;
		}
		static string GetServer(string emailAddress, string start)
		{
			if (emailAddress.IndexOf('@') < 0)
			{
				throw new Exception("不支持邮箱 " + emailAddress);
			}
			return start + "." + emailAddress.Substring(emailAddress.IndexOf("@") + 1);
		}
		public bool InitOver
		{
			get
			{
				return !string.IsNullOrEmpty(account) && !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(popServer) && !string.IsNullOrEmpty(smtpServer);
			}
		}
	}
	public class QMailInfo
	{

		public string Subject;
		public string From;
		public string Cc;
		public string To;
		public string Date;
		public string Body;
		public long Index;
		public string Id;
		public QMailInfo()
		{

		}
		public QMailInfo(string mailStr, long Index,string Id)
		{
			this.Index = Index;
			this.Id = Id;
			Subject = GetString(mailStr, "Subject: ");
			From = GetString(mailStr, "From: ").Trim();
			Cc = GetString(mailStr, "Cc: ").Trim(); 
			To = GetString(mailStr, "To: ").Trim();
			Date= GetString(mailStr, "Date: "); 
			if (GetString(mailStr, "Content-Type: ") == "text/html; charset=utf-8")
			{
				if (GetString(mailStr, "Content-Transfer-Encoding: ") == "base64")
				{
					Body = ParseBase64String(mailStr.Substring(mailStr.IndexOf("base64") + 6).Trim());
					return;
				}
			}

			//Body = "未能解析格式：\n" + mailStr;
		}
		public override string ToString()
		{
			return "【" + Subject + "】from：" + From +"  "+Date+ "\n" + Body;
		}


		private static string ParseBase64String(string base64Str)
		{
			return Encoding.UTF8.GetString(Convert.FromBase64String(base64Str));
		}
		private static string GetString(string SourceString, string Key)
		{
			var startIndex = SourceString.IndexOf('\n'+Key);
			if (startIndex >= 0)
			{
				startIndex += Key.Length+1;
				var endIndex = SourceString.IndexOf('\n', startIndex);
				var info = SourceString.Substring(startIndex, endIndex - startIndex);
				return  CheckString(info);
			}
			else
			{
				return "";
			}
		}


		private static string CheckString(string SourceString)
		{
			if (SourceString.Contains("=?"))
			{
				if (SourceString.Contains("\"=?"))
				{
					SourceString = SourceString.Replace("\"=?", "=?").Replace("?=\"", "?=");
				}
				var start = SourceString.IndexOf("=?");
				var end = SourceString.LastIndexOf("?=");
				var midStr = SourceString.Substring(start, end - start + 2);
				var newInfo = Attachment.CreateAttachmentFromString("", midStr).Name;
				if (midStr.Contains(newInfo))
				{
					if (midStr.ToLower().StartsWith("=?utf-8?b?"))
					{
						newInfo = ParseBase64String(midStr.Substring(10, midStr.Length - 12));
					}
					else
					{
						Debug.LogError("[" + midStr + "]  =>  " + newInfo);
					}
				}

				return SourceString.Substring(0, start) + newInfo + SourceString.Substring(end + 2);
			}
			else
			{
				return SourceString;
			}
		}
	
	}
}
