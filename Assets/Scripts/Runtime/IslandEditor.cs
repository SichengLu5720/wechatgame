using System;
using System.Linq;
using System.Collections.Generic;
using StairsCrowd.Core;
using UnityEngine;

namespace StairsCrowd.Runtime
{
    [Serializable] public class SavedIsland {public string id;public LevelSpec level;}
    [Serializable] public class IslandLibrary {public List<SavedIsland> items=new List<SavedIsland>();}
    public sealed partial class StairsGame
    {
        bool editing,customPlaying,linkMode,shareOpen,libraryOpen;int editNode=-1,linkFrom=-1,dragNode=-1,builtInCount,customIndex=-1;
        LevelSpec draft;IslandLibrary library;string draftId="",shareCode="",editorStatus="",seedText="2026";Vector2 editorScroll,libraryScroll;
        GUIStyle field;readonly Stack<LevelSpec> editorHistory=new Stack<LevelSpec>();LevelSpec editorSnapshot;
        string heightText="0",xText="0",zText="0";float heightAnchor;int propertyNode=-2;
        public bool EditorOpen {get{return editing;}}
        public LevelSpec EditorDraft {get{return draft;}}
        const string LibraryKey="islands.nightv1.editor.library.v1";
        public void OpenEditor()
        {
            LeaveDaily();
            DiscardPreparedScene();CancelPending();introKinds.Clear();Home=true;editing=true;selected=-1;scene.root.SetActive(false);
            if(builtInCount==0)builtInCount=catalog.levels.Length;
            if(draft==null)draft=LevelShare.Copy(catalog.levels[0]);
            if(library==null){try{library=JsonUtility.FromJson<IslandLibrary>(PlayerPrefs.GetString(LibraryKey,""));}catch(Exception){library=null;}if(library==null||library.items==null)library=new IslandLibrary();}
            editNode=draft.nodes.Length>0?Mathf.Clamp(editNode,0,draft.nodes.Length-1):-1;editorSnapshot=LevelShare.Copy(draft);propertyNode=-2;
            editorStatus="选择平台修改；选择添加后，点击画面放置";editorFitRequested=true;editorPreviewDirty=true;SetEditorViewport();RefreshEditorPreview();
        }
        public void SetEditorDraft(LevelSpec level){var candidate=LevelShare.Copy(level);candidate.provenance=null;candidate.Validate();draft=candidate;draftId="";editNode=draft.nodes.Length>0?0:-1;editorHistory.Clear();editorSnapshot=LevelShare.Copy(draft);propertyNode=-2;editorFitRequested=true;editorPreviewDirty=true;editorStatus="已载入，可以自由编辑";}
        void Changed()
        {
            if(editorSnapshot!=null)editorHistory.Push(editorSnapshot);draft.solution=new EdgeSpec[0];draft.provenance=null;editorSnapshot=LevelShare.Copy(draft);
            editorPreviewDirty=true;editorStatus="已修改";
        }
        public void UndoEditor()
        {
            if(editorHistory.Count==0)return;draft=editorHistory.Pop();editorSnapshot=LevelShare.Copy(draft);editNode=draft.nodes.Length>0?Mathf.Clamp(editNode,0,draft.nodes.Length-1):-1;
            propertyNode=-2;editorPreviewDirty=true;editorStatus="已撤销";
        }
        void EditorAction(Action action){try{action();}catch(Exception e){editorStatus=e.Message;}}
        public void PlayDraft()
        {
            draft.Validate();CancelPending();DisposeEditorPreview();
            if(customIndex<0){customIndex=catalog.levels.Length;var list=catalog.levels.ToList();list.Add(LevelShare.Copy(draft));catalog.levels=list.ToArray();}else catalog.levels[customIndex]=LevelShare.Copy(draft);
            spaces.Remove(customIndex);editing=false;customPlaying=true;scene.root.SetActive(true);LoadLevel(customIndex);
        }
        void SaveDraft()
        {
            draft.Validate();if(string.IsNullOrWhiteSpace(draft.name))draft.name="我的群岛";
            if(string.IsNullOrEmpty(draftId))draftId=Guid.NewGuid().ToString("N");var saved=library.items.FirstOrDefault(s=>s.id==draftId);
            if(saved==null){saved=new SavedIsland{id=draftId};library.items.Add(saved);}saved.level=LevelShare.Copy(draft);
            PlayerPrefs.SetString(LibraryKey,JsonUtility.ToJson(library));PlayerPrefs.Save();editorStatus="已保存："+draft.name;
        }
        public void PlaceEditorNode(bool transit,Vector3 world)
        {
            var nodes=draft.nodes.ToList();var added=CloudLevels.N(world.x/1.4f,WalkSpace.LevelHeight(world.y),-world.z/1.96f,transit);int index=nodes.Count;
            if(index>0){int nearest=Enumerable.Range(0,index).OrderBy(i=>(WalkSpace.WorldCenter(nodes[i])-world).sqrMagnitude).First();draft.edges=draft.edges.Concat(new[]{CloudLevels.E(nearest,index)}).ToArray();}
            nodes.Add(added);draft.nodes=nodes.ToArray();editNode=index;propertyNode=-2;Changed();
        }
        public void RotateGatheringSurface(int node)
        {
            if(node<0||node>=draft.nodes.Length||draft.nodes[node].transit)return;
            draft.nodes[node].surfaceDirection=((draft.nodes[node].surfaceDirection%4+4)%4+1)%4;Changed();
        }
        void RemoveNode()
        {
            if(editNode<0)return;int removed=editNode;var keep=draft.nodes.Where((n,i)=>i!=removed).ToArray();draft.edges=draft.edges.Where(e=>e.a!=removed&&e.b!=removed).Select(e=>CloudLevels.E(e.a>removed?e.a-1:e.a,e.b>removed?e.b-1:e.b)).ToArray();draft.nodes=keep;
            ReindexGroups();editNode=keep.Length>0?Mathf.Clamp(editNode,0,keep.Length-1):-1;propertyNode=-2;Changed();
        }
        void ReindexGroups(){var next=new List<GroupSpec>();foreach(var n in draft.nodes){var queue=new List<int>();foreach(int id in n.queue){var old=draft.groups[id];queue.Add(next.Count);next.Add(new GroupSpec{id=next.Count,color=old.color,hidden=old.hidden});}n.queue=queue.ToArray();}draft.groups=next.ToArray();}
        public void ToggleEditorLink(int a,int b)
        {
            if(a==b)return;var list=draft.edges.ToList();int found=list.FindIndex(e=>e.a==a&&e.b==b||e.a==b&&e.b==a);
            if(found>=0)list.RemoveAt(found);else list.Add(CloudLevels.E(a,b));draft.edges=list.ToArray();Changed();
        }
        static readonly Color[] EditorColors=ColorCatalog.Rgb.Select(c=>new Color(((c>>16)&255)/255f,((c>>8)&255)/255f,(c&255)/255f)).ToArray();
        static readonly string[] ColorNames=ColorCatalog.Names;
        string TextField(Rect rect,string value){if(field==null)field=new GUIStyle{font=font,fontSize=16,padding=new RectOffset(9,9,6,6),normal={textColor=new Color(.26f,.32f,.36f)}};Fill(rect,new Color(.97f,.94f,.87f));return GUI.TextField(rect,value??"",field);}
        void DrawEditor(float w,float h)
        {
            GUI.enabled=!shareOpen&&!libraryOpen;
            var map=EditorMapRect(w,h);Fill(new Rect(0,0,w,map.y),new Color(.91f,.89f,.83f));Fill(new Rect(0,map.yMax,w,h-map.yMax),new Color(.91f,.89f,.83f));
            GUI.Label(new Rect(24,12,w-150,38),"关卡编辑器",title);
            if(Button(new Rect(w-110,16,86,36),"主页")){ReturnHome();return;}
            var name=TextField(new Rect(24,60,w-188,36),draft.name);if(name!=draft.name){draft.name=name;Changed();}
            if(Button(new Rect(w-150,60,126,36),"关卡库"))libraryOpen=true;
            DrawEditorViewport(map);
            int toolCount=UserRotationEnabled?5:4;if(!UserRotationEnabled&&editorTool==5)editorTool=0;
            float bw=(w-28-8*toolCount)/toolCount,toolbar=map.yMax+10;string[] tools={"集结点","中转点","连线","平移","视角"};
            for(int i=0;i<toolCount;i++)if(Button(new Rect(18+i*(bw+8),toolbar,bw,34),tools[i],true,editorTool==i+1?new Color(.63f,.77f,.71f):(Color?)null)){editorTool=editorTool==i+1?0:i+1;linkFrom=-1;}
            float panelY=toolbar+44;
            editorScroll=GUI.BeginScrollView(new Rect(18,panelY,w-36,Mathf.Max(40,h-panelY-110)),editorScroll,new Rect(0,0,w-60,760));
            DrawNodeProperties(w-66);GUI.EndScrollView();
            GUI.Label(new Rect(24,h-106,w-48,30),editorStatus,small);
            float actionW=(w-84)/4;
            if(Button(new Rect(24,h-64,actionW,42),"撤销",editorHistory.Count>0))UndoEditor();
            if(Button(new Rect(36+actionW,h-64,actionW,42),"保存"))EditorAction(SaveDraft);
            if(Button(new Rect(48+2*actionW,h-64,actionW,42),"分享／导入")){shareOpen=true;shareCode="";EditorAction(()=>{shareCode=LevelShare.Encode(draft);editorStatus="分享码已生成";});}
            if(Button(new Rect(60+3*actionW,h-64,actionW,42),"试玩",true,new Color(.67f,.8f,.71f)))EditorAction(PlayDraft);
            GUI.enabled=true;if(libraryOpen)DrawLibrary(w,h);if(shareOpen)DrawShare(w,h);
        }
        void SyncPropertyText(){if(editNode<0)return;var n=draft.nodes[editNode];heightText=n.y.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture);xText=n.x.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture);zText=n.z.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture);heightAnchor=n.y;propertyNode=editNode;}
        bool Number(string text,out float value){return float.TryParse(text,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out value)&&!float.IsNaN(value)&&!float.IsInfinity(value);}
        void DrawNodeProperties(float width)
        {
            if(editNode<0||editNode>=draft.nodes.Length){GUI.Label(new Rect(4,8,width,60),"选择集结点或中转点工具，在上方画面放置平台。",body);DrawSeed(width,100);return;}
            if(propertyNode!=editNode)SyncPropertyText();var n=draft.nodes[editNode];
            GUI.Label(new Rect(4,0,width-170,30),"平台 "+(editNode+1)+" · "+(n.transit?"中转点":"集结点"),body);
            if(Button(new Rect(width-162,0,92,30),n.transit?"改为集结":"改为中转")){if(!n.transit&&n.queue.Select(id=>draft.groups[id].color).Distinct().Count()>1){editorStatus="请先统一人物颜色，再改为中转平台";}else{n.transit=!n.transit;Changed();}}
            if(Button(new Rect(width-62,0,58,30),"删除")){RemoveNode();return;}
            float mw=(width-30)/4;string[] names={"帘幕","冰块","胶水","颜色集结"};
            bool[] active={n.curtain||n.queue.Any(id=>draft.groups[id].hidden),n.unlockAfter>0,n.sticky,n.colorRestricted};
            for(int i=0;i<4;i++)if(Button(new Rect(4+i*(mw+8),40,mw,36),(active[i]?"✓ ":"")+names[i],true,active[i]?new Color(.66f,.79f,.72f):(Color?)null)){
                if(i==0){n.curtain=!active[0];for(int r=0;r<n.queue.Length;r++)draft.groups[n.queue[r]].hidden=n.curtain&&r>0;}
                if(i==1)n.unlockAfter=n.unlockAfter>0?0:1;if(i==2)n.sticky=!n.sticky;if(i==3)n.colorRestricted=!n.colorRestricted;Changed();
            }
            GUI.Label(new Rect(4,87,112,32),"融化所需集结",small);string count=TextField(new Rect(120,87,66,32),n.unlockAfter.ToString());int parsed;if(int.TryParse(count,out parsed)&&parsed>=0&&parsed!=n.unlockAfter){n.unlockAfter=parsed;Changed();}
            if(Button(new Rect(200,87,136,32),"目标："+ColorNames[n.targetColor],true,EditorColors[n.targetColor])){n.targetColor=(n.targetColor+1)%ColorCatalog.Count;Changed();}
            GUI.Label(new Rect(4,133,62,30),"高度",body);var hy=TextField(new Rect(70,133,86,32),heightText);if(hy!=heightText){heightText=hy;float value;if(Number(hy,out value)){n.y=value;heightAnchor=value;Changed();}}
            float slider=GUI.HorizontalSlider(new Rect(175,145,width-182,20),n.y,heightAnchor-10,heightAnchor+10);if(Mathf.Abs(slider-n.y)>.001f){n.y=slider;heightText=n.y.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture);Changed();}
            GUI.Label(new Rect(4,176,38,30),"左右",small);var tx=TextField(new Rect(50,176,110,32),xText);if(tx!=xText){xText=tx;float value;if(Number(tx,out value)){n.x=value;Changed();}}
            GUI.Label(new Rect(180,176,38,30),"前后",small);var tz=TextField(new Rect(228,176,110,32),zText);if(tz!=zText){zText=tz;float value;if(Number(tz,out value)){n.z=value;Changed();}}
            if(!n.transit&&Button(new Rect(350,176,width-354,32),"转向 90°"))RotateGatheringSurface(editNode);
            GUI.Label(new Rect(4,223,width,30),"人物 "+n.queue.Length*4+" / 16 · "+(n.transit?"同色聚集":"从最外排到最内排"),body);
            for(int row=0;row<n.queue.Length;row++){
                int id=n.queue[row];var g=draft.groups[id];float y=264+row*42;
                if(Button(new Rect(4,y,84,34),ColorNames[g.color]+" × 4",true,EditorColors[g.color])){int color=(g.color+1)%ColorCatalog.Count;if(n.transit){foreach(int other in n.queue)draft.groups[other].color=color;}else g.color=color;Changed();}
                if(Button(new Rect(96,y,82,34),g.hidden?"未知":"已知")){g.hidden=!g.hidden;Changed();}
                if(Button(new Rect(186,y,56,34),"上移",!n.transit&&row>0)){int tmp=n.queue[row-1];n.queue[row-1]=id;n.queue[row]=tmp;Changed();}
                if(Button(new Rect(250,y,56,34),"下移",!n.transit&&row+1<n.queue.Length)){int tmp=n.queue[row+1];n.queue[row+1]=id;n.queue[row]=tmp;Changed();}
                if(Button(new Rect(width-64,y,60,34),"删除")){n.queue=n.queue.Where((v,i)=>i!=row).ToArray();ReindexGroups();Changed();break;}
            }
            if(Button(new Rect(4,440,160,34),n.transit?"＋ 4 人":"＋ 一排 4 人",n.queue.Length<4)){var groups=draft.groups.ToList();int id=groups.Count;groups.Add(new GroupSpec{id=id,color=n.transit&&n.queue.Length>0?draft.groups[n.queue[0]].color:0,hidden=n.curtain&&n.queue.Length>0});draft.groups=groups.ToArray();n.queue=n.queue.Concat(new[]{id}).ToArray();Changed();}
            GUI.Label(new Rect(4,488,width,44),"集结点 "+draft.GoalCount+" 个 · 总人数 "+draft.groups.Length*4+" 人",small);
            DrawSeed(width,544);
        }
        void DrawSeed(float width,float y)
        {
            GUI.Label(new Rect(4,y,width,30),"关卡种子",body);seedText=TextField(new Rect(4,y+40,150,34),seedText);
            if(Button(new Rect(168,y+40,140,34),"选取关卡"))EditorAction(()=>{int seed;if(!int.TryParse(seedText,out seed))throw new Exception("请输入整数种子");SetEditorDraft(CampaignRepository.SelectForEditor(catalog,seed));editorStatus="已载入关卡；修改后需重新验证";});
        }
        void DrawLibrary(float w,float h)
        {
            drawingModal=true;
            Fill(new Rect(0,0,w,h),new Color(.91f,.89f,.83f));GUI.Label(new Rect(28,24,w-170,42),"关卡库",title);
            if(Button(new Rect(w-220,28,95,36),"新关卡")){var fresh=new LevelSpec{name="我的群岛",tip="",nodes=new NodeSpec[0],edges=new EdgeSpec[0],groups=new GroupSpec[0],solution=new EdgeSpec[0]};SetEditorDraft(fresh);libraryOpen=false;}
            if(Button(new Rect(w-115,28,87,36),"关闭"))libraryOpen=false;
            int count=builtInCount+library.items.Count;libraryScroll=GUI.BeginScrollView(new Rect(24,88,w-48,h-120),libraryScroll,new Rect(0,0,w-75,count*56+30));
            for(int i=0;i<count;i++){bool builtin=i<builtInCount;var l=builtin?catalog.levels[i]:library.items[i-builtInCount].level;
                if(Button(new Rect(0,i*56,w-92,45),(builtin?"示例 "+(i+1)+" · ":"我的 · ")+l.name)){SetEditorDraft(l);if(!builtin)draftId=library.items[i-builtInCount].id;libraryOpen=false;}}
            GUI.EndScrollView();
        }
        void DrawShare(float w,float h)
        {
            drawingModal=true;
            Fill(new Rect(0,0,w,h),new Color(.16f,.24f,.26f,.93f));var panel=new Rect(24,h*.18f,w-48,h*.62f);Fill(panel,new Color(.95f,.92f,.85f));
            GUI.Label(new Rect(44,panel.y+20,w-88,42),"分享与导入",title);GUI.Label(new Rect(44,panel.y+70,w-88,55),"分享码包含完整关卡。复制给朋友，对方粘贴即可还原。",body);
            if(field==null)TextField(new Rect(0,0,0,0),"");shareCode=GUI.TextArea(new Rect(44,panel.y+140,w-88,panel.height-290),shareCode,new GUIStyle(field){wordWrap=true});
            GUI.Label(new Rect(44,panel.yMax-146,w-88,34),editorStatus,small);
            float bw=(w-108)/3,y=panel.yMax-104;
            if(Button(new Rect(44,y,bw,38),"复制"))WeChatPlatform.CopyText(shareCode,()=>editorStatus="分享码已复制",error=>editorStatus=error);
            if(Button(new Rect(54+bw,y,bw,38),"粘贴"))WeChatPlatform.PasteText(text=>shareCode=text,error=>editorStatus=error);
            if(Button(new Rect(64+2*bw,y,bw,38),"导入"))EditorAction(()=>{SetEditorDraft(LevelShare.Decode(shareCode));shareOpen=false;editorStatus="导入成功，可以直接试玩";});
            if(Button(new Rect(44,panel.yMax-52,w-88,36),"关闭"))shareOpen=false;
        }
    }
}
