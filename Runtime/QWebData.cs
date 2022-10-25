using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Xml.Serialization;
using System.Xml;
namespace QTool
{


	public class QWebData : MonoBehaviour
	{
		public string url = "";
		// Start is called before the first frame update
		[QName("运行")]
		private async void Run()
		{
			var data = (await url.RunURLAsync()).SplitEndString("<!DOCTYPE html>\n");
			GUIUtility.systemCopyBuffer = data;
			var xml =new XmlDocument();
			xml.LoadXml(data);
			
			Debug.LogError(xml.ChildNodes.Count);
		}
		public static async Task<string> HttpGet(string url)
		{
			//cookie是在要爬网页F12看到的cookie都复制粘贴到这里就可以
			string cookieStr = " ";

			//创建请求
			HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
			//请求方式
			httpWebRequest.Method = "GET";
			//设置请求超时时间
			httpWebRequest.Timeout = 20000;

			//设置cookie
			httpWebRequest.Headers.Add("Cookie", cookieStr);
			//发送请求
			HttpWebResponse httpWebResponse = (HttpWebResponse)await httpWebRequest.GetResponseAsync();
			//利用Stream流读取返回数据
			StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
			//获得最终数据，一般是json
			string responseContent =await streamReader.ReadToEndAsync();

			streamReader.Close();
			httpWebResponse.Close();

			//结果 返回给你的一般都是json格式的字符串
			return responseContent;
		}
	}

}
