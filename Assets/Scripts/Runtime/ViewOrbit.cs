using UnityEngine;

namespace StairsCrowd.Runtime
{
    public sealed partial class CrowdScene
    {
        float orbitDepth=34;
        public void OrbitView(float degrees)
        {
            if(float.IsNaN(degrees)||float.IsInfinity(degrees))return;
            // Pan and zoom remain intact: orbit about the current view centre.
            var pivot=camera.transform.position+camera.transform.forward*orbitDepth;
            camera.transform.RotateAround(pivot,Vector3.up,degrees);
            if(!HomeFraming&&KeepOrbitInFrame){
                float required=0;
                for(int n=0;n<space.Centers.Length;n++){
                    float extent=space.Half(n)/Mathf.Cos(Mathf.PI/space.Sides[n])+.15f;
                    for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)for(int edge=0;edge<2;edge++){
                        float y=edge==0?-2.6f:PersonHeight+.25f;
                        var offset=space.Centers[n]+new Vector3(x*extent,y,z*extent)-pivot;
                        required=Mathf.Max(required,Mathf.Max(Mathf.Abs(Vector3.Dot(offset,camera.transform.up))/.68f,Mathf.Abs(Vector3.Dot(offset,camera.transform.right))/Mathf.Max(.1f,camera.aspect)));
                    }
                }
                // Do not shrink while dragging; this avoids zoom pulsing during a turn.
                if(KeepOrbitInFrame)camera.orthographicSize=Mathf.Max(camera.orthographicSize,required*1.04f/PresentationScale);
            }
            KeepPresentationVisible();FaceClouds();
        }
        public bool KeepOrbitInFrame=true;
    }

    public sealed partial class StairsGame
    {
        public static bool UserRotationEnabled => false;
        bool viewPointerActive,viewPointerDragged,viewPointerPreview;Vector2 viewPointerStart,viewPointerLast;int viewFinger=-1;
        public bool PressPreviewActive => viewPointerPreview;
        public bool ViewPointerDragged {get{return viewPointerDragged;}}
        public void CancelViewPointer(){if(viewPointerPreview&&scene!=null&&board!=null)scene.Highlight(board,selected);viewPointerPreview=false;viewPointerActive=false;viewPointerDragged=false;viewFinger=-1;}
        bool CanUseView(){return !FailureLocked&&!InterfaceBlocksInput&&board!=null&&!editing&&!IntroVisible&&!chooseLevel&&!shareOpen&&!libraryOpen&&!(board.Solved&&!Busy&&!Home);}
        bool ViewArea(Vector2 point){return view.pixelRect.Contains(point)&&(Home?(point.y>Screen.height*.26f&&point.y<Screen.height*.77f):GameplayLayout.Current.PlayPixels.Contains(point));}
        public void BeginViewPointer(Vector2 point)
        {
            CancelViewPointer();if(TuningContains(point)||!CanUseView()||!ViewArea(point))return;
            viewPointerActive=true;viewPointerStart=viewPointerLast=point;
            // Visual response on contact; committing on release preserves drag cancellation.
            if(!Home&&!IsAssembling&&selected<0&&PropSelection==0&&!scene.HasAddition&&!board.Solved){
                Physics.SyncTransforms();int node;
                if((TryPickPerson(point,out node)||TryPickNode(point,out node))&&CanSelect(node)){scene.Highlight(board,node);viewPointerPreview=true;}
            }
        }
        public void MoveViewPointer(Vector2 point)
        {
            if(!viewPointerActive)return;if(TuningContains(point)||!CanUseView()){CancelViewPointer();return;}
            float threshold=Mathf.Max(8,Screen.width*.018f);
            if(!viewPointerDragged&&(point-viewPointerStart).sqrMagnitude>threshold*threshold){viewPointerDragged=true;viewPointerLast=viewPointerStart;if(viewPointerPreview){scene.Highlight(board,selected);viewPointerPreview=false;}}
            if(UserRotationEnabled&&viewPointerDragged&&PropSelection==0&&!scene.HasAddition){scene.OrbitView((point.x-viewPointerLast.x)/Mathf.Max(1,view.pixelWidth)*180);viewPointerLast=point;}
        }
        public void EndViewPointer(Vector2 point,bool cancelled=false)
        {
            if(!viewPointerActive)return;MoveViewPointer(point);
            bool tap=viewPointerActive&&!viewPointerDragged&&!cancelled&&CanUseView()&&ViewArea(point)&&!Home;
            CancelViewPointer();if(tap)HandlePointer(point);
        }
        void TickViewInput()
        {
            RefreshGameplayLayout();
            if(RuntimeVerification.Active||CloudVerification.Active||SettingsPropsVerification.Active)return;
            if(!CanUseView()){CancelViewPointer();return;}
            if(Input.touchCount>0){
                if(Input.touchCount!=1){CancelViewPointer();return;}
                var touch=Input.GetTouch(0);
                if(touch.phase==TouchPhase.Began){BeginViewPointer(touch.position);if(viewPointerActive)viewFinger=touch.fingerId;}
                else if(touch.fingerId==viewFinger){
                    if(touch.phase==TouchPhase.Ended||touch.phase==TouchPhase.Canceled)EndViewPointer(touch.position,touch.phase==TouchPhase.Canceled);
                    else MoveViewPointer(touch.position);
                }
                return;
            }
            if(viewFinger>=0){CancelViewPointer();return;}
            if(Input.GetMouseButtonDown(0))BeginViewPointer(Input.mousePosition);
            if(Input.GetMouseButton(0))MoveViewPointer(Input.mousePosition);
            if(Input.GetMouseButtonUp(0))EndViewPointer(Input.mousePosition);
        }
        void OnApplicationFocus(bool focus){PauseDaily(StairsCrowd.Core.DailyPause.Focus,!focus);if(Feedback!=null)Feedback.Suspend(!focus);if(!focus){CancelViewPointer();CancelTuningGesture();}}
        void OnApplicationPause(bool paused){PauseDaily(StairsCrowd.Core.DailyPause.Application,paused);if(Feedback!=null)Feedback.Suspend(paused);if(paused){CancelViewPointer();CancelTuningGesture();}}
    }
}

