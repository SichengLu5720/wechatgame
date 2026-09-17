using System;
using UnityEngine;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public sealed partial class StairsGame : MonoBehaviour
    {
        public Catalog catalog;public Board board;public WalkSpace space;public CrowdScene scene;public Camera view;
        public MotionPlan motion;public float clock;public int levelIndex,selected=-1;
        public string message="";public InteractionFeedback Feedback {get;private set;}bool[] completedFeedback;
        readonly System.Collections.Generic.List<float> arrivalTimes=new System.Collections.Generic.List<float>();
        Font font;bool ownsFont;GUIStyle title,body,small,button;Texture2D white,startHero;
        public bool Home {get;private set;}public bool IsAssembling{get{return assembly!=null||reveal<.22f;}} public double LastMoveMilliseconds{get;private set;}public double LastPlanningMilliseconds{get;private set;}public double LastCompositionMilliseconds{get;private set;} public bool LastMoveCached{get;private set;} AssemblySequence assembly;float reveal=1;bool drawingModal; readonly System.Collections.Generic.Dictionary<int,WalkSpace> spaces=new System.Collections.Generic.Dictionary<int,WalkSpace>(); int lastWidth,lastHeight;bool chooseLevel;public bool Busy{get{return TutorialExiting||PreparingMove||motion!=null||IsAssembling||(scene!=null&&(scene.HasReveal||scene.HasRetreat||scene.HasAddition));}}
        void InitializeGame()
        {
            var preview=GameObject.Find("Editor Preview");if(preview)DestroyImmediate(preview);Application.targetFrameRate=60;Application.runInBackground=true;
#if UNITY_WEBGL && !UNITY_EDITOR
            QualitySettings.antiAliasing=2;
#else
            QualitySettings.antiAliasing=4;
#endif
            catalog=CampaignRepository.Load();
            builtInCount=catalog.levels.Length;
            font=Resources.Load<Font>("Fonts/IslandsUI");if(!font){ownsFont=true;font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Noto Sans CJK SC","Arial"},24);}
            white=Texture2D.whiteTexture;startHero=Resources.Load<Texture2D>("start_hero");
            view=Camera.main;if(!view){view=new GameObject("Main Camera",typeof(Camera)).GetComponent<Camera>();view.tag="MainCamera";}
            view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=NightBackground;nightBackdrop=new GameObject("Night backdrop",typeof(Camera)).GetComponent<Camera>();nightBackdrop.depth=view.depth-10;nightBackdrop.cullingMask=0;nightBackdrop.clearFlags=CameraClearFlags.SolidColor;nightBackdrop.backgroundColor=NightBackground;
            TowerBackground=nightBackdrop.gameObject.AddComponent<FixedTowerBackdrop>();TowerBackground.Initialize(view);
            RenderSettings.ambientLight=new Color(.78f,.78f,.87f);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            var light=new GameObject("Soft key light",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.transform.rotation=Quaternion.Euler(45,-30,0);light.shadows=LightShadows.Soft;
            islandAudio=gameObject.AddComponent<IslandAudio>();Feedback=new InteractionFeedback();ReturnHome();
            
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-perftest")>=0)gameObject.AddComponent<PerformanceVerification>();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-autotest")>=0)gameObject.AddComponent<RuntimeVerification>();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-cloudtest")>=0)gameObject.AddComponent<CloudVerification>();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-campaigntest")>=0)gameObject.AddComponent<CampaignRuntimeVerification>();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-propstest")>=0)gameObject.AddComponent<SettingsPropsVerification>();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-hiddenarttest")>=0)gameObject.AddComponent<HiddenArtVerification>();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tutorialtest")>=0)gameObject.AddComponent<TutorialVerification>();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-skytest")>=0)gameObject.AddComponent<SkyPreviewVerification>();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-playbacktest")>=0)gameObject.AddComponent<PlaybackVerification>();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-loadingtest")>=0)gameObject.AddComponent<LoadingVerification>();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-deliverytest")>=0)gameObject.AddComponent<DeliveryPlayVerification>();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-failureprogresstest")>=0)gameObject.AddComponent<FailureProgressRuntimeVerification>();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-dailytest")>=0)gameObject.AddComponent<DailyChallengeRuntimeVerification>();

            
        }
        public void LoadLevel(int index){LeaveDaily();DisposeEditorPreview();editing=false;Home=false;if(index<builtInCount)customPlaying=false;LoadBoard(index,true);}
        void LoadBoard(int index,bool animate)
        {
            CancelPending();ClearFailureState("level loaded",true);ResetProps();levelIndex=DailyActive?-1:Mathf.Clamp(index,0,catalog.levels.Length-1);board=new Board(DailyActive?dailyLevel:catalog.levels[levelIndex]);BeginAttempt();SyncCompletionFeedback();
            var prepared=DailyActive?null:ConsumePreparedScene(levelIndex);
            if(prepared!=null){if(spaces.Count>=3)spaces.Clear();spaces[levelIndex]=prepared.space;}
            if(!spaces.TryGetValue(levelIndex,out space)||space.Level!=board.Level){if(spaces.Count>=3)spaces.Clear();var nav=Resources.Load<TextAsset>(DailyActive?DailyChallengeRepository.NavigationPath(board.Level):CampaignRepository.NavigationPath(board.Level,levelIndex));space=new WalkSpace(board.Level,nav?nav.bytes:null,CampaignRepository.GeometryOnly(board.Level));spaces[levelIndex]=space;}
            SetViewport();if(scene==null||scene.space!=space){var previous=scene;scene=prepared??new CrowdScene(space,view);scene.TakePeopleFrom(previous);if(previous!=null)previous.Dispose();}scene.root.SetActive(true);scene.HomeFraming=Home;scene.FitCamera();scene.ClearPeople();selected=-1;chooseLevel=false;message="";scene.Highlight(board,-1);
            scene.RestoreWorld(board.Current);ConfigureIntro();WarmMoves();if(animate)BeginAssembly();EvaluateCurrentBoard();
        }
        void WarmMoves(){FastMovement.Prepare(space);}
        public void ReturnHome(){LeaveDaily();DisposeEditorPreview();editing=false;customPlaying=false;shareOpen=false;libraryOpen=false;Home=true;LoadBoard(CampaignProgress.CurrentIndex(builtInCount),true);scene.root.SetActive(true);}
        public void StartGame(){if(!Home||DailyCalendarOpen)return;LeaveDaily();editing=false;Home=false;BeginAttempt();SetViewport();scene.HomeFraming=false;scene.FitCamera();ConfigureIntro();WarmMoves();BeginAssembly();EvaluateCurrentBoard();}
        void BeginAssembly(){if(assembly!=null)assembly.Advance(assembly.Duration);scene.RestoreWorld(board.Current);scene.ClearPeople();assembly=new AssemblySequence(scene);reveal=1;message="";}        void CancelPending(bool settle=true){arrivalTimes.Clear();CancelTutorialExit();CancelPendingMove();CancelViewPointer();DisposeIntroPreview();if(Feedback!=null)Feedback.Cancel();if(settle&&assembly!=null)assembly.Advance(assembly.Duration);assembly=null;reveal=1;motion=null;clock=0;}
        public void ResetLevel(){if(Home||!ResetDailyAttempt())return;CancelPending();ClearFailureState("level reset",true);board.Reset();BeginAttempt();ResetProps();if(space.Level!=board.Level){scene.Dispose();space=new WalkSpace(board.Level);scene=new CrowdScene(space,view);scene.HomeFraming=false;SetViewport();scene.FitCamera();}SyncCompletionFeedback();ConfigureIntro();WarmMoves();BeginAssembly();selected=-1;scene.Highlight(board,-1);message="";EvaluateCurrentBoard();}
        public void Undo(){if(Home||IsAssembling||IntroVisible||FailureLocked||!DailyInput())return;if(!board.CanUndo)return;board.Undo();RefreshPropWorld();message="";EvaluateCurrentBoard();}
        void Rebuild(){selected=-1;scene.RestoreWorld(board.Current);scene.Populate(board.Current);scene.Highlight(board,-1);for(int n=0;n<board.Level.nodes.Length;n++)if(Rules.Complete(board.Level,board.Current,n))scene.StartRetreat(n);}
        public bool TryMoveSync(int a,int b)
        {
            if(FailureLocked||PropSelection!=0||(scene!=null&&scene.HasAddition)||InterfaceBlocksInput||Home||IsAssembling||IntroVisible||editing||!DailyInput())return false;var move=Rules.Preview(board.Level,board.Current,a,b);if(!move.ok){message=move.reason;return false;}
            if(!DailyInput(true))return false;
            try{var timer=System.Diagnostics.Stopwatch.StartNew();LastMoveCached=false;var plan=FastMovement.Build(space,board.Current,move);LastPlanningMilliseconds=timer.Elapsed.TotalMilliseconds;if(!Commit(plan))return false;LastMoveMilliseconds=timer.Elapsed.TotalMilliseconds;return true;}
            catch(Exception error){if(CloudVerification.Active)Debug.Log("MOVE DIAGNOSTIC "+error);message="这条路线暂时无法通行，可以继续调整布局";return false;}
        }
        bool Commit(MotionPlan plan){var composition=System.Diagnostics.Stopwatch.StartNew();var combined=MotionComposer.Append(motion,clock,plan);if(combined!=plan)combined.PreparePlayback(space);LastCompositionMilliseconds=composition.Elapsed.TotalMilliseconds;if(!DailyInput())return false;board.TryMove(plan.move.from,plan.move.to,plan.finalSlots);EvaluateCurrentBoard();for(int i=0;i<arrivalTimes.Count;i++)arrivalTimes[i]=Mathf.Max(0,arrivalTimes[i]-clock);
            float arrival=0;foreach(int group in plan.move.ids)for(int member=0;member<Rules.MembersPerGroup;member++){var t=combined.Track(group*Rules.MembersPerGroup+member);if(t!=null)arrival=Mathf.Max(arrival,t.End);}arrivalTimes.Add(arrival);
            motion=combined;WarmMoves();clock=0;selected=-1;PropTarget=-1;message="";scene.Highlight(board,-1);Advance(1f/120);return true;}
        bool CanSelect(int node)
        {
            return node>=0&&node<board.Level.nodes.Length&&!board.Level.nodes[node].sticky&&board.Current.queues[node].Count>0&&!Rules.Gated(board.Level,board.Current,node)&&!Rules.Complete(board.Level,board.Current,node);
        }
        void SelectNode(int node)
        {
            selected=node;message=board.Level.nodes[node].transit?"已选中同色人群，请选择目标平台":"已选中这一排，请选择目标平台";scene.Highlight(board,selected);Feedback.Select();if(islandAudio)islandAudio.Play();
        }
        bool SameFrontColor(int a,int b)
        {
            var qa=board.Current.queues[a];var qb=board.Current.queues[b];
            return qa.Count>0&&qb.Count>0&&board.Level.groups[qa[0]].color==board.Level.groups[qb[0]].color;
        }
        public void ClickPerson(int node)
        {
            if(FailureLocked||InterfaceBlocksInput||Home||IsAssembling||IntroVisible||editing||board.Solved||node<0||node>=board.Level.nodes.Length||!DailyInput(true))return;if(PropSelection!=0){ChoosePropTarget(node);return;}if(scene.HasAddition)return;RememberPropTarget(node);
            if(selected==node){selected=-1;message="";scene.Highlight(board,-1);return;}
            if(selected>=0&&SameFrontColor(selected,node)){RequestMove(selected,node);return;}
            if(CanSelect(node))SelectNode(node);
        }
        public void ClickNode(int node)
        {
            if(FailureLocked||InterfaceBlocksInput||Home||IsAssembling||IntroVisible||editing||board.Solved||node<0||node>=board.Level.nodes.Length||!DailyInput(true))return;if(PropSelection!=0){ChoosePropTarget(node);return;}if(scene.HasAddition)return;RememberPropTarget(node);
            if(selected==node){selected=-1;message="";scene.Highlight(board,-1);return;}
            if(selected<0){
                if(CanSelect(node)){SelectNode(node);return;}
                if(board.Current.queues[node].Count==0&&!Rules.Gated(board.Level,board.Current,node)){message="先选择要移动的人群，再点击目标平台";return;}
                message=Rules.Gated(board.Level,board.Current,node)?"先完成一种颜色，解锁此平台":Rules.Complete(board.Level,board.Current,node)?"这个排序点已经完成集合":"先选择要移动的人群";return;
            }
            var move=Rules.Preview(board.Level,board.Current,selected,node);
            if(move.ok){RequestMove(selected,node);return;}
            if(CanSelect(node)&&!SameFrontColor(selected,node)){SelectNode(node);return;}
            message=move.reason;
        }
        void SyncCompletionFeedback(bool remember=false)
        {
            completedFeedback=new bool[board.Level.nodes.Length];
            for(int n=0;n<completedFeedback.Length;n++)completedFeedback[n]=remember&&Rules.Complete(board.Level,board.Current,n);
        }
        void CheckCompletionFeedback()
        {
            for(int n=0;n<completedFeedback.Length;n++){
                if(completedFeedback[n]||!Rules.Complete(board.Level,board.Current,n))continue;bool arrived=true;
                for(int row=0;row<board.Current.queues[n].Count&&arrived;row++)for(int member=0;member<Rules.MembersPerGroup;member++){
                    int id=board.Current.queues[n][row]*Rules.MembersPerGroup+member;var track=motion==null?null:motion.Track(id);
                    if((track!=null&&clock<track.End-.0001f)||Vector3.Distance(scene.people[id].root.position,space.SeatFor(board.Current,n,row,member))>.005f){arrived=false;break;}
                }
                if(arrived&&SafeToRetreat(n)){completedFeedback[n]=true;Feedback.Complete();if(islandAudio)islandAudio.Play(true);scene.StartRetreat(n);}
            }
            if(board.Solved&&motion==null&&!PreparingMove&&Rules.Completed(board.Level,board.Current)==board.Level.GoalCount)scene.StartFinalRetreat();
        }
        void Awake(){WeChatPlatform.BackgroundChanged+=DailyPlatformBackground;WeChatPlatform.Initialize(InitializeGame);}
        void Update()
        {
            TickDaily();
            if(settingsOpen){if(Input.GetKeyDown(KeyCode.Escape))CloseSettings();return;}TickEditorEntrance();
            if(editing)TickEditorPreview();
            if(board==null)return;
            if(Screen.width!=lastWidth||Screen.height!=lastHeight){SetViewport();if(editing){editorFitRequested=true;editorPreviewDirty=true;}else scene.FitCamera();}
            if(!HiddenArtVerification.Active&&!TutorialVerification.Active&&!RuntimeVerification.Active&&!CloudVerification.Active&&!SettingsPropsVerification.Active&&!FailureProgressRuntimeVerification.Active)Advance(Mathf.Min(Time.unscaledDeltaTime,.05f));
            TickViewInput();
            if(Input.GetKeyDown(KeyCode.Escape)){CancelPropSelection();chooseLevel=false;}
        }
        public void Advance(float delta)
        {
            if(settingsOpen)return;if(scene!=null)scene.TickReveals(delta);if(AdvanceTutorialExit(delta))return;if(Feedback!=null)Feedback.Tick(delta);
            if(assembly!=null){if(!Home)scene.PreparePresentation(board.Current,2);assembly.Advance(delta);if(assembly.Complete&&(Home||scene.PresentationPrepared)){assembly=null;if(Home){reveal=1;return;}scene.Populate(board.Current);reveal=0;foreach(var p in scene.people)p.root.localScale=Vector3.one*.001f;}return;}
            if(reveal<.22f){reveal=Mathf.Min(.22f,reveal+Mathf.Max(0,delta));float scale=Mathf.SmoothStep(.001f,1,reveal/.22f);foreach(var p in scene.people)p.root.localScale=Vector3.one*scale;return;}
            if(Home||editing||IntroVisible)return;
            if(motion==null){Feedback.Walk(delta,false);CheckCompletionFeedback();scene.TickWorld(board.Current,delta);PrepareNextScene();return;}clock=Mathf.Min(clock+Mathf.Max(0,delta),motion.duration);
            bool walking=false;foreach(var track in motion.tracks){int id=track.actor;
                if(clock<track.start||clock>=track.End&&clock-delta>=track.End)continue;
                if(scene.IsRetiring(scene.people[id].tag.node))continue;var p=track.PlaybackPosition(clock);var previous=scene.people[id].root.position;
                if(p.x==previous.x&&p.z==previous.z){scene.people[id].Pose(previous,Vector3.zero,0,false);continue;}
                var direction=Vector3.ProjectOnPlane(p-previous,Vector3.up);
                if(clock<track.End&&direction.sqrMagnitude>.000001f)walking=true;
                scene.people[id].Pose(p,direction,clock*15*FastMovement.SpeedMultiplier+id,clock<track.End&&direction.sqrMagnitude>.000001f);
            }

            if(clock>=motion.duration){motion=null;for(int n=0;n<board.Current.queues.Length;n++)foreach(int group in board.Current.queues[n])for(int member=0;member<Rules.MembersPerGroup;member++){int id=group*Rules.MembersPerGroup+member;scene.people[id].waterBird.Reset();scene.people[id].Pose(space.SeatFor(board.Current,n,board.Current.queues[n].IndexOf(group),member),space.Facing(n),0,false);}scene.RefreshPeople(board.Current);scene.Highlight(board,selected);message="";}
            int completions=Feedback.CompletionCount;CheckCompletionFeedback();
            bool arrived=false;for(int i=arrivalTimes.Count-1;i>=0;i--)if(clock>=arrivalTimes[i]-.0001f){arrived=true;arrivalTimes.RemoveAt(i);}
            if(arrived&&Feedback.CompletionCount==completions)Feedback.Arrive();
            Feedback.Walk(delta,walking&&motion!=null);
            scene.TickWorld(board.Current,delta);
        }
        void SetViewport(){lastWidth=Screen.width;lastHeight=Screen.height;if(editing){SetEditorViewport();return;}view.rect=new Rect(0,0,1,1);}
        void OnDestroy(){WeChatPlatform.BackgroundChanged-=DailyPlatformBackground;DiscardPreparedScene();DisposeEditorPreview();CancelPending(false);ClearFailureState("game destroyed",true);if(scene!=null)scene.Dispose();if(ownsFont&&font)Destroy(font);if(uiCircle)Destroy(uiCircle);if(nightBackdrop)Destroy(nightBackdrop.gameObject);}
        GUIStyle Style(int size,FontStyle weight=FontStyle.Normal,TextAnchor anchor=TextAnchor.MiddleLeft)
        {return new GUIStyle{font=font,fontSize=size,fontStyle=weight,alignment=anchor,normal={textColor=new Color(.26f,.32f,.36f)},wordWrap=true};}
        void InitStyles(){if(title!=null)return;title=Style(31,FontStyle.Bold);body=Style(17);small=Style(13);button=Style(17,FontStyle.Bold,TextAnchor.MiddleCenter);}
        void Fill(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,white);GUI.color=Color.white;}
        bool Button(Rect r,string label,bool enabled=true,Color? tint=null)
        {
            bool previous=GUI.enabled;enabled=enabled&&previous&&(!(chooseLevel||shareOpen||libraryOpen)||drawingModal);Fill(r,enabled?(tint??new Color(.88f,.84f,.76f)):new Color(.85f,.83f,.79f));GUI.enabled=enabled;bool clicked=GUI.Button(r,label,button);GUI.enabled=previous;return clicked;
        }
        public void HandlePointer(Vector2 screen)
        {
            if(InterfaceBlocksInput)return;Physics.SyncTransforms();int personNode,platformNode;bool hitPerson=TryPickPerson(screen,out personNode),hitPlatform=TryPickNode(screen,out platformNode);
            if(selected>=0&&hitPlatform&&(!hitPerson||personNode!=platformNode)){ClickNode(platformNode);return;}
            if(hitPerson){ClickPerson(personNode);return;}if(hitPlatform){ClickNode(platformNode);return;}
            if(selected>=0&&TryPickNearestNode(screen,out platformNode)){ClickNode(platformNode);return;}
            if(TryPickNearestPerson(screen,out personNode)){ClickPerson(personNode);return;}if(TryPickNearestNode(screen,out platformNode))ClickNode(platformNode);
        }
        bool TryPickNearestNode(Vector2 screen,out int node)
        {
            node=-1;if(scene==null||space==null)return false;float best=float.PositiveInfinity;int picked=-1;
            for(int n=0;n<space.Centers.Length;n++){
                var root=scene.NodeRoot(n);if(root==null||!root.gameObject.activeInHierarchy||scene.IsRetiring(n))continue;
                var world=space.Centers[n]+Vector3.up*.02f;var point=view.WorldToScreenPoint(world);if(point.z<=0||!view.pixelRect.Contains(point))continue;
                float half=space.Half(n)*.92f;var axisX=view.WorldToScreenPoint(world+root.right*half);var axisZ=view.WorldToScreenPoint(world+root.forward*half);
                float radius=Mathf.Max(24,Mathf.Max(Vector2.Distance(point,axisX),Vector2.Distance(point,axisZ))*1.12f);float score=Vector2.Distance(screen,new Vector2(point.x,point.y))/radius;
                if(score<best){best=score;picked=n;}
            }
            if(picked<0||best>1f)return false;node=picked;return true;
        }
        bool TryPickNearestPerson(Vector2 screen,out int node)
        {
            node=-1;if(scene==null||scene.people==null)return false;float best=float.PositiveInfinity,limit=Mathf.Max(26,Screen.width*.065f);int picked=-1;
            foreach(var person in scene.people){
                if(person==null||!person.root.gameObject.activeInHierarchy||scene.IsRetiring(person.tag.node))continue;
                var point=view.WorldToScreenPoint(person.root.position+Vector3.up*(CrowdScene.PersonHeight*.65f));if(point.z<=0||!view.pixelRect.Contains(point))continue;
                float distance=Vector2.Distance(screen,new Vector2(point.x,point.y));if(distance<best){best=distance;picked=person.tag.node;}
            }
            if(picked<0||best>limit)return false;node=picked;return true;
        }
        public bool TryPickPerson(Vector2 screen,out int node)
        {
            var ray=view.ScreenPointToRay(screen);RaycastHit person,ground;
            if(Physics.Raycast(ray,out person,100,1<<9,QueryTriggerInteraction.Collide)){
                // Do not select a character occluded by a higher platform.
                if(!Physics.Raycast(ray,out ground,100,1<<8,QueryTriggerInteraction.Collide)||person.distance<=ground.distance+.01f){
                    var tag=person.collider.GetComponent<PlatformTag>();if(tag){node=tag.node;return true;}
                }
            }
            node=-1;return false;
        }
        public bool TryPickNode(Vector2 screen,out int node)
        {
            var ray=view.ScreenPointToRay(screen);RaycastHit hit;
            // A passing crowd must not hide the platform underneath the pointer.
            if(Physics.Raycast(ray,out hit,100,1<<8,QueryTriggerInteraction.Collide)){
                var platform=hit.collider.GetComponent<PlatformTag>();if(platform!=null){node=platform.node;return true;}
            }
            if(Physics.Raycast(ray,out hit,100,1<<9,QueryTriggerInteraction.Collide)){
                var person=hit.collider.GetComponent<PlatformTag>();if(person!=null){node=person.node;return true;}
            }
            node=-1;return false;
        }
        void OnGUI()
        {
            DrawCloudGUI();
        }
        void LegacyGUI()
        {
            if(board==null)return;drawingModal=false;InitStyles();float scale=Mathf.Min(Screen.width/600f,Screen.height/900f);float w=Screen.width/scale,h=Screen.height/scale;
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));float pad=28;if(Home){Fill(new Rect(0,0,w,h*.25f),new Color(.9f,.85f,.8f));Fill(new Rect(0,h*.72f,w,h*.28f),new Color(.9f,.85f,.8f));if(startHero!=null){float heroW=Mathf.Min(w-pad*2,320),heroH=heroW*startHero.height/startHero.width,maxH=h*.18f;if(heroH>maxH){heroH=maxH;heroW=heroH*startHero.width/startHero.height;}GUI.DrawTexture(new Rect((w-heroW)/2,h*.025f,heroW,heroH),startHero,ScaleMode.ScaleToFit,true);}if(Button(new Rect(w/2-105,h*.80f,210,54),"开始",true,new Color(.68f,.78f,.69f)))StartGame();GUI.matrix=Matrix4x4.identity;return;}Fill(new Rect(0,0,w,h*.18f),new Color(.9f,.85f,.8f));
            GUI.Label(new Rect(pad,18,w-150,25),"S T A I R S   S O R T     /     群 岛",small);
            GUI.Label(new Rect(pad,49,w-150,44),board.Level.name,title);
            if(Button(new Rect(w-118,44,90,42),"第 "+(levelIndex+1)+" 关",!IsAssembling))chooseLevel=!chooseLevel;
            GUI.Label(new Rect(pad,103,220,27),"集合 "+Rules.Completed(board.Level,board.Current)+" / "+board.Level.ColorCount+"      步数 "+board.Moves,body);
            float bottom=h*.88f;Fill(new Rect(0,bottom,w,h-bottom),new Color(.95f,.91f,.84f));
            if(!string.IsNullOrEmpty(message))GUI.Label(new Rect(pad,bottom-35,w-pad*2,30),message,small);
            float gap=12,bw2=(w-pad*2-gap*2)/3,y=bottom+20;
            if(Button(new Rect(pad,y,bw2,44),"↶ 撤销",board.CanUndo&&!IsAssembling))Undo();
            if(Button(new Rect(pad+(bw2+gap),y,bw2,44),"重开",!IsAssembling))ResetLevel();
            if(Button(new Rect(pad+(bw2+gap)*2,y,bw2,44),"主页"))ReturnHome();

            if(board.Solved&&!Busy&&!SeamlessTutorial){Fill(new Rect(w/2-190,h*.39f,380,164),new Color(.96f,.92f,.83f,.99f));GUI.Label(new Rect(w/2-160,h*.39f+15,320,45),"集合完成",title);GUI.Label(new Rect(w/2-160,h*.39f+64,320,26),"用 "+board.Moves+" 步整理好了全部人群",body);if(Button(new Rect(w/2-160,h*.39f+106,320,42),levelIndex+1<catalog.levels.Length?"下一关 →":"再次挑战",true,new Color(.63f,.79f,.69f)))LoadLevel((levelIndex+1)%catalog.levels.Length);}
            if(chooseLevel){drawingModal=true;
                Fill(new Rect(0,0,w,h),new Color(.76f,.79f,.76f,.95f));float left=w/2-235,top=h/2-225;Fill(new Rect(left,top,470,450),new Color(.96f,.92f,.83f));
                GUI.Label(new Rect(left+25,top+20,350,44),"选择关卡",title);
                if(Button(new Rect(left+390,top+22,54,40),"关闭")){chooseLevel=false;}
                for(int i=0;i<catalog.levels.Length;i++)if(Button(new Rect(left+25,top+83+i*54,420,44),(i+1)+"   "+catalog.levels[i].name))LoadLevel(i);
            }
            GUI.matrix=Matrix4x4.identity;
        }
    }
}


