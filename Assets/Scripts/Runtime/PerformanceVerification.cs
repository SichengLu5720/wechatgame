using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
namespace StairsCrowd.Runtime
{
    public sealed class PerformanceVerification : MonoBehaviour
    {
        [Serializable] public sealed class Report
        {
            public int levels,renderersBeforePeople,renderersWithPeople;
            public double assemblyMeanMs,assemblyP95Ms,motionMeanMs,motionP95Ms,boardMeanMs,planningMeanMs;
            public int assemblySamples,motionSamples;
        }
        static double Mean(List<double> a){double sum=0;foreach(double n in a)sum+=n;return sum/Math.Max(1,a.Count);}
        static double P95(List<double> a){a.Sort();return a[Math.Min(a.Count-1,(int)(a.Count*.95))];}
        IEnumerator Start()
        {
            var game=GetComponent<StairsGame>();var assembly=new List<double>();var motion=new List<double>();var boards=new List<double>();var planning=new List<double>();var report=new Report();
            for(int level=0;level<game.catalog.levels.Length;level++){
                var timer=Stopwatch.StartNew();game.LoadLevel(level);boards.Add(timer.Elapsed.TotalMilliseconds);
                report.renderersBeforePeople=Math.Max(report.renderersBeforePeople,game.scene.root.GetComponentsInChildren<Renderer>(true).Length);
                while(game.IsAssembling){timer.Restart();game.Advance(1f/60);assembly.Add(timer.Elapsed.TotalMilliseconds);}
                while(game.IntroVisible)game.ContinueIntro();
                report.renderersWithPeople=Math.Max(report.renderersWithPeople,game.scene.root.GetComponentsInChildren<Renderer>(true).Length);
                foreach(var edge in game.board.Level.solution){
                    timer.Restart();game.TryMoveSync(edge.a,edge.b);planning.Add(timer.Elapsed.TotalMilliseconds);
                    while(game.motion!=null){timer.Restart();game.Advance(1f/60);motion.Add(timer.Elapsed.TotalMilliseconds);}
                }
                if(!game.board.Solved)throw new Exception("Performance run failed to solve level");report.levels++;yield return null;
            }
            report.assemblySamples=assembly.Count;report.motionSamples=motion.Count;report.assemblyMeanMs=Mean(assembly);report.assemblyP95Ms=P95(assembly);report.motionMeanMs=Mean(motion);report.motionP95Ms=P95(motion);report.boardMeanMs=Mean(boards);report.planningMeanMs=Mean(planning);
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../artifacts/performance.json"));Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(report,true));UnityEngine.Debug.Log("PERFORMANCE CHECK PASSED "+output);Application.Quit(0);
        }
    }
}
