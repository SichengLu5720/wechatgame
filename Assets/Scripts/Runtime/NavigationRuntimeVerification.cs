using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    // Explicit test entry only; production sessions never attach this component.
    public sealed class NavigationRuntimeVerification : MonoBehaviour
    {
        [Serializable] sealed class Report {public bool passed,editorHeight,editorUndo,saveRestore,share,customMove,reset,incompleteDraft;public string error;}
        StairsGame game;readonly Report report=new Report();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-navigationtest")<0)return;
            var game=UnityEngine.Object.FindFirstObjectByType<StairsGame>();if(game)game.gameObject.AddComponent<NavigationRuntimeVerification>();
        }
        static void Require(bool value,string message){if(!value)throw new Exception("Navigation runtime: "+message);}
        static void Invoke(StairsGame target,string name)=>typeof(StairsGame).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
        void Settle()
        {
            int guard=0;while(game.IsAssembling&&guard++<1500)game.Advance(.04f);Require(!game.IsAssembling,"assembly timeout");
            while(game.IntroVisible)game.ContinueIntro();
        }
        IEnumerator Start()
        {
            yield return null;game=GetComponent<StairsGame>();game.enabled=false;
            try{Check();report.passed=true;}catch(Exception e){report.error=e.ToString();Debug.LogException(e);}
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../navigation-check"));Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));
            Debug.Log("NAVIGATION RUNTIME "+(report.passed?"PASSED":"FAILED"));Application.Quit(report.passed?0:1);
        }
        void Check()
        {
            Settle();var source=LevelShare.Copy(game.catalog.levels[10]);game.OpenEditor();game.SetEditorDraft(source);
            var before=NavigationFactory.ForDraft(game.EditorDraft);float oldHeight=game.EditorDraft.nodes[0].y;
            game.EditorDraft.nodes[0].y+=.25f;Invoke(game,"Changed");game.RefreshEditorPreview();var changed=NavigationFactory.ForDraft(game.EditorDraft);
            Require(changed.GeometrySignature!=before.GeometrySignature&&Mathf.Abs(changed.Centers[0].y-before.Centers[0].y-.375f)<.00001f,"height did not regenerate");report.editorHeight=true;
            SaveRoundtrip();
            var code=LevelShare.Encode(game.EditorDraft);var imported=LevelShare.Decode(code);Require(LevelShare.Encode(imported)==code,"share roundtrip changed");
            Require(NavigationFactory.ForDraft(imported).GeometrySignature==changed.GeometrySignature,"import geometry changed");report.share=true;
            game.UndoEditor();Require(game.EditorDraft.nodes[0].y==oldHeight,"editor undo height");report.editorUndo=true;
            game.SetEditorDraft(source);game.PlayDraft();Settle();Require(game.space.Heights.Length==0,"custom runtime dense grid");
            var initial=Rules.Key(game.board.Level,game.board.Current);var first=source.solution[0];Require(game.TryMoveSync(first.a,first.b),"custom move rejected");
            game.Advance(.12f);game.Undo();Require(Rules.Key(game.board.Level,game.board.Current)==initial,"custom moving undo");report.customMove=true;
            game.ResetLevel();Settle();Require(game.board.Moves==0&&Rules.Key(game.board.Level,game.board.Current)==initial,"custom reset changed");report.reset=true;
            game.OpenEditor();var overlap=LevelShare.Copy(source);overlap.nodes[1].x=overlap.nodes[0].x;overlap.nodes[1].z=overlap.nodes[0].z;
            game.SetEditorDraft(overlap);game.RefreshEditorPreview();game.PlayDraft();Settle();Require(game.space.Heights.Length==0,"incomplete draft load");report.incompleteDraft=true;
            game.ReturnHome();Settle();Require(game.Home,"return home");
        }
        void SaveRoundtrip()
        {
            // Exercise the existing serializer and PlayerPrefs path, restore user data
            // synchronously even on assertion failure, and isolate the in-memory library.
            const string key="islands.nightv1.editor.library.v1";bool existed=PlayerPrefs.HasKey(key);string original=PlayerPrefs.GetString(key,"");
            var field=typeof(StairsGame).GetField("library",BindingFlags.Instance|BindingFlags.NonPublic);var prior=field.GetValue(game);
            try{
                field.SetValue(game,new IslandLibrary());Invoke(game,"SaveDraft");
                var saved=JsonUtility.FromJson<IslandLibrary>(PlayerPrefs.GetString(key));Require(saved.items.Count==1,"saved draft missing");
                Require(NavigationFactory.ForDraft(saved.items[0].level).GeometrySignature==NavigationFactory.ForDraft(game.EditorDraft).GeometrySignature,"saved geometry mismatch");report.saveRestore=true;
            }finally{field.SetValue(game,prior);if(existed)PlayerPrefs.SetString(key,original);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
        }
    }
}
