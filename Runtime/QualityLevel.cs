using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class QualityLevelObject
{
    public int minLevel=0;
    public int maxLevel=0;
    public GameObject obj;
    public bool InLevel(int level)
    {
        return level >= minLevel && level<= maxLevel;
    }
}
[ExecuteInEditMode]
public class QualityLevel : MonoBehaviour
{
    public List<QualityLevelObject> levelObj = new List<QualityLevelObject>();
    public int curLevel=-1;
    private void OnValidate()
    {
        Fresh();
    }
    public void CheckFresh()
    {
        if (curLevel != QualitySettings.GetQualityLevel())
        {
            Fresh();
        }
    }
    void Fresh()
    {
        curLevel = QualitySettings.GetQualityLevel();
        for (int i = 0; i < levelObj.Count; i++)
        {
            if (levelObj[i].obj != null)
            {
                levelObj[i].obj?.SetActive(levelObj[i].InLevel(curLevel));
            }
        }
    }
    private void LateUpdate()
    {
        CheckFresh();
    }
}
