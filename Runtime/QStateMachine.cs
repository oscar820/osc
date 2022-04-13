using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.Command;
namespace QTool.StateMachine
{

    public class QStateMachine
    {
        QList<string,QState> stateList = new QList<string,QState>();
         string startKey; 
        public QState StartState
        {
            get
            {
                return stateList[startKey];
            }
        }
        public QState this[string key]
        {
            get
            {
                return stateList[key];
            }
        }
        public void Add(QState state)
        {
            if (stateList.Count == 0)
            {
                startKey = state.Key;
            }
            stateList.Add(state);
        }
        public IEnumerator Run(string startKey=null)
        {
            var curState = startKey==null? StartState:this[startKey];
            while (curState!=null)
            {
                yield return curState.Update();
                curState = this[curState.NextPort.connectState];
            }
        }
    }
    public enum PortType
    {
        Connect,
        Output,
        Input,
    }
    public static class PortKey
    {
        public const string Next = "#next";
    }
    public class QStatePort : IKey<string>
    {
       
        public string Key { get; set; }
        public PortType portType= PortType.Connect;
        public string connectState;
        public string connectPort;
        public string value;
        public Type valueType;
        public object GetValue()
        {
            return value.ParseQData(valueType);
        }
        public void SetValue(object value)
        {
            value= value.ToQData();
        }
    }
    public class QState:IKey<string>
    {
        public string Key { get;  set; } = QId.GetNewId();
        public string commandKey;
        public QStatePort this[string key]
        {
            get
            {
                return Ports[key];
            }
        }
        QCommandInfo command;

        public void Init()
        {
            command= QCommand.KeyDictionary[Key];
            NextPort.portType = PortType.Connect;
            commandParams = new object[command.paramInfos.Length];
            foreach (var paramInfo in command.paramInfos)
            {
                var port= Ports[paramInfo.Name];
                port.portType = PortType.Input;
                port.valueType = paramInfo.ParameterType;
                if (paramInfo.HasDefaultValue)
                {
                    port.SetValue( paramInfo.DefaultValue);
                }
            }
        }
        public QStatePort NextPort
        {
            get
            {
                return Ports[PortKey.Next];
            }
        }
        public void Connect(QState targetState)
        {
            NextPort.connectState = targetState.Key;
        }
        public QAutoList<string, QStatePort> Ports { get; private set; } = new QAutoList<string, QStatePort>();

        object[] commandParams;
        public IEnumerator Update()
        {
            for (int i = 0; i < command.paramInfos.Length; i++)
            {
                var info = command.paramInfos[i];
                commandParams[i] = this[info.Name].GetValue();
            }
            command.Invoke(commandParams);
            yield break;
        }
    }
}