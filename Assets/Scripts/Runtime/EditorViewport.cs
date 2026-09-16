using System;
using System.Linq;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public sealed partial class StairsGame
    {
        CrowdScene editorScene;bool editorPreviewDirty,editorFitRequested;int editorTool;
        float editorPreviewTime;Vector3 editorDragOffset;LevelSpec editorDragBefore;bool editorDragChanged;
        static Rect EditorMapRect(float w,float h){return new Rect(18,110,w-36,Mathf.Max(180,h*.43f));}
        void SetEditorViewport()
        {
            float scale=Mathf.Min(Screen.width/600f,Screen.height/900f);var rect=EditorMapRect(Screen.width/scale,Screen.height/scale);
            view.rect=new Rect(rect.x*scale/Screen.width,1-rect.yMax*scale/Screen.height,rect.width*scale/Screen.width,rect.height*scale/Screen.height);
        }
        void DisposeEditorPreview(){if(editorScene!=null){editorScene.Dispose();editorScene=null;}editorPreviewDirty=false;dragNode=-1;editorDragBefore=null;}
        void TickEditorPreview()
        {
            if(!editing)return;
            if(editorPreviewDirty&&Time.unscaledTime-editorPreviewTime>.06f)RefreshEditorPreview();
        }
        public void RefreshEditorPreview()
        {
            if(!editing||draft==null)return;SetEditorViewport();
            var position=view.transform.position;var rotation=view.transform.rotation;float size=view.orthographicSize;
            if(editorScene!=null)editorScene.Dispose();
            editorScene=new CrowdScene(new WalkSpace(draft,null,true),view);editorScene.KeepOrbitInFrame=false;
            var state=Rules.Initial(draft);state.revealed=draft.groups.Select(g=>!g.hidden).ToArray();editorScene.Populate(state);
            if(!editorFitRequested){view.transform.SetPositionAndRotation(position,rotation);view.orthographicSize=size;}
            editorScene.FaceClouds();editorFitRequested=false;editorPreviewDirty=false;editorPreviewTime=Time.unscaledTime;Physics.SyncTransforms();
        }
        Vector3 EditorWorld(int n){var spec=draft.nodes[n];return WalkSpace.WorldCenter(spec);}
        Vector2 EditorScreen(Vector2 gui){float scale=Mathf.Min(Screen.width/600f,Screen.height/900f);return new Vector2(gui.x*scale,Screen.height-gui.y*scale);}
        bool EditorPlanePoint(Vector2 gui,float height,out Vector3 point)
        {
            var ray=view.ScreenPointToRay(EditorScreen(gui));float distance;var plane=new Plane(Vector3.up,new Vector3(0,height,0));bool hit=plane.Raycast(ray,out distance);point=hit?ray.GetPoint(distance):Vector3.zero;return hit;
        }
        int EditorPick(Vector2 gui)
        {
            Physics.SyncTransforms();foreach(var hit in Physics.RaycastAll(view.ScreenPointToRay(EditorScreen(gui)),100000,(1<<8)|(1<<9),QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){
                var tag=hit.collider.GetComponent<PlatformTag>();if(tag&&tag.node>=0&&tag.node<draft.nodes.Length)return tag.node;
            }return -1;
        }
        void DrawEditorViewport(Rect rect)
        {
            Color background=new Color(.91f,.89f,.83f);Fill(new Rect(0,rect.y,rect.x,rect.height),background);Fill(new Rect(rect.xMax,rect.y,rect.x,rect.height),background);
            if(editNode>=0&&editorScene!=null&&editNode<editorScene.space.Centers.Length){
                var center=EditorWorld(editNode);float extent=editorScene.space.Half(editNode)+.12f;Vector2? previous=null;
                int sides=editorScene.space.Sides[editNode];float radius=extent/Mathf.Cos(Mathf.PI/sides);
                for(int i=0;i<=sides;i++){float angle=editorScene.space.Rotations[editNode]+(i+.5f)*Mathf.PI*2/sides;var world=center+new Vector3(Mathf.Cos(angle)*radius,.04f,Mathf.Sin(angle)*radius);var pixel=view.WorldToScreenPoint(world);
                    float scale=Mathf.Min(Screen.width/600f,Screen.height/900f);var point=new Vector2(pixel.x/scale,(Screen.height-pixel.y)/scale);
                    if(previous.HasValue&&rect.Contains(previous.Value)&&rect.Contains(point))DrawEditorLine(previous.Value,point,2,new Color(.25f,.43f,.41f));previous=point;
                }
            }
            if(editNode>=0&&editorScene!=null&&!draft.nodes[editNode].transit){
                var center=EditorWorld(editNode);var forward=editorScene.space.Facing(editNode);var right=Vector3.Cross(Vector3.up,forward);
                var front=center+forward*1.55f+Vector3.up*.07f;float scale=Mathf.Min(Screen.width/600f,Screen.height/900f);
                var a=view.WorldToScreenPoint(front-right*.75f);var b=view.WorldToScreenPoint(front+right*.75f);
                var ga=new Vector2(a.x/scale,(Screen.height-a.y)/scale);var gb=new Vector2(b.x/scale,(Screen.height-b.y)/scale);
                if(rect.Contains(ga)&&rect.Contains(gb))DrawEditorLine(ga,gb,4,new Color(.77f,.44f,.32f));
            }
            string[] labels={"点击选中；拖动平台","点击空白处放置集结点","点击空白处放置中转点",linkFrom<0?"选择连接起点":"选择连接终点","拖动画面平移","左右滑动切换视角"};
            GUI.Label(new Rect(rect.x+10,rect.y+8,rect.width-20,25),labels[editorTool],small);
            if(Button(new Rect(rect.xMax-142,rect.yMax-40,40,32),"−"))view.orthographicSize*=1.18f;
            if(Button(new Rect(rect.xMax-95,rect.yMax-40,40,32),"＋"))view.orthographicSize=Mathf.Max(.1f,view.orthographicSize/1.18f);
            if(Button(new Rect(rect.xMax-48,rect.yMax-40,40,32),"全")){editorFitRequested=true;editorPreviewDirty=true;}
            if(shareOpen||libraryOpen)return;var ev=Event.current;
            if(ev.type==EventType.ScrollWheel&&rect.Contains(ev.mousePosition)){view.orthographicSize=Mathf.Max(.1f,view.orthographicSize*Mathf.Exp(ev.delta.y*.07f));ev.Use();}
            if(ev.type==EventType.MouseDown&&rect.Contains(ev.mousePosition)){
                if(editorTool==5&&UserRotationEnabled){dragNode=-3;ev.Use();return;}
                if(editorTool==4||ev.button==1){dragNode=-2;ev.Use();return;}
                int node=EditorPick(ev.mousePosition);
                if(editorTool==1||editorTool==2){Vector3 point;float height=editNode>=0?WalkSpace.WorldHeight(draft.nodes[editNode].y):2;if(EditorPlanePoint(ev.mousePosition,height,out point)){PlaceEditorNode(editorTool==2,point);ev.Use();}return;}
                if(node>=0){editNode=node;propertyNode=-2;
                    if(editorTool==3){if(linkFrom<0)linkFrom=node;else{ToggleEditorLink(linkFrom,node);linkFrom=-1;}}
                    else{Vector3 point;if(EditorPlanePoint(ev.mousePosition,WalkSpace.WorldHeight(draft.nodes[node].y),out point)){dragNode=node;editorDragOffset=EditorWorld(node)-point;editorDragBefore=LevelShare.Copy(draft);editorDragChanged=false;}}
                    ev.Use();
                }
            }
            if(ev.type==EventType.MouseDrag&&dragNode!=-1){
                if(dragNode==-3){if(UserRotationEnabled)editorScene.OrbitView(ev.delta.x/rect.width*180);}
                else if(dragNode==-2){float scale=Mathf.Min(Screen.width/600f,Screen.height/900f),units=view.orthographicSize*2/view.pixelHeight;view.transform.position+=(-view.transform.right*ev.delta.x+view.transform.up*ev.delta.y)*scale*units;}
                else{Vector3 point;var node=draft.nodes[dragNode];if(EditorPlanePoint(ev.mousePosition,WalkSpace.WorldHeight(node.y),out point)){point+=editorDragOffset;node.x=point.x/1.4f;node.z=-point.z/1.96f;draft.solution=new EdgeSpec[0];editorPreviewDirty=true;editorDragChanged=true;propertyNode=-2;}}
                ev.Use();
            }
            if(ev.rawType==EventType.MouseUp&&dragNode!=-1){if(editorDragChanged&&editorDragBefore!=null){editorHistory.Push(editorDragBefore);editorSnapshot=LevelShare.Copy(draft);}dragNode=-1;editorDragBefore=null;editorDragChanged=false;}
        }
        void DrawEditorLine(Vector2 a,Vector2 b,float width,Color color){var matrix=GUI.matrix;GUI.matrix=matrix*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg),Vector3.one);Fill(new Rect(0,-width/2,Vector2.Distance(a,b),width),color);GUI.matrix=matrix;}
    }
}



