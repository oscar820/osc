using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.Command;
using System.Threading.Tasks;
using QTool.Reflection;

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
        public void Parse(IList<FlowNode> nodes,Vector2 startPos)
        {
            var lastKeys = new List<string>();
            var keys = new List<string>();
            var offsetPos = Vector2.one * float.MaxValue;
            foreach (var node in nodes)
            {
                offsetPos = new Vector2(Mathf.Min(offsetPos.x, node.rect.x), Mathf.Min(offsetPos.y, node.rect.y));
                lastKeys.Add(node.Key);
                node.Key = QId.GetNewId();
                keys.Add(node.Key);
            }

            foreach (var node in nodes)
            {
                node.rect.position = node.rect.position - offsetPos + startPos;
                foreach (var port in node.Ports)
                {
                    var lastConnect = port.ConnectList.ToArray();
                    port.ConnectList.Clear();
                    foreach (var connect in lastConnect)
                    {
                        var keyIndex = lastKeys.IndexOf(connect.node);
                        if (keyIndex >= 0)
                        {
                            port.ConnectList.Add(new PortId
                            {
                                node = keys[keyIndex],
                                port = connect.port,
                            });
                        }
                    }
                }
            }
            AddRange(nodes);
        }
        public void AddRange(IList<FlowNode> nodes)
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
        public string name;
        public bool isOutput = false;
        public string stringValue;
        public QList<PortId> ConnectList = new QList<PortId>();
        public bool onlyoneConnect = false;
        [QIgnore]
        public int paramIndex = -1;
        [QIgnore]
        public Type valueType;
        [QIgnore]
        public Rect rect;
        [QIgnore]
        public FlowNode Node { get; internal set; }
        public bool HasConnect
        {
            get
            {
                return ConnectList.Count > 0;
            }
        }
        [QIgnore]
        public FlowPort ConnectPort
        {
            get
            {
                if (HasConnect)
                {
                    var connect = ConnectList.QueuePeek();
                    return Node?.Graph[connect];
                }
                else
                {
                    return null;
                }
            }
        }
        object _value;
        [QIgnore]
        public object Value
        {
            get
            {
                if (isOutput)
                {
                    return _value;
                }
                else
                {
                    if (HasConnect)
                    {
                        return ConnectPort.Value;
                    }
                    else
                    {
                        return stringValue.ParseQData(valueType);
                    }
                }

            }
            set
            {
                if (isOutput)
                {
                    _value = value;
                }
                else
                {
                    if (HasConnect)
                    {
                        _value = value;
                    }
                    else
                    {
                        stringValue = value.ToQData(valueType);
                    }
                }

            }
        }

        public void Init(FlowNode state)
        {
            this.Node = state;
        }
        public static QDictionary<Type, List<Type>> CanConnectList = new QDictionary<Type, List<Type>>()
        {
            new QKeyValue<Type, List<Type>>
            {
                 Key= typeof(int),
                 Value=new List<Type>{typeof(float),typeof(double)}
            }
        };
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
                }else if(CanConnectList.ContainsKey(valueType))
                {
                    return CanConnectList[valueType].Contains(type);
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
            if (isOutput == port.isOutput) return false;
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
      
    }
    public static class QFlowKey
    {
        public const string FromPort = "#From";
        public const string NextPort = "#Next";
        public const string ResultPort = "#Result";
    }
    
    public class FlowNode:IKey<string>
    {
        [System.Flags]
        public enum ReturnType
        {
            Void,
            ReturnValue,
            CoroutineDelay,
            TaskDelayVoid,
            TaskDelayValue
        }

        [QIgnore]
        public QFlowGraph Graph { private set; get; }
        [QIgnore]
        public ReturnType returnType { private set; get; }= ReturnType.Void;
        [QIgnore]
        public List<FlowPort> OutParamPorts = new List<FlowPort>();
        public string Key { get;  set; } = QId.GetNewId();
        public string name;
        public string commandKey; 
        public Rect rect;
        
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
        public FlowPort AddPort(string key,string name,Type type,bool isOutput=false)
        {
            if (!Ports.ContainsKey(key))
            {
                Ports.Set(key, new FlowPort());
            }
            var port = Ports[key];
            port.Key = key;
            port.name = name;
            port.valueType = type;
            port.isOutput = isOutput;
            port.onlyoneConnect =  (type == null)== isOutput ;
            port.Init(this);
            return port;
        }
        public void Init(QFlowGraph graph)
        {
            this.Graph = graph;
            command= QCommand.GetCommand(commandKey);
            if (command == null)
            {
                Debug.LogError("不存在命令【" + commandKey + "】");
                return;
            }
            if (command.method.ReturnType == typeof(void))
            {
                returnType = ReturnType.Void;
            }
            else if (command.method.ReturnType == typeof(IEnumerator))
            {
                returnType = ReturnType.CoroutineDelay;
            }
            else if (typeof(Task).IsAssignableFrom(command.method.ReturnType)) 
            {
                if (typeof(Task) == command.method.ReturnType)
                {
                    returnType = ReturnType.TaskDelayVoid;
                }
                else
                {
                    returnType = ReturnType.TaskDelayValue;
                    TaskReturnValueGet= command.method.ReturnType.GetProperty("Result").GetValue;
                    AddPort(QFlowKey.ResultPort,"result", null, true);
                }
            }
            else
            {
                AddPort(QFlowKey.ResultPort,"result", null, true);
                returnType = ReturnType.ReturnValue;
            } 
            AddPort(QFlowKey.FromPort,"", null);
            AddPort(QFlowKey.NextPort,"", null,true);
            commandParams = new object[command.paramInfos.Length];
            OutParamPorts.Clear();
            for (int i = 0; i < command.paramInfos.Length; i++)
            {
                var paramInfo = command.paramInfos[i];
                var port = AddPort(paramInfo.Name, paramInfo.ViewName(), paramInfo.ParameterType.GetTrueType(), paramInfo.IsOut);
                port.paramIndex = i;
                if (paramInfo.HasDefaultValue)
                {
                    port.Value = paramInfo.DefaultValue;
                }
                if (paramInfo.IsOut)
                {
                    OutParamPorts.Add(port);
                }
            }
            Ports.RemoveAll((port) => port.Node == null);
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
        static Func<object,object> TaskReturnValueGet;
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
                commandParams[i] = this[info.Name].Value;
            }
            var returnObj= command.Invoke(commandParams);
            switch (returnType)
            {
                case ReturnType.ReturnValue:
                    Ports[QFlowKey.ResultPort].Value= returnObj;
                    break;
                case ReturnType.CoroutineDelay:
                    yield return returnObj;
                    break;
                case ReturnType.TaskDelayVoid:
                case ReturnType.TaskDelayValue:
                    var task= returnObj as Task;
                    while (!task.IsCompleted)
                    {
                        yield return null;
                    }
                    if (returnType== ReturnType.TaskDelayValue)
                    {
                        Ports[QFlowKey.ResultPort].Value= TaskReturnValueGet(returnObj);
                    }
                    break;
                default:
                    break;
            }
            foreach (var port in OutParamPorts)
            {
                port.Value = commandParams[port.paramIndex];
            }
        }

    }
}