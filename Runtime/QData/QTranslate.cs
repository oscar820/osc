using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using QTool.Inspector;
using System.Threading.Tasks;
using System.Xml.Serialization;
namespace QTool
{
    public class QTranslate : MonoBehaviour
    {
        public static QDataList LanguageData => QDataList.GetResourcesData("LanguageData", (data) => {
            data.SetTitles("Key", "中文", "English");
            data["文本语言"].SetValue("中文", "文本语言").SetValue("English", "Language");
        }); 
        #region 基础数据

        [HideInInspector]
        public string curValue;
        [ViewName("文本")]
        [SerializeField]
        private string value;
        [SerializeField]
        [ViewName("翻译语言")]
        private string language = "";
        [SerializeField]
        [ViewName("翻译结果")]
        [ReadOnly]
        private string translateResult = "";
        public string Language
        {
            get
            {
                return string.IsNullOrWhiteSpace(language) ? globalLanguage : language;
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
        public static string globalLanguage = "中文";
        public static void ChangeGlobalLanguage(string value)
        {
            if (globalLanguage == value)
            {
                OnLanguageChange?.Invoke();
            }
            else
            {
                globalLanguage = value;
                OnLanguageChange?.Invoke();
            }
            Debug.Log("文本语言：" + value);
        }
        static event System.Action OnLanguageChange;
        #endregion
        public StringEvent OnValueChange;
        public StringEvent OnTranslateChange;
        private void Awake()
        {
            OnLanguageChange += ChangeLanguage;
        }
        private void Start()
        {
            CheckFresh();
        }
        private void OnDestroy()
        {
            OnLanguageChange -= ChangeLanguage;
        }
        public void ChangeLanguage()
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                CheckFresh();
            }
        }

        public static string Translate(string value)
        {
            if (string.IsNullOrEmpty(value)) { return value; }
            value = TranslateKey(value);
            var start = value.IndexOf('{');
            var end = value.IndexOf('}');
            while (start >= 0 && end >= 0)
            {
                var key = value.Substring(start + 1, end - start - 1);
                value = value.Replace("{" + key + "}", TranslateKey(key));
                start = value.IndexOf('{');
                end = value.IndexOf('}');
            }
            return value;
        }
        public static QDictionary<string, System.Func<string>> KeyReplace = new QDictionary<string, System.Func<string>>();
        static string TranslateKey(string value)
        {
            if (LanguageData.ContainsKey(value))
            {
                var translate = LanguageData[value].GetValue<string>(globalLanguage); ;
                if (!string.IsNullOrEmpty(translate))
                {
                    return translate;
                }
            }
            else if(KeyReplace.ContainsKey(value))
            {
                return KeyReplace[value]?.Invoke();
            }
            return value;
        }
        [ViewButton("翻译刷新")]
        private void CheckFresh()
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
