using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using QTool.Inspector;
using System.Threading.Tasks;
using System.Xml.Serialization;
namespace QTool
{
	public class QTranslateKey:IKey<string>
	{
		public string Key { get; set; }
		public string Name { get; set; }
		public string WebAPI { get; set; }
	}
	public class QTranslate : MonoBehaviour
	{

		public static QList<string, QTranslateKey> TranslateKeys = new QList<string, QTranslateKey>
		{
			new QTranslateKey
			{
				Key="schinese",
				Name="简体中文",
				WebAPI="zh-CN",
			},
			new QTranslateKey
			{
				Key="tchinese",
				Name="繁體中文",
				WebAPI="zh-TW",
			},
			new QTranslateKey
			{
				Key="english",
				Name="English",
				WebAPI="en",
			},
			new QTranslateKey
			{
				Key="japanese",
				Name="日本語",
				WebAPI="ja",
			},
			new QTranslateKey
			{
				Key="koreana",
				Name="한국어",
				WebAPI="ko",
			},
		};
		public static QDataList LanguageData => GetQDataList();
		public static QDataList GetQDataList(string name=null)
		{
			if (string.IsNullOrEmpty(name))
			{
				name = nameof(LanguageData);
			}
			else
			{
				name = nameof(LanguageData) + "/" + name;
			}
			return QDataList.GetResourcesData(name, () => {
				var data = new QDataList();
				List<string> titleList = new List<string>();
				titleList.Add("Key");
				foreach (var translateKey in TranslateKeys)
				{
					titleList.Add(translateKey.Key);
				}
				data.SetTitles(titleList.ToArray());
				if (name == nameof(LanguageData))
				{
					data["测试文本"].SetValue(GlobalLanguage, @"测试文
本123");
					data["测试文本"].SetValue("english", "test123");
				}
				return data;
			});
		}
		#region 基础数据

		[HideInInspector]
        public string curValue;
        [ViewName("文本")]
        [SerializeField]
        private string value;
        [SerializeField]
        [ViewName("固定翻译语言")]
        private string language = "";
        [SerializeField]
        [ViewName("翻译结果")]
        [ReadOnly]
        private string translateResult = "";
        public string Language
        {
            get
            {
                return string.IsNullOrWhiteSpace(language) ? GlobalLanguage : language;
            }
        }
        public string Value
        {
            get
            {
                return value;
            }
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    CheckFresh();
                }

            }
        }
        #endregion
        #region 全局翻译
        public static string GlobalLanguage { get; private set; } = "schinese";
        public static void ChangeGlobalLanguage(string value)
        {
			value = value.ToLower();
			if (!TranslateKeys.ContainsKey(value))
			{
				var obj = TranslateKeys.Get(value, (item) => item.Name);
				if (obj != null)
				{
					value = obj.Key;
				}
				else
				{
					Debug.LogError("不支持语言 [" + value + "]");
					value = "english";
				}
			}
			if (GlobalLanguage == value)
			{
				QEventManager.Trigger(nameof(QTranslate) + "_语言",GlobalLanguage);
            }
            else
            {
                GlobalLanguage = value;
				QEventManager.Trigger(nameof(QTranslate) + "_语言", GlobalLanguage);
            }
            QDebug.Log("文本语言：" + value);
        }
      //  static event System.Action OnLanguageChange;
        #endregion
        public StringEvent OnValueChange;
        public StringEvent OnTranslateChange;
        private void Awake()
        {
			QEventManager.Register<string>(nameof(QTranslate) + "_语言", CheckFresh);
        }
        private void Start()
        {
            CheckFresh();
        }
        private void OnDestroy()
        {
			QEventManager.UnRegister<string>(nameof(QTranslate) + "_语言", CheckFresh);
        }

        public static string Translate(string value,params QKeyValue<string,string>[] keyValues)
        {
            if (string.IsNullOrEmpty(value)) { return value; }
            value = TranslateKey(value);
            var start = value.IndexOf('{');
            var end = value.IndexOf('}');
            while (start >= 0 && end >= 0)
            {
                var key = value.Substring(start + 1, end - start - 1);
				if (keyValues.ContainsKey(key))
				{
					value = value.Replace("{" + key + "}", keyValues.Get(key).Value);
				}
				else
				{
					value = value.Replace("{" + key + "}", TranslateKey(key));
				}
				start = value.IndexOf('{');
				end = value.IndexOf('}');
			}
            return value;
        }
        public static QDictionary<string, System.Func<string>> KeyReplace = new QDictionary<string, System.Func<string>>();
        static string TranslateKey(string value)
        {
            if (LanguageData.ContainsKey(value)&& LanguageData[value].HasValue(GlobalLanguage))
            {
                var translate = LanguageData[value].GetValue<string>(GlobalLanguage);
				return translate;
			}
            else if(KeyReplace.ContainsKey(value))
            {
                return KeyReplace[value]?.Invoke();
            }
            return value;
        }
	
        [ViewButton("翻译刷新")]
        private void CheckFresh(string key=null)
        {
            if (curValue != value)
            {
                curValue = value;
                OnValueChange?.Invoke(value);
            }
            try
            {
                translateResult = Translate(value);
                OnTranslateChange?.Invoke(translateResult);
               
            }
            catch (System.Exception e)
            {
                Debug.LogError("翻译[" + value + "]出错" + e);
            }
        }
       
      

    }
}
