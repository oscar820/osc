using System;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{

    public static class QPoolManager
    {
        
        static QDictionary<string, PoolBase> poolDic = new QDictionary<string, PoolBase>();

        public static GameObject Get(string poolKey ,GameObject prefab)
        {
            return GetPool(poolKey,prefab).Get() ;
        }
        public static ObjectPool<GameObject> GetPool(string poolKey,GameObject prefab)
        {
            return GetPool(poolKey, () => GameObject.Instantiate(prefab));
        }
        public static T Get<T>(string poolName, System.Func<T> newFunc = null) where T : class
        {
            return GetPool<T>(poolName, newFunc).Get();
        }
        public static ObjectPool<T> GetPool<T>(string poolName, System.Func<T> newFunc = null) where T : class
        {
            var key = poolName;
            if (string.IsNullOrEmpty(key))
            {
                key = typeof(T).ToString();
            }
			if (poolDic.ContainsKey(key))
			{
				if (poolDic[key] is ObjectPool<T>)
				{
					var pool = poolDic[key] as ObjectPool<T>;
					if (newFunc != null && pool.newFunc == null)
					{
						pool.newFunc = newFunc;
					}
					return pool;
				}
				else
				{
					throw new Exception("已存在重名不同类型对象池" + poolDic[key]);
				}

			}
			else
			{
				lock (poolDic)
				{
					var pool = new ObjectPool<T>(key, newFunc);
					poolDic[key] = pool;
					return pool;
				}
			}
		
        }
        public static void Push(GameObject gameObject)
        {
            Push(gameObject.name, gameObject);
        }
        public static void Push<T>(string poolName, T obj) where T : class
        {
			if (poolDic.ContainsKey(poolName))
			{
				(poolDic[poolName] as ObjectPool<T>).Push(obj);
			}
			else
			{
				Debug.LogError("不存在对象池 " + obj);
			}
        }
    }
    public abstract class PoolObject<T>:IPoolObject where T : PoolObject<T>,new()
    {
        internal static ObjectPool<T> _pool;
        public static ObjectPool<T> Pool
        {
            get
            {
                if (_pool == null)
                {
                   _pool= QPoolManager.GetPool(typeof(T).FullName, () => new T());
                }
                return _pool;
            }
        }
        public static T Get()
        {
            lock (Pool)
            {
                return Pool.Get();
            }
        }
        public static void Push(T obj)
        {
			lock (Pool)
			{
				Pool.Push(obj);
			}
        }
        public void Recover()
        {
            Push(this as T);
        }
        public abstract void OnPoolRecover();
        public abstract void OnPoolReset();
    }
    public interface IPoolObject
    {
        void OnPoolReset();
        void OnPoolRecover();
    }
    public abstract class PoolBase
    {
        public string Key { get; set; }
        public override string ToString()
        {
            var type = GetType();
            return "对象池["+ Key + "](" + type.Name + (type.IsGenericType ? "<" + type.GenericTypeArguments[0] + ">":"")+")";
        }
    }
    public class ObjectPool<T> : PoolBase where T : class
    {
        public readonly List<T> UsingPool = new List<T>();
        public readonly List<T> CanUsePool = new List<T>();
        public int AllCount
        {
            get
            {
                return UsingPool.Count + CanUsePool.Count;
            }
        }
        T CheckGet(T obj)
        {

            if (isCom || isGameObject)
            {
                if ((obj as T).Equals(null))
                {
					lock (UsingPool)
					{
						UsingPool.Remove(obj);
					}
                    obj = PrivateGet();
                }
                var gameObj = GetGameObj(obj);
                if (gameObj != null)
                {
                    gameObj.SetActive(true);
                    foreach (var poolObj in gameObj.GetComponents<IPoolObject>())
                    {
                        poolObj.OnPoolReset();
                    }
                }
            }
            else if (isPoolObj) (obj as IPoolObject).OnPoolReset();
			lock (UsingPool)
			{
				UsingPool.AddCheckExist(obj);
			}
            return obj;
        }
		Transform _poolParent = null;	
		public Transform PoolParent
		{
			get
			{
				if (_poolParent == null)
				{
					_poolParent = QToolManager.Instance.transform.GetChild("QPoolManager." + Key,true);
				}
				return _poolParent;
			}
		}
        T CheckPush(T obj)
        {
            var gameObj = GetGameObj(obj);
            if (gameObj != null)
            {
                gameObj.SetActive(false);
                gameObj.transform.SetParent(PoolParent, false);
                foreach (var poolObj in gameObj.GetComponents<IPoolObject>())
                {
                    poolObj.OnPoolRecover();
                }
            }
            else if (isPoolObj)
            {
                (obj as IPoolObject).OnPoolRecover();
            }
			lock (UsingPool)
			{
				UsingPool.Remove(obj);
			}
            return obj;
        }
        private T PrivateGet()
        {
			if (CanUsePool.Count > 0)
			{
				lock (CanUsePool)
				{
					var obj = CanUsePool.Dequeue();
					QDebug.ChangeProfilerCount(Key + " UseCount", AllCount - CanUseCount);
					return CheckGet(obj);
				}
			}
			else
			{
				if (newFunc == null)
				{
					throw new Exception("对象池创建函数为空  " + this);
				}
				var obj = newFunc();
				QDebug.ChangeProfilerCount(Key + " "+nameof(AllCount), AllCount);
				return CheckGet(obj);
			}
        }
		public T Get(T obj = null)
		{

			if (obj != null && CanUsePool.Contains(obj))
			{
				lock (CanUsePool)
				{
					CanUsePool.Remove(obj);
				}
				return CheckGet(obj);
			}
			else
			{
				return PrivateGet();
			}
		}
        GameObject GetGameObj(T obj)
        {
            if (isGameObject)
            {
                return obj as GameObject;
            }
            else if (isCom)
            {
                return (obj as Component)?.gameObject;
            }
            return null;
        }
        public void Push(T obj)
        {
			lock (this)
			{

				if (obj == null || CanUsePool.Contains(obj)) return;
				if (!UsingPool.Contains(obj))
				{
					var gameObj = GetGameObj(obj);
					if (Application.isPlaying)
					{
						GameObject.Destroy(gameObj);
						Debug.LogWarning("物体[" + obj + "]对象池[" + Key + "]中并不存在 无法回收强制删除");
					}
					return;
				}
				var resultObj = CheckPush(obj);
				lock (CanUsePool)
				{
					CanUsePool.Enqueue(resultObj);
				}
				QDebug.ChangeProfilerCount(Key + " UseCount" , AllCount-CanUseCount);
			}
        }
        public int CanUseCount
        {
            get
            {
                return CanUsePool.Count;
            }
        }
        public void Clear()
        {
			lock (UsingPool)
			{
				UsingPool.Clear();
			}
			lock (CanUsePool)
			{
				CanUsePool.Clear();
			}
			QDebug.ChangeProfilerCount(Key + " " + nameof(AllCount), AllCount);
			QDebug.ChangeProfilerCount(Key + " UseCount", AllCount - CanUseCount);
		}

        public Func<T> newFunc;
        public bool isPoolObj = false;
        public bool isCom = false;
        public bool isGameObject = false;
        public ObjectPool(string poolName,Func<T> newFunc=null)
        {
            var type = typeof(T);
            isPoolObj = typeof(IPoolObject).IsAssignableFrom(type);
            isCom = type.IsSubclassOf(typeof(Component));
            isGameObject = type == typeof(GameObject);
            this.newFunc = newFunc;
            this.Key = poolName;
        }
    }
}
