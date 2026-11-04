using UnityEngine;
using UnityEngine.EventSystems;

namespace XRStarter
{
    public sealed class DemoButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public InteractionFeedback feedback;
        Vector3 original;
        void Awake() => original = transform.localScale;
        public void OnPointerEnter(PointerEventData _) => transform.localScale = original * 1.025f;
        public void OnPointerExit(PointerEventData _) => transform.localScale = original;
        public void OnPointerClick(PointerEventData _) => feedback?.Play(FeedbackCue.Button, transform.position);
        void OnDisable() { if (original != Vector3.zero) transform.localScale = original; }
    }
}
