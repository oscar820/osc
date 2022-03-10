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
                        Debug.LogError("通过[" + commandStr + "]调用命令[" + commandStr + "]出错");
                        return false;
                    }
                }
            }
            return true;
        }
        public class QCommandInfo
        {
            public string name;
            public MethodInfo method;
            public ParameterInfo[] paramInfos;
            public List<string> paramNames;
            public QCommandInfo( MethodInfo method)
            {
                name = method.ViewName();
                this.method = method;
                paramInfos = method.GetParameters();
                paramNames = new List<string>();
                foreach (var paramInfo in paramInfos)
                {
                    paramNames.Add(paramInfo.ViewName());
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
                        if(commands[i].TryParse(pInfo.ParameterType,out var obj))
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
        public static QDictionary<string, QCommandInfo> KeyDictionary = new QDictionary<string, QCommandInfo>();
        public static QDictionary<string, QCommandInfo> NameDictionary = new QDictionary<string, QCommandInfo>();
        public static List<Type> TypeList = new List<Type>();
        public static void FreshCommands(params Type[] types)
        {
            foreach (var t in TypeList)
            {
                FreshCommands(t);
            }
            foreach (var t in types)
            {
                if (!TypeList.Contains(t))
                {
                    FreshCommands(t);
                }
            }
            NameDictionary.Sort((a, b) => {
                return string.Compare(a.Key, b.Key);
            });
        }
        static void FreshCommands(Type type)
        {
            type.ForeachFunction((methodInfo) =>
            {
                var typeName = type.ViewName();
                if (methodInfo.DeclaringType != typeof(object))
                {
                    var info = new QCommandInfo(methodInfo);
                    foreach (var viewName in methodInfo.GetCustomAttributes<ViewNameAttribute>())
                    {
                        NameDictionary[viewName.name] = info;
                        KeyDictionary[typeName + '/' + viewName.name] = info;
                    }
                    NameDictionary[methodInfo.Name] = info;
                    KeyDictionary[typeName + '/' + methodInfo.Name] = info;
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