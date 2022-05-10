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
        public Action<IEnumerator> StartCoroutineOverride;

        public QFlowNode this[string key]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(key)) return null;
                return NodeList[key];
            }
        }
        public ConnectInfo GetConnectInfo(PortId? portId)
        {
            if (portId == null) return null;
            return this[portId]?[portId.Value.listKey];
        }
        public QFlowPort this[PortId? portId]
        {
            get
            {
                if (portId == null) return null;
                return this[portId.Value.node]?.Ports[portId.Value.port];
            }
        }
        public void Remove(QFlowNode node)
        {
            if (node == null) return;
            node.ClearAllConnect();   
            NodeList.Remove(node);
        }
        public QFlowNode Add(string commandKey)
        {
            return Add(new QFlowNode(commandKey));
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
                    foreach (var c in port.ConnectInfolist)
                    {
                        var lastConnect = c.ConnectList.ToArray();
                        c.ConnectList.Clear();
                        foreach (var connect in lastConnect)
                        {
                            var keyIndex = lastKeys.IndexOf(connect.node);
                            if (keyIndex >= 0)
                            {
                               c.ConnectList.Add(new PortId
                                {
                                    node = keys[keyIndex],
                                    port = connect.port,
                                });
                            }
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
            NodeList.Add(node);
            node.Init(this);
            return node;
        }
        internal void StartCoroutine(IEnumerator coroutine)
        {
            if (StartCoroutineOverride == null)
            {
                QToolManager.Instance.StartCoroutine(coroutine);
            }
            else
            {
                StartCoroutineOverride(coroutine);
            }
        }
        public void Run(string startNode, Action<IEnumerator> StartCoroutineOverride = null)
        {
            this.StartCoroutineOverride = StartCoroutineOverride;
            StartCoroutine(RunIEnumerator(startNode));
        }
        public IEnumerator RunIEnumerator(string startNode)
        {
            var curNode = this[startNode];
            while (curNode!=null)
            {
                yield return curNode.RunIEnumerator();
                var port= curNode.NextNodePort;
                if (port != null)
                {
                    if (port.Key == QFlowKey.FromPort)
                    {
                        curNode = port.Node;
                    }
                    else
                    {
                        port.Node.TriggerPort(port);
                        curNode = null; ;
                    }
                  
                }
                else
                {
                    curNode = null;
                }
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
        public int listKey ;
        public PortId(QFlowPort statePort,int listKey=0)
        {
            node = statePort.Node.Key;
            port = statePort.Key;
            this.listKey = listKey;
        }
        public override string ToString()
        {
            return listKey==0? port:port+"["+listKey+"]";
        }
    }
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class QOutputPortAttribute : Attribute
    {
        public static QOutputPortAttribute Normal = new QOutputPortAttribute();

        public bool autoRunNode=false;
        public QOutputPortAttribute()
        {
        }
    }
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class QFlowPortAttribute : Attribute
    {
        public static QFlowPortAttribute Normal = new QFlowPortAttribute();

        public bool showValue = false;
        public QFlowPortAttribute()
        {
        }
    }
    public sealed class QFlow
    {
        public static Type Type = typeof(QFlow);

    }
    public class ConnectInfo
    {
        public Rect rect;
        public QList<PortId> ConnectList = new QList<PortId>();
        public void ChangeKey(int oldKey, int newKey,QFlowPort port)
        {
            var newId = new PortId(port, newKey);
            var oldId = new PortId(port, oldKey);
            foreach (var portId in ConnectList)
            {
                var list = port.Node.Graph.GetConnectInfo(portId).ConnectList;
                list.Remove(oldId);
                list.Add(newId);
            }
        }
        public PortId? ConnectPort()
        {
            if (ConnectList.Count > 0)
            {
                return ConnectList.QueuePeek();
            }
            else
            {
                return null;
            }
        }
    }
    public class QFlowPort : IKey<string>
    {
        public override string ToString()
        {
            return "端口" + Key + "(" + ValueType + ")";
        }
        public string Key { get; set; }
        public string name;
        public bool isOutput = false;
        public string stringValue;

        public bool isFlowList;
        public void IndexChange(int a,int b)
        {
            var list = Value as IList;
            if (a < 0)
            {
                ConnectInfolist.Insert(b, new ConnectInfo());
                for (int i = b; i < list.Count; i++)
                {
                    var c = this[i];
                    c.ChangeKey(i-1,i ,this);
                }
            }
            else if(b<0)
            {
                ClearAllConnect(a);
                ConnectInfolist.RemoveAt(a);
                for (int i = a; i < list.Count; i++)
                {
                    var c = this[i];
                    c.ChangeKey(i+1,i , this);
                }
            }
        }
        public ConnectInfo this[int listKey]
        {
            get
            {
                if (listKey < 0) return null;
                if (ConnectInfolist[listKey]== null)
                {
                    ConnectInfolist[listKey] = new ConnectInfo();
                }
                return ConnectInfolist[listKey];
            }
        }
        public bool onlyoneConnect;
        public ConnectInfo ConnectInfo => this[0];
        public QList<ConnectInfo> ConnectInfolist { set; get; } = new QList<ConnectInfo>();
        [QIgnore]
        public Type ConnectType { internal set; get; }
        public bool ShowValue
        {
            get
            {
                if (FlowPort == null)
                {
                    return !isOutput && ConnectInfo.ConnectList.Count == 0;
                }
                else
                {
                    return isFlowList || (FlowPort.showValue && ValueType != QFlow.Type);
                }
            }
        }
        [QIgnore]
        public QOutputPortAttribute OutputPort;
        [QIgnore]
        public QFlowPortAttribute FlowPort;
        [QIgnore]
        public int paramIndex = -1;
        [QIgnore]
        public Type ValueType { internal set; get; }

        [QIgnore]
        public QFlowNode Node { get; internal set; }
        public bool HasConnect
        {
            get
            {
                return ConnectInfo.ConnectList.Count > 0;
            }
        }


        object _value;
        [QIgnore]
        public object Value
        {
            get
            {
                if (ValueType == QFlow.Type) return null;
                if (FlowPort == null || ValueType == QFlow.Type)
                {
                    if (isOutput)
                    {
                        return _value;
                    }
                    else if (HasConnect)
                    {
                        var port = Node.Graph[ConnectInfo.ConnectPort()];
                        if (port.OutputPort.autoRunNode)
                        {
                            port.Node.Run();
                        }
                        return port.Value;
                    }
                }
                if (_value == null)
                {
                    _value = stringValue.ParseQData(ValueType, true, _value);
                }
                return _value;
            }
            set
            {
                if (ValueType == QFlow.Type) return;
                if (FlowPort == null)
                {
                    if (isOutput || HasConnect)
                    {
                        _value = value;
                    }
                }
                stringValue = value.ToQData(ValueType);
            }
        }

        public void Init(QFlowNode node)
        {
            this.Node = node;
            if (isFlowList)
            {
                var list = Value as IList;
                ConnectInfolist.RemoveAll((obj) => ConnectInfolist.IndexOf(obj)>= list.Count|| ConnectInfolist.IndexOf(obj) < 0); 
           
            }
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
            if (ConnectType == type)
            {
                return true;
            }
            else if (ConnectType != QFlow.Type && type != QFlow.Type)
            {
                if (type == typeof(object))
                {
                    return true;
                }
                else if (ConnectType.IsAssignableFrom(type))
                {
                    return true;
                }
                else if (type.IsAssignableFrom(ConnectType))
                {
                    return true;
                } else if (CanConnectList.ContainsKey(ConnectType))
                {
                    return CanConnectList[ConnectType].Contains(type);
                }
            }
            return false;
        }
        public bool CanConnect(QCommandInfo info, out string portKey)
        {
            if (ConnectType == QFlow.Type)
            {
                portKey = QFlowKey.FromPort;
                return true;
            }
            foreach (var paramInfo in info.paramInfos)
            {
                if (paramInfo.IsOut) continue;
                var can = CanConnect(paramInfo.ParameterType);
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
            return CanConnect(port.ConnectType);
        }
        public void Connect(QFlowPort port, int listKey = 0)
        {
            if (port == null) return;
            Connect(new PortId(port), listKey);
        }
        public void Connect(PortId? portId, int listKey =0)
        {
            if (portId == null) return;
            var targetPort = Node.Graph[portId];
            if (targetPort == null) return;
            if (!CanConnect(targetPort))
            {
                Debug.LogError("不能将 " + this + " 连接 " + targetPort);
                return;
            }
            if (onlyoneConnect)
            {
                ClearAllConnect(listKey);
            }
            if (targetPort.onlyoneConnect)
            {
                targetPort.ClearAllConnect(portId.Value.listKey);
            }
            this[listKey].ConnectList.AddCheckExist(portId.Value);
            targetPort[portId.Value.listKey].ConnectList.AddCheckExist(new PortId(this, listKey));


        }
     
        public void DisConnect(PortId? connect, int listKey = 0)
        {
            if (connect == null) return;
            this[listKey].ConnectList.Remove(connect.Value);
            var port = Node.Graph[connect];
            if (port==null)
            {
                Debug.LogError("不存在端口 " + port);
                return;
            }
            port[connect.Value.listKey].ConnectList.Remove(new PortId(this, listKey));
        }
     
        public void ClearAllConnect(int listKey = 0)
        {
            foreach (var connect in this[listKey].ConnectList.ToArray())
            {
                DisConnect(connect,listKey);
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
            return "(" + commandKey + ")";
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
        [QIgnore]
        public List<string> TriggerPortList { get; private set; } = new List<string>();
        public QFlowNode()
        {

        }
        public QFlowNode(string commandKey)
        {
            this.commandKey = commandKey;
        }
        public QFlowPort AddPort(string key, QOutputPortAttribute outputPort = null, string name="",Type type=null,QFlowPortAttribute FlowPort=null)
        {
            if (type == null)
            {
                type = QFlow.Type;
            }
         
            if (!Ports.ContainsKey(key))
            {
                Ports.Set(key, new QFlowPort());
            }
            var port = Ports[key];
            if (string.IsNullOrEmpty(name))
            {
                port.name = key;
            }
            else
            {
                port.name = name;
            }
            port.Key = key;
            port.ValueType = type;
            port.ConnectType = FlowPort ==null ? type : QFlow.Type;
            port.isOutput = outputPort!=null;
            port.FlowPort = FlowPort ?? (type == QFlow.Type ? QFlowPortAttribute.Normal : null);
            port.OutputPort = outputPort;
            port.onlyoneConnect = (port.FlowPort != null)== port.isOutput ;
            port.isFlowList = (type.IsArray || typeof(IList).IsAssignableFrom(type))&&port.FlowPort!=null;
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
            this.name = command.name;
            AddPort(QFlowKey.FromPort);
            AddPort(QFlowKey.NextPort, QOutputPortAttribute.Normal);
            commandParams = new object[command.paramInfos.Length];
            OutParamPorts.Clear();
            for (int i = 0; i < command.paramInfos.Length; i++)
            {
                var paramInfo = command.paramInfos[i];
                if (paramInfo.Name.Equals(QFlowKey.This)) continue;
                var outputAtt = paramInfo.GetAttribute<QOutputPortAttribute>() ?? (paramInfo.IsOut ? QOutputPortAttribute.Normal : null);
                var port = AddPort(paramInfo.Name, outputAtt, paramInfo.ViewName(), paramInfo.ParameterType.GetTrueType(), paramInfo.GetAttribute<QFlowPortAttribute>());
                port.paramIndex = i;
                if (paramInfo.HasDefaultValue)
                {
                    port.Value = paramInfo.DefaultValue;
                }
                if (port.isOutput)
                {
                    if (port.OutputPort.autoRunNode)
                    {
                        Ports.RemoveKey(QFlowKey.FromPort);
                        Ports.RemoveKey(QFlowKey.NextPort);
                    }
                    else if(port.FlowPort!=null)
                    {
                        Ports.RemoveKey(QFlowKey.NextPort);
                    }
                    if (port.FlowPort == null)
                    {
                        if (paramInfo.IsOut ||( Key != QFlowKey.ResultPort && !port.ValueType.IsValueType))
                        {
                            OutParamPorts.Add(port);
                        }
                    }
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
                    AddPort(QFlowKey.ResultPort, QOutputPortAttribute.Normal, "结果", command.method.ReturnType.GetTrueType());
                }
            }
            else
            {
                AddPort(QFlowKey.ResultPort, QOutputPortAttribute.Normal, "结果", command.method.ReturnType.GetTrueType());
                returnType = ReturnType.ReturnValue;
            }
            Ports.RemoveAll((port) => port.Node == null);
        }
        internal QFlowPort NextNodePort
        {
            get
            {
                return Graph[Ports[_nextFlowPort]?.ConnectInfo.ConnectPort()];
            }
        }
        public void ClearAllConnect()
        {
            foreach (var port in Ports)
            {
                port.ClearAllConnect();
            }
        }
        public void SetNextNode(QFlowNode targetState)
        {
            Ports[QFlowKey.NextPort].Connect(new PortId(targetState.Ports[QFlowKey.FromPort]));
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
        internal void TriggerPort(QFlowPort port)
        {
            TriggerPortList.AddCheckExist(port.Key);
        }
        public void RunPort(string portKey)
        {
            Graph.StartCoroutine(RunPortIEnumerator(portKey));
        }
        public IEnumerator RunPortIEnumerator(string portKey)
        {
            if (Ports.ContainsKey(portKey))
            {
                var node = Graph[ Ports[portKey].ConnectInfo.ConnectPort()].Node;
                return node.Graph.RunIEnumerator(node.Key);
            }
            else
            {
                Debug.LogError("不存在端口[" + portKey + "]");
                return null;
            }
        }
        public IEnumerator RunIEnumerator()
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