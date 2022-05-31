using QTool.Inspector;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace QTool
{
	public class QAnalysisTest : MonoBehaviour
	{		
	
	
		[ViewButton("开始")]
		public void Login()
		{
			QAnalysis.Start("TestAccount"+ "_" + Random.Range(1, 10)); 
		}

		[ViewButton("战斗模拟")]
		public async void Fight()
		{
			QAnalysis.Trigger("战斗开始","Level-"+Random.Range(1,10));
			for (int i = 0; i < Random.Range(10,30); i++)
			{
				await Task.Delay(Random.Range(1000,3000));
				QAnalysis.Trigger("使用技能", "技能-" + Random.Range(1, 4));
			}
			await Task.Delay(Random.Range(1000, 3000));
			QAnalysis.Trigger("战斗结束",Random.Range(0,100)<80);
		}
		[ViewButton("结束")]
		public void Logout()
		{
			QAnalysis.Stop();
		}
	}

}
