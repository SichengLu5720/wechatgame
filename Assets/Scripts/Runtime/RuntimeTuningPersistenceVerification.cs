using System;
using System.IO;
using UnityEngine;
namespace StairsCrowd.Runtime
{
    // Two separate player invocations exercise an actual process restart.
    public static class RuntimeTuningPersistenceVerification
    {
        [Serializable] sealed class Backup {public bool exists;public string value;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Run()
        {
            var args=Environment.GetCommandLineArgs();bool write=Array.IndexOf(args,"-tuningpersistwrite")>=0,read=Array.IndexOf(args,"-tuningpersistread")>=0;if(!write&&!read)return;
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../tuning-check"));Directory.CreateDirectory(folder);string backup=Path.Combine(folder,"persistence-backup.json");bool passed=false;
            try{
                if(!RuntimeTuning.Available)throw new Exception("Requires development player");
                if(write){File.WriteAllText(backup,JsonUtility.ToJson(new Backup{exists=PlayerPrefs.HasKey(RuntimeTuning.Key),value=PlayerPrefs.GetString(RuntimeTuning.Key,"")}));RuntimeTuning.SetLinked(false);RuntimeTuning.ChangeSpeed(20);RuntimeTuning.ChangeScale(false,.8f);RuntimeTuning.ChangeScale(true,1.2f);RuntimeTuning.Save();RuntimeTuning.ChangeSpeed(.5f);RuntimeTuning.ChangeScale(false,.9f);PlayerPrefs.Save();passed=RuntimeTuning.Dirty;}
                else{RuntimeTuning.Reload();passed=RuntimeTuning.Speed==20&&RuntimeTuning.Platform==.8f&&RuntimeTuning.Person==1.2f&&!RuntimeTuning.Working.linked&&!RuntimeTuning.Dirty;}
            }catch(Exception e){Debug.LogException(e);}
            finally{if(read&&File.Exists(backup)){var original=JsonUtility.FromJson<Backup>(File.ReadAllText(backup));if(original.exists)PlayerPrefs.SetString(RuntimeTuning.Key,original.value);else PlayerPrefs.DeleteKey(RuntimeTuning.Key);PlayerPrefs.Save();RuntimeTuning.Reload();}}
            File.WriteAllText(Path.Combine(folder,write?"persistence-write.json":"persistence-read.json"),"{\"passed\":"+passed.ToString().ToLowerInvariant()+",\"processId\":"+System.Diagnostics.Process.GetCurrentProcess().Id+"}");Debug.Log("TUNING PERSISTENCE "+(write?"WRITE ":"READ ")+(passed?"PASSED":"FAILED"));Application.Quit(passed?0:1);
        }
    }
}
