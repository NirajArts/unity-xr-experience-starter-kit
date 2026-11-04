using System;
using System.Linq;
using Oculus.Interaction.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ClientXR.Grips.Editor
{
    public sealed partial class GripEditorWindow : EditorWindow
    {
        [SerializeField] GripTarget target;
        [SerializeField] GripProfile draft;
        [SerializeField] int index;
        [SerializeField] int previewMode=2;
        [SerializeField] bool sharedEdit;
        [SerializeField] bool showJoints;
        [SerializeField] bool sceneWasDirty;
        Matrix4x4 lastTargetMatrix;
        bool lastTargetActive;
        Vector2 scroll;
        string notice;
        readonly GripPreview left=new GripPreview(),right=new GripPreview();
        static GripDefinition clipboard;
        public GripTarget Target=>target;
        public GripProfile Draft=>draft;
        public GripDefinition Current=>draft && draft.grips.Count>0?draft.grips[Mathf.Clamp(index,0,draft.grips.Count-1)]:null;
        [MenuItem("Tools/XR/Grip Editor")]
        public static void ShowWindow(){GetWindow<GripEditorWindow>("Grip Editor").Show();}
        public static GripEditorWindow Open(GripTarget value)
        {
            var w=GetWindow<GripEditorWindow>("Grip Editor");w.UseTarget(value);w.Show();return w;
        }
        void OnEnable()
        {
            minSize=new Vector2(380,480);saveChangesMessage="Apply the Grip Editor draft before closing? Discard leaves saved data unchanged.";
            Selection.selectionChanged+=SelectionChanged;Undo.undoRedoPerformed+=UndoChanged;
            SceneView.duringSceneGui+=SceneGUI;EditorApplication.playModeStateChanged+=PlayChanged;
            AssemblyReloadEvents.beforeAssemblyReload+=Remember;
            if(draft)draft.hideFlags=HideFlags.HideInHierarchy | HideFlags.DontSave;
            if(target && !draft)LoadDraft();Refresh();
        }
        void Update(){if(!target){if(left.Ghost || right.Ghost)ClearPreview();return;}if(lastTargetMatrix!=target.transform.localToWorldMatrix || lastTargetActive!=target.gameObject.activeInHierarchy){lastTargetMatrix=target.transform.localToWorldMatrix;lastTargetActive=target.gameObject.activeInHierarchy;Refresh();}}
        void OnDisable()
        {
            Remember();ClearPreview();Selection.selectionChanged-=SelectionChanged;Undo.undoRedoPerformed-=UndoChanged;
            SceneView.duringSceneGui-=SceneGUI;EditorApplication.playModeStateChanged-=PlayChanged;AssemblyReloadEvents.beforeAssemblyReload-=Remember;
        }
        string DraftKey=>target?"ClientXR.GripDraft."+GlobalObjectId.GetGlobalObjectIdSlow(target):"";
        void Remember(){if(target && draft && hasUnsavedChanges)SessionState.SetString(DraftKey,JsonUtility.ToJson(draft));}
        void PlayChanged(PlayModeStateChange mode){ClearPreview();if(mode==PlayModeStateChange.EnteredEditMode)Refresh();}
        void UndoChanged(){if(!hasUnsavedChanges && target)LoadDraft();Refresh();}
        void ClearPreview(){left.Clear();right.Clear();SceneView.RepaintAll();}
        bool ConfirmLeaving()
        {
            if(!hasUnsavedChanges)return true;
            int answer=EditorUtility.DisplayDialogComplex("Unsaved grip draft","Apply or discard the current target's draft?","Apply","Cancel","Discard");
            if(answer==1)return false;if(answer==0){SaveChanges();return !hasUnsavedChanges;}DiscardChanges();return true;
        }
        void SelectionChanged()
        {
            if(!followSelection)return;var go=Selection.activeGameObject;if(!go)return;
            var candidate=go.GetComponentInParent<GripTarget>();
            if(candidate && candidate!=target && ConfirmLeaving())UseTarget(candidate);
            Repaint();
        }
        public void UseTarget(GripTarget value)
        {
            if(target==value && draft)return;
            ClearPreview();if(draft)DestroyImmediate(draft);
            previewAngle=0;previewSize=1;selectedJoint=-1;target=value;sceneWasDirty=target && target.gameObject.scene.isDirty;sharedEdit=false;index=0;LoadDraft();Refresh();
        }
        void LoadDraft()
        {
            if(!target)return;
            if(!draft){draft=CreateInstance<GripProfile>();draft.hideFlags=HideFlags.HideInHierarchy | HideFlags.DontSave;}
            draft.grips=target.Definitions.Select(g=>g.Clone()).ToList();hasUnsavedChanges=false;
            string recovery=SessionState.GetString(DraftKey,"");
            if(!string.IsNullOrEmpty(recovery)){JsonUtility.FromJsonOverwrite(recovery,draft);hasUnsavedChanges=true;notice="Recovered unsaved draft; review before Apply.";}
        }
        public override void SaveChanges()
        {
            try
            {
                if(!target || !draft)return;
                GripAuthoring.Apply(target,draft,sharedEdit);
                if(target.sharedProfile)AssetDatabase.SaveAssetIfDirty(target.sharedProfile);
                else if(PrefabStageUtility.GetCurrentPrefabStage()!=null)
                {
                    var stage=PrefabStageUtility.GetCurrentPrefabStage();
                    PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot,stage.assetPath);
                }
                else if(!sceneWasDirty && !string.IsNullOrEmpty(target.gameObject.scene.path))EditorSceneManager.SaveScene(target.gameObject.scene);
                else notice="Applied with Undo. Scene already had unsaved work: save the scene explicitly to persist everything.";
                hasUnsavedChanges=false;SessionState.EraseString(DraftKey);base.SaveChanges();Refresh();
            }
            catch(Exception e){notice=e.Message;Repaint();}
        }
        public override void DiscardChanges()
        {
            if(target)SessionState.EraseString(DraftKey);hasUnsavedChanges=false;LoadDraft();base.DiscardChanges();Refresh();
        }
        public void Edit(Action<GripDefinition> change,string name)
        {
            if(Current==null)return;Undo.RecordObject(draft,name);change(Current);hasUnsavedChanges=true;Remember();Refresh();
        }
        public static int VisibleModeForEdit(int mode, Handedness side)=>mode==0 || mode==1?(side==Handedness.Left?0:1):mode;
        public void SetPreviewMode(int mode)
        {
            previewMode=mode;
            if((mode==0 || mode==1) && Current!=null)
            {
                var side=mode==0?Handedness.Left:Handedness.Right;
                int matching=draft.grips.FindIndex(g=>g.id==Current.id && g.side==side);
                if(matching>=0 && matching!=index){index=matching;previewAngle=0;selectedJoint=-1;}
            }
            Refresh();
        }
        public void Refresh()
        {
            if(!target || !draft || EditorApplication.isPlayingOrWillChangePlaymode){ClearPreview();Repaint();return;}
            if(Current==null){ClearPreview();Repaint();return;}
            index=Mathf.Clamp(index,0,draft.grips.Count-1);
            if(Current!=null)previewMode=VisibleModeForEdit(previewMode,Current.side);
            // Undo changes draft serialization, not persistent source data, until Apply.
            var l=draft.grips.FirstOrDefault(g=>g.id==Current.id && g.side==Handedness.Left);
            var r=draft.grips.FirstOrDefault(g=>g.id==Current.id && g.side==Handedness.Right);
            if(previewMode==0 || previewMode==2)left.Show(target.transform,l,true,previewAngle,previewSize);else left.Clear();
            if(previewMode==1 || previewMode==2)right.Show(target.transform,r,previewMode!=2,previewAngle,previewSize);else right.Clear();
            Repaint();SceneView.RepaintAll();
        }
        void OnGUI()
        {
            EditorGUILayout.LabelField("Grip Editor",EditorStyles.boldLabel);
            if(EditorApplication.isPlayingOrWillChangePlaymode){EditorGUILayout.HelpBox("Play Mode: saved grips are read by the SDK visual adapter. Authoring is suspended.",MessageType.Info);return;}
            scroll=EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.ObjectField("Target",target,typeof(GripTarget),true);
            var selected=Selection.activeGameObject;
            if(selected){var parent=selected.GetComponentInParent<Oculus.Interaction.HandGrab.HandGrabInteractable>(true);if(parent)selected=parent.gameObject;}
            if(selected && (!target || selected!=target.gameObject))
            {
                string unsupported=GripAuthoring.Unsupported(selected);
                if(unsupported!=null)EditorGUILayout.HelpBox(unsupported,MessageType.Info);
                else if(GUILayout.Button("Set up / edit selected target (Undoable)") && ConfirmLeaving())
                {
                    bool dirty=selected.scene.isDirty;UseTarget(GripAuthoring.Bind(selected));sceneWasDirty=dirty;
                }
            }
            if(!target || !draft){EditorGUILayout.HelpBox("Select the part/tool owning a Meta HandGrabInteractable. Setup explicitly binds only this object and compatible near-grab visual sources in the loaded rig.",MessageType.Info);EditorGUILayout.EndScrollView();return;}
            EditorGUILayout.HelpBox(GripAuthoring.Destination(target),MessageType.Info);
            if(!target.gameObject.activeInHierarchy)EditorGUILayout.HelpBox("Inactive target: temporary geometry preview only; scene activation is unchanged.",MessageType.Info);
            EditorGUILayout.LabelField("Adapter",target.interactable?target.interactable.GetType().Name:"MISSING");
            if(PrefabStageUtility.GetCurrentPrefabStage()==null && !Resources.FindObjectsOfTypeAll<GripVisualState>().Any(v=>v.gameObject.scene==target.gameObject.scene))
            {EditorGUILayout.HelpBox("This scene needs the grip visual connection on its Meta near-grab rig.",MessageType.Warning);if(GUILayout.Button("Install grip visual support (Undoable)")){int bound=GripAuthoring.BindSceneVisuals();notice="Installed "+bound+" compatible visual connections; save the scene explicitly.";}}
            if(target.sharedProfile)
            {
                var users=Resources.FindObjectsOfTypeAll<GripTarget>().Where(t=>t.sharedProfile==target.sharedProfile).Select(t=>t.name).Distinct();
                EditorGUILayout.HelpBox("Loaded shared consumers: "+string.Join(", ",users)+". Other unloaded assets may also reference this profile.",MessageType.Warning);
                sharedEdit=EditorGUILayout.Toggle("Edit shared asset deliberately",sharedEdit);
                if(GUILayout.Button("Duplicate into this object's own data"))
                {Undo.RecordObject(target,"Isolate grip ownership");target.grips=target.Definitions.Select(g=>g.Clone()).ToList();target.sharedProfile=null;EditorUtility.SetDirty(target);EditorSceneManager.MarkSceneDirty(target.gameObject.scene);sharedEdit=false;}
            }
            string problem=target.Validate();if(problem!=null)EditorGUILayout.HelpBox(problem,MessageType.Warning);
            EditorGUI.BeginChangeCheck();
            int nextIndex=EditorGUILayout.Popup("Edit grip",index,draft.grips.Select(g=>g.id+" / "+g.side).ToArray());if(nextIndex!=index){index=nextIndex;previewAngle=0;selectedJoint=-1;}
            int requestedPreview=GUILayout.Toolbar(previewMode,new[]{"Edit / Preview Left","Edit / Preview Right","Both","Off"});
            if(requestedPreview!=previewMode)SetPreviewMode(requestedPreview);
            if(EditorGUI.EndChangeCheck())Refresh();
            DrawParityControls();
            var grip=Current;
            if(grip!=null)EditorGUILayout.LabelField("Editing "+grip.side+" hand • "+(grip.side==Handedness.Left?"blue":"orange"),EditorStyles.boldLabel);
            if(grip!=null)
            {
                var so=new SerializedObject(draft);so.Update();var g=so.FindProperty("grips").GetArrayElementAtIndex(index);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(g.FindPropertyRelative("policy"),new GUIContent("Contact policy"));
                EditorGUILayout.HelpBox("Existing keeps all runtime behavior. Fingers Only preserves free/fingertip contact. Fixed Visual Wrist aligns the rendered hand only; the existing object/tool transformer keeps ownership.",MessageType.None);
                EditorGUILayout.PropertyField(g.FindPropertyRelative("basePose"),new GUIContent("Meta base pose"));
                EditorGUILayout.PropertyField(g.FindPropertyRelative("positionMetres"),new GUIContent(target.scaleGripPositions?"Wrist offset (reference metres)":"Wrist offset (metres)"));
                Vector3 euler=EditorGUILayout.Vector3Field("Wrist rotation (degrees)",grip.rotation.eulerAngles);
                if(euler!=grip.rotation.eulerAngles)g.FindPropertyRelative("rotation").quaternionValue=Quaternion.Euler(euler);
                for(int f=0;f<5;f++)
                {
                    EditorGUILayout.LabelField(((HandFinger)f).ToString(),EditorStyles.boldLabel);
                    var curl=g.FindPropertyRelative("curl").GetArrayElementAtIndex(f);curl.floatValue=EditorGUILayout.Slider("Curl adjustment",curl.floatValue,-80,100);
                    var spread=g.FindPropertyRelative("spread").GetArrayElementAtIndex(f);spread.floatValue=EditorGUILayout.Slider("Spread adjustment",spread.floatValue,-35,35);
                    var ov=g.FindPropertyRelative("overrideFreedom").GetArrayElementAtIndex(f);EditorGUILayout.PropertyField(ov,new GUIContent("Override base freedom"));
                    if(ov.boolValue)EditorGUILayout.PropertyField(g.FindPropertyRelative("freedom").GetArrayElementAtIndex(f),new GUIContent("Visual finger freedom"));
                }
                var thumb=g.FindPropertyRelative("thumbOpposition");thumb.floatValue=EditorGUILayout.Slider("Thumb opposition",thumb.floatValue,-90,90);
                showJoints=EditorGUILayout.Foldout(showJoints,"Precise joint adjustments (degrees)",true);
                if(showJoints)for(int j=0;j<FingersMetadata.HAND_JOINT_IDS.Length;j++)EditorGUILayout.PropertyField(g.FindPropertyRelative("jointDegrees").GetArrayElementAtIndex(j),new GUIContent(FingersMetadata.HAND_JOINT_IDS[j].ToString()));
                if(EditorGUI.EndChangeCheck()){so.ApplyModifiedProperties();hasUnsavedChanges=true;Remember();Refresh();}else so.ApplyModifiedProperties();
                if(grip.Validate()!=null)EditorGUILayout.HelpBox(grip.Validate(),MessageType.Warning);
                EditorGUILayout.BeginHorizontal();
                if(GUILayout.Button("Copy grip"))clipboard=grip.Clone();
                using(new EditorGUI.DisabledScope(clipboard==null || clipboard.mapping!=GripMath.Mapping || clipboard.side!=grip.side))
                {
                    if(GUILayout.Button("Paste full"))Paste(false);
                    if(GUILayout.Button("Paste placement"))Edit(x=>{x.positionMetres=clipboard.positionMetres;x.rotation=clipboard.rotation;x.axisEnabled=clipboard.axisEnabled;x.axisPivot=clipboard.axisPivot;x.axisDirection=clipboard.axisDirection;x.axisLimited=clipboard.axisLimited;x.axisMinimum=clipboard.axisMinimum;x.axisMaximum=clipboard.axisMaximum;},"Paste grip placement");
                    if(GUILayout.Button("Paste fingers")){PasteFingers();}
                }
                EditorGUILayout.EndHorizontal();
                if(GUILayout.Button("Mirror to opposite hand (replace its draft)"))
                {
                    var mirror=grip.Mirrored(mirrorPlane);int other=draft.grips.FindIndex(x=>x.side==mirror.side && x.id==grip.id);
                    Undo.RecordObject(draft,"Mirror grip");if(other>=0)draft.grips[other]=GripClipboard.Rebase(mirror,draft.grips[other]);else draft.grips.Add(mirror);hasUnsavedChanges=true;Remember();Refresh();
                }
                EditorGUILayout.BeginHorizontal();
                if(GUILayout.Button("Reset fingers to base"))Edit(x=>{Array.Clear(x.curl,0,5);Array.Clear(x.spread,0,5);Array.Clear(x.jointDegrees,0,x.jointDegrees.Length);Array.Clear(x.overrideFreedom,0,5);x.thumbOpposition=0;},"Reset grip fingers");
                if(GUILayout.Button("Reset placement"))Edit(x=>{x.positionMetres=Vector3.zero;x.rotation=Quaternion.identity;},"Reset grip placement");
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.LabelField(hasUnsavedChanges?"UNSAVED DRAFT":"Draft matches last Apply/Revert",EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Apply / Save"))SaveChanges();if(GUILayout.Button("Revert draft"))DiscardChanges();
            EditorGUILayout.EndHorizontal();
            if(!string.IsNullOrEmpty(notice))EditorGUILayout.HelpBox(notice,MessageType.Info);
        }
        void ReplaceCurrent(GripDefinition value){Undo.RecordObject(draft,"Paste grip");value.id=Current.id;draft.grips[index]=value;hasUnsavedChanges=true;Remember();Refresh();}
        void SceneGUI(SceneView view)
        {
            if(!target || Current==null || previewMode==3 || EditorApplication.isPlayingOrWillChangePlaymode)return;
            var previousColor=Handles.color;
            try{DrawParityScene();}finally{Handles.color=previousColor;}
        }
    }
}
