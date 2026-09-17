using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using StairsCrowd.Core;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    [Serializable] public sealed class LevelPresentationEntry
    {
        public string sourceSignature,geometrySignature;
        public Vector3 direction;
        public Vector3[] positions;
        public string[] operations;
    }
    [Serializable] public sealed class LevelPresentationCatalog
    {
        public int version=1;
        public LevelPresentationEntry[] entries=Array.Empty<LevelPresentationEntry>();
    }
    // Presentation metadata is authored during delivery. Runtime never searches.
    public static class LevelPresentation
    {
        static LevelPresentationCatalog catalog;
        public static string Signature(LevelSpec level)
        {
            // Include all authored facts to reject stale layout metadata.
            using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(level)))).Replace("-","");
        }
        public static LevelPresentationEntry Find(LevelSpec level)
        {
            if(catalog==null){var asset=Resources.Load<TextAsset>("gameplay-presentation-v1");catalog=asset?JsonUtility.FromJson<LevelPresentationCatalog>(asset.text):new LevelPresentationCatalog();}
            if(catalog.version!=1)return null;
            var key=Signature(level);foreach(var e in catalog.entries)if(e.sourceSignature==key||e.geometrySignature==key)return e;
            return null;
        }
        public static Vector3 Direction(LevelSpec level)
        {
            var entry=Find(level);var d=entry==null?GameplayPresentation.DefaultDirection:entry.direction;
            return d.sqrMagnitude>.01f&&float.IsFinite(d.x)&&float.IsFinite(d.y)&&float.IsFinite(d.z)?d:GameplayPresentation.DefaultDirection;
        }
        public static LevelSpec Apply(LevelSpec source)
        {
            var entry=Find(source);
            if(entry==null||entry.sourceSignature!=Signature(source)||entry.operations==null||entry.operations.Length==0)return source;
            if(entry.positions==null||entry.positions.Length!=source.nodes.Length)return source;
            var copy=LevelShare.Copy(source);
            for(int i=0;i<copy.nodes.Length;i++){
                var p=entry.positions[i];if(!float.IsFinite(p.x)||!float.IsFinite(p.z)||p.y!=source.nodes[i].y)return source;
                copy.nodes[i].x=p.x;copy.nodes[i].z=p.z;
            }
            // Damaged or stale metadata never changes a playable board.
            return Signature(copy)==entry.geometrySignature?copy:source;
        }
    }
    public static class GameplayFraming
    {
        public static Vector3[] Envelope(WalkSpace space)
        {
            var points=new List<Vector3>();
            // Enclose empty/full platforms, selection scale/lift and stepping bodies.
            const float height=CrowdScene.PersonHeight*1.1f+.5f;
            for(int n=0;n<space.Centers.Length;n++){
                // Explicit full 16-seat envelope, including selected scale and lift.
                for(int row=0;row<4;row++)for(int member=0;member<4;member++){
                    var seat=space.Seat(n,row,member);float r=WalkSpace.ActorRadius*1.1f;
                    foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1}){var p=seat+new Vector3(x*r,0,z*r);points.Add(p);points.Add(p+Vector3.up*height);}
                }
                float radius=space.Half(n)/Mathf.Cos(Mathf.PI/space.Sides[n])+.12f;
                for(int corner=0;corner<space.Sides[n];corner++){
                    float angle=space.Rotations[n]+(corner+.5f)*Mathf.PI*2/space.Sides[n];
                    var p=space.Centers[n]+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                    points.Add(p-Vector3.up*.65f);points.Add(p+Vector3.up*height);
                }
            }
            foreach(var stair in space.Stairs){
                if(stair.polygon!=null){foreach(var p in stair.polygon){points.Add(p-Vector3.up*.4f);points.Add(p+Vector3.up*height);}continue;}
                var side=Vector3.Cross(Vector3.up,stair.Direction)*(stair.width*.5f+.2f);
                foreach(var end in new[]{stair.start,stair.end})foreach(int sign in new[]{-1,1}){points.Add(end+side*sign-Vector3.up*.7f);points.Add(end+side*sign+Vector3.up*height);}
            }
            return points.ToArray();
        }
        public static Rect Project(Vector3[] points,Quaternion rotation)
        {
            Vector3 right=rotation*Vector3.right,up=rotation*Vector3.up;
            float minX=float.PositiveInfinity,minY=minX,maxX=float.NegativeInfinity,maxY=maxX;
            foreach(var p in points){float x=Vector3.Dot(p,right),y=Vector3.Dot(p,up);minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
            return points.Length==0?new Rect(-1,-1,2,2):Rect.MinMaxRect(minX,minY,maxX,maxY);
        }
        public static float RequiredSize(Rect bounds,Rect playPixels,float pixelHeight)
        {
            float pixelsPerUnit=Mathf.Min(playPixels.width/Mathf.Max(.01f,bounds.width),playPixels.height/Mathf.Max(.01f,bounds.height));
            return pixelHeight/(2*Mathf.Max(.0001f,pixelsPerUnit));
        }
        public static void Fit(Camera camera,Vector3[] points,Vector3 direction,Rect playable)
        {
            var rotation=Quaternion.LookRotation(-direction.normalized,Vector3.up);
            var bounds=Project(points,rotation);
            // Small fixed safety inset for raster rounding; never reduce scale to force downshift.
            var area=new Rect(playable.x+3,playable.y+3,Mathf.Max(1,playable.width-6),Mathf.Max(1,playable.height-6));
            camera.orthographic=true;camera.orthographicSize=RequiredSize(bounds,area,camera.pixelHeight);
            float pixelsPerUnit=camera.pixelHeight/(2*camera.orthographicSize);
            float remaining=Mathf.Max(0,area.height-bounds.height*pixelsPerUnit);
            var screenCenter=new Vector2(area.center.x,area.y+remaining*.25f+bounds.height*pixelsPerUnit*.5f);
            var pixelRect=camera.pixelRect;
            var targetPlane=bounds.center-(screenCenter-pixelRect.center)/pixelsPerUnit;
            var right=rotation*Vector3.right;var up=rotation*Vector3.up;var forward=rotation*Vector3.forward;
            float depth=float.PositiveInfinity,maxDepth=float.NegativeInfinity;
            foreach(var p in points){float d=Vector3.Dot(p,forward);depth=Mathf.Min(depth,d);maxDepth=Mathf.Max(maxDepth,d);}
            if(points.Length==0){depth=0;maxDepth=0;}
            camera.transform.SetPositionAndRotation(right*targetPlane.x+up*targetPlane.y+forward*(depth-40),rotation);
            camera.nearClipPlane=.1f;camera.farClipPlane=Mathf.Max(1000,maxDepth-depth+100);
        }
    }
    public sealed partial class CrowdScene
    {
        Vector3[] framingEnvelope;Vector3 deliveredDirection;bool directionReady;
        bool FitGameplayCamera()
        {
            if(HomeFraming||camera.rect!=new Rect(0,0,1,1)||space.Centers.Length==0)return false;
            if(framingEnvelope==null)framingEnvelope=GameplayFraming.Envelope(space);
            if(!directionReady){deliveredDirection=LevelPresentation.Direction(space.Level);directionReady=true;}
            GameplayFraming.Fit(camera,framingEnvelope,deliveredDirection,GameplayLayout.Current.PlayPixels);
            FaceClouds();return true;
        }
    }
    public sealed partial class StairsGame
    {
        Rect lastPlayArea;int lastDisplayRevision=-1;
        void RefreshGameplayLayout()
        {
            if(scene==null||editing||Home)return;
            var area=GameplayLayout.Current.PlayPixels;
            if(area==lastPlayArea&&lastDisplayRevision==WeChatPlatform.DisplayRevision)return;
            lastPlayArea=area;lastDisplayRevision=WeChatPlatform.DisplayRevision;CancelViewPointer();scene.FitCamera();
        }
    }
}
