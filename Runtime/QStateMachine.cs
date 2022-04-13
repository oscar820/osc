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
        private QState this[string key]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(key)) return null;
                return stateList[key];
            }
        }
        public QState Add(string commandKey)
        {
            return Add(new QState(commandKey));
        }
        public QState Add(QState state)
        {
            if (stateList.Count == 0)
            {
                startKey = state.Key;
            }
            stateList.Add(state);
            return state;
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
        public void SetValue(object obj)
        {
            value= obj.ToQData(valueType);
        }
    }
    public class QState:IKey<string>
    {
        public static class PortKey
        {
            public const string Next = "#next";
        }

        public string Key { get;  set; } = QId.GetNewId();
        public string name;
        public string commandKey;
        bool hasDelay = false;
        public QStatePort this[string key]
        {
            get
            {
                return Ports[key];
            }
        }
        QCommandInfo command;
        private QState()
        {
        }
        public QState(string commandKey)
        {
            name = commandKey;
            Init(commandKey);
        }
        void Init(string commandKey)
        {
            this.commandKey = commandKey;
            command= QCommand.GetCommand(commandKey);
            if (command == null)
            {
                Debug.LogError("≤ª¥Ê‘⁄√¸¡Ó°æ" + commandKey + "°ø");
            }
            hasDelay = command.method.ReturnType == typeof(IEnumerator);
            Ports[PortKey.Next].portType = PortType.Connect;
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
                return Ports[runtimeNext];
            }
        }
        public void Connect(QState targetState)
        {
            Ports[PortKey.Next].connectState = targetState.Key;
        }
        public QAutoList<string, QStatePort> Ports { get; private set; } = new QAutoList<string, QStatePort>();
        string runtimeNext;
        object[] commandParams;
        public void SetNextPort(string portKey)
        {
            runtimeNext = portKey;
        }
        public IEnumerator Update()
        {
            runtimeNext = PortKey.Next;
            for (int i = 0; i < command.paramInfos.Length; i++)
            {
                var info = command.paramInfos[i];
                commandParams[i] = this[info.Name].GetValue();
            }
            var returnValue= command.Invoke(commandParams);
            if (hasDelay)
            {
                yield return returnValue;
            }
            else
            {
                yield return null;
            }
        }
    }
}