using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.Command;
namespace QTool.Flow
{

    public class QFlowGraph
    {
        public QList<string,FlowNode> NodeList { private set; get; } = new QList<string,FlowNode>();
        string startKey; 
        public FlowNode StartNode
        {
            get
            {
                return NodeList[startKey];
            }
        }
        public FlowNode this[string key]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(key)) return null;
                return NodeList[key];
            }
        }
        public FlowPort this[PortId portId]
        {
            get
            {
                return this[portId.node]?[portId.port];
            }
        }
        public void Remove(FlowNode state)
        {
            state.ClearAllConnect();  
            NodeList.Remove(state);
        }
        public FlowNode Add(string commandKey,string name=null)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = commandKey;
            }
            return Add(new FlowNode(commandKey,name));
        }
        public void AddRange(params FlowNode[] nodes)
        {
            foreach (var node in nodes)
            {
                Add(node);
            }
        }
        public FlowNode Add(FlowNode node)
        {
            if (NodeList.Count == 0)
            {
                startKey = node.Key;
            }
            NodeList.Add(node);
            node.Init(this);
            return node;
        }
        public IEnumerator Run(string startKey=null)
        {
            var curState = startKey==null? StartNode:this[startKey];
            while (curState!=null)
            {
                yield return curState.Update();
                curState = curState.RuntimeNext.ConnectPort?.Node;
            }
        }
        public QFlowGraph Init()
        {
            foreach (var state in NodeList)
            {
                state.Init(this);
            }
            return this;
        }
    }
    public enum PortType
    {
        Output,
        Input,
    }
    public struct PortId
    {
        public string node;
        public string port;
        public PortId(FlowPort statePort)
        {
            node = statePort.Node.Key;
            port = statePort.Key;
        }
    }
    public class FlowPort : IKey<string>
    {
        public override string ToString()
        {
            return Key + "[" + valueType + "]";
        }
        public string Key { get; set; }
        public PortType portType = PortType.Input;
        public string value;
        public QList<PortId> ConnectList = new QList<PortId>();
        public bool onlyoneConnect = false;
        [QIgnore]
        public Type valueType;
        [QIgnore]
        public Rect rect;
        [QIgnore]
        public FlowNode Node { get; private set; }
        [QIgnore]
        public FlowPort ConnectPort
        {
            get
            {
                if (ConnectList.Count > 0)
                {
                    var connect = ConnectList.QueuePeek();
                    return Node.Graph[connect];
                }
                else
                {
                    return null;
                }
            }
        }
        public void Init(FlowNode state)
        {
            this.Node = state;
        }
        public bool CanConnect(Type type)
        {
            if (valueType == type)
            {
                return true;
            }
            else if (valueType != null && type != null)
            {
                if (type == typeof(object))
                {
                    return true;
                }
                else if (valueType.IsAssignableFrom(type))
                {
                    return true;
                }
                else if (type.IsAssignableFrom(valueType))
                {
                    return true;
                }
                else if (valueType.Name.Equals(type.Name + "&"))
                {
                    return true;
                }

            }
            return false;
        }
        public bool CanConnect(QCommandInfo info,out string portKey)
        {
            if (valueType == null)
            {
                portKey = QFlowKey.FromPort;
                return true;
            }
            foreach (var paramInfo in info.paramInfos)
            {
                if (paramInfo.IsOut) continue;
                var can= CanConnect(paramInfo.ParameterType);
                if (can)
                {
                    portKey = paramInfo.Name;
                    return true;
                }
            }
            portKey = "";
            return false;
        }
        public bool CanConnect(FlowPort port)
        {
            if (portType == port.portType) return false;
            return CanConnect(port.valueType);
        }
        public void Connect(FlowPort port)
        {
            if (!CanConnect(port)) {
                Debug.LogError("不能将 " + this + " 连接 " + port);
                return;
            }
            if (onlyoneConnect)
            {
                ClearAllConnect();
            }
            if (port != null)
            {
                if (port.onlyoneConnect)
                {
                    port.ClearAllConnect();
                }
                ConnectList.AddCheckExist(new PortId(port));
                port.ConnectList.AddCheckExist(new PortId(this)); 
            }
          
        }
        public void DisConnect(FlowPort port)
        {
            if (port == null) return;
            ConnectList.Remove(new PortId(port));
            port.ConnectList.Remove(new PortId(this));
        }
        public void ClearAllConnect()
        {
            foreach (var connect in ConnectList.ToArray())
            {
                DisConnect(Node.Graph[connect]);
            }
        }
        public object GetValue()
        {
            return value.ParseQData(valueType);
        }
        public void SetValue(object obj)
        {
            value= obj.ToQData(valueType);
        }
    }
    public static class QFlowKey
    {
        public const string FromPort = "#From";
        public const string NextPort = "#Next";
    }
    public class FlowNode:IKey<string>
    {

        [QIgnore]
        public QFlowGraph Graph { private set; get; }
        public Rect rect;
        public string Key { get;  set; } = QId.GetNewId();
        public string name;
        public string commandKey;
        bool hasDelay = false;
        public FlowPort this[string key]
        {
            get
            {
                return Ports[key];
            }
        }
        QCommandInfo command;
        public FlowNode()
        {

        }
        public FlowNode(string commandKey,string name)
        {
            this.name = name;
            this.commandKey = commandKey;
        }
        public FlowPort AddPort(string key,Type type,PortType portType)
        {
            if (!Ports.ContainsKey(key))
            {
                Ports.Set(key, new FlowPort());
            }
            var port = Ports[key];
            port.Key = key;
            port.valueType = type;
            port.portType = portType;
            port.onlyoneConnect = portType == (type == null ? PortType.Output : PortType.Input);
            return port;
        }
        public void Init(QFlowGraph graph)
        {
            this.Graph = graph;
            command= QCommand.GetCommand(commandKey);
            if (command == null)
            {
                Debug.LogError("不存在命令【" + commandKey + "】");
            }
            hasDelay = command.method.ReturnType == typeof(IEnumerator);
            AddPort(QFlowKey.FromPort, null, PortType.Input);
            AddPort(QFlowKey.NextPort, null, PortType.Output);
            commandParams = new object[command.paramInfos.Length];
            foreach (var paramInfo in command.paramInfos)
            {
                var port = AddPort(paramInfo.Name, paramInfo.ParameterType, paramInfo.IsOut?PortType.Output: PortType.Input);
                if (paramInfo.HasDefaultValue)
                {
                    port.SetValue( paramInfo.DefaultValue);
                }
            }
            foreach (var port in Ports)
            {
                port.Init(this);
            }
        }
        public FlowPort RuntimeNext
        {
            get
            {
                return Ports[runtimeNext];
            }
        }
        public void ClearAllConnect()
        {
            foreach (var port in Ports)
            {
                port.ClearAllConnect();
            }
        }
        public void Connect(FlowNode targetState)
        {
            Ports[QFlowKey.NextPort].Connect(targetState[QFlowKey.FromPort]) ;
        }

        public QList<string, FlowPort> Ports { get; private set; } = new QList<string, FlowPort>();
        string runtimeNext;
        object[] commandParams;
        public void SetNextPort(string portKey)
        {
            runtimeNext = portKey;
        }
        public IEnumerator Update()
        {
            runtimeNext = QFlowKey.NextPort;
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