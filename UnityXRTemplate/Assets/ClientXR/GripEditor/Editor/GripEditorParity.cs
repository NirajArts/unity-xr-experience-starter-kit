using System;
using System.Linq;
using Oculus.Interaction.Input;
using Oculus.Interaction.HandGrab.Visuals;
using UnityEditor;
using UnityEngine;
namespace ClientXR.Grips.Editor
{
    public sealed partial class GripEditorWindow
    {
        [SerializeField] bool followSelection=true;
        [SerializeField] int editMode,mirrorPlane;
        float previewAngle,previewSize=1;
        [SerializeField] int selectedJoint=-1;
        public int SelectedJoint=>selectedJoint;
        public void SelectJoint(int joint)
        {
            if(joint<0 || joint>=FingersMetadata.HAND_JOINT_IDS.Length)throw new ArgumentOutOfRangeException(nameof(joint));
            selectedJoint=joint;editMode=2;scroll=Vector2.zero;Repaint();SceneView.RepaintAll();
        }
        public Transform SelectedJointTransform=>SelectedPreview.Ghost && selectedJoint>=0?SelectedPreview.Ghost.GetComponent<HandPuppet>().JointMaps.FirstOrDefault(m=>m.id==FingersMetadata.HAND_JOINT_IDS[selectedJoint])?.transform:null;
        public void RotateSelectedJoint(Quaternion world)
        {
            if(Current==null || !SelectedPreview.Ghost || selectedJoint<0)return;
            var selected=SelectedPreview.Ghost.GetComponent<HandPuppet>().JointMaps.FirstOrDefault(m=>m.id==FingersMetadata.HAND_JOINT_IDS[selectedJoint]);
            if(selected==null || !selected.transform || !selected.transform.parent)return;
            var desired=Quaternion.Inverse(selected.RotationOffset)*Quaternion.Inverse(selected.transform.parent.rotation)*world;
            Edit(x=>x.jointDegrees[selectedJoint]=GripClipboard.JointAdjustment(x,selectedJoint,desired),"Rotate finger joint");
        }
        string gripName="new-grip";
        GripPreview SelectedPreview=>Current!=null && Current.side==Handedness.Left?left:right;
        void DrawParityControls()
        {
            followSelection=EditorGUILayout.Toggle("Follow selection",followSelection);
            if(Current!=null && target && target.GetComponent<IGripObjectMotion>()!=null)
                EditorGUILayout.HelpBox("The interaction component controls object motion and wrist behavior. Saved grip position, rotation and fingers guide the object fit; constrained tools can keep rendered hands on their authored contacts. Existing disables the authored override.",MessageType.Info);
            else if(Current!=null && Current.policy!=GripContactPolicy.FixedVisualWrist)
            {
                EditorGUILayout.HelpBox(Current.policy==GripContactPolicy.FingersOnly?"Fingers Only applies your finger pose while the wrist follows tracking. Fixed Visual Wrist uses an authored wrist contact; moving the object to that contact requires a compatible mechanical motion adapter.":"Runtime override is OFF: Existing does not apply this authored preview.",Current.policy==GripContactPolicy.FingersOnly?MessageType.Info:MessageType.Warning);
                if(GUILayout.Button("Align rendered wrist to authored grip"))Edit(g=>g.policy=GripContactPolicy.FixedVisualWrist,"Enable authored wrist alignment");
            }
            editMode=GUILayout.SelectionGrid(editMode,new[]{"Move Hand","Rotate Hand","Fingers","Move Axis","Rotate Axis"},3);
            if(editMode==2 && Current!=null)
            {
                if(selectedJoint<0)SelectJoint(0);
                int joint=EditorGUILayout.Popup("Finger bone",selectedJoint,FingersMetadata.HAND_JOINT_IDS.Select(j=>j.ToString()).ToArray());
                if(joint!=selectedJoint)SelectJoint(joint);
                EditorGUILayout.HelpBox("Choose a bone here or click a joint dot in Scene view, then drag its rotation rings. Each bone supports independent X/Y/Z rotation for custom shapes. Apply / Save keeps your draft edits.",MessageType.Info);
                var degrees=EditorGUILayout.Vector3Field("Bone adjustment (degrees)",Current.jointDegrees[selectedJoint]);
                if(degrees!=Current.jointDegrees[selectedJoint])Edit(x=>x.jointDegrees[selectedJoint]=degrees,"Rotate finger joint");
                if(GUILayout.Button("Reset selected bone adjustment"))Edit(x=>x.jointDegrees[selectedJoint]=Vector3.zero,"Reset finger joint");
            }
            EditorGUI.BeginChangeCheck();
            previewSize=EditorGUILayout.Slider("Preview size",previewSize,.75f,1.25f);
            if(Current!=null && Current.axisEnabled)previewAngle=EditorGUILayout.Slider("Preview angle",previewAngle,-180,180);
            if(EditorGUI.EndChangeCheck())Refresh();
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Reset preview")){previewAngle=0;previewSize=1;Refresh();}
            if(GUILayout.Button("Frame Target and Hand"))
            {
                var b=GripPreview.GeometryBounds(target.transform);
                foreach(var preview in new[]{left,right})if(preview.Ghost)foreach(var r in preview.Ghost.GetComponentsInChildren<Renderer>())b.Encapsulate(r.bounds);
                SceneView.lastActiveSceneView?.Frame(b,false);
            }
            EditorGUILayout.EndHorizontal();
            if(Current==null)return;
            if(!string.IsNullOrEmpty(SelectedPreview.Error))EditorGUILayout.HelpBox(SelectedPreview.Error,MessageType.Error);
            EditorGUILayout.HelpBox("Ghost shows authored fallback. Free fingers follow tracking at runtime. Preview size/angle do not save. Wrist handles edit the reference pose; reset preview for alignment.",MessageType.None);
            gripName=EditorGUILayout.TextField("Grip name / ID",gripName);
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Create"))ManageGrip(0);
            if(GUILayout.Button("Duplicate"))ManageGrip(1);
            if(GUILayout.Button("Rename"))ManageGrip(2);
            if(GUILayout.Button("Remove") && EditorUtility.DisplayDialog("Remove grip definition?","Remove only "+Current.id+" / "+Current.side+" from the draft? Apply is still required.","Remove","Cancel"))ManageGrip(3);
            EditorGUILayout.EndHorizontal();
            if(Current.id!=target.activeGripId)EditorGUILayout.HelpBox("Editing "+Current.id+"; runtime still selects active ID '"+target.activeGripId+"'. Change GripTarget.activeGripId explicitly to use another grip.",MessageType.Info);
            var so=new SerializedObject(draft);so.Update();var g=so.FindProperty("grips").GetArrayElementAtIndex(index);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(g.FindPropertyRelative("axisEnabled"),new GUIContent("Enable approach axis"));
            if(g.FindPropertyRelative("axisEnabled").boolValue)
            {
                EditorGUILayout.PropertyField(g.FindPropertyRelative("axisPivot"),new GUIContent("Pivot (target-local metres)"));
                var direction=g.FindPropertyRelative("axisDirection");var v=EditorGUILayout.Vector3Field("Axis (target-local)",direction.vector3Value);if(v.sqrMagnitude>1e-8f)direction.vector3Value=v.normalized;
                EditorGUILayout.PropertyField(g.FindPropertyRelative("axisLimited"),new GUIContent("Limit angle"));
                if(g.FindPropertyRelative("axisLimited").boolValue){EditorGUILayout.PropertyField(g.FindPropertyRelative("axisMinimum"));EditorGUILayout.PropertyField(g.FindPropertyRelative("axisMaximum"));}
            }
            EditorGUILayout.PropertyField(g.FindPropertyRelative("contactEnabled"),new GUIContent("Compensate hand size"));
            if(g.FindPropertyRelative("contactEnabled").boolValue)EditorGUILayout.PropertyField(g.FindPropertyRelative("contactWristLocal"),new GUIContent("Explicit contact (wrist-local m)"));
            if(EditorGUI.EndChangeCheck()){so.ApplyModifiedProperties();hasUnsavedChanges=true;Remember();Refresh();}
            if(Current.policy!=GripContactPolicy.FixedVisualWrist && (Current.axisEnabled || Current.contactEnabled) && !(target && target.GetComponent<IGripObjectMotion>()!=null))EditorGUILayout.HelpBox("Axis and size compensation are preview-only for this policy. Runtime wrist alignment requires Fixed Visual Wrist. Existing passes everything through.",MessageType.Warning);
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Pivot at Wrist"))Edit(x=>x.axisPivot=x.positionMetres,"Place grip pivot");
            foreach(var axis in new[]{Vector3.right,Vector3.up,Vector3.forward})if(GUILayout.Button(axis==Vector3.right?"Hand X":axis==Vector3.up?"Hand Y":"Hand Z"))Edit(x=>x.axisDirection=(x.rotation*axis).normalized,"Orient grip axis");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("Contact is an explicit reference-hand point. Finger edits do not move it; recapture when changing the intended contact.",MessageType.None);
            using(new EditorGUI.DisabledScope(selectedJoint<0 || !SelectedPreview.Ghost))if(GUILayout.Button("Use selected joint as contact"))
            {
                var ghost=SelectedPreview.Ghost;var map=ghost.GetComponent<HandPuppet>().JointMaps.FirstOrDefault(m=>m.id==FingersMetadata.HAND_JOINT_IDS[selectedJoint]);
                if(map!=null){var h=ghost.transform.InverseTransformPoint(map.transform.position);Edit(x=>{x.contactWristLocal=h;x.contactEnabled=true;},"Capture explicit contact");}
            }
            mirrorPlane=EditorGUILayout.Popup("Local mirror plane",mirrorPlane,new[]{"YZ / flip X","XZ / flip Y","XY / flip Z"});
            using(new EditorGUI.DisabledScope(clipboard==null || clipboard.side==Current.side))if(GUILayout.Button("Paste mirrored into selected grip"))Paste(true);
        }
        public void ManageGrip(int operation)
        {
            if(Current==null)return;
            if(target.sharedProfile && (operation==2 || operation==3)){notice="Copy shared data to this object before renaming/removing IDs; unloaded consumers may reference them.";return;}
            string name=gripName.Trim();
            if(operation!=3 && (string.IsNullOrWhiteSpace(name)||draft.grips.Any(x=>x.id==name&&x.side==Current.side))){notice="Choose a unique nonempty name for this hand.";return;}
            if((operation==2 || operation==3)&&Current.id==target.activeGripId){notice="Active runtime grip is protected. Change activeGripId explicitly before renaming/removing it.";return;}
            if(operation==3 && draft.grips.Count<=1){notice="Keep at least one definition.";return;}
            Undo.RecordObject(draft,"Manage named grip");
            if(operation==0 || operation==1){var added=operation==1?Current.Clone():new GripDefinition{side=Current.side,basePose=Current.basePose};added.id=name;draft.grips.Add(added);index=draft.grips.Count-1;}
            else if(operation==2)Current.id=name;
            else{draft.grips.RemoveAt(index);index=Mathf.Clamp(index,0,draft.grips.Count-1);}
            previewAngle=0;selectedJoint=-1;hasUnsavedChanges=true;Remember();Refresh();
        }
        void Paste(bool mirror)
        {
            try{var incoming=mirror?clipboard.Mirrored(mirrorPlane):clipboard.Clone();incoming=GripClipboard.Rebase(incoming,Current);ReplaceCurrent(incoming);}catch(Exception e){notice=e.Message;}
        }
        void PasteFingers()
        {
            try{var incoming=GripClipboard.Rebase(clipboard.Clone(),Current);Edit(x=>{x.curl=incoming.curl;x.spread=incoming.spread;x.thumbOpposition=incoming.thumbOpposition;x.jointDegrees=incoming.jointDegrees;x.overrideFreedom=incoming.overrideFreedom;x.freedom=incoming.freedom;x.contactEnabled=incoming.contactEnabled;x.contactWristLocal=incoming.contactWristLocal;},"Paste grip fingers");}catch(Exception e){notice=e.Message;}
        }
        void DrawParityScene()
        {
            var grip=Current;Pose p=GripMath.ToWorld(target.transform,grip);var t=target.transform;
            var c=t.position+t.rotation*(grip.axisPivot*target.GripPositionScale);var a=t.rotation*grip.axisDirection;
            if(grip.axisEnabled && a.sqrMagnitude>.5f){Handles.color=Color.yellow;float radius=Mathf.Max(.05f,Vector3.ProjectOnPlane(p.position-c,a).magnitude);Handles.DrawLine(c-a*.15f,c+a*.15f);Handles.DrawWireDisc(c,a,radius);Handles.Label(c,"Approach axis - latched on grab");}
            EditorGUI.BeginChangeCheck();
            if(editMode==0){var pos=Handles.PositionHandle(p.position,Tools.pivotRotation==PivotRotation.Local?p.rotation:Quaternion.identity);if(EditorGUI.EndChangeCheck())Edit(x=>x.positionMetres=GripMath.ToLocal(t,new Pose(pos,p.rotation)).position,"Move hand");}
            else if(editMode==1){var rot=Handles.RotationHandle(p.rotation,p.position);if(EditorGUI.EndChangeCheck())Edit(x=>x.rotation=Quaternion.Inverse(t.rotation)*rot,"Rotate hand");}
            else if(editMode==3){var pos=Handles.PositionHandle(c,t.rotation);if(EditorGUI.EndChangeCheck())Edit(x=>x.axisPivot=Quaternion.Inverse(t.rotation)*(pos-t.position)/target.GripPositionScale,"Move axis");}
            else if(editMode==4){var rotation=Handles.RotationHandle(Quaternion.FromToRotation(Vector3.up,a),c);if(EditorGUI.EndChangeCheck())Edit(x=>x.axisDirection=(Quaternion.Inverse(t.rotation)*(rotation*Vector3.up)).normalized,"Rotate axis");}
            else
            {
                EditorGUI.EndChangeCheck();var ghost=SelectedPreview.Ghost;if(!ghost)return;
                var maps=ghost.GetComponent<HandPuppet>().JointMaps;
                foreach(var map in maps){int j=Array.IndexOf(FingersMetadata.HAND_JOINT_IDS,map.id);if(j<0 || !map.transform)continue;Handles.color=j==selectedJoint?Color.yellow:Color.cyan;float size=HandleUtility.GetHandleSize(map.transform.position)*.022f;if(Handles.Button(map.transform.position,Quaternion.identity,size,size*1.4f,Handles.DotHandleCap))SelectJoint(j);}
                if(selectedJoint<0)return;var selected=maps.FirstOrDefault(m=>m.id==FingersMetadata.HAND_JOINT_IDS[selectedJoint]);if(selected==null)return;
                Handles.color=Color.white;Handles.Label(selected.transform.position,Current.side+" / "+selected.id);
                EditorGUI.BeginChangeCheck();var world=Handles.RotationHandle(selected.transform.rotation,selected.transform.position);
                if(EditorGUI.EndChangeCheck())RotateSelectedJoint(world);
            }
        }
    }
    public static class GripClipboard
    {
        public static Vector3 JointAdjustment(GripDefinition grip,int joint,Quaternion desired)
        {
            var basis=grip.Clone();Array.Clear(basis.curl,0,5);Array.Clear(basis.spread,0,5);basis.thumbOpposition=0;basis.jointDegrees[joint]=Vector3.zero;
            var delta=Quaternion.Inverse(basis.Compose().JointRotations[joint])*desired;if(grip.side==Handedness.Right)delta=HandMirroring.Mirror(delta);
            int finger=(int)HandJointUtils.JointToFingerList[(int)FingersMetadata.HAND_JOINT_IDS[joint]];Vector3 shortcut=Vector3.zero;
            if(finger>=0 && finger<5 && FingersMetadata.HAND_JOINT_CAN_MOVE[joint]){shortcut.z=grip.curl[finger];if(FingersMetadata.HAND_JOINT_CAN_SPREAD[joint])shortcut.y=grip.spread[finger];if(finger==0 && joint==0)shortcut.x=grip.thumbOpposition;}
            return delta.eulerAngles-shortcut;
        }
        public static GripDefinition Rebase(GripDefinition incoming,GripDefinition destination)
        {
            if(incoming.side!=destination.side || incoming.mapping!=destination.mapping)throw new InvalidOperationException("Clipboard hand/skeleton mismatch.");
            string error=incoming.Validate()??destination.Validate();if(error!=null)throw new InvalidOperationException(error);
            var pose=incoming.Compose();var baseGrip=destination.Clone();Array.Clear(baseGrip.curl,0,5);Array.Clear(baseGrip.spread,0,5);Array.Clear(baseGrip.jointDegrees,0,baseGrip.jointDegrees.Length);baseGrip.thumbOpposition=0;var basis=baseGrip.Compose();
            incoming.basePose=destination.basePose;Array.Clear(incoming.curl,0,5);Array.Clear(incoming.spread,0,5);incoming.thumbOpposition=0;
            for(int i=0;i<incoming.jointDegrees.Length;i++){var q=Quaternion.Inverse(basis.JointRotations[i])*pose.JointRotations[i];if(incoming.side==Handedness.Right)q=HandMirroring.Mirror(q);incoming.jointDegrees[i]=q.eulerAngles;}
            for(int f=0;f<5;f++){incoming.overrideFreedom[f]=true;incoming.freedom[f]=pose.FingersFreedom[f];}incoming.id=destination.id;return incoming;
        }
    }
}
