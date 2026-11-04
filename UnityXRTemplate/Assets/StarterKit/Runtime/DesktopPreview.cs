using Oculus.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace XRStarter
{
    /// <summary>Explicit desktop preview. It exercises SDK pointer events, not tracked headset input.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class DesktopPreview : MonoBehaviour
    {
        public GameObject xrRig;
        public Camera desktopCamera;
        public GameObject desktopInput;
        public GameObject xrInput;
        public bool forceDesktop;
        ExperiencePart held;
        float distance;
        float wristYaw;
        const int PointerId = -9001;
        public bool DesktopActive { get; private set; }
        void Awake()
        {
            DesktopActive = forceDesktop || !XRSettings.isDeviceActive;
#if UNITY_ANDROID && !UNITY_EDITOR
            DesktopActive = forceDesktop;
#endif
            xrRig.SetActive(!DesktopActive);
            desktopCamera.gameObject.SetActive(DesktopActive);
            // Avoid briefly registering two EventSystems while changing input mode.
            desktopInput.SetActive(false); xrInput.SetActive(false);
            (DesktopActive ? desktopInput : xrInput).SetActive(true);
            // Package canvas surfaces require their matching module even in Start.
            // Disable the complete XR surface objects before Start when previewing on desktop.
            foreach (var surface in FindObjectsOfType<PointableCanvas>(true)) surface.gameObject.SetActive(!DesktopActive);
            var rig = xrRig.GetComponentInChildren<OVRCameraRig>(true);
            Camera eventCamera = DesktopActive ? desktopCamera : rig.centerEyeAnchor.GetComponent<Camera>();
            foreach (var canvas in FindObjectsOfType<Canvas>(true)) if (canvas.renderMode == RenderMode.WorldSpace) canvas.worldCamera = eventCamera;
        }
        void Update()
        {
            if (!DesktopActive || Mouse.current == null) return;
            var mouse = Mouse.current;
            Ray ray = desktopCamera.ScreenPointToRay(mouse.position.ReadValue());
            if (mouse.leftButton.wasPressedThisFrame && !(UnityEngine.EventSystems.EventSystem.current && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()))
            {
                if (Physics.Raycast(ray, out var hit, 6) && hit.collider.GetComponentInParent<ExperiencePart>() is ExperiencePart part && part.State != PartState.Animating)
                {
                    held = part; distance = hit.distance; wristYaw = 0;
                    held.grabbable.ProcessPointerEvent(new PointerEvent(PointerId, PointerEventType.Hover, new Pose(hit.point, Quaternion.identity)));
                    held.grabbable.ProcessPointerEvent(new PointerEvent(PointerId, PointerEventType.Select, new Pose(hit.point, Quaternion.identity)));
                }
            }
            if (held && held.grabbable.enabled && held.Held)
            {
                distance = Mathf.Clamp(distance + mouse.scroll.ReadValue().y * .001f, .25f, 4);
                if (Keyboard.current != null) wristYaw += (Keyboard.current.qKey.isPressed ? -1 : Keyboard.current.eKey.isPressed ? 1 : 0) * 70 * Time.unscaledDeltaTime;
                Quaternion rotation = Quaternion.Euler(0, wristYaw, 0);
                var pose = new Pose(ray.GetPoint(distance), rotation);
                held.grabbable.ProcessPointerEvent(new PointerEvent(PointerId, PointerEventType.Move, pose));
                if (mouse.leftButton.wasReleasedThisFrame) Release(pose);
            }
            else held = null;
        }
        void Release(Pose pose)
        {
            if (!held) return;
            held.grabbable.ProcessPointerEvent(new PointerEvent(PointerId, PointerEventType.Unselect, pose));
            held.grabbable.ProcessPointerEvent(new PointerEvent(PointerId, PointerEventType.Unhover, pose)); held = null;
        }
        void OnDisable() { if (held) Release(new Pose(held.transform.position, held.transform.rotation)); }
    }
}
