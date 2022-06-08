using QTool.Inspector;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace QTool
{
	public class QAnalysisTest : MonoBehaviour
	{

		[Range(1,100)]
		public int idRange=1;
		[ViewButton("开始")]
		public void Login()
		{
			QAnalysis.Start("TestAccount"+ "_" + Random.Range(1, idRange)); 
		}

		[ViewButton("战斗模拟")]
		public async Task Fight()
		{
			for (int level = 0; level < 10; level++)
			{
				QAnalysis.Trigger("战斗开始", "Level-" + level);
				for (int i = 0; i < 10; i++)
				{
					await Task.Delay(50);
					QAnalysis.Trigger("战斗开始_使用技能", "技能-" + i);
					QAnalysis.Trigger("战斗开始_获得分数",10+i);
				}
				QAnalysis.Trigger("战斗结束",level<5);
			}
		}
		[ViewButton("结束")]
		public async void Logout()
		{
			await QAnalysis.Stop();
		}
		[ViewButton("大量测试")]
		public async void AllTest()
		{
			for (int id = 0; id < idRange; id++)
			{
				QAnalysis.Start("AllTest" + "_" +id);
				await Fight();
				await QAnalysis.Stop();
				Debug.LogError(id+1 + "/" + idRange);
			} 
		}
	}

}
