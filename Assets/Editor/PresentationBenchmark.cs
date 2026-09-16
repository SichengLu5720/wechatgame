using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using StairsCrowd.Core;
using StairsCrowd.Runtime;

public static class PresentationBenchmark
{
    [Serializable] public sealed class Report
    {
        public bool passed;
        public int iterations=200000, constructionSamples=5;
        public double halfLookupMs, halfLookupAllocatedBytes, geometryMeanMs, focusRefreshMs, focusAllocatedBytes;
        public float checksum;
    }
    public static void Run()
    {
        try {
            var level=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("levels").text).levels[0];
            var space=new WalkSpace(level,null,true);
            for(int i=0;i<1000;i++)space.Half(i%level.nodes.Length);
            var report=new Report();var timer=new Stopwatch();
            long allocated=GC.GetAllocatedBytesForCurrentThread();timer.Start();
            for(int i=0;i<report.iterations;i++)report.checksum+=space.Half(i%level.nodes.Length);
            timer.Stop();report.halfLookupMs=timer.Elapsed.TotalMilliseconds;
            report.halfLookupAllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
            new WalkSpace(level);
            timer.Restart();
            for(int i=0;i<report.constructionSamples;i++)new WalkSpace(level);
            timer.Stop();report.geometryMeanMs=timer.Elapsed.TotalMilliseconds/report.constructionSamples;
            var cameraObject=new GameObject("Benchmark camera",typeof(Camera));
            var scene=new CrowdScene(space,cameraObject.GetComponent<Camera>());var state=Rules.Initial(level);scene.Populate(state);
            var targets=new System.Collections.Generic.HashSet<int>{0,1};scene.SetPropFocus(targets,state);scene.SetPropFocus(null,state);
            allocated=GC.GetAllocatedBytesForCurrentThread();timer.Restart();
            for(int i=0;i<40;i++)scene.SetPropFocus(i%2==0?targets:null,state);
            timer.Stop();report.focusRefreshMs=timer.Elapsed.TotalMilliseconds;report.focusAllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
            scene.Dispose();UnityEngine.Object.DestroyImmediate(cameraObject);
            report.passed=report.checksum>0;
            Directory.CreateDirectory("artifacts/presentation-benchmark");
            File.WriteAllText("artifacts/presentation-benchmark/result.json",JsonUtility.ToJson(report,true));
            UnityEngine.Debug.Log("PRESENTATION BENCHMARK "+JsonUtility.ToJson(report));EditorApplication.Exit(0);
        } catch(Exception e){UnityEngine.Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
