using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
using System.Xml.Serialization;
namespace QTool.TileMap
{
    using Pos = Vector2Int;
    public interface IMergeTile
    {
        void UnMerge();
        void Merge(GameObject[] objects, Vector3 startPosition, Vector3 endPosition);
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
        public Pos Key { get=>_key; set=>_key=value; }
        [XmlElement("预制体")]
        public string prefabKey { get; set; }
        [XmlIgnore]
        public Pos _key;

        [XmlIgnore]
        public Transform Value;
    }
    [System.Serializable]
    public class PosList: QAutoList<Pos, PosObject>
    {
    }
    public class TileMapData:QTool.Data.QData<TileMapData>
    {
        [XmlArray("地图信息")]
        public List<PosList> posList = new List<PosList>();
    }
    public class QTileMap : MonoBehaviour
    {

        #region 基础属性
        [HideInInspector]
        public PosList tileList { get => mapData.posList[EditorModeIndex]; }
        [HideInInspector]
        public TileMapData mapData = new TileMapData();
        [HideInInspector]
        public int left = int.MaxValue;
        [HideInInspector]
        public int right = int.MinValue;
        [HideInInspector]
        public int down = int.MaxValue;
        [HideInInspector]
        public int up = int.MinValue;
        public float Left
        {
            get
            {
                return left * tilePrefabSize.x;
            }
        }
        public float Right
        {
            get
            {
                return right * tilePrefabSize.x;
            }
        }
        public float Down
        {
            get
            {
                return down * tilePrefabSize.y;
            }
        }
        public float Up
        {
            get
            {
                return up * tilePrefabSize.y;
            }
        }
      
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
            CheckAllTileBorder();
        }
        //[ViewButton("保存")]
        //public void SaveTest()
        //{
        //    save = FileManager.Serialize(mapData);
        //}
        //[ViewButton("加载测试")]
        //public void LoadTest()
        //{
        //    Load(FileManager.Deserialize<TileMapData>( save));
          
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
        public void ChangeSize(Pos pos)
        {
            if (pos.x < left)
            {
                left = pos.x;
            }
            if (pos.x > right)
            {
                right = pos.x;
            }
            if (pos.y < down)
            {
                down = pos.y;
            }
            if (pos.y > up)
            {
                up = pos.y;
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
            left = int.MaxValue;
            right = int.MinValue;
            down = int.MaxValue;
            up = int.MinValue;
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
                    mapData.posList[type][pos].Value = child;
                    mapData.posList[type][pos].prefabKey= key;
                    ChangeSize(pos);
                }
            }

            CheckAllTileBorder();
           
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
        public GameObject SetPrefab(QAutoList<Pos, PosObject> dic, Pos pos, GameObject prefab, bool checkMerge = true)
        {
            ChangeSize(pos);
            var obj = prefab == null ? null : CheckInstantiate(prefab, GetPosition(pos), transform);
            if (obj != null)
            {
                obj.hideFlags = showChild ? HideFlags.None : HideFlags.HideInHierarchy;
            }
            if (dic[pos].Value!=null)
            {

                dic[pos].Value.GetComponent<IMergeTile>()?.UnMerge();
                this.CheckDestory(dic[pos].Value.gameObject);
                if (obj != null)
                {
                    dic[pos].prefabKey = prefab.name;
                    dic[pos].Value = obj.transform;
                }
                else
                {
                    dic[pos].prefabKey = "";
                    dic[pos].Value = null;
                }

            }
            else
            {
                if (obj != null)
                {
                    dic[pos].prefabKey = prefab.name;
                    dic[pos].Value = obj.transform;
                }
            }
            CheckBorder(pos);
            return obj;
        }
        bool selectBox = false;
        List<Pos> selectPosList = new List<Pos>();
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
         List<GameObject> mergetTileList = new List<GameObject>();
        [SceneMouseEvent(EventType.MouseUp)]
        public void MouseUp(Vector3 position, RaycastHit hit, bool shift)
        {
            if (!EditorMode) return;
            //   selectBox = tileBrushModeIndex == 1;
            if (!selectBox)
            {
                //  Brush(position, hit, shift);
            }
            else
            {

                selectBox = false;
                var endPos = GetPos(position);
                selectPosList = GetBoxPos(startPos, endPos);
                mergetTileList.Clear();
               
                if (!shift)
                {
                    foreach (var pos in selectPosList)
                    {
                        Draw(pos);
                        mergetTileList.Add(Draw(pos));
                      
                    }
                    if (mergetTileList.Count > 0)
                    {
                        mergetTileList[0]?.GetComponent<IMergeTile>()?.Merge(mergetTileList.ToArray(), GetPosition(startPos), GetPosition(endPos));
                    }
                }
                else
                {
                    foreach (var pos in selectPosList)
                    {
                        Clear(pos);
                    }

                }
                selectPosList.Clear();
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
        public void Clear(Pos pos)
        {
            SetPrefab(tileList, pos, null);
        }
        public GameObject Draw(Pos pos)
        {
            GameObject obj = null;
            if (tileBrush != null)
            {
                obj = SetPrefab(tileList, pos, tileBrush);
            }
            //    StartState(obj);
            return obj;

        }
         Pos curPos;
        [SceneMouseEvent(EventType.MouseMove)]
        public void MouseMove(Vector3 position, RaycastHit hit, bool shift)
        {
            if (!EditorMode) return;
            curPos = GetPos(position);
        }
        [SceneMouseEvent(EventType.MouseDrag)]
        public void Brush(Vector3 position, RaycastHit hit, bool shift)
        {
            if (!EditorMode) return;
            if (selectBox)
            {
                selectPosList = GetBoxPos(startPos, GetPos(position));
            }
            else
            {
                curPos = GetPos(position);
                if (!shift)
                {
                    Draw(curPos);
                }
                else
                {
                    Clear(curPos);
                }

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
                foreach (var pos in selectPosList)
                {
                    Gizmos.DrawWireCube(GetPosition(pos), new Vector3(tilePrefabSize.x, 0, tilePrefabSize.y));
                }
            }
        }
    }
}