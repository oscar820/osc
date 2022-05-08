using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using QTool.Reflection;
using QTool.Command;
namespace QTool.FlowGraph
{

    public class QFlowGraph
    {
        public override string ToString()
        {
            return this.ToQData();
        }
        public QList<string,QFlowNode> NodeList { private set; get; } = new QList<string,QFlowNode>();
        string startKey; 
        public QFlowNode StartNode
        {
            get
            {
                return NodeList[startKey];
            }
        }
        public QFlowNode this[string key]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(key)) return null;
                return NodeList[key];
            }
        }
        public QFlowPort this[PortId portId]
        {
            get
            {
                return this[portId.node]?.Ports[portId.port];
            }
        }
        public void Remove(QFlowNode node)
        {
            if (node == null) return;
            node.ClearAllConnect();   
            NodeList.Remove(node);
        }
        public QFlowNode Add(string commandKey,string name=null)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = commandKey;
            }
            return Add(new QFlowNode(commandKey,name));
        }
        public void Parse(IList<QFlowNode> nodes,Vector2 startPos)
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
        public void AddRange(IList<QFlowNode> nodes)
        {
            foreach (var node in nodes)
            {
                Add(node);
            }
        }
        public QFlowNode Add(QFlowNode node)
        {
            if (NodeList.Count == 0)
            {
                startKey = node.Key;
            }
            NodeList.Add(node);
            node.Init(this);
            return node;
        }
        public IEnumerator RunCoroutine(string startKey=null)
        {
            var curState = startKey==null? StartNode:this[startKey];
            while (curState!=null)
            {
                yield return curState.RunCoroutine();
                curState = curState.NextNode;
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
        public PortId(QFlowPort statePort)
        {
            node = statePort.Node.Key;
            port = statePort.Key;
        }
    }
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class QNodeOutputAttribute : Attribute
    {
        public bool autoRunNode;
        public QNodeOutputAttribute(bool autoRunNode=false)
        {
            this.autoRunNode = autoRunNode;
        }
    }
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class QFlowPortAttribute : Attribute
    {
        public QFlowPortAttribute()
        {
        }
    }

    public class QFlowPort : IKey<string>
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
        public bool autoRunNode = false;
        [QIgnore]
        public int paramIndex = -1;
        [QIgnore]
        public Type valueType;
        public Rect rect;
        [QIgnore] 
        public QFlowNode Node { get; internal set; }
        public bool HasConnect
        {
            get
            {
                return ConnectList.Count > 0;
            }
        }
        [QIgnore]
        public QFlowPort ConnectPort
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
                        if (ConnectPort.autoRunNode)
                        {
                            ConnectPort.Node.Run();
                        }
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

        public void Init(QFlowNode node)
        {
            this.Node = node;
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
        public bool CanConnect(QFlowPort port)
        {
            if (isOutput == port.isOutput) return false;
            return CanConnect(port.valueType);
        }
        public void Connect(QFlowPort port)
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
        public void DisConnect(PortId connect)
        {
            ConnectList.Remove(connect);
            Node.Graph[connect]?.ConnectList.Remove(new PortId(this));
        }
        public void DisConnect(QFlowPort port)
        {
            ConnectList.Remove(new PortId(port));
            port.ConnectList.Remove(new PortId(this));
        }
        public void ClearAllConnect()
        {
            foreach (var connect in ConnectList.ToArray())
            {
                DisConnect(connect);
            }
        }
      
    }
    
    public static class QFlowKey
    {
        public const string FromPort = "#From";
        public const string NextPort = "#Next";
        public const string ResultPort = "#Result";
        public const string This = "This";
    }
    
    public class QFlowNode:IKey<string>
    {
        public override string ToString()
        {
            return this.ToQData();
        }
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
        public List<QFlowPort> OutParamPorts = new List<QFlowPort>();
        public string Key { get;  set; } = QId.GetNewId();
        public string name;
        public string ViewName { 
            get
            {
                switch (returnType)
                {
                    case ReturnType.CoroutineDelay:
                        return name + " (协程)";
                    case ReturnType.TaskDelayValue:
                    case ReturnType.TaskDelayVoid:
                        return name + " (线程)";
                    default:
                        return name;
                }
            }
        }
        public string commandKey; 
        public Rect rect;
        
        public object this[string key]
        {
            get
            {
                return Ports[key].Value;
            }
            set
            {
                Ports[key].Value = value;
            }
        }
        [QIgnore]
        public QCommandInfo command { get; private set; }
        public QFlowNode()
        {

        }
        public QFlowNode(string commandKey,string name)
        {
            this.name = name;
            this.commandKey = commandKey;
        }
        public QFlowPort AddPort(string key,string name,Type type,bool isOutput=false)
        {
            if (!Ports.ContainsKey(key))
            {
                Ports.Set(key, new QFlowPort());
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
            command = QCommand.GetCommand(commandKey);
            if (command == null)
            {
                foreach (var port in Ports)
                {
                    port.Init(this);
                }
                Debug.LogError("不存在命令【" + commandKey + "】");
                return;
            }

            AddPort(QFlowKey.FromPort, "", null);
            AddPort(QFlowKey.NextPort, "", null, true);
            commandParams = new object[command.paramInfos.Length];
            OutParamPorts.Clear();
            for (int i = 0; i < command.paramInfos.Length; i++)
            {
                var paramInfo = command.paramInfos[i];
                if (paramInfo.Name.Equals(QFlowKey.This)) continue;
                var portAtt = paramInfo.GetAttribute<QNodeOutputAttribute>();
                var port = AddPort(paramInfo.Name, paramInfo.ViewName(), paramInfo.GetAttribute<QFlowPortAttribute>() == null ? paramInfo.ParameterType.GetTrueType() : null, paramInfo.IsOut || portAtt != null);
                port.paramIndex = i;
                port.autoRunNode = portAtt != null && portAtt.autoRunNode;
                if (paramInfo.HasDefaultValue)
                {
                    port.Value = paramInfo.DefaultValue;
                }
                if (port.autoRunNode)
                {
                    Ports.RemoveKey(QFlowKey.FromPort);
                    Ports.RemoveKey(QFlowKey.NextPort);
                }
                if (port.valueType == null)
                {
                    Ports.RemoveKey(QFlowKey.NextPort);
                }
                else if (paramInfo.IsOut || (port.isOutput && Key != QFlowKey.ResultPort && !port.valueType.IsValueType))
                {
                    OutParamPorts.Add(port);
                }

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
                    TaskReturnValueGet = command.method.ReturnType.GetProperty("Result").GetValue;
                    AddPort(QFlowKey.ResultPort, "结果", command.method.ReturnType.GetTrueType(), true);
                }
            }
            else
            {
                AddPort(QFlowKey.ResultPort, "结果", command.method.ReturnType.GetTrueType(), true);
                returnType = ReturnType.ReturnValue;
            }
            Ports.RemoveAll((port) => port.Node == null);
        }
        internal QFlowNode NextNode
        {
            get
            {
                return Ports[_nextFlowPort]?.ConnectPort?.Node;
            }
        }
        public void ClearAllConnect()
        {
            foreach (var port in Ports)
            {
                port.ClearAllConnect();
            }
        }
        public void Connect(QFlowNode targetState)
        {
            Ports[QFlowKey.NextPort].Connect(targetState.Ports[QFlowKey.FromPort]) ;
        }

        public QList<string, QFlowPort> Ports { get; private set; } = new QList<string, QFlowPort>();
        string _nextFlowPort;
        public void SetNetFlowPort(string portKey)
        {
            if (!Ports.ContainsKey(portKey))
            {
                Debug.LogError(ViewName + "不存在端口[" + portKey + "]");
            }
            _nextFlowPort = portKey;
        }
        object[] commandParams;
        static Func<object,object> TaskReturnValueGet;
        object InvokeCommand()
        {
            _nextFlowPort = QFlowKey.NextPort;
            for (int i = 0; i < command.paramInfos.Length; i++)
            {
                var info = command.paramInfos[i];
                if (info.Name == QFlowKey.This)
                {
                    commandParams[i] = this;
                }
                else
                {
                    commandParams[i] = this[info.Name];
                }
            }
            return command.Invoke(commandParams);
        }
        internal void Run()
        {
            var returnObj = InvokeCommand();
            switch (returnType)
            {
                case ReturnType.ReturnValue:
                    Ports[QFlowKey.ResultPort].Value = returnObj;
                    break;
                case ReturnType.CoroutineDelay:
                case ReturnType.TaskDelayVoid:
                case ReturnType.TaskDelayValue:
                    Debug.LogError(commandKey+" 等待逻辑无法自动运行");
                    break;
                default:
                    break;
            }
            foreach (var port in OutParamPorts)
            {
                port.Value = commandParams[port.paramIndex];
            }
        }
        public IEnumerator RunCoroutine()
        {
            var returnObj = InvokeCommand();
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