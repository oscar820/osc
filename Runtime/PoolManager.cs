using System;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
    public class ToolDebug : DebugBase<ToolDebug>
    {

    }
    public class DebugBase<T> where T:DebugBase<T>
    {
        static bool Init = false;
        static bool _show=false;
        public static bool ShowLog
        {
            get
            {
                if (!Init)
                {
                    Init = true;
                    _show = PlayerPrefs.GetInt(Key+ "日志", 0) == 1;
                }
                return _show;
            }
            set
            {
                if (!Init)
                {
                    Init = true;
                }
                _show = value;
                PlayerPrefs.SetInt(Key + "日志", value ? 1 : 0);
            }
        }
        public static string Key
        {
            get
            {
                return "【" + typeof(T).Name + "】";
            }
        }
        public static void Log(object log)
        {
            if (ShowLog)
            {
                Debug.Log(Key+":"+log);
            }
        }
        public static void Log(Func<object> log)
        {
            if (ShowLog)
            {
                Debug.Log(Key + ":" + log?.Invoke());
            }
        }
        public static void LogWarning(object log)
        {
            if (ShowLog)
            {
                Debug.LogWarning(Key + ":" + log);
            }
        }
        public static void LogWarning(Func<object> log)
        {
            if (ShowLog)
            {
                Debug.LogWarning(Key + ":" + log?.Invoke());
            }
        }
    }
    public static class PoolManager
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
                if(poolDic[key] is ObjectPool<T>)
                {
                    var pool= poolDic[key] as ObjectPool<T>;
                    if (newFunc!=null&& pool.newFunc == null)
                    {
                        pool.newFunc = newFunc;
                    }
                    return pool;
                }
                else
                {
                    throw new Exception("已存在重名不同类型对象池" + poolDic[key]);
                }
                    
            }else
            {
                var pool = new ObjectPool<T>(key,newFunc);
                poolDic[key]= pool;
                return pool;
            }
        }
        public static void Push(GameObject gameObject)
        {
            Push(gameObject.name, gameObject);
        }
        public static void Push<T>(string poolName, T obj) where T : class
        {
            var pool = GetPool<T>(poolName);
            pool?.Push(obj);
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
                   _pool= PoolManager.GetPool(typeof(T).FullName, () => new T());
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
            Pool.Push(obj);
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

            if (isMonobehaviour || isGameObject)
            {
                if ((obj as T).Equals(null))
                {
                
                        UsingPool.Remove(obj);
                    obj = Get();
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
                UsingPool.AddCheckExist(obj);
            return obj;
        }
        private static Dictionary<string, Transform> parentList = new Dictionary<string, Transform>();
        private static Transform GetPoolParent(string name)
        {
            if (!Application.isPlaying) return null;
            if (parentList.ContainsKey(name))
            {
                return parentList[name];
            }
            else
            {
                var poolName = name + "_QPool";
                var parent = new GameObject(poolName).transform;
                parentList.Add(name, parent);
                return parent;
            }
        }
        T CheckPush(T obj)
        {
            var gameObj = GetGameObj(obj);
            if (gameObj != null)
            {
                gameObj.SetActive(false);
                gameObj.transform.SetParent(GetPoolParent(Key),false);
                foreach (var poolObj in gameObj.GetComponents<IPoolObject>())
                {
                    poolObj.OnPoolRecover();
                }
            }
            else if (isPoolObj)
            {
                (obj as IPoolObject).OnPoolRecover();
            }
                UsingPool.Remove(obj);
            return obj;
        }
        public T Get()
        {

            if (CanUsePool.Count > 0)
            {
                    var obj = CanUsePool.Dequeue();
                    return CheckGet(obj);
            }
            else
            {
                if (newFunc == null)
                {
                    throw new Exception("对象池创建函数为空  " + this);
                }
                var obj = newFunc();
                ToolDebug.Log(() =>
                {
                    var info = "【" + Key + "】对象池当前池大小：" + AllCount + '\n';
                    foreach (var item in UsingPool)
                    {
                        info += "[" + item + "]" + item.GetType() + "|" + item.GetHashCode() + "\n";
                    }
                    return info;
                });
                return CheckGet(obj);
            }
        }
        public T Get(T obj)
        {

            if (CanUsePool.Contains(obj))
            {
                CanUsePool.Remove(obj);
                return CheckGet(obj);
            }
            else
            {
                return Get();
            }
        }
        GameObject GetGameObj(T obj)
        {
            if (isGameObject)
            {
                return obj as GameObject;
            }
            else if (isMonobehaviour)
            {
                return (obj as MonoBehaviour)?.gameObject;
            }
            return null;
        }
        public void Push(T obj)
        {
            if (obj==null||CanUsePool.Contains(obj)) return;
            if (!UsingPool.Contains(obj))
            {
                var gameObj = GetGameObj(obj);
                if (gameObj != null)
                {
                    GameObject.Destroy(gameObj);
                    Debug.LogWarning("物体[" + obj + "]对象池[" + Key + "]中并不存在 无法回收强制删除");
                }
                else
                {
                    Debug.LogWarning("对象[" + obj + "]对象池[" + Key + "]中不存在 无法回收");
                }
                return;
            }
            var resultObj = CheckPush(obj);
            CanUsePool.Enqueue(resultObj);
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
            UsingPool.Clear();
            CanUsePool.Clear();
        }

        public Func<T> newFunc;
        public bool isPoolObj = false;
        public bool isMonobehaviour = false;
        public bool isGameObject = false;
        public ObjectPool(string poolName,Func<T> newFunc=null)
        {
            var type = typeof(T);
            isPoolObj = typeof(IPoolObject).IsAssignableFrom(type);
            isMonobehaviour = type.IsSubclassOf(typeof(MonoBehaviour));
            isGameObject = type == typeof(GameObject);
            this.newFunc = newFunc;
            this.Key = poolName;
        }
    }
}