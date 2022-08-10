using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool;
using System.Text;
using System;
using QTool.Binary;
using System.Reflection;
using QTool.Inspector;
using System.Runtime.Serialization.Formatters.Binary;
using QTool.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using System.Runtime.ExceptionServices;

namespace QTool.Test
{
   
    
    [ScriptToggle(nameof(scriptList))]
    public class QToolTest : MonoBehaviour
    {
        public QIdObject instanceTest;
        public QDictionary<string, string> qDcitionaryTest = new QDictionary<string, string>();
        public static List<string> scriptList=> new List<string> { "QId" };
        public bool toggle;
        public List<string> tansList;
        [ReadOnly]
        [ViewName(name ="索引"  )]
        public int index = 0;
        public int[] b;

        public Vector3 v3 = new Vector3();
        public byte[] info;

        public byte[] testBytes;

        public TTestClass test1;
        public TTestClass test2;
        [ContextMenu("解析类型测试")]
        public void CreateTest()
        {
            var run = Assembly.GetExecutingAssembly();
            Tool.RunTimeCheck("系统创建", () =>
            {
                for (int i = 0; i < 10000; i++)
                {
                    run.CreateInstance(nameof(TTestClass));
                }
            });
            var ar = new object[0];
            Tool.RunTimeCheck("QInstance创建", () =>
            {
                for (int i = 0; i < 10000; i++)
                {
                    Activator.CreateInstance(QReflection.ParseType(nameof(TTestClass)));
                }
            });
           Debug.LogError( (TTestClass)Activator.CreateInstance(QReflection.ParseType(nameof(TTestClass))));

        }
        [ContextMenu("切换语言")]
        public void ChangeLangua()
        {
            QTranslate.ChangeGlobalLanguage(QTranslate.GlobalLanguage == "schinese" ? "english" : "schinese");
			for (int i = 0; i < 10; i++)
			{
				var testData = QTranslate.GetQDataList("测试翻译"+i);
				testData["测试翻译" + i].SetValue(QTranslate.GlobalLanguage, "【翻译结果" + i + "】");
				testData.Save();
			}
			
		}
        public byte[] scenebytes;
		//[ContextMenu("保存场景")]
		//public void SaveAll()
		//{
		//    scenebytes=QId.InstanceIdList.SaveAllInstance();
		//}
		//[ContextMenu("读取场景")]
		//public void LoadAll()
		//{
		//    QId.InstanceIdList.LoadAllInstance(scenebytes);
		//}\
		[ContextMenu("StringReader测试")]
		public void StringReaderTest()
		{
			Task.Run(() =>
			{
				using (var reader = new StringReader(commandStr))
				{
					while (!reader.IsEnd())
					{
						Debug.LogError(reader.ReadCheckString("", " "));
						while (!reader.IsEnd() && reader.NextIs(' ')) ;
					}
				}
			});
		
		}
		[ContextMenu("运算符测试")]
		public void OperarterTest()
		{
			var a = UnityEngine.Random.Range(1,100);
			var b = UnityEngine.Random.Range(1, 100);
			QDebug.Log(a + " + " + b + " = " + a.OperaterAdd(b) + " " + (a + b));
			Vector2 v2A = new Vector2(UnityEngine.Random.Range(1, 100), UnityEngine.Random.Range(1, 100));
			Vector2 v2B = new Vector2(UnityEngine.Random.Range(1, 100), UnityEngine.Random.Range(1, 100));
			QDebug.Log(v2A + " + " + v2B + " = " + v2A.OperaterAdd(v2B) + " " + (v2A + v2B));
		}
		public int testTimes = 1;
        [ContextMenu("序列化测试")]
        public void TestFunc()
        {

			Debug.LogError(test1.ToQData().ToIdString());

			//Tool.RunTimeCheck("Xml写入", () =>
   //         {
   //             for (int i = 0; i < testTimes; i++)
   //             {
   //                 testBytes = QFileManager.QXmlSerialize(test1).GetBytes();
   //         }
   //         },() => testBytes.Length,()=> QFileManager.QXmlSerialize(test1));
   //         Tool.RunTimeCheck("Xml读取", () =>
   //         {
   //             for (int i = 0; i < testTimes; i++)
   //             {
   //                 test2 = QFileManager.QXmlDeserialize<TTestClass>(testBytes.GetString());
   //             }
   //         });
   //         Tool.RunTimeCheck("QSerialize写入", () =>
   //         {
   //             for (int i = 0; i < testTimes; i++)
   //             {
   //                 testBytes = QSerialize.Serialize(test1);
   //         }
   //         }, () => testBytes.Length);
			//Tool.RunTimeCheck("QSerialize读取", () =>
			//{
			//	for (int i = 0; i < testTimes; i++)
			//	{
			//		test2 = QSerialize.Deserialize<TTestClass>(testBytes);
			//	}
			//});
			//Tool.RunTimeCheck("QSerialize读取 有Target", () =>
   //         {
   //             for (int i = 0; i < testTimes; i++)
   //             {
   //                 test2 = QSerialize.Deserialize<TTestClass>(testBytes, test2);
   //             }
   //         });
            Tool.RunTimeCheck("QData写入", () =>
            {
                for (int i = 0; i < testTimes; i++)
                {
                    testBytes = test1.ToQData().GetBytes();
                }
            }, () => testBytes.Length, () => test1.ToQData());
			Tool.RunTimeCheck("QData读取", () =>
			{
				for (int i = 0; i < testTimes; i++)
				{
					test2 = testBytes.GetString().ParseQData<TTestClass>(null, true);
				}
			});
			Tool.RunTimeCheck("QData读取 有Target", () =>
            {
                for (int i = 0; i < testTimes; i++)
                {
					test2 = testBytes.GetString().ParseQData<TTestClass>(test2, true);
                }
            });
            Tool.RunTimeCheck("QData写入 无Name", () =>
            {
                for (int i = 0; i < testTimes; i++)
                {
                    testBytes = test1.ToQData(false).GetBytes();
                }
            }, () => testBytes.Length, () => test1.ToQData(false));

			Tool.RunTimeCheck("QData读取 无Name", () =>
			{
				for (int i = 0; i < testTimes; i++)
				{
					test2 = testBytes.GetString().ParseQData<TTestClass>(null,false);
				}
			});
			Tool.RunTimeCheck("QData读取 无Name 有Target", () =>
            {
                for (int i = 0; i < testTimes; i++)
                {
					test2 = testBytes.GetString().ParseQData<TTestClass>(test2,false);
                }
            });
			//QDebug.Log("QData类型更改反序列化测试：" + "{newKey:asldkj,rect:{position:{x:1,z:2}}".ParseQData<TTestClass>().rect.ToQData());
			QDebug.Log("QData QDictionary序列化测试：" +
				new QDictionary<string, string>{ {"a1","1" },
				 {"a2","2" }
				}.ToQData().ParseQData<Dictionary<string, string>>().ToQData());
			//QDebug.Log("QData Dictionary序列化测试：" + new Dictionary<string, string> { {"asd","askdj" } }.ToQData());
		}
        public string commandStr;
        [ViewButton("命令测试")]
        public void CommandTest()
        { 
            QCommand.Invoke(commandStr);
        }
        [TextArea(5,10)]
        public string QDataStr;

		[ViewButton("时间测试")]
		public void TimeTest()
		{
			QTime.ChangeScale("测试时间", UnityEngine.Random.Range(0, 2));
		}
		[ViewButton("QDataList测试")]
        public async void QDataTest()
		{
			Debug.LogError("\"aslkdasdj,asldjl\"".ParseElement());
			var enumValue = TestEnum.攻击 | TestEnum.死亡;

			Debug.LogError(enumValue.ToQData() + "  :  " + enumValue.ToQData().Trim('\"').ParseQData<TestEnum>());
            var data = new QDataList(QDataStr);
            Debug.LogError(data.ToString());
            data[2].SetValue("3", "2 3");
            data[3][4] = "3\n4";
            data["newLine"][4] = "n 4";
            data["newLine"].SetValue("5", true);
            data["setting"].SetValue( "off\nOn");
            Debug.LogError(data);
            Debug.LogError(data["setting"].GetValue<string>());
            Debug.LogError(test1.ToQData());
            var tobj = test1.ToQData().ParseQData<TTestClass>();
            Debug.LogError(tobj.ToQData());

            Debug.LogError(test1.ToQData(false));
            tobj = test1.ToQData(false).ParseQData<TTestClass>(null,false);
            Debug.LogError(tobj.ToQData(false));

            Debug.LogError((new int[][] {new int[] { 1, 2 },new int[] { 3, 4 } }).ToQData().ParseQData<int[][]>().ToQData());


			Debug.LogError(QDataListTestType.list.ToOneString());
			Debug.LogError(new List<TTestClass>() { new TTestClass { Key = "1" }, new TTestClass { Key = "2" } }.ToQDataList());
		}
		[ViewButton("ToComuteFloatTest")]
		public void ToComuteFloatTest()
		{
			QDebug.Log("1.1"+"  :  "+"1.1".ToComputeFloat());
			QDebug.Log("1.2" + "  :  " + "1.2".ToComputeFloat());
			QDebug.Log("1.25" + "  :  " + "1.25".ToComputeFloat());
			QDebug.Log("" + "  :  " + "".ToComputeFloat());
			QDebug.Log("0.4.18" + "  :  " + "0.4.18".ToComputeFloat());
			QDebug.Log("0.4.20" + "  :  " + "0.4.20".ToComputeFloat());
		}
		[ViewButton("PlayerLoop")]
		public static void PlayerLoop()
		{
			var playerLoop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();

			var sb = new System.Text.StringBuilder();
			sb.AppendLine($"PlayerLoop List");
			foreach (var header in playerLoop.subSystemList)
			{
				sb.AppendFormat("------{0}------", header.type.Name);
				sb.AppendLine();
				foreach (var subSystem in header.subSystemList)
				{
					sb.AppendFormat("{0}", subSystem.type.Name);
					sb.AppendLine();

					if (subSystem.subSystemList != null)
					{
						UnityEngine.Debug.LogWarning("More Subsystem:" + subSystem.subSystemList.Length);
					}
				}
			}

			UnityEngine.Debug.Log(sb.ToString());
		}
		[ViewButton("QTaskTest")]
		public async void QTaskTest()
		{
			//UnityEngine.LowLevel.PlayerLoop.GetDefaultPlayerLoop();
			//await Cysharp.Threading.Tasks.UniTask.Yield(Cysharp.Threading.Tasks.PlayerLoopTiming.Update);
			Debug.LogError(await Resources.LoadAsync<Texture2D>("NodeEditorBackground"));
			Debug.LogError("开始10秒完成");
			if (await QTask.Wait(10).IsCancel())
			{
				Debug.LogError("取消运行");
			}
			else
			{

				Debug.LogError("等待10秒完成");
			}
		
		}
		public class QDataListTestType : QDataList<QDataListTestType>, IKey<string>
		{
			public TestEnum testEnum;
			public string Key { get ; set ; }
			[ViewName("数值")]
			[ViewEnum(nameof(QDataListTestType)+".get_list")]
			public string value="";
			public Vector3 v3;
			public List<int> array;
			public override string ToString()
			{
				return Key + ":[" + value+"]";
			}
		}
	
	}
    [Flags]
    public enum TestEnum
    {
        无 = 0,
        攻击 = 1 << 1,
        防御 = 1 << 2,
        死亡 = 1 << 3,
    }
	[QDynamic]
    [System.Serializable]
    public class TTestClass:IKey<string>
    {
		public string Key { get; set; }
        public Rect rect;
        public TestEnum testEnume = TestEnum.攻击 | TestEnum.死亡;

        public List<float> list;
        public List<List<float>> list2Test = new List<List<float>> { new List<float>() { 1, 2, 3 } };
        [ViewName("名字测试1")]
        public string asdl;
        public float p2;
        public byte[] array = new byte[] { 123 };
        [XmlIgnore] 
        public byte[,,] arrayTest = new byte[1, 2, 2] { { { 1, 2 }, { 3, 4 } } };
        public TestClass2 child; 
        [XmlIgnore]
        public object obj = new Vector3
        {
            x = 1,
            y = 2,
            z = 3
        };
       
    }
    [System.Serializable]
    public class TestClass2 :IQData
    {
        public List<float> list;
        [ViewName("名字测试2")]
        public string asdl;

		public void ParseQData(StringReader reader)
		{
			reader.ReadQData(list);
			reader.ReadQData(asdl);
		}

		public void ToQData(StringWriter writer)
		{
			writer.WriteQData(list);
			writer.WriteQData(asdl);
		}
	}

}
