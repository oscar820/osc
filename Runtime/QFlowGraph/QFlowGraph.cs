using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using QTool.Reflection;
namespace QTool.FlowGraph
{
	
	[System.Serializable]
    public class QFlowGraph:ISerializationCallbackReceiver
    {

		[TextArea(1, 30)]
		[QIgnore]
		public string SerializeString;
		public void OnBeforeSerialize()
		{
			SerializeString = this.ToQData();
		}

		public void OnAfterDeserialize()
		{
			SerializeString.ParseQData(this);
			InitOver = false;
			InitCheck();
		}
		public QFlowGraph CreateInstance()
		{
			return this.ToQData().ParseQData<QFlowGraph>().InitCheck();
		}
        public override string ToString()
        {
            return this.ToQData();
        }
		[QName]
		public string Name {  set; get; }
		[QName]
		public QList<string,QFlowNode> NodeList { private set; get; } = new QList<string,QFlowNode>();
		[QName]
		public QDictionary<string, object> Values { private set; get; } = new QDictionary<string, object>();
		public bool IsRunning => CoroutineList.Count > 0;
        public T GetValue<T>(string key="")
        {
            var type = typeof(T);
			if (key.IsNullOrEmpty())
			{
				key = type.Name;
			}
			var obj = Values[key];
            if (obj==null&& type.IsValueType)
            {
                obj = type.CreateInstance();
            }
            return (T)obj;
        }
		public void SetValue<T>( T value)
		{
			SetValue(typeof(T).Name, value);
		}
		public void SetValue<T>(string key,T value)
        {
            Values[key] = value;
        }
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
            return this[portId]?[portId.Value.index];
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
		[QIgnore]
		public Func<IEnumerator,Coroutine> StartCoroutineOverride;
		[QIgnore]
		public Action<Coroutine> StopCoroutineOverride;
		[QIgnore]
		public QDictionary<string, Coroutine> CoroutineList { private set; get; } = new QDictionary<string, Coroutine>();
		internal void StartCoroutine(string key,IEnumerator coroutine)
        {
            if (StartCoroutineOverride == null)
            {
				CoroutineList[key] = QToolManager.Instance.StartCoroutine(coroutine);
            }
            else
            {
				CoroutineList[key] = StartCoroutineOverride(coroutine);
            }
        }
		public void Stop()
		{
			if (StopCoroutineOverride == null)
			{
				foreach (var kv in CoroutineList)
				{
					if(kv.Value is Coroutine cor)
					{
						QToolManager.Instance.StopCoroutine(cor);
					}
				}
			}
			else
			{
				foreach (var kv in CoroutineList)
				{
					StopCoroutineOverride(kv.Value);
				}
			}
			CoroutineList.Clear();
		}
		public void Run(string startNode)
		{
			StartCoroutine(startNode, RunIEnumerator(startNode));
		}
        public void Run(string startNode, Func<IEnumerator, Coroutine> StartCoroutineOverride , Action<Coroutine> StopCoroutineOverride)
        {
            this.StartCoroutineOverride = StartCoroutineOverride;
			this.StopCoroutineOverride = StopCoroutineOverride;
            StartCoroutine(startNode,RunIEnumerator(startNode));
		}
	

		public IEnumerator RunIEnumerator(string startNode)
        {
			if (!Application.isPlaying)
			{
				Debug.LogError("运行流程图[" + Name + "]出错 不能在非运行时运行流程图");
				yield break;
			}
			InitCheck();
			var curNode = this[startNode]; 
			if (curNode != null)
			{
				QDebug.Log("以[" + startNode + "]为起点 运行流程图 [" + Name + "]");
				while (curNode != null)
				{
					yield return curNode.RunIEnumerator();
					var port = curNode.NextNodePort;
					if (port != null)
					{
						if (port.Value.port == QFlowKey.FromPort)
						{
							curNode = this[port.Value.node];
						}
						else
						{
							var portObj = this[port];
							if (portObj != null && !portObj.isOutput && portObj.InputPort.autoRunNode)
							{
								if (!portObj.Node.IsRunning)
								{
									Run(portObj.Node.Key);
								}
							}
							this[port.Value.node].TriggerPort(port.Value);
							curNode = null; ;
						}

					}
					else
					{
						curNode = null;
					}
				}
			}
			else
			{
				Debug.LogError("不存在开始节点 [" + startNode + "]");
			}
			CoroutineList.RemoveKey(startNode);
        }
		[QIgnore]
		public bool InitOver { set; get; }
        internal QFlowGraph InitCheck() 
        {
			if (InitOver) return this;
			InitOver = true;
			foreach (var state in NodeList)
            {
				if (!state.Init(this))
				{
					InitOver = false;
				}
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
        public int index ;
        public PortId(QFlowPort statePort,int index=0)
        {
            node = statePort.Node.Key;
            port = statePort.Key;
            this.index = index;
        }
        public override string ToString()
        {
            return index==0? port:port+"["+index+"]";
        }
    }

	public abstract class QPortAttribute : Attribute
	{
		public bool autoRunNode = false;
	}
	
	/// <summary>
	///  指定参数端口为输入端口
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
	public class QInputPortAttribute : QPortAttribute
	{
		public static QInputPortAttribute Normal = new QInputPortAttribute();

		public bool HasValue
		{
			get
			{
				return !string.IsNullOrEmpty(autoGetValue);
			}
		}
		public string autoGetValue= "";
		public QInputPortAttribute(string autoGetValue ="")
		{
			this.autoGetValue = autoGetValue;
		}
		public QInputPortAttribute(bool autoRunNode )
		{
			this.autoRunNode = autoRunNode;
		}
	}
	/// <summary>
	///  指定参数端口为输出端口
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter| AttributeTargets.ReturnValue, AllowMultiple = false)]
    public class QOutputPortAttribute : QPortAttribute
	{
        public static QOutputPortAttribute Normal = new QOutputPortAttribute();

       
        public QOutputPortAttribute(bool autoRunNode=false)
        {
			this.autoRunNode = autoRunNode;
        }
    }
    /// <summary>
    /// 指定参数端口为流程端口
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class QFlowPortAttribute : Attribute
	{
        public static QFlowPortAttribute Normal = new QFlowPortAttribute();

        public bool showValue = false;
        public QFlowPortAttribute()
        {
        }
    }
    /// <summary>
    /// 指定参数端口自动更改节点Key值与名字 两个相同Key节点会报错
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class QNodeKeyNameAttribute : Attribute
    {
        public QNodeKeyNameAttribute()
        {
        }
    }
    /// <summary>
    /// 指定函数节点为起点节点 即没有流程输入端口 节点Key为函数名
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class QStartNodeAttribute : Attribute
    {
        public QStartNodeAttribute()
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
		public string ViewName
		{
			get
			{
				if (isOutput)
				{
					return name;
				}
				else if(InputPort!=null&&InputPort.HasValue&&!HasConnect)
				{
					return name + " = [" + InputPort.autoGetValue + "]";
				}
				else
				{
					return name;
				}
			}
		}
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
        public ConnectInfo this[int index]
        {
            get
            {
                if (index < 0) return null;
                if (ConnectInfolist[index]== null)
                {
                    ConnectInfolist[index] = new ConnectInfo();
                }
                return ConnectInfolist[index];
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
                    return InputPort!=null && ConnectInfo.ConnectList!=null&& ConnectInfo.ConnectList.Count == 0&&!InputPort.HasValue;
                }
                else
                {
                    return isFlowList || (FlowPort.showValue && ValueType != QFlow.Type);
                }
            }
        }
		[QIgnore]
		public System.Reflection.ParameterInfo parameterInfo;
        [QIgnore]
        public QNodeKeyNameAttribute KeyNameAttribute;
        [QIgnore]
        public QOutputPortAttribute OutputPort;
		[QIgnore]
		public QInputPortAttribute InputPort;
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
                if (ValueType == QFlow.Type|| Node.command==null) return null;
                if (FlowPort == null&&!isOutput )
                {
					if (HasConnect)
					{
						var port = Node.Graph[ConnectInfo.ConnectPort()];
						if (port.OutputPort.autoRunNode)
						{
							port.Node.Run();
						}
						return port.Value;
					}
					else
					{
						if (InputPort.HasValue)
						{
							return Node.Graph.Values[InputPort.autoGetValue];
						}
					}
                }
                if (_value == null)
                {
                    _value = stringValue.ParseQDataType(ValueType, true, _value);
                }
                return _value;
            }
			set
			{
				if (ValueType == QFlow.Type || Node.command == null) return;
             
                _value = value;
                stringValue = value.ToQDataType(ValueType);
                if (KeyNameAttribute != null)
                {
                    Node.Key = _value?.ToString();
                    Node.name = Node.Key;
                }
            }
        }

        public void Init(QFlowNode node)
        {
            this.Node = node;
            if (isFlowList && Value is IList list)
            {
                ConnectInfolist.RemoveAll((obj) => ConnectInfolist.IndexOf(obj)>= list.Count|| ConnectInfolist.IndexOf(obj) < 0); 
            }
        }
        public static QDictionary<Type, List<Type>> CanConnectList = new QDictionary<Type, List<Type>>()
        {
            {
                 typeof(int),
                 new List<Type>{typeof(float),typeof(double)}
            }
        };
        public bool CanConnect(Type type)
        {
			if (ConnectType == null) return false;
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
        public void Connect(QFlowPort port, int index = 0)
        {
            if (port == null) return;
            Connect(new PortId(port), index);
        }
        public void Connect(PortId? portId, int index =0)
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
                ClearAllConnect(index);
            }
            if (targetPort.onlyoneConnect)
            {
                targetPort.ClearAllConnect(portId.Value.index);
            }
            this[index].ConnectList.AddCheckExist(portId.Value);
            targetPort[portId.Value.index].ConnectList.AddCheckExist(new PortId(this, index));


        }
     
        public void DisConnect(PortId? connect, int index = 0)
        {
            if (connect == null) return;
            this[index].ConnectList.Remove(connect.Value);
            var port = Node.Graph[connect];
            if (port==null)
            {
                Debug.LogError("不存在端口 " + port);
                return;
            }
            port[connect.Value.index]?.ConnectList.Remove(new PortId(this, index));
        }
     
        public void ClearAllConnect(int index = 0)
        {
            foreach (var connect in this[index].ConnectList.ToArray())
            {
                DisConnect(connect,index);
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
		public bool IsRunning { private set; get; }
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
        public Rect rect = new Rect(Vector2.zero, new Vector2(300, 80));

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
        public List<PortId> TriggerPortList { get; private set; } = new List<PortId>();
        public QFlowNode()
        {

        }
        public QFlowNode(string commandKey)
        {
            this.commandKey = commandKey;
        }
        public QFlowPort AddPort(string key, Attribute portAttribute , string name="",Type type=null,QFlowPortAttribute FlowPort=null)
        {
            
            if (type == null)
            {
                type = QFlow.Type;
            }

            var typeInfo = QSerializeType.Get(type);


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
			port.isOutput = portAttribute is QOutputPortAttribute;
            port.FlowPort = FlowPort ?? ((type == QFlow.Type|| typeInfo.ElementType==QFlow.Type) ? QFlowPortAttribute.Normal : null);
		
            port.ConnectType = port.FlowPort == null ? type : QFlow.Type;
			port.InputPort = portAttribute as QInputPortAttribute;
			port.OutputPort = portAttribute as QOutputPortAttribute;
			port.onlyoneConnect = (port.FlowPort != null)== port.isOutput ;
            port.isFlowList = typeInfo .IsList&& port.FlowPort!=null;
            port.Init(this);
            return port;
        }
        public bool Init(QFlowGraph graph)
        {
            this.Graph = graph;
			if (command != null) return true;
            command = QCommand.GetCommand(commandKey);
            if (command == null)
            {
                foreach (var port in Ports)
                {
                    port.Init(this);
                }
                Debug.LogError("不存在命令【" + commandKey + "】");
                return false; 
            }

			this.name = command.name.SplitEndString("/");
            if (command.method.GetAttribute<QStartNodeAttribute>() == null)
            {
                AddPort(QFlowKey.FromPort,QInputPortAttribute.Normal);
            }
            else
            {
                Key = this.name;
            }
            AddPort(QFlowKey.NextPort, QOutputPortAttribute.Normal);
            commandParams = new object[command.paramInfos.Length];
            OutParamPorts.Clear();
            for (int i = 0; i < command.paramInfos.Length; i++)
            {
                var paramInfo = command.paramInfos[i];
				if (paramInfo.Name.Equals(QFlowKey.This)) continue;
                var portAtt = paramInfo.GetAttribute<QPortAttribute>() ??( paramInfo.IsOut ? (QPortAttribute)QOutputPortAttribute.Normal : (QPortAttribute)QInputPortAttribute.Normal);
                var port = AddPort(paramInfo.Name, portAtt, paramInfo.QName(), paramInfo.ParameterType.GetTrueType(), paramInfo.GetAttribute<QFlowPortAttribute>());
                port.paramIndex = i;
                port.KeyNameAttribute = paramInfo.GetAttribute<QNodeKeyNameAttribute>();
				port.parameterInfo = paramInfo;
				if (paramInfo.HasDefaultValue)
                {
					if(string.IsNullOrEmpty( port.stringValue ))
					{
						port.Value = paramInfo.DefaultValue;
					}
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
				else
				{
					if (port.InputPort.autoRunNode)
					{
						Ports.RemoveKey(QFlowKey.FromPort);
					}
				}
				if (port.KeyNameAttribute != null)
				{
					if (port.Value != null)
					{
						Key = port.Value.ToString();
						name = Key;
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
				var outputAtt = command.method.ReturnTypeCustomAttributes.GetAttribute<QOutputPortAttribute>() ??  QOutputPortAttribute.Normal;
				if (outputAtt.autoRunNode)
				{
					Ports.RemoveKey(QFlowKey.FromPort);
					Ports.RemoveKey(QFlowKey.NextPort);
				}
				AddPort(QFlowKey.ResultPort, outputAtt, "结果", command.method.ReturnType.GetTrueType());
                returnType = ReturnType.ReturnValue;
            }
            Ports.RemoveAll((port) => port.Node == null);
			return true;
        }
        internal PortId? NextNodePort
        {
            get
            {
                if (_nextFlowPort == null)
                {
                    return Ports[QFlowKey.NextPort]?.ConnectInfo.ConnectPort();
                }
                else
                {
                    return Ports[_nextFlowPort.Value.port]?[_nextFlowPort.Value.index].ConnectPort();
                }
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
		[QName]
        public QList<string, QFlowPort> Ports { get; private set; } = new QList<string, QFlowPort>();
        PortId? _nextFlowPort;
        public void SetNetFlowPort(string portKey,int listIndex=0)
        {
            if (!Ports.ContainsKey(portKey))
            {
                Debug.LogError(ViewName + "不存在端口[" + portKey + "]");
            }
            _nextFlowPort = new PortId(Ports[portKey], listIndex);
        }
        object[] commandParams;
        static Func<object,object> TaskReturnValueGet;
        object InvokeCommand()
        {
            _nextFlowPort = null;
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
        internal void TriggerPort(PortId port)
        {
            TriggerPortList.AddCheckExist(port);
        }
        public void RunPort(string portKey,int index=0)
        {
            Graph.StartCoroutine(Key,RunPortIEnumerator(portKey,index));
        }
        public IEnumerator RunPortIEnumerator(string portKey, int index = 0)
        {
            if (Ports.ContainsKey(portKey))
            {
                var node = Graph[ Ports[portKey][index].ConnectPort()].Node;
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
			if (command == null)
			{
				Debug.LogError("不存在命令【" + commandKey + "】");
				yield break;
			}
			IsRunning = true;
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
			IsRunning = false;

			foreach (var port in OutParamPorts)
            {
                port.Value = commandParams[port.paramIndex];
            }
        }

    }
}
