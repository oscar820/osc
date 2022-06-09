using QTool.Inspector;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace QTool
{
	public class QAnalysisTest : MonoBehaviour
	{

		public StringEvent OnOverInfo;
		[Range(1,10000)]
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
				await Task.Delay(10);
				QAnalysis.Trigger("战斗开始", "Level-" + level);
				for (int i = 0; i < 10; i++)
				{
					await Task.Delay(10);
					QAnalysis.Trigger("战斗开始_使用技能", "技能-" + i);
					await Task.Delay(10);
					QAnalysis.Trigger("战斗开始_获得分数",10+i);
					await Task.Delay(10);
				}
				QAnalysis.Trigger("战斗结束",level<5);
				await Task.Delay(10);
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
			for (int id = 0; id < idRange&&Application.isPlaying; id++)
			{
				QAnalysis.Start("AllTest" + "_" +id);
				OnOverInfo.Invoke(id + 1 + "/" + idRange + " 战斗开始");
				await Task.Delay(10);
				await Fight();
				await QAnalysis.Stop();
				await Task.Delay(10);
				OnOverInfo.Invoke(id + 1 + "/" + idRange + " 战斗结束");
			} 
		}
	}

}
