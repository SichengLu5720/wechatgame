using UnityEngine;
namespace StairsCrowd.Runtime
{
    public sealed partial class CrowdScene
    {
        public bool HasReveal {get{if(people!=null)foreach(var p in people)if(p.RevealAnimating)return true;return false;}}
        public void TickReveals(float delta){if(people!=null)foreach(var p in people)p.TickReveal(delta);}
    }
    public sealed class ColorReveal
    {
        readonly PersonView person;readonly MaterialPropertyBlock block=new MaterialPropertyBlock();Renderer hiddenRenderer;
        bool known,hidden;float clock=.42f;
        public bool Active=>clock<.42f;
        public bool UsesMask=>hidden||(Active&&clock<.1f);
        public float Tint {get;private set;}=1;
        public ColorReveal(PersonView person){this.person=person;}
        public void Reset(){known=false;clock=.42f;}
        public void Set(bool value){
            if(!known){known=true;hidden=value;clock=.42f;Apply();return;}
            if(value==hidden)return;
            bool previous=hidden;hidden=value;clock=previous&&!value?0:.42f;Apply();
        }
        public void Tick(float delta){if(!Active)return;clock=Mathf.Min(.42f,clock+Mathf.Max(0,delta));Apply();}
        void Apply(){
            // Fade the question into black, then bring the original character out of black.
            bool mask=UsesMask;
            person.waterBird.SetHidden(mask);
            Tint=hidden?1:Mathf.SmoothStep(0,1,Mathf.Clamp01((clock-.1f)/.32f));
            person.waterBird.SetReveal(Tint);
            if(person.hiddenCharacter){
                person.hiddenCharacter.SetActive(mask);
                if(!hiddenRenderer)hiddenRenderer=person.hiddenCharacter.GetComponent<Renderer>();
                hiddenRenderer.GetPropertyBlock(block);block.SetFloat("_QuestionVisibility",hidden?1:1-Mathf.SmoothStep(0,1,Mathf.Clamp01(clock/.1f)));hiddenRenderer.SetPropertyBlock(block);
            }
        }
    }
}
