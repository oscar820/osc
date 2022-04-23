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

namespace QTool.Test
{
    [Flags]
    public enum TestEnum
    {
        无 = 0,
        攻击 = 1 << 1,
        防御 = 1 << 2,
        死亡 = 1 << 3,
    }
    [System.Serializable]
    public struct V2
    {
        public float x;
        public float y;
        public static bool operator ==(V2 a, Vector2 b)
        {
            return a.x == b.x && a.y == b.y;
        }
        public static bool operator !=(V2 a, Vector2 b)
        {
            return a.x != b.x || a.y != b.y;
        }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public V2(Vector2 vector2)
        {
            x = vector2.x;
            y = vector2.y;
        }
      //  [JsonIgnore]
        public Vector2 Vector2
        {
            get
            {
                return new Vector2(x, y);
            }
        }
    }
    [System.Serializable]
    public class NetInput : PoolObject<NetInput>
    {
        public bool NetStay = false;
        public V2 NetVector2;

        public override void OnPoolRecover()
        {
        }

        public override void OnPoolReset()
        {
        }
    }

    //[QType()]
    [System.Serializable]
    public class TTestClass//:IQSerialize
    {
        public TestEnum testEnume = TestEnum.攻击 | TestEnum.死亡;

        public List<float> list;
        public List<List<float>> list2Test = new List<List<float>> { new List<float>() { 1, 2, 3 } };
        [ViewName("名字测试1")]
        public string asdl;
        public float p2;
        public byte[] array = new byte[] { 123 };
        [XmlIgnore]
        public byte[,,] arrayTest = new byte[1,2,2] { { { 1, 2 }, { 3, 4 } } };
        public TestClass2 child;
        public void Read(QBinaryReader read)
        {
            list = read.ReadObject(list); 
            p2 = read.ReadSingle();
            // list = read.ReadObject<List<float>>();
            asdl = read.ReadString();
        }

        public void Write(QBinaryWriter write)
        {
            write.WriteObject(list);
            write.Write(p2);
            write.Write(asdl);
        }
        public TTestClass()
        {

        }
        public TTestClass(int a)
        {

        }
    }

    //[QType()]
    [System.Serializable]
    public class TestClass2//:IQSerialize
    {
        public List<float> list;
        [ViewName("名字测试2")]
        public string asdl;
        public float p1;

        public void Read(QBinaryReader read)
        {
            list = read.ReadObject(list);
            p1 = read.ReadSingle();
            // list = read.ReadObject<List<float>>();
            asdl = read.ReadString();
        }

        public void Write(QBinaryWriter write)
        {
            write.WriteObject(list);
            write.Write(p1);
            write.Write(asdl);
        }
    }
    public interface ITest
    {

    }
    public class T1 : ITest
    {
        public string a;
    }
    [ScriptToggle("scriptList")]
    public class QToolTest : MonoBehaviour
    {
        public InstanceReference instanceTest;
        public QDictionary<string, string> qDcitionaryTest = new QDictionary<string, string>();
        public static List<string> scriptList=> new List<string> { "QId" };
        //[ViewToggle("开关")]
        public bool toggle;
        // Start is called before the first frame update
        void Start()
        {
            for (int i = 0; i < 3; i++)
            {

                Debug.LogError("start "+i);
            }
            //qDcitionaryTest["123"] = "123";
            //qDcitionaryTest["456"] = "456";
            //var a= qDcitionaryTest["789"];
            //qDcitionaryTest["456"] = "789";
            //var writer = new BinaryWriter().Write(new Vector3(9,8,7)).Write(v3);
            //var reader = new BinaryReader().Reset(writer.ToArray());
            // Debug.LogError(reader.ReadVector3()+":"+ reader.ReadVector3());
        }
        public List<string> tansList;
        // Update is called once per frame
        void Update()
        {
            Debug.Log("1");
        }
        [ReadOnly]
        [ViewName(name ="索引"  )]
        public int index = 0;

        public void AsyncTest()
        {

        }
        public class QTestTypeInfo : QTypeInfo<QTestTypeInfo>
        {

        }
        // public TestClass a = new TestClass { };
        // public Dictionary<string, float> aDic = new Dictionary<string, float>();
        // public Dictionary<string, float> bDic = new Dictionary<string, float>();
        public int[] b;

        public Vector3 v3 = new Vector3();
        public byte[] info;

        [ViewButton("ScreenSize",control = "togle")]
        public void SetSize()
        {
            QScreen.SetResolution(920, 630, false);
        }
        [ContextMenu("Name")]
        public void FullName()
        {
            UnityEngine.Debug.LogError(typeof(TTestClass).Name);
        }

        public byte[] testBytes;

      //   [HorizontalGroup("t1", "toggle")]
        public TTestClass test1;
     //   [HorizontalGroup("t1", "toggle")]
        public TTestClass test2;
        public TTestClass creatObj;
        NetInput last;
        public string email;
        public string emailPassword;
        public string testInfo;
        public string toAddress;
        [ContextMenu("测试邮件")]
        public void EmailTest()
        {
            FileManager.Save("test.txt", testInfo);
            MailTool.Send(email, emailPassword, "测试用户", "测试邮件", testInfo, toAddress,"test.txt");
        }
        [ContextMenu("对象池测试")]
        public void PoolTest()
        {
            last = NetInput.Get();
            last = NetInput.Get();
            last.Recover();
        }
        [ContextMenu("解析类型测试")]
        public void CreateTest()
        {
            UnityEngine.Debug.LogError(QTestTypeInfo.Get(typeof(List<string>)));
            var run = Assembly.GetExecutingAssembly();
            Tool.RunTimeCheck("系统创建", () =>
            {
                for (int i = 0; i < 10000; i++)
                {
                    run.CreateInstance("TestClass");
                }
            });
            var ar = new object[0];
            Tool.RunTimeCheck("QInstance创建", () =>
            {
                for (int i = 0; i < 10000; i++)
                {
                    Activator.CreateInstance(QReflection.ParseType("TestClass"));
                }
            });
            creatObj = (TTestClass)Activator.CreateInstance(QReflection.ParseType("TestClass"));
            UnityEngine.Debug.LogError(creatObj);
        }
        public void GetTable(string startStr, Func<double, double> sinFunc)
        {
            var str = startStr;
            for (double i = -180; i <= 180; i++)
            {
                var value = sinFunc(2 * Math.PI * i / 360);
                str +=value.ToString("f4") + " , ";
            }
            Debug.LogError(str);
        }
        public void SinTest(string startStr,Func<float, float> sinFunc,Func<float, float> asinFunc)
        {
            var str = startStr;
            for (double i = -180; i <= 180; i++)
            {
                var value = sinFunc((float)(2 * Math.PI * i / 360));
               // asinFunc(value).ToString("f4");
                str +=asinFunc(value).ToString("f4")+":"+  value.ToString("f4") + " , ";
            }
            Debug.LogError(str);
        }
        public byte[] scenebytes;
        [ContextMenu("保存场景")]
        public void SaveAll()
        {
            scenebytes=QId.InstanceIdList.SaveAllInstance();
        }
        [ContextMenu("读取场景")]
        public void LoadAll()
        {
            QId.InstanceIdList.LoadAllInstance(scenebytes);
        }
        [ContextMenu("输出三角函数值")]
        public void SinTabFunc()
        {
           // Task.Run(() =>
           // {
                //GetTable("SinTable:", Math.Sin);
                //GetTable("CosTable:", Math.Cos);
                //GetTable("TanTable:", Math.Tan);
                //SinTest("Sin:", Math.Sin, Math.Asin);
                //SinTest("Cos:", Math.Cos, Math.Acos);
                //SinTest("Tan:", Math.Tan, Math.Atan);
                //SinTest("FixedSin:", (a) => Fix64.Sin(a).ToFloat(), (a) => Fix64.Asin(a).ToFloat());
                //SinTest("FixedCos:", (a) => Fix64.Cos(a).ToFloat(), (a) => Fix64.Acos(a).ToFloat());
                //SinTest("FixedTan:", (a) => Fix64.Tan(a).ToFloat(), (a) => Fix64.Atan(a).ToFloat());
           // });
        }
        [ContextMenu("序列化测试")]
        public void TestFunc()
        {
            Tool.RunTimeCheck("Xml写入", () =>
            {
                for (int i = 0; i < 4000; i++)
                {
                    testBytes = FileManager.XmlSerialize(test1).GetBytes();
            }
            }, () => testBytes.Length);
            Tool.RunTimeCheck("Xml读取", () =>
            {
                for (int i = 0; i < 4000; i++)
                {
                    test2 = FileManager.XmlDeserialize<TTestClass>(testBytes.GetString());
                }
            });
            Tool.RunTimeCheck("QSerialize写入", () =>
            {
                for (int i = 0; i < 4000; i++)
                {
                    testBytes = QSerialize.Serialize(test1);
            }
            }, () => testBytes.Length);
            Tool.RunTimeCheck("QSerialize读取", () =>
            {
                for (int i = 0; i < 4000; i++)
                {
                    test2 = QSerialize.Deserialize<TTestClass>(testBytes);
                }
            });
            Tool.RunTimeCheck("QData写入", () =>
            {
                for (int i = 0; i < 4000; i++)
                {
                    testBytes = test1.ToQData().GetBytes();
                }
            }, () => testBytes.Length);
            Tool.RunTimeCheck("QData读取", () =>
            {
                for (int i = 0; i < 4000; i++)
                {
                    test2 = testBytes.GetString().ParseQData<TTestClass>();
                }
            });
            Tool.RunTimeCheck("QData写入无Name", () =>
            {
                for (int i = 0; i < 4000; i++)
                {
                    testBytes = test1.ToQData(false).GetBytes();
                }
            }, () => testBytes.Length);
            Tool.RunTimeCheck("QData读取Name", () =>
            {
                for (int i = 0; i < 4000; i++)
                {
                    test2 = testBytes.GetString().ParseQData<TTestClass>(false);
                }
            });
        }
        public string commandStr;
        [ViewButton("命令测试")]
        public void CommandTest()
        {
            QTool.Command.QCommand.Invoke(commandStr);
        }
        [TextArea(5,10)]
        public string QDataStr;
        [ViewButton("QData测试")]
        public void QDataTest()
        {
            var data = new QDataList(QDataStr);
            data[2]["3"] = "2 3";
            data[3][4] = "3 4";
            data["newLine"][4] = "n 4";
            data["setting"].Value = "off";
            Debug.LogError(data);
            Debug.LogError(test1.ToQData());
            var tobj = test1.ToQData().ParseQData<TTestClass>();
            Debug.LogError(tobj.ToQData());

            Debug.LogError(test1.ToQData(false));
            tobj = test1.ToQData(false).ParseQData<TTestClass>(false);
            Debug.LogError(tobj.ToQData(false));

            Debug.LogError((new int[][] {new int[] { 1, 2 },new int[] { 3, 4 } }).ToQData().ParseQData<int[][]>().ToQData());
        }
    }
}