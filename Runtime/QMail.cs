using System.Collections;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;
using UnityEngine;
namespace QTool
{

    public static class MailTool
    {
        public static async Task Send(string fromAddress, string password, string disPlayName, string title, string messageInfo, string toAddres, params string[] files)
        {
            SmtpClient client = null;
            if (fromAddress.Contains("@"))
            {
                client = new SmtpClient("smtp." + fromAddress.Substring(fromAddress.IndexOf("@") + 1));
            }
            if (client == null)
            {
                Debug.LogError("不支持的邮箱:" + fromAddress);
                return;
            }
            client.Credentials = new System.Net.NetworkCredential(fromAddress, password);
            client.EnableSsl = true;
            await Send(client, fromAddress, disPlayName, title, messageInfo, toAddres, files);
        }
        private static async Task Send(SmtpClient stmpClient, string fromAddress, string disPlayName, string title, string messageInfo, string toAddres,params string[] files)
        {
            var message = new MailMessage();
            message.From = new MailAddress(fromAddress, disPlayName);
            message.To.Add(toAddres);
            message.IsBodyHtml = true;
            message.BodyEncoding = System.Text.Encoding.UTF8;
            message.Subject = title;
            message.Body = messageInfo;
            foreach (var filePath in files)
            {
                message.Attachments.Add(new Attachment(filePath));
            }
            //for (int i = 0; i < files.Length; i++)
            //{
            //    message.Attachments.Add(new Attachment(files[i], "附件" + i));
            //}
            await Send(stmpClient, message);
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
                Debug.Log("发送邮件成功");
            }
        }
    }
}