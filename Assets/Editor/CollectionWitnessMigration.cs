using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using StairsCrowd.Core;

public static class CollectionWitnessMigration
{
    public static void Run()
    {
        try {
            var path="Assets/Resources/campaign-v1.json";
            var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText(path));
            var report=new List<string>();
            foreach(var level in catalog.levels){
                int old=level.solution.Length;var board=new Board(level);var moves=new List<EdgeSpec>();
                foreach(var step in level.solution)if(board.TryMove(step.a,step.b).ok)moves.Add(step);
                if(!board.Solved){
                    var result=OmniscientSearch.Find(level,500000,120000);
                    if(result.status!="SolvedFound")throw new Exception(level.name+": "+result.status);
                    moves=new List<EdgeSpec>(result.moves);level.provenance.visited=result.visited;
                }
                level.solution=moves.ToArray();level.provenance.solutionMoves=moves.Count;
                level.provenance.solverStatus="SolvedFound";
                CampaignValidator.Validate(level);
                report.Add(level.name+": "+old+" -> "+moves.Count);
            }
            File.WriteAllText(path,JsonUtility.ToJson(catalog,true));
            Directory.CreateDirectory("artifacts/sky-v7.6");
            File.WriteAllLines("artifacts/sky-v7.6/witness-migration.txt",report);
            EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
