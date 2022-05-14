using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Serialization;
using System;
using System.Threading.Tasks;
using QTool.Reflection;
using System.Reflection;
using System.IO;

namespace QTool
{
   
    
    public static partial class Tool
    {
        static QDictionary<string, Color> KeyColor = new QDictionary<string, Color>();
        public static Color ToColor(this string key, float s = 0.5f, float v =1f)
        {
            if (string.IsNullOrWhiteSpace(key)) return Color.white;
            var colorKey = key + s + v;
            if (!KeyColor.ContainsKey(colorKey))
            {
                var colorValue = Mathf.Abs(key[0].GetHashCode() %800) + Mathf.Abs(key.GetHashCode() %200f);
                KeyColor[colorKey] = Color.HSVToRGB(colorValue / 1000, s, v);
            }
            return KeyColor[colorKey];
        }
        public static void RunTimeCheck(string name, System.Action action, Func<int> getLength = null, Func<string> getInfo = null)
        {
            var last = System.DateTime.Now;
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError("【" + name + "】运行出错:"+e);
                return;
            }
            var checkInfo = "【" + name + "】运行时间:" + (System.DateTime.Now - last).TotalMilliseconds;
            if (getLength != null)
            {
                checkInfo += " " + " 大小" + getLength().ToSizeString();
            }
            if (getInfo != null)
            {
                checkInfo += "\n" + getInfo();
            }
            Debug.LogError(checkInfo);
        }

        public static bool PercentRandom(float percent)
        {
            var value = UnityEngine.Random.Range(0, 100);
            return value <= percent;
        }
        public static T RandomGet<T>(this IList<T> list)
        {
            return list[UnityEngine.Random.Range(0, list.Count)];
        }
      
        public static IList<T> Random<T>(this IList<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var cur = list[i];
                list.Remove(cur);
                list.Insert(UnityEngine.Random.Range(0, i), cur);
            }
            return list;
        }
        public static async Task<bool> DelayGameTime(float second, bool ignoreTimeScale = false)
        {
            var startTime = (ignoreTimeScale ? Time.unscaledTime : Time.time);
            return await Tool.Wait(() => startTime + second <= (ignoreTimeScale ? Time.unscaledTime : Time.time));
        }
        public static async Task<bool> Wait(Func<bool> flagFunc)
        {
            if (flagFunc == null) return Application.isPlaying;
            while (!flagFunc.Invoke() && Application.isPlaying)
            {
                await Task.Delay(100);
            }
            return Application.isPlaying;
        }
        internal static void ForeachArray(this Array array, int deep, int[] indexArray, Action<int[]> Call,Action start=null,Action end=null,Action mid=null)
        {
            start?.Invoke();
            var length = array.GetLength(deep);
            for (int i = 0; i < length; i++)
            {
                indexArray[deep] = i;
                if (deep + 1 < indexArray.Length)
                {
                    ForeachArray(array, deep + 1, indexArray, Call,start,end,mid);
                }
                else
                {
                    Call?.Invoke(indexArray);
                }
                if (i < length-1)
                {

                    mid?.Invoke();
                }
            
            }
            end?.Invoke();
        }
        public static string ToQData<T>(this T obj,bool hasName=true)
        {
            var type = typeof(T);
            return ToQData(obj, type,hasName);
        }
        
        public static string ToQData(this object obj,Type type, bool hasName = true)
        {
            var typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                case TypeCode.Object:
                    {
                        using (var writer = new StringWriter())
                        {
                            var typeInfo = QSerializeType.Get(type);
                            switch (typeInfo.objType)
                            {
                                case QObjectType.Object:
                                    {
                                        if (obj == null) return "";
                                        
                                        writer.Write('{');
                                        if (type == typeof(object))
                                        {
                                            var runtimeType = obj.GetType();
                                            writer.Write(runtimeType.FullName+"=");
                                            writer.Write(ToQData(obj, runtimeType,hasName));
                                        }else if(typeInfo.IsUnityObject)
                                        {
                                            writer.Write( QObjectReference.GetId(obj as UnityEngine.Object));
                                        }
                                        else if (typeInfo.IsIQData)
                                        {
                                            writer.Write((obj as IQData).ToQData());
                                        }
                                        else
                                        {
                                            if (hasName)
                                            {
                                                for (int i = 0; i < typeInfo.Members.Count; i++)
                                                {
                                                    var memberInfo = typeInfo.Members[i];
                                                    var member = memberInfo.Get(obj);
                                                    writer.Write(memberInfo.Name + "=");
                                                    writer.Write(ToQData(member, memberInfo.Type, hasName));
                                                    if (i < typeInfo.Members.Count - 1)
                                                    {
                                                        writer.Write(';');
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                for (int i = 0; i < typeInfo.Members.Count; i++)
                                                {
                                                    var memberInfo = typeInfo.Members[i];
                                                    var member = memberInfo.Get(obj);
                                                    writer.Write(ToQData(member, memberInfo.Type, hasName));
                                                    if (i < typeInfo.Members.Count - 1)
                                                    {
                                                        writer.Write(';');
                                                    }
                                                }
                                            }
                                         
                                        }
                                        writer.Write('}');

                                        return writer.ToString();
                                    }
                                case QObjectType.List:
                                    {
                                        var list = obj as IList;
                                        if (list == null) return "";
                                        writer.Write('[');
                                        for (int i = 0; i < list.Count; i++)
                                        {
                                            writer.Write(ToQData(list[i], typeInfo.ElementType,hasName));
                                            if (i < list.Count - 1)
                                            {
                                                writer.Write(',');
                                            }
                                        }
                                        writer.Write(']');


                                        return writer.ToString();
                                    }
                                case QObjectType.Array:
                                    {
                                        var array = obj as Array;
                                        if (array == null) return "";
                                        array.ForeachArray(0, typeInfo.IndexArray, (indexArray) =>
                                        {
                                            writer.Write(ToQData(array.GetValue(indexArray), typeInfo.ElementType,hasName) );
                                        },()=>writer.Write('['),()=>writer.Write(']'),()=>writer.Write(','));
                                        return writer.ToString();
                                    }
                                default:
                                    Debug.LogError("不支持类型[" + type + "]");
                                    return "";
                            }
                        }
                    }
                case TypeCode.String:
                    return "\""+ obj+"\"";
                default:
                    return obj?.ToString();
            }
        }
        static bool ArrayParse(string[] strs,List<int> indexArray,List<string> strList,int rank,bool addLength=true)
        {
            if (addLength)
            {
                indexArray.Add(strs.Length);
            }
          
            int i = 0;
            foreach (var str in strs)
            {
                using (var childReader = new StringReader(str))
                {
                    if (rank>1&& childReader.Peek() == '[')
                    {
                      
                        if (childReader.ReadSplit('[', ']', ',', out var childStrs))
                        {
                            if (!ArrayParse(childStrs,indexArray,strList,rank,i==0))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        strList.Add(str);
                    }
                }
                i++;
            }
           
            return true;
        }
        public static object ParseQData(this string qdataStr,Type type, bool hasName=true,object target=null)
        {

            if (string.IsNullOrEmpty(qdataStr))
            {
                return type.IsValueType ? QReflection.CreateInstance(type,target) : null;
            }
            var typeCode = Type.GetTypeCode(type);
            if (type.IsEnum)
            {
                return Enum.Parse(type, qdataStr);
            }
            try
            {
                switch (typeCode)
                {
                    case TypeCode.Object:
                        {
                            using (var reader = new StringReader(qdataStr))
                            {
                                var typeInfo = QSerializeType.Get(type);

                                switch (typeInfo.objType)
                                {
                                    case QObjectType.Object:
                                        {
                                            if (string.IsNullOrEmpty(qdataStr)) return null;

                                            if (reader.ReadSplit('{', '}', ';', out var strs))
                                            {
                                                for (int i = 0; i < strs.Length; i++)
                                                {
                                                    var str = strs[i];
                                                    if (type == typeof(object))
                                                    {
                                                        using (var childReader = new StringReader(str))
                                                        {
                                                            if (childReader.ReadSplit('=', out var name, out var memberStr))
                                                            {
                                                                var runtimeType = QReflection.ParseType(name);
                                                                target = ParseQData(memberStr, runtimeType, hasName);
                                                            }
                                                        }     
                                                    }
                                                    else if (typeInfo.IsUnityObject)
                                                    {
                                                        target = QObjectReference.GetObject(str);

                                                    }
                                                    else if (typeInfo.IsIQData)
                                                    {
                                                        target = (QReflection.CreateInstance(type, target) as IQData).ParseQData(str);
                                                    }
                                                    else
                                                    {
                                                        if (target == null)
                                                        {
                                                            target = QReflection.CreateInstance(type, target);
                                                        }
                                                        if (hasName)
                                                        {
                                                            using (var childReader = new StringReader(str))
                                                            {
                                                                if (childReader.ReadSplit('=', out var name, out var memberStr))
                                                                {
                                                                    var memeberInfo = typeInfo.Members[name];
                                                                    if (memeberInfo != null)
                                                                    {
                                                                        var result = ParseQData(memberStr, memeberInfo.Type, hasName, memeberInfo.Get(target));
                                                                        try
                                                                        {
                                                                            memeberInfo.Set(target, result);
                                                                        }
                                                                        catch (Exception e)
                                                                        {
                                                                            Debug.LogError("读取成员【" + type.Name + "." + memeberInfo.Name + "】出错" + memeberInfo.Type + ":" + result + ":" + memeberInfo.Get(target) + "\n" + e);
                                                                            throw e;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        Debug.LogWarning("不存在成员" + typeInfo.Key + "." + name);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            typeInfo.Members[i].Set.Invoke(target, ParseQData(str, typeInfo.Members[i].Type, hasName, typeInfo.Members[i].Get(target)));
                                                        }

                                                    }
                                                }
                                                return target;
                                            };
                                            Debug.LogError("读取类型出错[" + type + "]:[" + qdataStr + "]");
                                            return null;
                                        }
                                    case QObjectType.List:
                                        {
                                            var list = QReflection.CreateInstance(type, target) as IList;
                                            if (reader.ReadSplit('[', ']', ',', out var strs))
                                            {
                                                for (int i = 0; i < strs.Length; i++)
                                                {
                                                    if (i < list.Count)
                                                    {
                                                        list[i] = ParseQData(strs[i], typeInfo.ElementType, hasName, list[i]);
                                                    }
                                                    else
                                                    {
                                                        list.Add(ParseQData(strs[i], typeInfo.ElementType, hasName));
                                                    }
                                                }


                                                return list;
                                            };
                                            Debug.LogError("读取List出错[" + type + "][" + qdataStr + "]");


                                            return list;
                                        }
                                    case QObjectType.Array:
                                        {

                                            List<int> intArray = new List<int>();
                                            List<string> strArray = new List<string>();
                                            if (reader.ReadSplit('[', ']', ',', out var strs))
                                            {

                                                ArrayParse(strs, intArray, strArray, type.GetArrayRank());
                                                var array = (Array)QReflection.CreateInstance(type, target, intArray.ToObjects());
                                                array.ForeachArray(0, intArray.ToArray(), (indexArray) =>
                                                {

                                                    var obj = ParseQData(strArray.Dequeue(), typeInfo.ElementType, hasName, array.GetValue(indexArray));
                                                    try
                                                    {

                                                        array.SetValue(obj, indexArray);
                                                    }
                                                    catch (Exception)
                                                    {
                                                        Debug.LogError("设置类型出错：【" + obj + "】【" + typeInfo.ElementType + "】");
                                                        throw;
                                                    }

                                                });

                                                return array;
                                            }
                                            Debug.LogError("读取数组出错");
                                            return null;
                                        }
                                    default:
                                        Debug.LogError("不支持类型[" + type + "]");
                                        return null;
                                }
                            }
                        }
                    case TypeCode.Boolean:
                        return bool.Parse(qdataStr);
                    case TypeCode.Byte:
                        return byte.Parse(qdataStr);
                    case TypeCode.Char:
                        return char.Parse(qdataStr);
                    case TypeCode.DateTime:
                        return DateTime.Parse(qdataStr);
                    case TypeCode.DBNull:
                        return null;
                    case TypeCode.Decimal:
                        return decimal.Parse(qdataStr);
                    case TypeCode.Double:
                        return double.Parse(qdataStr);
                    case TypeCode.Empty:
                        return null;
                    case TypeCode.Int16:
                        return short.Parse(qdataStr);
                    case TypeCode.Int32:
                        return int.Parse(qdataStr);
                    case TypeCode.Int64:
                        return long.Parse(qdataStr);
                    case TypeCode.SByte:
                        return sbyte.Parse(qdataStr);
                    case TypeCode.Single:
                        return float.Parse(qdataStr);
                    case TypeCode.String:
                        if (qdataStr.StartsWith("\"") && qdataStr.EndsWith("\""))
                        {
                            return qdataStr.Substring(1, qdataStr.Length - 2);
                        }
                        return qdataStr;
                    case TypeCode.UInt16:
                        return ushort.Parse(qdataStr);
                    case TypeCode.UInt32:
                        return uint.Parse(qdataStr);
                    case TypeCode.UInt64:
                        return ulong.Parse(qdataStr);
                    default:
                        Debug.LogError("不支持类型[" + typeCode + "]");
                        return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("解析类型【"+type+"】出错：" + qdataStr+"\n");
                return type.IsValueType ? QReflection.CreateInstance(type, target) : null;
            }
      

        }
        public static T ParseQData<T>(this string qdataStr,bool hasName=true,T target=default)
        {
            return (T)ParseQData(qdataStr, typeof(T),hasName,target);
        }
        public static bool NextIs(this StringReader reader, char value)
        {
            if (reader.Peek() == value)
            {
                reader.Read();
                return true;
            }
            return false;
        }
        public static void NextIgnore(this StringReader reader, char value)
        {
            if (reader.Peek() == value)
            {
                reader.Read();
            }
        }
        public static bool IsEnd(this StringReader reader)
        {
            return reader.Peek() < 0;
        }
        public static bool ReadSplit(this StringReader reader, char split, out string start,out string end)
        {
            using (var writer = new StringWriter())
            {
                while (!reader.NextIs(split) && !reader.IsEnd())
                {
                    var c = (char)reader.Read();
                    writer.Write(c);
                }
                start = writer.ToString();
                end = reader.ReadToEnd();
                return true;
            }
        }
        static List<string> splitStrList = new List<string>();
        public static bool ReadSplit(this StringReader reader, char start, char end, char split, out string[] strs)
        {
            splitStrList.Clear();
            if (reader.NextIs(start))
            {
                var writer = new StringWriter();
                int child = 0;
                while (!reader.IsEnd())
                {
                    var peek = reader.Peek();
                    if (peek == end)
                    {
                        if (child > 0)
                        {
                            child--;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (child == 0 && peek == split)
                    {

                        splitStrList.Add(writer.ToString());
                        writer.Dispose();
                        writer = new StringWriter();

                        reader.Read();
                    }
                    else
                    {
                        var c = (char)reader.Read();
                        writer.Write(c);
                        if (c == start)
                        {
                            child++;
                        }
                    }


                }
                var str = writer.ToString();
                if (!(splitStrList.Count == 0 && string.IsNullOrEmpty(str)))
                {
                    splitStrList.Add(str);
                }
                writer.Dispose();
                strs = splitStrList.ToArray();
                return true;
            }
            else
            {
                strs = null;
                return false;
            }
        }
        /// <summary>
        /// 获取异或校验值
        /// </summary>
        public static byte ToCheckFlag(this byte[] bytes, byte flag = 0)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                flag ^= bytes[i];
            }
            return flag;
        }
    }
    public class SecondsAverageList
    {
        public float Value
        {
            get; private set;
        }
        QDictionary<double, float> list = new QDictionary<double, float>();
        public float AllSum { private set; get; }
        
        double _lastSumTime=-1;
        float _secondeSum = 0;
        public double StartTime { private set; get; } = -1;
        public double EndTime { private set; get; }
        public float SecondeSum
        {
            get
            {
                if (EndTime == 0) return 0;
                if (EndTime == _lastSumTime)
                {
                    return _secondeSum;
                }
                _lastSumTime = EndTime;
                _secondeSum = 0f;
                foreach (var kv in list)
                {
                    _secondeSum += kv.Value;
                }
                return _secondeSum ;
            }
        }
        int maxCount = -1;
        static double CurTime
        {
            get
            {
                return (DateTime.Now - new DateTime()).TotalSeconds;
            }
        }
        public void Push(float value)
        {
            if (StartTime<0)
            {
                StartTime = CurTime;
            }
            AllSum += value;
            list.RemoveAll((kv) => (CurTime - kv.Key) > 1);
            list[CurTime] = value;
            EndTime = CurTime;
            Value = SecondeSum/list.Count;
        }
        public void Clear()
        {
            list.Clear();
            StartTime = -1;
            _lastSumTime = -1;
            _secondeSum = 0;
        }
        public override string ToString()
        {
            return "总记[" + AllSum + "]平均[" + Value + "/s]";
        }
        public  string ToString(Func<float,string> toString)
        {
            return "总记[" + toString(AllSum) + "]平均[" + toString(Value) + "/s]";
        }
    }


    public interface IQData
    {
        string ToQData();
        object ParseQData(string qdataStr);
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Interface)]
    public class QIgnoreAttribute : Attribute
    {

    }


    public enum QObjectType
    {
        Object,
        List,
        Array,
    }
    public class QSerializeType : QTypeInfo<QSerializeType>
    {
        public static QDictionary<Type, List<string>> TypeMembers = new QDictionary<Type, List<string>>()
        {
            new QKeyValue<Type, List<string>>
            {
                 Key=typeof(Rect),
                 Value=new List<string>
                 {
                     "position",
                     "size",
                 }
            }
        };
        static bool IsQSValue(MemberInfo info)
        {
            if (info.GetCustomAttribute<QIgnoreAttribute>() != null)
            {
                return false;
            }
            return true;
        }
        public QObjectType objType = QObjectType.Object;
        public bool IsIQSerialize { private set; get; }
        public bool IsIQData { private set; get; }
        public bool IsUnityObject { private set; get; }
        protected override void Init(Type type)
        {  
            Functions = null;
            base.Init(type);
            if (Code == TypeCode.Object)
            {
                IsIQSerialize = typeof(Binary.IQSerialize).IsAssignableFrom(type);
                IsIQData = typeof(IQData).IsAssignableFrom(type);
                IsUnityObject = typeof(UnityEngine.Object).IsAssignableFrom(type);
                if (IsIQSerialize || IsIQData)
                {
                    objType = QObjectType.Object;
                    return;
                }
                if (IsArray)
                {
                    objType = QObjectType.Array;
                }
                else if (IsList)
                {
                    objType = QObjectType.List;
                }
                else
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        Debug.LogError("不支持序列化【" + type + "】Nullable类型");
                    }
                    Members.RemoveAll((info) =>
                    {
                        if (TypeMembers.ContainsKey(type))
                        {
                            return !TypeMembers[type].Contains(info.Key);
                        }
                        return !IsQSValue(info.MemeberInfo) || info.Key == "Item" || info.Set == null || info.Get == null;
                    });
                }
            }
        }

    }

}