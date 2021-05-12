using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
namespace QTool.TileMap
{
    using Pos = Vector2Int;
    //public interface ITileMapObj
    //{
    //    void StartState();
    //}
    //public interface MergeTile
    //{
    //    void Destory();
    //}
    //public interface BorderTile
    //{
    //    void MergeBoder();
    //    void ClearBorderCheck();
    //}
    public class TileAsset:QTool.Resource.PrefabResourceList<TileAsset>
    {

    }
    [System.Serializable]
    public class PosObject : IKey<Pos>
    {
        public Pos Key { get=>_key; set=>_key=value; }
        public Pos _key;
        public Transform Value;
    }
    [System.Serializable]
    public class PosList: QAutoList<Pos, PosObject>
    {
    }
    public class QTileMap : MonoBehaviour
    {
        #region 基础属性
       // [HideInInspector]
        public PosList tileList = new PosList();
       // [HideInInspector]
        public PosList objList = new PosList();

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
                if (PrefabIndex < 0 || tileBrushList.Count <= PrefabIndex)
                {
                    return null;
                }
                return tileBrushList[PrefabIndex];
            }
        }
        public GameObject objBrush
        {
            get
            {
                if (ObjIndex < 0 || objBrushList.Count <= ObjIndex)
                {
                    return null;
                }
                return objBrushList[ObjIndex];
            }
        }
        public bool TileMode
        {
            get
            {
                return (EditorIndex == 0) && EditorMode;
            }
        }
        public bool ObjMode
        {
            get
            {
                return (EditorIndex == 1) && EditorMode;
            }
        }
        public bool EditorMode
        {
            get
            {
                return isEditorMap;
            }
        }
        public bool ContainsTile(Pos pos)
        {
            return tileList.ContainsKey(pos);
        }
        [ViewToggle("打开编辑模式")]
        public bool isEditorMap;
        [ToolbarList("editorMode", showControl = "EditorMode")]
        public int EditorIndex;

        [ToolbarList("tileBrushList", showControl = "TileMode")]
        public int PrefabIndex;
        [ToolbarList("objBrushList", showControl = "ObjMode")]
        public int ObjIndex;
        [ToolbarList("tileBurshMode", height = 40, showControl = "EditorMode")]
        public int tileBrushModeIndex;

        public static List<string> editorMode = new List<string> { "地板", "物体", };
        public static List<string> tileBurshMode = new List<string> { "画笔", "框选", };
        [HideInInspector]
        public List<GameObject> tileBrushList = new List<GameObject>();
        [HideInInspector]
        public List<GameObject> objBrushList = new List<GameObject>();

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
        public void CheckAddBrush()
        {

#if UNITY_EDITOR
            TileAsset.LoadOverRun(() =>
            {
                foreach (var kv in TileAsset.objDic)
                {
                    tileBrushList.AddCheckExist(kv.Value);
                }
            });
            tileBrushList.RemoveAll((obj) => obj == null);
            foreach (var item in tileList)
            {
                var brush = item.Value.gameObject.GetPrefab();
                if (brush != null && !tileBrushList.Contains(brush))
                {

                    tileBrushList.Add(brush);
                }
            }
            objBrushList.RemoveAll((obj) => obj == null);
            foreach (var item in objList)
            {
                var brush = item.Value.gameObject.GetPrefab();
                if (brush != null && !objBrushList.Contains(brush))
                {

                    objBrushList.Add(brush);
                }
            }
#endif
        }
        [ViewButton("重新加载引用")]
        [EidtorInitInvoke]
        public void EditorInit()
        {
            isEditorMap = false;
            left = int.MaxValue;
            right = int.MinValue;
            down = int.MaxValue;
            up = int.MinValue;
            tileList.Clear();
            objList.Clear();
            var destoryList = new List<GameObject>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);

                var pos = GetPos(child.position);
                if (tileList.ContainsKey(pos))
                {

                    if (objList.ContainsKey(pos))
                    {
                        destoryList.Add(objList[pos].Value.gameObject);
                    }
                    if (child.gameObject.GetBounds().center.y <= tileList[pos].Value.gameObject.GetBounds().center.y)
                    {
                        objList[pos] = tileList[pos];
                        tileList[pos].Value = child;

                    }
                    else
                    {
                        objList[pos].Value = child;
                    }
                }
                else
                {
                    tileList.Add(new PosObject { Key = pos, Value = child });

                }
                ChangeSize(pos);
            }

            foreach (var obj in destoryList)
            {
               this.CheckDestory(obj);
            }
            CheckAddBrush();
            this.SetDirty();

        }

        [SelectObjectButton("添加笔刷", showControl = "EditorMode")]
        public void AddBrush(GameObject obj)
        {
   
            if (TileMode)
            {
                if (tileBrushList.Contains(obj)) return;
                    tileBrushList.Add(obj);
                PrefabIndex = tileBrushList.Count - 1;
            }
            else if (ObjMode)
            {
                if (objBrushList.Contains(obj)) return;
                objBrushList.Add(obj);
                ObjIndex = objBrushList.Count - 1;
            }

        }

        [ViewButton("删除笔刷", showControl = "EditorMode")]
        public void DeleteBrush()
        {
            if (TileMode)
            {
                if (PrefabIndex >= 0 && tileBrushList.Count > PrefabIndex)
                {
                    tileBrushList.RemoveAt(PrefabIndex);
                }
                PrefabIndex--;

            }
            else if (ObjMode)
            {
                if (ObjIndex >= 0 && objBrushList.Count > ObjIndex)
                {
                    objBrushList.RemoveAt(ObjIndex);
                }
                ObjIndex--;
            }


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
        //[ViewButton("更新所有初始状态", 40, "EditorMode")]
        //public void UpdateAllStartState()
        //{
        //    foreach (var kv in tileList)
        //    {
        //        var value = kv.Value;
        //        StartState(value.gameObject);
        //    }
        //    foreach (var kv in objList)
        //    {
        //        var value = kv.Value;
        //        StartState(value.gameObject);
        //    }
        //}
        //public void StartState(GameObject obj)
        //{
        //    if (obj == null) return;
        //    var tileObj = obj.GetComponentInChildren<ITileMapObj>();
        //    if (tileObj != null)
        //    {
        //        tileObj.StartState();
        //    }

        //}
        //[ViewButton("更新所有地板边界", 40, "EditorMode")]
        //public async void CheckAllTileBorder()
        //{
        //    foreach (var kv in tileList)
        //    {
        //      //  CheckBorder(kv.Key, false);
        //    }
        //    await System.Threading.Tasks.Task.Delay(200);
        //    foreach (var kv in tileList)
        //    {
        //        if (kv.Value != null)
        //        {
        //            var borderTile = kv.Value.GetComponent<BorderTile>();
        //            if (borderTile != null)
        //            {
        //                borderTile.MergeBoder();
        //            }

        //        }
        //    }
        //    foreach (var kv in tileList)
        //    {
        //        if (kv.Value != null)
        //        {
        //            var borderTile = kv.Value.GetComponent<BorderTile>();
        //            if (borderTile != null)
        //            {
        //                borderTile.ClearBorderCheck();
        //            }

        //        }
        //    }


        //}

        //public void CheckBorder(Pos pos, bool checkNear = true)
        //{
        //    BorderTile borderTile = null;
        //    if (ContainsTile(pos))
        //    {
        //        var obj = tileList[pos];


        //        borderTile = obj.GetComponent<BorderTile>();
        //        if (borderTile != null)
        //        {


        //        }



        //    }
        //    List<BorderType> borderInfo = new List<BorderType>();
        //    foreach (var offset in nearPosOffset)
        //    {
        //        var targetPos = offset + pos;
        //        BorderTile nearBorderTile = null;
        //        if (tileList.ContainsKey(targetPos))
        //        {
        //            nearBorderTile = tileList[targetPos].GetComponent<BorderTile>();
        //        }
        //        if (borderTile != null)
        //        {
        //            if (nearBorderTile == null)
        //            {
        //                borderInfo.Add(BorderType.空地);
        //            }
        //            else
        //            {
        //                borderInfo.Add(borderTile.setting == nearBorderTile.setting ? BorderType.相同地板 : BorderType.不同地板);
        //            }

        //        }

        //        if (checkNear) { CheckBorder(targetPos, false); }
        //    }
        //    if (borderTile != null)
        //    {
        //        borderTile.UpdateBorder(borderInfo.ToArray());
        //    }


        //}
        public GameObject SetPrefab(QAutoList<Pos, PosObject> dic, Pos pos, GameObject prefab, bool checkMerge = true)
        {
            ChangeSize(pos);
            var obj = prefab == null ? null : CheckInstantiate(prefab, GetPosition(pos), transform);

            if (dic[pos].Value!=null)
            {
                // var mergeTile = dic[pos].GetComponent<MergeTile>();
                //if (!checkMerge || mergeTile == null )
                //{
                this.CheckDestory(dic[pos].Value.gameObject);
                //}
                //else
                //{
                //    mergeTile.Destory();
                //}
                if (obj != null)
                {
                    dic[pos].Value = obj.transform;
                }
                else
                {
                    dic.Remove(pos);
                }

            }
            else
            {
                if (obj != null)
                {
                    dic[pos].Value = obj.transform;
                }
            }
            this.SetDirty();
         //   CheckBorder(pos);
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
     //   List<MergeTile> mergetTileList = new List<MergeTile>();
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
             //   mergetTileList.Clear();
                if (TileMode)
                {

                }
                if (!shift)
                {
                    foreach (var pos in selectPosList)
                    {
                        Draw(pos);
                       // var mergeTile = Draw(pos).GetComponent<MergeTile>();
                        //if (mergeTile != null)
                        //{
                        //    mergetTileList.Add(mergeTile);
                        //}
                    }
                 //   MergeTile.Merge(mergetTileList.ToArray(), GetPosition(startPos), GetPosition(endPos), tilePrefabSize);

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
            if (TileMode)
            {
                //     if (tileBrush != null)
                {
                    SetPrefab(objList, pos, null);
                    SetPrefab(tileList, pos, null);

                }
            }
            else if (ObjMode)
            {
                //   if (objBrush != null)
                {
                    SetPrefab(objList, pos, null);
                }
            }
        }
        public GameObject Draw(Pos pos)
        {
            GameObject obj = null;
            if (TileMode)
            {
                if (tileBrush != null)
                {
                    obj = SetPrefab(tileList, pos, tileBrush);
                }
            }
            else if (ObjMode)
            {
                if (objBrush != null)
                {
                    obj = SetPrefab(objList, pos, objBrush);
                }
            }
        //    StartState(obj);
            return obj;

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
                var pos = GetPos(position);
                if (!shift)
                {
                    Draw(pos);
                }
                else
                {
                    Clear(pos);
                }

            }
        }
        private void OnDrawGizmosSelected()
        {
            if (selectBox)
            {
                foreach (var pos in selectPosList)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireCube(GetPosition(pos), new Vector3(tilePrefabSize.x, 0, tilePrefabSize.y));
                }
            }
        }
    }
}