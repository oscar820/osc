using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using QTool.Inspector;
using System.Threading.Tasks;
using QTool.Data;
using System.Xml.Serialization;
namespace QTool
{

    public class LanguageData : QData<LanguageData>
    {
        [XmlElement("翻译")]
        public string translate = "";
    }

    public class TextTranslate : MonoBehaviour
    {
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
        public static async void ChangeGlobalLanguage(string value)
        {
            if (globalLanguage == value)
            {
                OnLanguageChange?.Invoke();
            }
            else
            {
                globalLanguage = value;
                await LanguageData.LoadAsync(value);
                OnLanguageChange?.Invoke();
            }

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
            value = BaseReplace(value);
            var start = value.IndexOf('{');
            var end = value.IndexOf('}');
            while (start >= 0 && end >= 0)
            {
                var key = value.Substring(start + 1, end - start - 1);
                value = value.Replace("{" + key + "}", BaseReplace(key));
                start = value.IndexOf('{');
                end = value.IndexOf('}');
            }
            return value;
        }
        public static QDictionary<string, System.Func<string>> ValueReplace = new QDictionary<string, System.Func<string>>();
        static string BaseReplace(string value)
        {
            if (LanguageData.Contains(globalLanguage, value))
            {
                return LanguageData.Get(globalLanguage, value).translate;
            }
            else
            {
                if (ValueReplace.ContainsKey(value)&& ValueReplace[value]!=null)
                {
                    return ValueReplace[value].Invoke();
                }
                else
                {
                    return value;
                }
            }
        }
        private async void CheckFresh()
        {
            if (curValue != value)
            {
                curValue = value;
                OnValueChange?.Invoke(value);
            }
            try
            {
                await LanguageData.LoadAsync(Language);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
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