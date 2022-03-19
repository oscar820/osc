using QTool.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace QTool.Command
{
    public static class QCommand 
    {
        static QCommand()
        {
            FreshCommands(typeof(BaseCommands));
        }
        public static bool Invoke(string commandStr)
        {
            if (string.IsNullOrWhiteSpace(commandStr)) return false;
            List<string> commands =new List<string>( commandStr.Split(' '));
            commands.RemoveSpace();
            if (commands.Count > 0)
            {
                var name = commands.Dequeue();
                if (NameDictionary.ContainsKey(name))
                {
                    if(!NameDictionary[name].Invoke(commands))
                    {
                        Debug.LogError("通过[" + commandStr + "]调用命令[" + name + "]出错");
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
        public class QCommandInfo : IKey<string>
        {
            public string Key {  set; get; }
            public string name;
            public string fullName;
            public MethodInfo method;
            public ParameterInfo[] paramInfos;
            public List<string> paramNames;
            public QCommandInfo( MethodInfo method)
            {
                Key = method.DeclaringType.Name + "/" + method.Name;
                name = method.ViewName();
                fullName = method.DeclaringType.ViewName() + '/' + name;
                this.method = method;
                paramInfos = method.GetParameters();
                paramNames = new List<string>();
                foreach (var paramInfo in paramInfos)
                {
                    paramNames.Add(paramInfo.Name);
                }
            }
            public bool Invoke(IList<string> commands)
            {
                var paramObjs = new object[paramInfos.Length];
                for (int i = 0; i <paramInfos.Length; i++)
                {
                    var pInfo = paramInfos[i];
                    if (i < commands.Count)
                    {
                        if(commands[i].TryParseQData(pInfo.ParameterType,out var obj))
                        {
                            paramObjs[i] = obj;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else if (pInfo.HasDefaultValue)
                    {
                        paramObjs[i] = pInfo.DefaultValue;
                    }
                    else
                    {
                        return false;
                    }
                }
                method.Invoke(null, paramObjs);
                return true; ;
            }
            public override string ToString()
            {
                return method.ViewName() + " " + paramInfos.ToOneString(" ");
            }
        }
        public static QList<string, QCommandInfo> KeyDictionary = new QList<string, QCommandInfo>();
        public static QDictionary<string, QCommandInfo> NameDictionary = new QDictionary<string, QCommandInfo>();
        public static List<Type> TypeList = new List<Type>();
        public static void FreshCommands(params Type[] types)
        {
            foreach (var t in TypeList)
            {
                FreshTypeCommands(t);
            }
            foreach (var t in types)
            {
                if (!TypeList.Contains(t))
                {
                    FreshTypeCommands(t);
                }
            }
        }
        static void FreshTypeCommands(Type type)
        {
            type.ForeachFunction((methodInfo) =>
            {
                var typeKey = type.Name; 
                var typeName = type.ViewName();
                if (methodInfo.DeclaringType != typeof(object))
                {
                    var info = new QCommandInfo(methodInfo);
                    KeyDictionary[typeKey + '/' + methodInfo.Name] = info;
                    NameDictionary[methodInfo.ViewName()] = info;

                }
            }, BindingFlags.Public | BindingFlags.Static);
            TypeList.AddCheckExist(type);
        }
        [ViewName("基础")]
        public static class BaseCommands
        {
            [ViewName("日志")]
            public static void Log(object obj)
            {
                Debug.Log(obj);
            }
           
            [ViewName("错误")]
            public static void LogError(object obj)
            {
                Debug.LogError(obj);
            }
            [ViewName("时间日志")]
            public static void Time()
            {
                Debug.Log(DateTime.Now);
            }
        }

    }
}