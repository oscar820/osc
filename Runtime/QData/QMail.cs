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
		static async Task<bool> CheckReadLine(this StreamReader reader,Action<string[]> trueAction=null)
		{
			var task= reader.ReadLineAsync();
			var info = await task;
			if (task.Exception != null)
			{
				Debug.LogError("读取出错：\n" + task.Exception);
			}
			if (info.StartsWith("+OK"))
			{
				var infos = info.Split(' ');
				trueAction?.Invoke(infos);
				return true;
			}
			else
			{
				Debug.LogError(info);
				return false;
			}
		}
		public static List<string> OldMailIdList;
		public static event Action<QMailInfo> OnReceiveMail;
		static async Task GetEmail(StreamWriter writer,StreamReader reader, int index)
		{
			if (OldMailIdList == null)
			{
				OldMailIdList = PlayerPrefs.GetString(nameof(QMailInfo) + "." + nameof(OldMailIdList),new List<string>().ToQData()).ParseQData<List<string>>();
			}
		
			await writer.WriteLineAsync("RETR " + index);
			if(!await reader.CheckReadLine((infos)=> {
				Debug.Log("读取第 " + index + " 封邮件 大小："+int.Parse(infos[1]).ToSizeString());
			}
			))
			{
				Debug.LogError("读取第 " + index + " 封邮件出错");
				return;
			}
			var newMail = new QMailInfo(await reader.ReadAllAsync());
			Debug.Log("新邮件 " +newMail);
			
			PlayerPrefs.SetString(nameof(QMailInfo) + "." + nameof(OldMailIdList), OldMailIdList.ToQData());
			try
			{
				OnReceiveMail?.Invoke(newMail);
			}
			catch (Exception e)
			{
				Debug.LogError("接收邮件出错：" + e);
			}

		}
		static async Task<string> ReadAllAsync(this StreamReader reader)
		{
			var builder = Tool.StringBuilderPool.Get();
			builder.Clear();
			string result = null;
			while ((result = await reader.ReadLineAsync()) != ".")
			{
				builder.Append(result + "\n");
			}
			return builder.ToString();
		}
		public static async Task FreshEmails(QMailAccount account)
		{
			TcpClient clientSocket = new TcpClient();
			clientSocket.Connect(account.popServer, 995);
			//建立SSL连接
			SslStream stream = new SslStream(
				clientSocket.GetStream(),
				false,
				(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors errors) => {
					return true;//接收所有的远程SSL链接
				});
			stream.AuthenticateAsClient(account.popServer);
			StreamReader reader = new StreamReader(stream, Encoding.Default, true);
			StreamWriter writer = new StreamWriter(stream);
			writer.AutoFlush = true;
			if(!await reader.CheckReadLine())
			{
				Debug.LogError("连接服务器出错");
				return;
			}	
			await writer.WriteLineAsync("USER "+ account.account);
			if (!await reader.CheckReadLine())
			{
				Debug.LogError("用户名错误");
				return;
			}
			await writer.WriteLineAsync("PASS "+ account.password);
			if (!await reader.CheckReadLine())
			{
				Debug.LogError("密码错误");
				return;
			}
			var mailCount = 0;
			await writer.WriteLineAsync("STAT");
			if (!await reader.CheckReadLine((infos) =>
			{
				mailCount = int.Parse(infos[1]);
				Debug.Log("邮件总数：" + mailCount + " 总大小："+ long.Parse(infos[2]).ToSizeString());
			}))
			{
				Debug.LogError("获取邮件统计信息出错");
				return;
			}
			await writer.WriteLineAsync("LIST");
			if (!await reader.CheckReadLine())
			{
				Debug.LogError("获取邮件列表出错");
				return;
			}
			Debug.LogError(await reader.ReadAllAsync());
			//if (!newflie)
			//{
			//	return;
			//}
			//for (int i =0; i < mailCount; i++)
			//{
			//	await GetEmail(writer, reader, i+1);
			//}
			Debug.Log("接收邮件完成");
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
		public QMailInfo(string mailStr)
		{
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
