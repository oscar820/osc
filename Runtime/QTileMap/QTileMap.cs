using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
using System.Xml.Serialization;
namespace QTool.TileMap
{
    using Pos = Vector2Int;
    interface IMergeTile
    {
         PosList List { get; }
        bool Merge(PosList objects);
        void UnMergeAll();
    }
    public abstract class MergeTile<T>:TileBase , IMergeTile where T:MergeTile<T>
    {
        public void UnMergeAll()
        {
            if (posList == null || posList.Count == 0) return;
            foreach (var item in posList)
            {
                if (item == null || item.Value == null) continue;
                var mergeTile= item.Value.GetComponent<T>();
                if (mergeTile == null) continue;
                mergeTile.UnMerge();
                mergeTile.posList = null;
            }
        }
        public abstract void UnMerge();
        protected Bounds bounds = new Bounds();
        public PosList List { get => posList; }
        [ReadOnly]
        public PosList posList = new PosList();
        public virtual bool Merge(PosList objects)
        {
            if (objects.Count > 1)
            {
                if (posList.Count == objects.Count&&posList[0].Value== objects[0].Value)
                {
                    return false;
                }
                UnMergeAll();
                bounds = new Bounds(transform.position, Vector3.zero);
                this.posList = objects;
                for (int i = 0; i < posList.Count; i++)
                {
                    bounds.Encapsulate(posList[i].Value.position);
                }
                return true;
            }
            return false;
        }
    }
  
    public class TileBrush:QTool.Resource.PrefabResourceList<TileBrush>
    {

    }
    public class ObjectBrush : QTool.Resource.PrefabResourceList<ObjectBrush>
    {

    }
    [System.Serializable]
    public class PosObject : IKey<Pos>
    {
        [XmlElement("位置")]
        public Pos Key { get => _key; set => _key = value; }
        [XmlElement("预制体")]
        public string prefabKey { get; set; }
        [XmlIgnore]
        public Pos _key;

        [XmlIgnore]
        public Transform Value;
        public void SetValue(string brushKey, Transform value)
        {
            this.prefabKey = brushKey;
            this.Value = value;
        }
    }
    [System.Serializable]
    public class PosList: QAutoList<Pos, PosObject>
    {
       
    }
    public class TileMapData:QTool.Data.QData<TileMapData>
    {
        [XmlArray("地图信息")]
        public List<PosList> posList = new List<PosList>();
        [XmlArray("融合地板信息")]
        public QDictionary<Pos, PosList> mergeTileList = new QDictionary<Pos,PosList>();
    }
    public class QTileMap : MonoBehaviour
    {

        #region 基础属性
        [HideInInspector]
        public PosList tileList { get => mapData.posList[EditorModeIndex]; }
        [HideInInspector]
        public TileMapData mapData = new TileMapData();
        [HideInInspector]
        public Bounds Bounds = new Bounds();
      
        public GameObject tileBrush
        {
            get
            {
                if (PrefabIndex < 0 || curBrushList.Count <= PrefabIndex)
                {
                    return null;
                }
                return curBrushList[PrefabIndex];
            }
        }
        public void ClearAll()
        {
            foreach (var list in mapData.posList)
            {
                foreach (var kv in list)
                {
                    if (kv.Value != null)
                    {
                        SetPrefab(list, kv.Key, null, false);
                    }
                }
            }
        }
        public void Load(TileMapData newData)
        {
            ClearAll();
            for (int i = 0; i < newData.posList.Count; i++)
            {
                foreach (var kv in newData.posList[i])
                {
                    SetPrefab(mapData.posList[i], kv.Key, GetBrush(i, kv.prefabKey), false);
                }
            }
            foreach (var mergeList in newData.mergeTileList)
            {
                if (mergeList.Value.Count<=1) continue;
                var posList = new List<Pos>();
                foreach (var posObj in mergeList.Value)
                {
                    posList.Add(posObj.Key);
                }
                SetPrefab(mapData.posList[0], posList, GetBrush(0, mergeList.Value[0].prefabKey));
            }
            CheckAllTileBorder();
        }

        //public string save;
        //[ViewButton("保存测试")]
        //public void SaveTest()
        //{
        //    save = FileManager.Serialize(mapData);
        //}
        //[ViewButton("加载测试")]
        //public void LoadTest()
        //{
        //    Load(FileManager.Deserialize<TileMapData>(save));

        //}

        public bool ContainsTile(Pos pos)
        {
            return tileList.ContainsKey(pos)&&tileList[pos].Value!=null;
        }
      
        [ViewToggle("打开编辑模式")]
        public bool EditorMode;
        [ChangeCall("HideChild")]
        [ViewToggle("显示子物体", height = 20, showControl = "EditorMode")]
        public bool showChild = false;
     
        [ToolbarList("editorMode", showControl = "EditorMode")]
        public int EditorModeIndex;
        [ToolbarList("curBrushList", showControl = "EditorMode", pageSize = 15, name = "笔刷")]
        public int PrefabIndex;
        [ToolbarList("tileBurshMode", showControl = "EditorMode")]
        public int tileBrushModeIndex;
        public static List<string> editorMode = new List<string> { "地板", "物体", };
        public virtual List<string> tileBurshMode { get; set; } = new List<string> { "画笔", "框选", };
        List<GameObject> curBrushList {
            get
            {
                return allBrushList[EditorModeIndex];
            }
        }
        List<List<GameObject>> allBrushList = new List<List<GameObject>>();

        public GameObject GetBrush(int brushType,string key)
        {
            if (string.IsNullOrWhiteSpace(key)) { return null; }
            foreach (var item in allBrushList[brushType])
            {
                if (item.name == key)
                {
                    return item;
                }
            }
            return null;
        }
        [ViewName("地板块大小")]
        public Vector2 tilePrefabSize = Vector2.one * 2;
        #endregion
        public Pos GetPos(Vector3 vector3)
        {
            return new Pos { x = Mathf.FloorToInt(vector3.x / tilePrefabSize.x), y = Mathf.FloorToInt(vector3.z / tilePrefabSize.y) };
        }
        public Vector3 GetPosition(int x, int y)
        {
            return GetPosition(new Pos { x = x, y = y });
        }
        public Vector3 GetPosition(Pos pos)
        {
            return new Vector3((pos.x + 0.5f) * tilePrefabSize.x, 0, (pos.y + 0.5f) * tilePrefabSize.y);
        }
        public Vector3 Fix(Vector3 vector3)
        {
            return GetPosition(GetPos(vector3));
        }
        public void HideChild()
        {
            foreach (var list in mapData.posList)
            {
                foreach (var obj in list)
                {
                    if (obj.Value != null)
                    {
                        obj.Value.hideFlags = showChild ? HideFlags.None : HideFlags.HideInHierarchy;
                    }
                }
            }
        }
      
        [ViewButton("刷新笔刷",20,showControl ="EditorMode")]
        public virtual void FreshBrush()
        {
            TileBrush.Clear();
            TileBrush.Clear();
            InitBrush();
        }
        public virtual void InitBrush()
        {
            TileBrush.LoadOverRun(() =>
            {
                foreach (var kv in TileBrush.objDic)
                {
                    allBrushList[0].AddCheckExist(kv.Value);
                }
            });
            ObjectBrush.LoadOverRun(() =>
            {
                foreach (var kv in ObjectBrush.objDic)
                {
                    allBrushList[1].AddCheckExist(kv.Value);
                }
            });
        }
      
        public int GetBrushType(GameObject obj,out string prefabKey)
        {
            GameObject prefab =obj.GetPrefab();
            for (int i = 0; i < allBrushList.Count; i++)
            {
                if (prefab == null)
                {
                    foreach (var brush in allBrushList[i])
                    {

                        if (obj.name.Contains(brush.name))
                        {
                            prefabKey = brush.name;
                            return i;
                        }
                    }
                }
                else if(allBrushList[i].Contains(prefab))
                {
                    prefabKey = prefab.name;
                    return i;
                }
            }
            prefabKey = "";
            return -1;
        }
        [EidtorInitInvoke]
        public void EditorInit()
        {
            EditorMode = false;
            Bounds = new Bounds();
            mapData.mergeTileList.Clear();
            mapData.posList.Clear();
            allBrushList.Clear();
            for (int i = 0; i < editorMode.Count; i++)
            {
                allBrushList.Add(new List<GameObject>());
                mapData.posList.Add(new PosList());
            }
            InitBrush();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var key = "";
                var type = GetBrushType(child.gameObject,out key);
                if (type >= 0)
                {
                    var pos = GetPos(child.position);
                    child.name = key;
                    mapData.posList[type][pos].SetValue(key, child);
                    Bounds.Encapsulate(child.position);
                }
            }
            foreach (var tile in mapData.posList[0])
            {
                if (tile.Value == null) continue;
                var mergeTile = tile.Value.GetComponent<IMergeTile>();
                if (mergeTile != null&&mergeTile.List!=null&&mergeTile.List.Count>1)
                {
                    if (!mapData.mergeTileList.ContainsKey(mergeTile.List[0].Key))
                    {
                        mapData.mergeTileList[mergeTile.List[0].Key] =mergeTile.List;
                    }
                }
            }

            CheckAllTileBorder();
           
        }
        private void Awake()
        {
            EditorInit();
        }
        public GameObject CheckInstantiate(GameObject prefab, Vector3 position, Transform parent)
        {

            var obj = this.CheckInstantiate(prefab, parent);
            obj.transform.position = position;
            return obj;
        }
       
        public static Pos[] nearPosOffset = new Pos[]
        {
            new Pos(0,1),new Pos(1,1),new Pos(1,0),new Pos(1,-1),new Pos(0,-1),new Pos(-1,-1),new Pos(-1,0),new Pos(-1,1)
        };

     //   [ViewButton("更新所有地板边界", 40, "EditorMode")]
        public async void CheckAllTileBorder()
        {
            foreach (var kv in tileList)
            {
                  CheckBorder(kv.Key, false);
            }
            await System.Threading.Tasks.Task.Delay(200);
            foreach (var kv in tileList)
            {
                if (kv.Value != null)
                {
                    var borderTile = kv.Value.GetComponent<BorderTile>();
                    if (borderTile != null)
                    {
                        borderTile.MergeBoder();
                    }

                }
            }
            foreach (var kv in tileList)
            {
                if (kv.Value != null)
                {
                    var borderTile = kv.Value.GetComponent<BorderTile>();
                    if (borderTile != null)
                    {
                        borderTile.ClearBorderCheck();
                    }

                }
            }


        }

        public void CheckBorder(Pos pos, bool checkNear = true)
        {
            BorderTile borderTile = null;
            if (ContainsTile(pos))
            {
                borderTile = tileList[pos].Value.GetComponent<BorderTile>();
            }
            List<BorderType> borderInfo = new List<BorderType>();
            foreach (var offset in nearPosOffset)
            {
                var targetPos = offset + pos;
                if (borderTile != null)
                {
                    if (!tileList.ContainsKey(targetPos)|| tileList[targetPos].Value==null)
                    {
                        borderInfo.Add(BorderType.空地);
                    }
                    else
                    {
                        var nearBorderTile = tileList[targetPos].Value.GetComponent<BorderTile>();
                        borderInfo.Add((nearBorderTile==null|| borderTile.setting != nearBorderTile.setting ) ? BorderType.不同地板:BorderType.相同地板);
                    }
                }
                if (checkNear) { CheckBorder(targetPos, false); }
            }
            if (borderTile != null)
            {
                borderTile.UpdateBorder(borderInfo.ToArray());
            }
        }
        public void SetPrefab(QAutoList<Pos,PosObject> canvas,IList<Pos> posList,GameObject brush, bool checkMerge = true)
        {
            var mergetTileList = new PosList();
            if (brush!=null)
            {
                foreach (var pos in posList)
                {
                    mergetTileList[pos].SetValue( brush.name, SetPrefab(canvas,pos,brush,checkMerge).transform);
                }
                if (mergetTileList.Count > 1)
                {
                    foreach (var merge in mergetTileList)
                    {
                        var mergeTile = merge.Value.GetComponent<IMergeTile>();
                        if (mergeTile != null)
                        {
                            mergeTile.Merge(mergetTileList);
                            mapData.mergeTileList[mergetTileList[0].Key] = mergetTileList;
                        }
                    }
                }
            }
            else
            {
                foreach (var pos in posList)
                {
                    SetPrefab(canvas, pos, null, checkMerge);
                }
            }
        }
        public GameObject SetPrefab(QAutoList<Pos, PosObject> dic, Pos pos, GameObject prefab, bool checkMerge = true)
        {
            var obj = prefab == null ? null : CheckInstantiate(prefab, GetPosition(pos), transform);
            if (obj != null)
            {
                obj.hideFlags = showChild ? HideFlags.None : HideFlags.HideInHierarchy;
            }
            if (dic[pos].Value!=null)
            {
              
                var mergeTile = dic[pos].Value.GetComponent<IMergeTile>();
                if (mergeTile != null)
                {
                    if (mergeTile.List != null && mergeTile.List.Count > 0)
                    {
                        mapData.mergeTileList.RemoveKey(mergeTile.List[0].Key);
                    }
                    mergeTile?.UnMergeAll();
                }
              
                this.CheckDestory(dic[pos].Value.gameObject);
             
            }
            if (obj != null)
            {
                dic[pos].SetValue(prefab.name, obj.transform);
                Bounds.Encapsulate(obj.transform.position);
            }
            else
            {
                dic[pos].SetValue("",null);
            }
            CheckBorder(pos);
            return obj;
        }
        bool selectBox = false;
      //  List<Pos> selectPosList = new List<Pos>();
        Pos startPos;
        [SceneMouseEvent(EventType.MouseDown)]
        public void MouseDown(Vector3 position, RaycastHit hit, bool shift)
        {
            if (!EditorMode) return;
            selectBox = tileBrushModeIndex == 1;
            if (!selectBox)
            {
                Brush(position, hit, shift);
            }
            else
            {
                startPos = GetPos(position);
            }

        }
        
        [SceneMouseEvent(EventType.MouseUp)]
        public void MouseUp(Vector3 position, RaycastHit hit, bool shift)
        {
            if (!EditorMode) return;
            if (selectBox)
            {
                selectBox = false;
                Draw(selectList, shift);
            }

        }
        public List<Pos> GetBoxPos(Pos start, Pos end)
        {
            var posList = new List<Pos>();
            var minX = Mathf.Min(start.x, end.x);
            var maxX = Mathf.Max(start.x, end.x);
            var minY = Mathf.Min(start.y, end.y);
            var maxY = Mathf.Max(start.y, end.y);
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    posList.Add(new Pos { x = x, y = y });

                }
            }
            return posList;
        }
        public void Draw(List<Pos> listPos,bool clear=false)
        {
            SetPrefab(tileList, listPos, clear?null:tileBrush);
        }
        public GameObject Draw(Pos pos,bool clear=false)
        {
            return SetPrefab(tileList, pos, clear?null:tileBrush);
        }
         Pos curPos;
        [SceneMouseEvent(EventType.MouseMove)]
        public void MouseMove(Vector3 position, RaycastHit hit, bool shift)
        {
            if (!EditorMode) return;
            curPos = GetPos(position);
        }
         List<Pos> selectList = new List<Pos>();
        [SceneMouseEvent(EventType.MouseDrag)]
        public void Brush(Vector3 position, RaycastHit hit, bool shift)
        {
            if (!EditorMode) return;
            selectList.Clear();
            if (selectBox)
            {
                selectList = GetBoxPos(startPos, GetPos(position));
              
            }
            else
            {
                curPos = GetPos(position);
                Draw(curPos, shift);
            }
        }
        private void OnDrawGizmosSelected()
        {
            if (tileBrush == null)
            {
                return;
            }
            Gizmos.color = Color.HSVToRGB(((tileBrush.GetHashCode()%100)*1f/100) ,0.5f,0.8f);
            Gizmos.DrawWireCube(GetPosition(curPos), new Vector3(tilePrefabSize.x, 0, tilePrefabSize.y));
            if (selectBox)
            {
            
                foreach (var pos in selectList)
                {
                    Gizmos.DrawWireCube(GetPosition(pos), new Vector3(tilePrefabSize.x, 0, tilePrefabSize.y));
                }
            }
        }
    }
}