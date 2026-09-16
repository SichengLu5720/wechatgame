using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using StairsCrowd.Core;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    public static class LevelShare
    {
        public static LevelSpec Copy(LevelSpec value){return JsonUtility.FromJson<LevelSpec>(JsonUtility.ToJson(value));}
        static uint Hash(byte[] bytes){unchecked{uint h=2166136261;foreach(byte b in bytes)h=(h^b)*16777619;return h;}}
        public static string Encode(LevelSpec level)
        {
            level.Validate();var copy=Copy(level);copy.solution=new EdgeSpec[0];copy.provenance=null;byte[] raw=Encoding.UTF8.GetBytes(JsonUtility.ToJson(copy));
            using(var output=new MemoryStream()){using(var zip=new DeflateStream(output,System.IO.Compression.CompressionLevel.Optimal,true))zip.Write(raw,0,raw.Length);return "ISLE1."+Hash(raw).ToString("X8")+"."+Convert.ToBase64String(output.ToArray());}
        }
        public static LevelSpec Decode(string code)
        {
            if(string.IsNullOrWhiteSpace(code))throw new Exception("分享码为空");var parts=code.Trim().Split('.');
            if(parts.Length!=3||parts[0]!="ISLE1")throw new Exception("分享码格式或版本不支持");
            byte[] raw;using(var input=new MemoryStream(Convert.FromBase64String(parts[2])))using(var zip=new DeflateStream(input,CompressionMode.Decompress))using(var output=new MemoryStream()){
                var buffer=new byte[4096];int count;while((count=zip.Read(buffer,0,buffer.Length))>0)output.Write(buffer,0,count);raw=output.ToArray();
            }
            if(Hash(raw).ToString("X8")!=parts[1])throw new Exception("分享码不完整，请重新复制");
            var level=JsonUtility.FromJson<LevelSpec>(Encoding.UTF8.GetString(raw));if(level==null)throw new Exception("无效关卡");level.provenance=null;level.Validate();return level;
        }
    }
}
