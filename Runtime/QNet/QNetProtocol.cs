using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Net
{
	public enum Channels:byte
	{
		Reliable=0,
		Unreliable=1,
	}
	public abstract class QNetProtocol : MonoBehaviour
	{
		public static QNetProtocol activeTransport;

		/// <summary>Is this transport available in the current platform?</summary>
		public abstract bool Available();

		// client //////////////////////////////////////////////////////////////
		/// <summary>Called by Transport when the client connected to the server.</summary>
		public Action OnClientConnected;

		/// <summary>Called by Transport when the client received a message from the server.</summary>
		public Action<ArraySegment<byte>, Channels> OnClientDataReceived;

		/// <summary>Called by Transport when the client sent a message to the server.</summary>
		// Transports are responsible for calling it because:
		// - groups it together with OnReceived responsibility
		// - allows transports to decide if anything was sent or not
		// - allows transports to decide the actual used channel (i.e. tcp always sending reliable)
		public Action<ArraySegment<byte>, Channels> OnClientDataSent;

		/// <summary>Called by Transport when the client encountered an error.</summary>
		public Action<Exception> OnClientError;

		/// <summary>Called by Transport when the client disconnected from the server.</summary>
		public Action OnClientDisconnected;

		// server //////////////////////////////////////////////////////////////
		/// <summary>Called by Transport when a new client connected to the server.</summary>
		public Action<int> OnServerConnected;

		/// <summary>Called by Transport when the server received a message from a client.</summary>
		public Action<int, ArraySegment<byte>, Channels> OnServerDataReceived;

		/// <summary>Called by Transport when the server sent a message to a client.</summary>
		// Transports are responsible for calling it because:
		// - groups it together with OnReceived responsibility
		// - allows transports to decide if anything was sent or not
		// - allows transports to decide the actual used channel (i.e. tcp always sending reliable)
		public Action<int, ArraySegment<byte>, Channels> OnServerDataSent;

		/// <summary>Called by Transport when a server's connection encountered a problem.</summary>
		/// If a Disconnect will also be raised, raise the Error first.
		public Action<int, Exception> OnServerError;

		/// <summary>Called by Transport when a client disconnected from the server.</summary>
		public Action<int> OnServerDisconnected;

		// client functions ////////////////////////////////////////////////////
		/// <summary>True if the client is currently connected to the server.</summary>
		public abstract bool ClientConnected();

		/// <summary>Connects the client to the server at the address.</summary>
		public abstract void ClientConnect(string address);

		/// <summary>Connects the client to the server at the Uri.</summary>
		public virtual void ClientConnect(Uri uri)
		{
			// By default, to keep backwards compatibility, just connect to the host
			// in the uri
			ClientConnect(uri.Host);
		}

		/// <summary>Sends a message to the server over the given channel.</summary>
		// The ArraySegment is only valid until returning. Copy if needed.
		public abstract void ClientSend(ArraySegment<byte> segment, Channels channelId = Channels.Reliable);

		/// <summary>Disconnects the client from the server</summary>
		public abstract void ClientDisconnect();

		// server functions ////////////////////////////////////////////////////
		/// <summary>Returns server address as Uri.</summary>
		// Useful for NetworkDiscovery.
		public abstract Uri ServerUri();

		/// <summary>True if the server is currently listening for connections.</summary>
		public abstract bool ServerActive();

		/// <summary>Start listening for connections.</summary>
		public abstract void ServerStart();

		/// <summary>Send a message to a client over the given channel.</summary>
		public abstract void ServerSend(int connectionId, ArraySegment<byte> segment, Channels channelId = Channels.Reliable );

		/// <summary>Disconnect a client from the server.</summary>
		public abstract void ServerDisconnect(int connectionId);

		/// <summary>Get a client's address on the server.</summary>
		// Can be useful for Game Master IP bans etc.
		public abstract string ServerGetClientAddress(int connectionId);

		/// <summary>Stop listening and disconnect all connections.</summary>
		public abstract void ServerStop();

		///<summary>给定通道的最大消息大小。</summary>

		//不同的频道通常有不同的大小，从MTU到

		//几兆字节。

		//

		//需要始终返回值，即使Transport不是

		//正在运行或可用，因为初始化需要它。
		public abstract int GetMaxPacketSize(Channels channelId );

		///<summary>此传输的建议批处理阈值。</summary>

		//默认情况下使用GetMaxPacketSize。

		//某些传输（如kcp）支持较大的最大数据包大小，这应该

		//不会一直用于批处理，因为它们最终也会

		//缓慢（线路头阻塞等）。
		public virtual int GetBatchThreshold(Channels channelId )
		{
			return GetMaxPacketSize(channelId);
		}

		//阻止Update&LateUpdate，以便在传输仍在使用时显示警告

		//而不是使用

		//Client/ServerEarlyUpdate:处理传入消息

		//Client/ServerLateUpdate:处理传出消息

		//网络客户机/服务器在正确的时间调用它们。

		//

		//允许传输实现正确的网络更新顺序：

		//进程传入（）

		//update_world（）

		//处理_输出（）

		//

		//=>请参阅NetworkLoop。cs了解详细说明！
#pragma warning disable UNT0001 // Empty Unity message
		public void Update() { }
		public void LateUpdate() { }
#pragma warning restore UNT0001 // Empty Unity message

		///已为适当的网络添加NetworkLoop NetworkEarly/LateUpdate

		///更新订单。目标是：

		///进程传入（）

		///update_world（）

		///处理_输出（）

		///为了避免不必要的延迟和数据争用。

		///</summary>

		//=>分为客户端和服务器部分，以便我们可以干净地调用

		//它们来自NetworkClient/Server

		//=>现在是虚拟的，因此我们可以花时间转换传输

		//不会破坏任何东西。
		public virtual void ClientEarlyUpdate() { }
		public virtual void ServerEarlyUpdate() { }
		public virtual void ClientLateUpdate() { }
		public virtual void ServerLateUpdate() { }

		///<summary>以客户端和服务器身份关闭传输</summary>
		public abstract void Shutdown();

		//<summary>退出时由Unity呼叫。继承Transports应调用base以正确关闭。</summary>
		public virtual void OnApplicationQuit()
		{
			//停止传输（例如，关闭线程）

			//（当在编辑器中按Stop时，Unity保持线程活动

			//直到我们再次按下“开始”。因此，如果Transports使用线程，我们

			//真的希望他们现在就结束，而不是在下次开始后）
			Shutdown();
		}
	}


//	/// <summary>Synchronizes server time to clients.</summary>
//	public static class NetworkTime
//	{
//		/// <summary>Ping message frequency, used to calculate network time and RTT</summary>
//		public static float PingFrequency = 2.0f;

//		/// <summary>Average out the last few results from Ping</summary>
//		public static int PingWindowSize = 10;

//		static double lastPingTime;

//		static ExponentialMovingAverage _rtt = new ExponentialMovingAverage(10);
//		static ExponentialMovingAverage _offset = new ExponentialMovingAverage(10);

//		// the true offset guaranteed to be in this range
//		static double offsetMin = double.MinValue;
//		static double offsetMax = double.MaxValue;

//		/// <summary>Returns double precision clock time _in this system_, unaffected by the network.</summary>
//#if UNITY_2020_3_OR_NEWER
//		public static double localTime
//		{
//			[MethodImpl(MethodImplOptions.AggressiveInlining)]
//			get => Time.timeAsDouble;
//		}
//#else
//        // need stopwatch for older Unity versions, but it's quite slow.
//        // CAREFUL: unlike Time.time, this is not a FRAME time.
//        //          it changes during the frame too.
//        static readonly Stopwatch stopwatch = new Stopwatch();
//        static NetworkTime() => stopwatch.Start();
//        public static double localTime => stopwatch.Elapsed.TotalSeconds;
//#endif

//		/// <summary>The time in seconds since the server started.</summary>
//		//
//		// I measured the accuracy of float and I got this:
//		// for the same day,  accuracy is better than 1 ms
//		// after 1 day,  accuracy goes down to 7 ms
//		// after 10 days, accuracy is 61 ms
//		// after 30 days , accuracy is 238 ms
//		// after 60 days, accuracy is 454 ms
//		// in other words,  if the server is running for 2 months,
//		// and you cast down to float,  then the time will jump in 0.4s intervals.
//		//
//		// TODO consider using Unbatcher's remoteTime for NetworkTime
//		public static double time
//		{
//			[MethodImpl(MethodImplOptions.AggressiveInlining)]
//			get => localTime - _offset.Value;
//		}

//		/// <summary>Time measurement variance. The higher, the less accurate the time is.</summary>
//		// TODO does this need to be public? user should only need NetworkTime.time
//		public static double timeVariance => _offset.Var;

//		/// <summary>Time standard deviation. The highe, the less accurate the time is.</summary>
//		// TODO does this need to be public? user should only need NetworkTime.time
//		public static double timeStandardDeviation => Math.Sqrt(timeVariance);

//		/// <summary>Clock difference in seconds between the client and the server. Always 0 on server.</summary>
//		public static double offset => _offset.Value;

//		/// <summary>Round trip time (in seconds) that it takes a message to go client->server->client.</summary>
//		public static double rtt => _rtt.Value;

//		/// <summary>Round trip time variance. The higher, the less accurate the rtt is.</summary>
//		// TODO does this need to be public? user should only need NetworkTime.time
//		public static double rttVariance => _rtt.Var;

//		/// <summary>Round trip time standard deviation. The higher, the less accurate the rtt is.</summary>
//		// TODO does this need to be public? user should only need NetworkTime.time
//		public static double rttStandardDeviation => Math.Sqrt(rttVariance);

//		// RuntimeInitializeOnLoadMethod -> fast playmode without domain reload
//		[UnityEngine.RuntimeInitializeOnLoadMethod]
//		public static void ResetStatics()
//		{
//			PingFrequency = 2.0f;
//			PingWindowSize = 10;
//			lastPingTime = 0;
//			_rtt = new ExponentialMovingAverage(PingWindowSize);
//			_offset = new ExponentialMovingAverage(PingWindowSize);
//			offsetMin = double.MinValue;
//			offsetMax = double.MaxValue;
//#if !UNITY_2020_3_OR_NEWER
//            stopwatch.Restart();
//#endif
//		}

//		internal static void UpdateClient()
//		{
//			// localTime (double) instead of Time.time for accuracy over days
//			if (localTime - lastPingTime >= PingFrequency)
//			{
//				NetworkPingMessage pingMessage = new NetworkPingMessage(localTime);
//				NetworkClient.Send(pingMessage, Channels.Unreliable);
//				lastPingTime = localTime;
//			}
//		}

//		// executed at the server when we receive a ping message
//		// reply with a pong containing the time from the client
//		// and time from the server
//		internal static void OnServerPing(NetworkConnectionToClient conn, NetworkPingMessage message)
//		{
//			// Debug.Log($"OnPingServerMessage conn:{conn}");
//			NetworkPongMessage pongMessage = new NetworkPongMessage
//			{
//				clientTime = message.clientTime,
//				serverTime = localTime
//			};
//			conn.Send(pongMessage, Channels.Unreliable);
//		}

//		// Executed at the client when we receive a Pong message
//		// find out how long it took since we sent the Ping
//		// and update time offset
//		internal static void OnClientPong(NetworkPongMessage message)
//		{
//			double now = localTime;

//			// how long did this message take to come back
//			double newRtt = now - message.clientTime;
//			_rtt.Add(newRtt);

//			// the difference in time between the client and the server
//			// but subtract half of the rtt to compensate for latency
//			// half of rtt is the best approximation we have
//			double newOffset = now - newRtt * 0.5f - message.serverTime;

//			double newOffsetMin = now - newRtt - message.serverTime;
//			double newOffsetMax = now - message.serverTime;
//			offsetMin = Math.Max(offsetMin, newOffsetMin);
//			offsetMax = Math.Min(offsetMax, newOffsetMax);

//			if (_offset.Value < offsetMin || _offset.Value > offsetMax)
//			{
//				// the old offset was offrange,  throw it away and use new one
//				_offset = new ExponentialMovingAverage(PingWindowSize);
//				_offset.Add(newOffset);
//			}
//			else if (newOffset >= offsetMin || newOffset <= offsetMax)
//			{
//				// new offset looks reasonable,  add to the average
//				_offset.Add(newOffset);
//			}
//		}
//	}
}
