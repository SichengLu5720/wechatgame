using UnityEngine;

namespace StairsCrowd.Runtime
{
    public sealed partial class StairsGame
    {
        void TickEditorEntrance()
        {
            if(!Home||InterfaceBlocksInput||editing||board==null)return;
#if UNITY_EDITOR || UNITY_STANDALONE
            if((Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl))&&(Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift))&&Input.GetKeyDown(KeyCode.E))OpenEditor();
#endif
        }
        void DrawHomeEditorButton(float scale)
        {
            var layout=GameplayLayout.Current;
            if(Button(new Rect(layout.Safe.xMax-114,layout.Gear.y,94,44),"编辑器",true,new Color(.86f,.87f,.80f)))OpenEditor();
        }
    }
}
