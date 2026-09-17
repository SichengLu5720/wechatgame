using UnityEngine;

namespace StairsCrowd.Runtime
{
    public sealed partial class StairsGame
    {
        void TickEditorEntrance()
        {
            if(!Home||DailyCalendarOpen||editing||board==null)return;
#if UNITY_EDITOR || UNITY_STANDALONE
            if((Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl))&&(Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift))&&Input.GetKeyDown(KeyCode.E))OpenEditor();
#endif
        }
        void DrawHomeEditorButton(float scale)
        {
            float top=Mathf.Max(64,(Screen.height-Screen.safeArea.yMax)/scale+12);
            if(Button(new Rect(18,top,94,44),"编辑器",true,new Color(.86f,.87f,.80f)))OpenEditor();
        }
    }
}
