using System;
using System.Collections.Generic;

namespace XRTest.EditorTools.CodexSceneBridge
{
    internal static class BridgeProtocol
    {
        public const string Version = "1.2.0-starter";
    }

    [Serializable]
    internal sealed class BridgeRequest
    {
        public string requestId;
        public string action;
        public string expectedProjectRoot;
        public string expiresUtc;
        public string sessionToken;
        public string globalObjectId;
        public string propertyPath;
        public string expectedCurrentValue;
        public bool hasExpectedCurrentValue;
        public bool includeHidden;
        public bool dryRun;
        public bool save;
        public int maxObjects = 5000;
        public int maxProperties = 2000;
        public string nameContains;
        public string componentTypeContains;
        public List<string> assetPaths = new List<string>();
        public string scenePath;
        public bool saveCurrentScenes;
        public string objectName;
        public string primitiveType;
        public string parentGlobalObjectId;
        public string componentType;
        public string expectedParentGlobalObjectId;
        public bool hasExpectedParentGlobalObjectId;
        public string newParentGlobalObjectId;
        public BridgeValue value;
    }

    [Serializable]
    internal sealed class BridgeValue
    {
        public string kind;
        public bool boolValue;
        public long integerValue;
        public double numberValue;
        public string stringValue;
        public string enumName;
        public int enumIndex;
        public string globalObjectId;
        public float x;
        public float y;
        public float z;
        public float w;
        public float width;
        public float height;
        public float sizeX;
        public float sizeY;
        public float sizeZ;
        public int intX;
        public int intY;
        public int intZ;
    }

    [Serializable]
    internal sealed class BridgeResponse
    {
        public string requestId;
        public string action;
        public string bridgeVersion;
        public string timestampUtc;
        public bool success;
        public string error;
        public BridgeEditorState editorState;
        public List<BridgeSceneSnapshot> scenes = new List<BridgeSceneSnapshot>();
        public List<BridgeObjectSnapshot> objects = new List<BridgeObjectSnapshot>();
        public BridgeObjectInspection inspection;
        public BridgeMutationResult mutation;
        public BridgeAuthoringResult authoring;
        public List<BridgeSelfTestStep> selfTestSteps = new List<BridgeSelfTestStep>();
        public List<string> messages = new List<string>();
    }

    [Serializable]
    internal sealed class BridgeEditorState
    {
        public string projectRoot;
        public string unityVersion;
        public string activeSceneGuid;
        public bool isPlaying;
        public bool isPaused;
        public bool isCompiling;
        public bool isUpdating;
        public string activeScenePath;
        public string activeSceneName;
        public bool activeSceneIsDirty;
        public int loadedSceneCount;
        public bool prefabStageOpen;
        public string prefabStageAssetPath;
        public string exchangeDirectory;
    }

    [Serializable]
    internal sealed class BridgeSceneSnapshot
    {
        public string name;
        public string path;
        public int buildIndex;
        public bool isLoaded;
        public bool isActive;
        public bool isDirty;
        public int rootCount;
        public int objectCount;
        public bool objectsTruncated;
        public List<BridgeObjectSnapshot> objects = new List<BridgeObjectSnapshot>();
    }

    [Serializable]
    internal sealed class BridgeObjectSnapshot
    {
        public string globalObjectId;
        public string name;
        public string hierarchyPath;
        public string scenePath;
        public bool activeSelf;
        public bool activeInHierarchy;
        public int layer;
        public string layerName;
        public string tag;
        public bool isStatic;
        public bool isPrefabInstance;
        public bool hasPrefabOverrides;
        public string prefabAssetPath;
        public List<BridgeComponentSummary> components = new List<BridgeComponentSummary>();
    }

    [Serializable]
    internal sealed class BridgeComponentSummary
    {
        public string globalObjectId;
        public string type;
        public string assembly;
        public bool missing;
        public bool isBehaviour;
        public bool enabled;
    }

    [Serializable]
    internal sealed class BridgeObjectInspection
    {
        public string requestedGlobalObjectId;
        public string targetKind;
        public BridgeObjectSnapshot gameObject;
        public List<BridgeComponentInspection> inspectedTargets = new List<BridgeComponentInspection>();
    }

    [Serializable]
    internal sealed class BridgeComponentInspection
    {
        public string globalObjectId;
        public string type;
        public string assembly;
        public string assetPath;
        public bool propertiesTruncated;
        public List<BridgePropertySnapshot> properties = new List<BridgePropertySnapshot>();
    }

    [Serializable]
    internal sealed class BridgePropertySnapshot
    {
        public string propertyPath;
        public string displayName;
        public string propertyType;
        public string value;
        public int depth;
        public bool editable;
        public bool redacted;
        public bool truncated;
        public bool isArray;
        public int arraySize;
        public string objectReferenceGlobalObjectId;
        public string objectReferenceAssetPath;
    }

    [Serializable]
    internal sealed class BridgeMutationResult
    {
        public string targetGlobalObjectId;
        public string targetType;
        public string targetPath;
        public string propertyPath;
        public string propertyType;
        public string beforeValue;
        public string proposedValue;
        public string afterValue;
        public bool dryRun;
        public bool changed;
        public bool undoRecorded;
        public bool sceneWasDirtyBefore;
        public bool sceneIsDirtyAfter;
        public bool saved;
        public string saveDetails;
    }

    [Serializable]
    internal sealed class BridgeAuthoringResult
    {
        public string operation;
        public string assetPath;
        public string scenePath;
        public string targetGlobalObjectId;
        public string componentGlobalObjectId;
        public string hierarchyPathBefore;
        public string hierarchyPathAfter;
        public string componentType;
        public bool created;
        public bool changed;
        public bool undoRecorded;
        public bool saved;
        public bool sceneWasDirtyBefore;
        public bool sceneIsDirtyAfter;
        public string details;
        public List<string> affectedPaths = new List<string>();
    }

    [Serializable]
    internal sealed class BridgeSelfTestStep
    {
        public string name;
        public bool passed;
        public string details;
    }
}
