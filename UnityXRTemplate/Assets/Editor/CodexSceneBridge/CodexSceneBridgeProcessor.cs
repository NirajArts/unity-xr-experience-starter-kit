using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace XRTest.EditorTools.CodexSceneBridge
{
    internal static class CodexSceneBridgeProcessor
    {
        private const int MaxStringLength = 4096;

        public static BridgeResponse Process(BridgeRequest request, string exchangeDirectory)
        {
            var response = CreateResponse(request, exchangeDirectory);

            try
            {
                string actualRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (string.IsNullOrEmpty(request.expectedProjectRoot) || !string.Equals(Path.GetFullPath(request.expectedProjectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), actualRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Project identity mismatch. Send expectedProjectRoot for this exact Unity project.");
                if (!DateTime.TryParse(request.expiresUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiry) || expiry.ToUniversalTime() < DateTime.UtcNow)
                    throw new InvalidOperationException("Request is expired or has no valid deadline. Inspect status and send a new request.");
                switch ((request.action ?? string.Empty).Trim().ToLowerInvariant())
                {
                    case "starter_validate":
                    case "starter_capture":
                    case "starter_enter_play":
                    case "starter_exit_play":
                    case "starter_reset_demo":
                        response.messages.Add(XRStarter.Editor.StarterAutomation.Execute(request.action,request.requestId,request.sessionToken));
                        response.success = true;
                        break;
                    case "status":
                        response.success = true;
                        break;
                    case "scene_summary":
                        PopulateSceneSummary(request, response);
                        response.success = true;
                        break;
                    case "find_objects":
                        FindObjects(request, response);
                        response.success = true;
                        break;
                    case "inspect_object":
                        response.inspection = InspectObject(request);
                        response.success = true;
                        break;
                    case "set_property":
                        response.mutation = SetProperty(request);
                        response.success = true;
                        break;
                    case "ensure_asset_folders":
                        response.authoring = EnsureAssetFolders(request);
                        response.success = true;
                        break;
                    case "create_scene":
                        response.authoring = CreateScene(request);
                        PopulateSceneSummary(request, response);
                        response.success = true;
                        break;
                    case "open_scene":
                        response.authoring = OpenScene(request);
                        PopulateSceneSummary(request, response);
                        response.success = true;
                        break;
                    case "save_scene":
                        response.authoring = SaveScene(request);
                        response.success = true;
                        break;
                    case "create_game_object":
                        response.authoring = CreateGameObject(request, response);
                        response.success = true;
                        break;
                    case "add_component":
                        response.authoring = AddComponent(request, response);
                        response.success = true;
                        break;
                    case "reparent_object":
                        response.authoring = ReparentObject(request, response);
                        response.success = true;
                        break;
                    case "self_test":
                        if (EditorApplication.isPlayingOrWillChangePlaymode)
                            throw new InvalidOperationException("Bridge self-test requires Edit Mode; the current Play session is preserved.");
                        RunSelfTest(response);
                        response.success = AllSelfTestsPassed(response.selfTestSteps);
                        if (!response.success)
                        {
                            response.error = "One or more bridge self-test steps failed.";
                        }

                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Unsupported action '{request.action}'. Allowed actions: status, scene_summary, find_objects, inspect_object, set_property, ensure_asset_folders, create_scene, open_scene, save_scene, create_game_object, add_component, reparent_object, self_test.");
                }
            }
            catch (Exception exception)
            {
                response.success = false;
                response.error = exception.Message;
            }

            response.editorState = BuildEditorState(exchangeDirectory);
            return response;
        }

        public static BridgeResponse CreateFailure(string requestId, string action, string error, string exchangeDirectory)
        {
            var response = CreateResponse(
                new BridgeRequest { requestId = requestId, action = action },
                exchangeDirectory);
            response.success = false;
            response.error = error;
            return response;
        }

        private static BridgeResponse CreateResponse(BridgeRequest request, string exchangeDirectory)
        {
            return new BridgeResponse
            {
                requestId = request.requestId,
                action = request.action,
                bridgeVersion = BridgeProtocol.Version,
                timestampUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                editorState = BuildEditorState(exchangeDirectory)
            };
        }

        private static BridgeEditorState BuildEditorState(string exchangeDirectory)
        {
            var activeScene = SceneManager.GetActiveScene();
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            return new BridgeEditorState
            {
                projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath,"..")),
                unityVersion = Application.unityVersion,
                activeSceneGuid = AssetDatabase.AssetPathToGUID(activeScene.path),
                isPlaying = EditorApplication.isPlaying,
                isPaused = EditorApplication.isPaused,
                isCompiling = EditorApplication.isCompiling,
                isUpdating = EditorApplication.isUpdating,
                activeScenePath = activeScene.path,
                activeSceneName = activeScene.name,
                activeSceneIsDirty = activeScene.IsValid() && activeScene.isDirty,
                loadedSceneCount = SceneManager.sceneCount,
                prefabStageOpen = prefabStage != null,
                prefabStageAssetPath = prefabStage != null ? prefabStage.assetPath : string.Empty,
                exchangeDirectory = exchangeDirectory
            };
        }

        private static void PopulateSceneSummary(BridgeRequest request, BridgeResponse response)
        {
            var maxObjects = Mathf.Clamp(request.maxObjects <= 0 ? 5000 : request.maxObjects, 1, 10000);
            var remainingObjects = maxObjects;
            var activeScene = SceneManager.GetActiveScene();

            for (var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                var scene = SceneManager.GetSceneAt(sceneIndex);
                var sceneSnapshot = new BridgeSceneSnapshot
                {
                    name = scene.name,
                    path = scene.path,
                    buildIndex = scene.buildIndex,
                    isLoaded = scene.isLoaded,
                    isActive = scene == activeScene,
                    isDirty = scene.isDirty,
                    rootCount = scene.isLoaded ? scene.rootCount : 0
                };

                if (scene.isLoaded)
                {
                    var roots = scene.GetRootGameObjects();
                    for (var rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                    {
                        CollectHierarchy(roots[rootIndex].transform, sceneSnapshot, ref remainingObjects);
                        if (remainingObjects <= 0)
                        {
                            sceneSnapshot.objectsTruncated = true;
                            break;
                        }
                    }
                }

                sceneSnapshot.objectCount = sceneSnapshot.objects.Count;
                response.scenes.Add(sceneSnapshot);

                if (remainingObjects <= 0)
                {
                    response.messages.Add($"Scene summary stopped at the requested maxObjects limit ({maxObjects}).");
                    break;
                }
            }
        }

        private static void CollectHierarchy(
            Transform transform,
            BridgeSceneSnapshot sceneSnapshot,
            ref int remainingObjects)
        {
            if (remainingObjects <= 0)
            {
                return;
            }

            sceneSnapshot.objects.Add(BuildObjectSnapshot(transform.gameObject));
            remainingObjects--;

            for (var childIndex = 0; childIndex < transform.childCount; childIndex++)
            {
                CollectHierarchy(transform.GetChild(childIndex), sceneSnapshot, ref remainingObjects);
                if (remainingObjects <= 0)
                {
                    return;
                }
            }
        }

        private static void FindObjects(BridgeRequest request, BridgeResponse response)
        {
            var maxObjects = Mathf.Clamp(request.maxObjects <= 0 ? 200 : request.maxObjects, 1, 5000);
            var nameFilter = request.nameContains ?? string.Empty;
            var componentFilter = request.componentTypeContains ?? string.Empty;

            for (var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                var scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded)
                {
                    continue;
                }

                var roots = scene.GetRootGameObjects();
                for (var rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    FindMatchingObjects(
                        roots[rootIndex].transform,
                        nameFilter,
                        componentFilter,
                        maxObjects,
                        response.objects);
                    if (response.objects.Count >= maxObjects)
                    {
                        response.messages.Add($"Object search stopped at the requested maxObjects limit ({maxObjects}).");
                        return;
                    }
                }
            }
        }

        private static void FindMatchingObjects(
            Transform transform,
            string nameFilter,
            string componentFilter,
            int maxObjects,
            List<BridgeObjectSnapshot> results)
        {
            if (results.Count >= maxObjects)
            {
                return;
            }

            var gameObject = transform.gameObject;
            var nameMatches = string.IsNullOrEmpty(nameFilter) ||
                              gameObject.name.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0;
            var componentMatches = string.IsNullOrEmpty(componentFilter) || HasMatchingComponent(gameObject, componentFilter);

            if (nameMatches && componentMatches)
            {
                results.Add(BuildObjectSnapshot(gameObject));
            }

            for (var childIndex = 0; childIndex < transform.childCount; childIndex++)
            {
                FindMatchingObjects(transform.GetChild(childIndex), nameFilter, componentFilter, maxObjects, results);
                if (results.Count >= maxObjects)
                {
                    return;
                }
            }
        }

        private static bool HasMatchingComponent(GameObject gameObject, string componentFilter)
        {
            var components = gameObject.GetComponents<Component>();
            for (var componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                var component = components[componentIndex];
                if (component == null)
                {
                    if ("missing script".IndexOf(componentFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }

                    continue;
                }

                var type = component.GetType();
                if (type.Name.IndexOf(componentFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (type.FullName ?? string.Empty).IndexOf(componentFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static BridgeObjectInspection InspectObject(BridgeRequest request)
        {
            var target = ResolveGlobalObject(request.globalObjectId);
            var gameObject = GetOwningGameObject(target);
            if (gameObject == null)
            {
                throw new InvalidOperationException(
                    $"GlobalObjectId '{request.globalObjectId}' does not resolve to a loaded GameObject or Component.");
            }

            var inspection = new BridgeObjectInspection
            {
                requestedGlobalObjectId = request.globalObjectId,
                targetKind = target.GetType().FullName,
                gameObject = BuildObjectSnapshot(gameObject)
            };

            var maxProperties = Mathf.Clamp(request.maxProperties <= 0 ? 2000 : request.maxProperties, 1, 10000);
            if (target is GameObject)
            {
                inspection.inspectedTargets.Add(InspectSerializedTarget(gameObject, request.includeHidden, maxProperties));
                var components = gameObject.GetComponents<Component>();
                for (var componentIndex = 0; componentIndex < components.Length; componentIndex++)
                {
                    if (components[componentIndex] != null)
                    {
                        inspection.inspectedTargets.Add(
                            InspectSerializedTarget(components[componentIndex], request.includeHidden, maxProperties));
                    }
                }
            }
            else
            {
                inspection.inspectedTargets.Add(InspectSerializedTarget(target, request.includeHidden, maxProperties));
            }

            return inspection;
        }

        private static BridgeComponentInspection InspectSerializedTarget(
            Object target,
            bool includeHidden,
            int maxProperties)
        {
            var type = target.GetType();
            var inspection = new BridgeComponentInspection
            {
                globalObjectId = GetGlobalObjectId(target),
                type = type.FullName,
                assembly = type.Assembly.GetName().Name,
                assetPath = AssetDatabase.GetAssetPath(target)
            };

            var serializedObject = new SerializedObject(target);
            serializedObject.UpdateIfRequiredOrScript();
            var iterator = serializedObject.GetIterator();
            var enterChildren = true;

            while (includeHidden ? iterator.Next(enterChildren) : iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (inspection.properties.Count >= maxProperties)
                {
                    inspection.propertiesTruncated = true;
                    break;
                }

                inspection.properties.Add(BuildPropertySnapshot(iterator));
            }

            return inspection;
        }

        private static BridgeAuthoringResult EnsureAssetFolders(BridgeRequest request)
        {
            EnsureEditMode("Asset folder creation");
            if (request.assetPaths == null || request.assetPaths.Count == 0)
            {
                throw new InvalidOperationException("ensure_asset_folders requires at least one asset path.");
            }

            var result = new BridgeAuthoringResult
            {
                operation = "ensure_asset_folders",
                details = "All requested asset folders exist."
            };

            for (var index = 0; index < request.assetPaths.Count; index++)
            {
                var folderPath = NormalizeAssetFolderPath(request.assetPaths[index]);
                var created = EnsureAssetFolderExists(folderPath);
                result.created |= created;
                result.changed |= created;
                result.affectedPaths.Add(folderPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        private static BridgeAuthoringResult CreateScene(BridgeRequest request)
        {
            EnsureEditModeAndNoPrefabStage("Scene creation");
            var scenePath = NormalizeSceneAssetPath(request.scenePath);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null ||
                File.Exists(ToAbsoluteProjectPath(scenePath)))
            {
                throw new InvalidOperationException($"Scene asset '{scenePath}' already exists. Use open_scene instead of overwriting it.");
            }

            EnsureAssetFolderExists(Path.GetDirectoryName(scenePath)?.Replace('\\', '/'));
            PrepareForSceneSwitch(request.saveCurrentScenes);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureSceneActive(scene, "the newly created scene");

            if (!EditorSceneManager.SaveScene(scene, scenePath))
            {
                throw new InvalidOperationException($"Unity could not save the newly created scene to '{scenePath}'.");
            }

            AssetDatabase.Refresh();
            Selection.activeObject = null;
            return new BridgeAuthoringResult
            {
                operation = "create_scene",
                assetPath = scenePath,
                scenePath = scene.path,
                created = true,
                changed = true,
                saved = true,
                sceneWasDirtyBefore = false,
                sceneIsDirtyAfter = scene.isDirty,
                details = $"Created, saved, opened, and activated empty scene '{scenePath}'.",
                affectedPaths = new List<string> { scenePath }
            };
        }

        private static BridgeAuthoringResult OpenScene(BridgeRequest request)
        {
            EnsureEditModeAndNoPrefabStage("Scene opening");
            var scenePath = NormalizeSceneAssetPath(request.scenePath);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                throw new InvalidOperationException($"Scene asset '{scenePath}' does not exist or has not been imported.");
            }

            PrepareForSceneSwitch(request.saveCurrentScenes);
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            EnsureSceneActive(scene, $"scene '{scenePath}'");

            Selection.activeObject = null;
            return new BridgeAuthoringResult
            {
                operation = "open_scene",
                assetPath = scenePath,
                scenePath = scene.path,
                changed = true,
                sceneIsDirtyAfter = scene.isDirty,
                details = $"Opened and activated scene '{scenePath}'.",
                affectedPaths = new List<string> { scenePath }
            };
        }

        private static BridgeAuthoringResult SaveScene(BridgeRequest request)
        {
            EnsureEditModeAndNoPrefabStage("Scene saving");
            var scene = ResolveLoadedScene(request.scenePath);
            var result = new BridgeAuthoringResult
            {
                operation = "save_scene",
                assetPath = scene.path,
                scenePath = scene.path,
                sceneWasDirtyBefore = scene.isDirty,
                affectedPaths = new List<string> { scene.path }
            };

            result.saved = EditorSceneManager.SaveScene(scene);
            result.changed = result.sceneWasDirtyBefore && result.saved;
            result.sceneIsDirtyAfter = scene.isDirty;
            result.details = result.saved
                ? $"Saved scene '{scene.path}'."
                : $"Unity did not save scene '{scene.path}'.";
            return result;
        }

        private static BridgeAuthoringResult CreateGameObject(BridgeRequest request, BridgeResponse response)
        {
            EnsureEditModeAndNoPrefabStage("GameObject creation");
            var scene = ResolveLoadedScene(request.scenePath);
            var sceneWasDirty = scene.isDirty;
            RejectAutomaticSaveOverDirtyScene(request.save, sceneWasDirty);
            var objectName = ValidateObjectName(request.objectName);

            GameObject gameObject;
            var primitiveName = (request.primitiveType ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(primitiveName) || string.Equals(primitiveName, "Empty", StringComparison.OrdinalIgnoreCase))
            {
                gameObject = new GameObject(objectName);
            }
            else
            {
                if (!Enum.TryParse(primitiveName, true, out PrimitiveType primitiveType))
                {
                    throw new InvalidOperationException(
                        $"Primitive type '{request.primitiveType}' is unsupported. Use Empty, Cube, Sphere, Capsule, Cylinder, Plane, or Quad.");
                }

                gameObject = GameObject.CreatePrimitive(primitiveType);
                gameObject.name = objectName;
            }

            Undo.RegisterCreatedObjectUndo(gameObject, $"Codex Scene Bridge: create {objectName}");
            if (gameObject.scene != scene)
            {
                SceneManager.MoveGameObjectToScene(gameObject, scene);
            }

            if (!string.IsNullOrWhiteSpace(request.parentGlobalObjectId))
            {
                var parent = GetOwningGameObject(ResolveGlobalObject(request.parentGlobalObjectId));
                if (parent == null || parent.scene != scene)
                {
                    Object.DestroyImmediate(gameObject);
                    throw new InvalidOperationException("The requested parent must resolve to a GameObject in the target scene.");
                }

                Undo.SetTransformParent(gameObject.transform, parent.transform, $"Codex Scene Bridge: parent {objectName}");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = gameObject;
            var result = BuildSceneAuthoringResult(
                "create_game_object",
                scene,
                sceneWasDirty,
                request.save,
                gameObject,
                null,
                true,
                true,
                $"Created GameObject '{objectName}' using primitive '{(string.IsNullOrEmpty(primitiveName) ? "Empty" : primitiveName)}'.");
            response.objects.Add(BuildObjectSnapshot(gameObject));
            return result;
        }

        private static BridgeAuthoringResult AddComponent(BridgeRequest request, BridgeResponse response)
        {
            EnsureEditModeAndNoPrefabStage("Component creation");
            var gameObject = GetOwningGameObject(ResolveGlobalObject(request.globalObjectId));
            if (gameObject == null)
            {
                throw new InvalidOperationException("add_component requires a GlobalObjectId that resolves to a loaded GameObject or Component.");
            }

            var scene = ResolveLoadedScene(request.scenePath);
            if (gameObject.scene != scene)
            {
                throw new InvalidOperationException($"Target '{BuildHierarchyPath(gameObject.transform)}' is not in scene '{scene.path}'.");
            }

            var sceneWasDirty = scene.isDirty;
            RejectAutomaticSaveOverDirtyScene(request.save, sceneWasDirty);
            var componentType = ResolveComponentType(request.componentType);
            if (componentType == typeof(Transform))
            {
                throw new InvalidOperationException("Every GameObject already has a Transform; another Transform cannot be added.");
            }

            var component = Undo.AddComponent(gameObject, componentType);
            if (component == null)
            {
                throw new InvalidOperationException($"Unity could not add Component '{componentType.FullName}' to '{gameObject.name}'.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = component;
            var result = BuildSceneAuthoringResult(
                "add_component",
                scene,
                sceneWasDirty,
                request.save,
                gameObject,
                component,
                true,
                true,
                $"Added Component '{componentType.FullName}' to '{BuildHierarchyPath(gameObject.transform)}'.");
            response.objects.Add(BuildObjectSnapshot(gameObject));
            return result;
        }

        private static BridgeAuthoringResult ReparentObject(BridgeRequest request, BridgeResponse response)
        {
            EnsureEditModeAndNoPrefabStage("Hierarchy reparenting");
            if (!request.hasExpectedParentGlobalObjectId)
            {
                throw new InvalidOperationException("reparent_object requires the expected current parent GlobalObjectId; use an empty value for a root object.");
            }

            var gameObject = GetOwningGameObject(ResolveGlobalObject(request.globalObjectId));
            if (gameObject == null)
            {
                throw new InvalidOperationException("reparent_object requires a GlobalObjectId that resolves to a loaded GameObject or Component.");
            }

            var scene = ResolveLoadedScene(request.scenePath);
            if (gameObject.scene != scene)
            {
                throw new InvalidOperationException($"Target '{BuildHierarchyPath(gameObject.transform)}' is not in scene '{scene.path}'.");
            }

            var currentParent = gameObject.transform.parent;
            var currentParentId = currentParent == null ? string.Empty : GetGlobalObjectId(currentParent.gameObject);
            if (!string.Equals(currentParentId, request.expectedParentGlobalObjectId ?? string.Empty, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected-parent check failed. Re-read the hierarchy before retrying.");
            }

            Transform newParent = null;
            if (!string.IsNullOrWhiteSpace(request.newParentGlobalObjectId))
            {
                var newParentObject = GetOwningGameObject(ResolveGlobalObject(request.newParentGlobalObjectId));
                if (newParentObject == null || newParentObject.scene != scene)
                {
                    throw new InvalidOperationException("The new parent must resolve to a GameObject in the target scene.");
                }

                if (newParentObject == gameObject || newParentObject.transform.IsChildOf(gameObject.transform))
                {
                    throw new InvalidOperationException("The requested parent would create a hierarchy cycle.");
                }

                newParent = newParentObject.transform;
            }

            var sceneWasDirty = scene.isDirty;
            RejectAutomaticSaveOverDirtyScene(request.save, sceneWasDirty);
            var pathBefore = BuildHierarchyPath(gameObject.transform);
            Undo.SetTransformParent(gameObject.transform, newParent, $"Codex Scene Bridge: reparent {gameObject.name}");
            EditorSceneManager.MarkSceneDirty(scene);
            var result = BuildSceneAuthoringResult(
                "reparent_object",
                scene,
                sceneWasDirty,
                request.save,
                gameObject,
                null,
                false,
                !string.Equals(currentParentId, request.newParentGlobalObjectId ?? string.Empty, StringComparison.Ordinal),
                $"Reparented '{pathBefore}' to '{(newParent == null ? "<scene root>" : BuildHierarchyPath(newParent))}'.");
            result.hierarchyPathBefore = pathBefore;
            result.hierarchyPathAfter = BuildHierarchyPath(gameObject.transform);
            response.objects.Add(BuildObjectSnapshot(gameObject));
            return result;
        }

        private static BridgeAuthoringResult BuildSceneAuthoringResult(
            string operation,
            Scene scene,
            bool sceneWasDirty,
            bool save,
            GameObject gameObject,
            Component component,
            bool created,
            bool changed,
            string details)
        {
            var result = new BridgeAuthoringResult
            {
                operation = operation,
                assetPath = scene.path,
                scenePath = scene.path,
                targetGlobalObjectId = GetGlobalObjectId(gameObject),
                componentGlobalObjectId = GetGlobalObjectId(component),
                hierarchyPathAfter = BuildHierarchyPath(gameObject.transform),
                componentType = component == null ? string.Empty : component.GetType().FullName,
                created = created,
                changed = changed,
                undoRecorded = true,
                sceneWasDirtyBefore = sceneWasDirty,
                details = details,
                affectedPaths = new List<string> { $"{scene.path}:{BuildHierarchyPath(gameObject.transform)}" }
            };

            if (save)
            {
                result.saved = EditorSceneManager.SaveScene(scene);
                result.details += result.saved ? " Scene saved." : " Unity did not save the scene.";
            }

            result.sceneIsDirtyAfter = scene.isDirty;
            return result;
        }

        private static void EnsureEditMode(string operation)
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException($"{operation} is disabled in Play Mode and Play Mode transitions.");
            }
        }

        private static void EnsureEditModeAndNoPrefabStage(string operation)
        {
            EnsureEditMode(operation);
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                throw new InvalidOperationException($"{operation} is disabled while a Prefab Stage is open.");
            }
        }

        private static void PrepareForSceneSwitch(bool saveCurrentScenes)
        {
            var dirtyScenePaths = new List<string>();
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (!scene.isLoaded || !scene.isDirty)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(scene.path))
                {
                    throw new InvalidOperationException("An unsaved untitled scene is dirty. Save it manually before switching scenes.");
                }

                dirtyScenePaths.Add(scene.path);
            }

            if (dirtyScenePaths.Count == 0)
            {
                return;
            }

            if (!saveCurrentScenes)
            {
                throw new InvalidOperationException(
                    $"Scene switch rejected because loaded scenes are dirty: {string.Join(", ", dirtyScenePaths)}. Re-run with saveCurrentScenes=true only when saving them is intended.");
            }

            if (!EditorSceneManager.SaveOpenScenes())
            {
                throw new InvalidOperationException("Unity did not save all dirty open scenes; scene switch was cancelled.");
            }

            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.isLoaded && scene.isDirty)
                {
                    throw new InvalidOperationException($"Scene '{scene.path}' remained dirty after save; scene switch was cancelled.");
                }
            }
        }

        private static void EnsureSceneActive(Scene scene, string description)
        {
            if (SceneManager.GetActiveScene() == scene)
            {
                return;
            }

            if (!EditorSceneManager.SetActiveScene(scene))
            {
                throw new InvalidOperationException($"Unity could not activate {description}.");
            }
        }

        private static Scene ResolveLoadedScene(string requestedPath)
        {
            var scenePath = NormalizeSceneAssetPath(requestedPath);
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.isLoaded && string.Equals(scene.path, scenePath, StringComparison.Ordinal))
                {
                    return scene;
                }
            }

            throw new InvalidOperationException($"Scene '{scenePath}' is not currently loaded.");
        }

        private static string NormalizeSceneAssetPath(string path)
        {
            var normalized = NormalizeAssetPath(path);
            if (!normalized.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Scene path '{normalized}' must end with .unity.");
            }

            return normalized;
        }

        private static string NormalizeAssetFolderPath(string path)
        {
            var normalized = NormalizeAssetPath(path).TrimEnd('/');
            if (Path.HasExtension(normalized))
            {
                throw new InvalidOperationException($"Asset folder path '{normalized}' must not have a file extension.");
            }

            return normalized;
        }

        private static string NormalizeAssetPath(string path)
        {
            var normalized = (path ?? string.Empty).Trim().Replace('\\', '/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal) ||
                normalized.Contains("/../") || normalized.EndsWith("/..", StringComparison.Ordinal) ||
                normalized.Contains("/./") || normalized.Contains(":"))
            {
                throw new InvalidOperationException($"'{path}' is not a safe project-relative path below Assets/.");
            }

            return normalized;
        }

        private static bool EnsureAssetFolderExists(string folderPath)
        {
            var normalized = NormalizeAssetFolderPath(folderPath);
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return false;
            }

            var parts = normalized.Split('/');
            var current = "Assets";
            var createdAny = false;
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    var guid = AssetDatabase.CreateFolder(current, parts[index]);
                    if (string.IsNullOrEmpty(guid))
                    {
                        throw new InvalidOperationException($"Unity could not create asset folder '{next}'.");
                    }

                    createdAny = true;
                }

                current = next;
            }

            return createdAny;
        }

        private static string ToAbsoluteProjectPath(string assetPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        private static string ValidateObjectName(string objectName)
        {
            var value = (objectName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(value) || value.Length > 128 || value.Contains("/"))
            {
                throw new InvalidOperationException("GameObject name must contain 1-128 characters and cannot contain '/'.");
            }

            return value;
        }

        private static Type ResolveComponentType(string requestedType)
        {
            var typeName = (requestedType ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(typeName))
            {
                throw new InvalidOperationException("A non-empty componentType is required.");
            }

            var matches = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
            {
                var exactType = assemblies[assemblyIndex].GetType(typeName, false, true);
                if (exactType != null && typeof(Component).IsAssignableFrom(exactType) && !matches.Contains(exactType))
                {
                    matches.Add(exactType);
                }
            }

            if (matches.Count == 0)
            {
                var componentTypes = TypeCache.GetTypesDerivedFrom<Component>();
                for (var index = 0; index < componentTypes.Count; index++)
                {
                    var type = componentTypes[index];
                    if (string.Equals(type.Name, typeName, StringComparison.OrdinalIgnoreCase) && !matches.Contains(type))
                    {
                        matches.Add(type);
                    }
                }
            }

            if (matches.Count == 0)
            {
                throw new InvalidOperationException($"Component type '{requestedType}' was not found in the loaded Unity domain.");
            }

            if (matches.Count > 1)
            {
                var fullNames = new List<string>();
                for (var index = 0; index < matches.Count; index++)
                {
                    fullNames.Add(matches[index].FullName);
                }

                throw new InvalidOperationException(
                    $"Component type name '{requestedType}' is ambiguous. Use one of: {string.Join(", ", fullNames)}.");
            }

            var resolved = matches[0];
            if (resolved.IsAbstract || resolved.ContainsGenericParameters)
            {
                throw new InvalidOperationException($"Component type '{resolved.FullName}' cannot be instantiated.");
            }

            return resolved;
        }

        private static void RejectAutomaticSaveOverDirtyScene(bool save, bool sceneWasDirty)
        {
            if (save && sceneWasDirty)
            {
                throw new InvalidOperationException(
                    "save=true was rejected because the target scene already has unsaved changes. Apply with save=false, inspect the result, then use save_scene only when saving the entire dirty scene is intended.");
            }
        }

        private static BridgePropertySnapshot BuildPropertySnapshot(SerializedProperty property)
        {
            var redacted = IsSensitiveProperty(property);
            var snapshot = new BridgePropertySnapshot
            {
                propertyPath = property.propertyPath,
                displayName = property.displayName,
                propertyType = property.propertyType.ToString(),
                depth = property.depth,
                editable = property.editable && !redacted && property.propertyPath != "m_Script",
                redacted = redacted,
                isArray = property.isArray && property.propertyType != SerializedPropertyType.String,
                arraySize = property.isArray && property.propertyType != SerializedPropertyType.String
                    ? property.arraySize
                    : 0
            };

            if (redacted)
            {
                snapshot.value = "<redacted>";
                return snapshot;
            }

            snapshot.value = FormatPropertyValue(property, MaxStringLength, out var truncated);
            snapshot.truncated = truncated;

            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null)
            {
                snapshot.objectReferenceGlobalObjectId = GetGlobalObjectId(property.objectReferenceValue);
                snapshot.objectReferenceAssetPath = AssetDatabase.GetAssetPath(property.objectReferenceValue);
            }

            return snapshot;
        }

        private static BridgeMutationResult SetProperty(BridgeRequest request)
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Persistent serialized-property edits are disabled in Play Mode and Play Mode transitions.");
            }

            if (!request.hasExpectedCurrentValue)
            {
                throw new InvalidOperationException(
                    "set_property requires hasExpectedCurrentValue=true and the exact expectedCurrentValue returned by inspect_object.");
            }

            if (request.value == null || string.IsNullOrWhiteSpace(request.value.kind))
            {
                throw new InvalidOperationException("set_property requires a structured value with an explicit kind.");
            }

            var target = ResolveGlobalObject(request.globalObjectId);
            if (target == null)
            {
                throw new InvalidOperationException($"GlobalObjectId '{request.globalObjectId}' did not resolve to a loaded object.");
            }

            var serializedObject = new SerializedObject(target);
            serializedObject.UpdateIfRequiredOrScript();
            var property = serializedObject.FindProperty(request.propertyPath);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Property '{request.propertyPath}' was not found on '{target.GetType().FullName}'. Inspect the target again before editing.");
            }

            if (!property.editable || property.propertyPath == "m_Script")
            {
                throw new InvalidOperationException($"Property '{property.propertyPath}' is not editable through the bridge.");
            }

            if (IsSensitiveProperty(property))
            {
                throw new InvalidOperationException($"Property '{property.propertyPath}' is treated as sensitive and cannot be read or changed through the bridge.");
            }

            var beforeValue = FormatPropertyValue(property, int.MaxValue, out _);
            if (!string.Equals(beforeValue, request.expectedCurrentValue, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Expected-value check failed for '{property.propertyPath}'. Expected '{request.expectedCurrentValue}', current value is '{beforeValue}'. Re-inspect before retrying.");
            }

            var owningGameObject = GetOwningGameObject(target);
            var targetScene = owningGameObject != null ? owningGameObject.scene : default;
            var hasSceneTarget = targetScene.IsValid() && targetScene.isLoaded;
            var sceneWasDirty = hasSceneTarget && targetScene.isDirty;
            var isPersistentAsset = EditorUtility.IsPersistent(target);
            var assetWasDirty = isPersistentAsset && EditorUtility.IsDirty(target);
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            var isInPrefabStage = prefabStage != null && hasSceneTarget && prefabStage.scene == targetScene;

            if (request.save && sceneWasDirty)
            {
                throw new InvalidOperationException(
                    "save=true was rejected because the target scene already has unsaved changes. Apply with save=false and review/save through Unity.");
            }

            if (request.save && assetWasDirty)
            {
                throw new InvalidOperationException(
                    "save=true was rejected because the target asset already has unsaved changes. Apply with save=false and review/save through Unity.");
            }

            if (request.save && isInPrefabStage)
            {
                throw new InvalidOperationException(
                    "save=true is not supported while editing a Prefab Stage. Apply with save=false and save the Prefab Stage through Unity after review.");
            }

            // Validate and format the proposed value in the SerializedObject buffer, then discard it.
            ApplyBridgeValue(property, request.value);
            var proposedValue = FormatPropertyValue(property, int.MaxValue, out _);
            serializedObject.UpdateIfRequiredOrScript();

            var result = new BridgeMutationResult
            {
                targetGlobalObjectId = request.globalObjectId,
                targetType = target.GetType().FullName,
                targetPath = DescribeTarget(target),
                propertyPath = request.propertyPath,
                propertyType = property.propertyType.ToString(),
                beforeValue = beforeValue,
                proposedValue = proposedValue,
                afterValue = beforeValue,
                dryRun = request.dryRun,
                sceneWasDirtyBefore = sceneWasDirty,
                sceneIsDirtyAfter = sceneWasDirty,
                saveDetails = "Not requested."
            };

            if (request.dryRun)
            {
                return result;
            }

            Undo.RecordObject(target, $"Codex Scene Bridge: {request.propertyPath}");
            serializedObject.UpdateIfRequiredOrScript();
            property = serializedObject.FindProperty(request.propertyPath);
            ApplyBridgeValue(property, request.value);
            var applied = serializedObject.ApplyModifiedProperties();

            if (isPersistentAsset)
            {
                EditorUtility.SetDirty(target);
            }
            if (PrefabUtility.IsPartOfPrefabInstance(target))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }

            if (hasSceneTarget)
            {
                EditorSceneManager.MarkSceneDirty(targetScene);
            }

            serializedObject.UpdateIfRequiredOrScript();
            property = serializedObject.FindProperty(request.propertyPath);
            result.afterValue = FormatPropertyValue(property, int.MaxValue, out _);
            result.changed = applied || !string.Equals(result.beforeValue, result.afterValue, StringComparison.Ordinal);
            result.undoRecorded = true;
            result.sceneIsDirtyAfter = hasSceneTarget && targetScene.isDirty;

            if (request.save)
            {
                if (hasSceneTarget)
                {
                    result.saved = EditorSceneManager.SaveScene(targetScene);
                    result.saveDetails = result.saved
                        ? $"Saved scene '{targetScene.path}'."
                        : $"Unity did not save scene '{targetScene.path}'.";
                }
                else if (isPersistentAsset)
                {
                    AssetDatabase.SaveAssetIfDirty(target);
                    result.saved = !EditorUtility.IsDirty(target);
                    result.saveDetails = result.saved
                        ? $"Saved asset '{AssetDatabase.GetAssetPath(target)}'."
                        : $"Unity did not save asset '{AssetDatabase.GetAssetPath(target)}'.";
                }
                else
                {
                    result.saveDetails = "Target is neither a loaded scene object nor a persistent asset; no save operation was available.";
                }
            }

            result.sceneIsDirtyAfter = hasSceneTarget && targetScene.isDirty;

            return result;
        }

        private static void ApplyBridgeValue(SerializedProperty property, BridgeValue value)
        {
            var kind = (value.kind ?? string.Empty).Trim().ToLowerInvariant();
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean when kind == "bool":
                    property.boolValue = value.boolValue;
                    return;
                case SerializedPropertyType.Integer when kind == "integer":
                case SerializedPropertyType.LayerMask when kind == "integer":
                    property.longValue = value.integerValue;
                    return;
                case SerializedPropertyType.Float when kind == "number":
                    property.doubleValue = value.numberValue;
                    return;
                case SerializedPropertyType.String when kind == "string":
                    property.stringValue = value.stringValue ?? string.Empty;
                    return;
                case SerializedPropertyType.Character when kind == "string":
                    property.intValue = string.IsNullOrEmpty(value.stringValue) ? 0 : value.stringValue[0];
                    return;
                case SerializedPropertyType.Enum when kind == "enum":
                    property.enumValueIndex = ResolveEnumIndex(property, value);
                    return;
                case SerializedPropertyType.Vector2 when kind == "vector2":
                    property.vector2Value = new Vector2(value.x, value.y);
                    return;
                case SerializedPropertyType.Vector3 when kind == "vector3":
                    property.vector3Value = new Vector3(value.x, value.y, value.z);
                    return;
                case SerializedPropertyType.Vector4 when kind == "vector4":
                    property.vector4Value = new Vector4(value.x, value.y, value.z, value.w);
                    return;
                case SerializedPropertyType.Vector2Int when kind == "vector2int":
                    property.vector2IntValue = new Vector2Int(value.intX, value.intY);
                    return;
                case SerializedPropertyType.Vector3Int when kind == "vector3int":
                    property.vector3IntValue = new Vector3Int(value.intX, value.intY, value.intZ);
                    return;
                case SerializedPropertyType.Quaternion when kind == "quaternion":
                    property.quaternionValue = new Quaternion(value.x, value.y, value.z, value.w);
                    return;
                case SerializedPropertyType.Color when kind == "color":
                    property.colorValue = new Color(value.x, value.y, value.z, value.w);
                    return;
                case SerializedPropertyType.Rect when kind == "rect":
                    property.rectValue = new Rect(value.x, value.y, value.width, value.height);
                    return;
                case SerializedPropertyType.Bounds when kind == "bounds":
                    property.boundsValue = new Bounds(
                        new Vector3(value.x, value.y, value.z),
                        new Vector3(value.sizeX, value.sizeY, value.sizeZ));
                    return;
                case SerializedPropertyType.ObjectReference when kind == "null":
                    property.objectReferenceValue = null;
                    return;
                case SerializedPropertyType.ObjectReference when kind == "objectreference":
                    property.objectReferenceValue = ResolveGlobalObject(value.globalObjectId);
                    return;
                case SerializedPropertyType.Generic when kind == "arraysize" && property.isArray:
                    if (value.integerValue < 0 || value.integerValue > 100000)
                    {
                        throw new InvalidOperationException("Array size must be between 0 and 100000.");
                    }

                    property.arraySize = (int)value.integerValue;
                    return;
                default:
                    throw new InvalidOperationException(
                        $"Value kind '{value.kind}' is not supported for SerializedPropertyType.{property.propertyType}.");
            }
        }

        private static int ResolveEnumIndex(SerializedProperty property, BridgeValue value)
        {
            if (!string.IsNullOrEmpty(value.enumName))
            {
                var displayNames = property.enumDisplayNames;
                for (var index = 0; index < displayNames.Length; index++)
                {
                    if (string.Equals(displayNames[index], value.enumName, StringComparison.OrdinalIgnoreCase))
                    {
                        return index;
                    }
                }

                var names = property.enumNames;
                for (var index = 0; index < names.Length; index++)
                {
                    if (string.Equals(names[index], value.enumName, StringComparison.OrdinalIgnoreCase))
                    {
                        return index;
                    }
                }

                throw new InvalidOperationException(
                    $"Enum value '{value.enumName}' is not valid for '{property.propertyPath}'.");
            }

            if (value.enumIndex < 0 || value.enumIndex >= property.enumNames.Length)
            {
                throw new InvalidOperationException(
                    $"Enum index {value.enumIndex} is outside the valid range for '{property.propertyPath}'.");
            }

            return value.enumIndex;
        }

        private static string FormatPropertyValue(SerializedProperty property, int maxLength, out bool truncated)
        {
            truncated = false;
            string value;

            if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
            {
                value = $"Array(size={property.arraySize})";
            }
            else
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Boolean:
                        value = property.boolValue ? "true" : "false";
                        break;
                    case SerializedPropertyType.Integer:
                    case SerializedPropertyType.LayerMask:
                        value = property.longValue.ToString(CultureInfo.InvariantCulture);
                        break;
                    case SerializedPropertyType.Float:
                        value = property.doubleValue.ToString("R", CultureInfo.InvariantCulture);
                        break;
                    case SerializedPropertyType.String:
                        value = property.stringValue ?? string.Empty;
                        break;
                    case SerializedPropertyType.Character:
                        value = property.intValue == 0 ? string.Empty : ((char)property.intValue).ToString();
                        break;
                    case SerializedPropertyType.Enum:
                        var enumIndex = property.enumValueIndex;
                        value = enumIndex >= 0 && enumIndex < property.enumDisplayNames.Length
                            ? $"{property.enumDisplayNames[enumIndex]} ({enumIndex})"
                            : enumIndex.ToString(CultureInfo.InvariantCulture);
                        break;
                    case SerializedPropertyType.Vector2:
                        value = FormatVector(property.vector2Value.x, property.vector2Value.y);
                        break;
                    case SerializedPropertyType.Vector3:
                        value = FormatVector(property.vector3Value.x, property.vector3Value.y, property.vector3Value.z);
                        break;
                    case SerializedPropertyType.Vector4:
                        value = FormatVector(
                            property.vector4Value.x,
                            property.vector4Value.y,
                            property.vector4Value.z,
                            property.vector4Value.w);
                        break;
                    case SerializedPropertyType.Vector2Int:
                        value = $"({property.vector2IntValue.x}, {property.vector2IntValue.y})";
                        break;
                    case SerializedPropertyType.Vector3Int:
                        value = $"({property.vector3IntValue.x}, {property.vector3IntValue.y}, {property.vector3IntValue.z})";
                        break;
                    case SerializedPropertyType.Quaternion:
                        value = FormatVector(
                            property.quaternionValue.x,
                            property.quaternionValue.y,
                            property.quaternionValue.z,
                            property.quaternionValue.w);
                        break;
                    case SerializedPropertyType.Color:
                        value = FormatVector(
                            property.colorValue.r,
                            property.colorValue.g,
                            property.colorValue.b,
                            property.colorValue.a);
                        break;
                    case SerializedPropertyType.Rect:
                        value = $"(x:{FormatFloat(property.rectValue.x)}, y:{FormatFloat(property.rectValue.y)}, width:{FormatFloat(property.rectValue.width)}, height:{FormatFloat(property.rectValue.height)})";
                        break;
                    case SerializedPropertyType.Bounds:
                        value = $"center:{FormatVector(property.boundsValue.center.x, property.boundsValue.center.y, property.boundsValue.center.z)} size:{FormatVector(property.boundsValue.size.x, property.boundsValue.size.y, property.boundsValue.size.z)}";
                        break;
                    case SerializedPropertyType.ObjectReference:
                        value = property.objectReferenceValue == null
                            ? "null"
                            : GetGlobalObjectId(property.objectReferenceValue);
                        break;
                    case SerializedPropertyType.AnimationCurve:
                        value = property.animationCurveValue == null
                            ? "null"
                            : $"AnimationCurve(keys={property.animationCurveValue.length})";
                        break;
                    case SerializedPropertyType.Hash128:
                        value = property.hash128Value.ToString();
                        break;
                    case SerializedPropertyType.ManagedReference:
                        value = string.IsNullOrEmpty(property.managedReferenceFullTypename)
                            ? "null"
                            : property.managedReferenceFullTypename;
                        break;
                    default:
                        value = $"<{property.propertyType}>";
                        break;
                }
            }

            if (value.Length > maxLength)
            {
                truncated = true;
                return value.Substring(0, maxLength) + $"... <truncated {value.Length - maxLength} chars>";
            }

            return value;
        }

        private static string FormatVector(params float[] values)
        {
            var parts = new string[values.Length];
            for (var index = 0; index < values.Length; index++)
            {
                parts[index] = FormatFloat(values[index]);
            }

            return $"({string.Join(", ", parts)})";
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static bool IsSensitiveProperty(SerializedProperty property)
        {
            var candidate = $"{property.propertyPath} {property.displayName}".ToLowerInvariant();
            var sensitiveMarkers = new[]
            {
                "apikey", "api_key", "api key", "access_token", "accesstoken", "access token",
                "auth_token", "authtoken", "auth token", "bearer", "client_secret", "clientsecret",
                "client secret", "credential", "password", "private_key", "privatekey", "private key"
            };

            for (var index = 0; index < sensitiveMarkers.Length; index++)
            {
                if (candidate.Contains(sensitiveMarkers[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private static BridgeObjectSnapshot BuildObjectSnapshot(GameObject gameObject)
        {
            var snapshot = new BridgeObjectSnapshot
            {
                globalObjectId = GetGlobalObjectId(gameObject),
                name = gameObject.name,
                hierarchyPath = BuildHierarchyPath(gameObject.transform),
                scenePath = gameObject.scene.path,
                activeSelf = gameObject.activeSelf,
                activeInHierarchy = gameObject.activeInHierarchy,
                layer = gameObject.layer,
                layerName = LayerMask.LayerToName(gameObject.layer),
                tag = gameObject.tag,
                isStatic = gameObject.isStatic,
                isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(gameObject),
                hasPrefabOverrides = PrefabUtility.IsPartOfPrefabInstance(gameObject) &&
                                     PrefabUtility.HasPrefabInstanceAnyOverrides(gameObject, false)
            };

            var prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            snapshot.prefabAssetPath = prefabSource != null ? AssetDatabase.GetAssetPath(prefabSource) : string.Empty;

            var components = gameObject.GetComponents<Component>();
            for (var componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                var component = components[componentIndex];
                if (component == null)
                {
                    snapshot.components.Add(new BridgeComponentSummary
                    {
                        type = "<Missing Script>",
                        missing = true
                    });
                    continue;
                }

                var type = component.GetType();
                var behaviour = component as Behaviour;
                snapshot.components.Add(new BridgeComponentSummary
                {
                    globalObjectId = GetGlobalObjectId(component),
                    type = type.FullName,
                    assembly = type.Assembly.GetName().Name,
                    isBehaviour = behaviour != null,
                    enabled = behaviour == null || behaviour.enabled
                });
            }

            return snapshot;
        }

        private static string BuildHierarchyPath(Transform transform)
        {
            var parts = new List<string>();
            var current = transform;
            while (current != null)
            {
                parts.Add($"{current.name}[{current.GetSiblingIndex()}]");
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static Object ResolveGlobalObject(string globalObjectId)
        {
            if (string.IsNullOrWhiteSpace(globalObjectId))
            {
                throw new InvalidOperationException("A non-empty globalObjectId is required.");
            }

            if (!GlobalObjectId.TryParse(globalObjectId, out var parsedId))
            {
                throw new InvalidOperationException($"'{globalObjectId}' is not a valid Unity GlobalObjectId.");
            }

            var target = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(parsedId);
            if (target == null)
            {
                throw new InvalidOperationException(
                    $"GlobalObjectId '{globalObjectId}' could not be resolved. The scene or asset may be unloaded, stale, or deleted.");
            }

            return target;
        }

        private static string GetGlobalObjectId(Object target)
        {
            return target == null ? string.Empty : GlobalObjectId.GetGlobalObjectIdSlow(target).ToString();
        }

        private static GameObject GetOwningGameObject(Object target)
        {
            if (target is GameObject gameObject)
            {
                return gameObject;
            }

            return target is Component component ? component.gameObject : null;
        }

        private static string DescribeTarget(Object target)
        {
            var gameObject = GetOwningGameObject(target);
            if (gameObject != null)
            {
                return $"{gameObject.scene.path}:{BuildHierarchyPath(gameObject.transform)} ({target.GetType().FullName})";
            }

            var assetPath = AssetDatabase.GetAssetPath(target);
            return string.IsNullOrEmpty(assetPath) ? target.name : assetPath;
        }

        private static void RunSelfTest(BridgeResponse response)
        {
            var originalActiveScene = SceneManager.GetActiveScene();
            Scene testScene = default;

            try
            {
                testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                EnsureSceneActive(testScene, "the temporary self-test scene");

                var gameObject = new GameObject("CodexSceneBridgeSelfTest");
                var light = gameObject.AddComponent<Light>();

                var lightSerializedObject = new SerializedObject(light);
                var intensity = lightSerializedObject.FindProperty("m_Intensity");
                var originalIntensity = intensity.doubleValue;
                ApplyBridgeValue(intensity, new BridgeValue { kind = "number", numberValue = 4.25d });
                lightSerializedObject.ApplyModifiedPropertiesWithoutUndo();
                var intensityPassed = Math.Abs(light.intensity - 4.25f) < 0.0001f;
                response.selfTestSteps.Add(new BridgeSelfTestStep
                {
                    name = "Float serialized property",
                    passed = intensityPassed,
                    details = $"Light intensity {originalIntensity.ToString("R", CultureInfo.InvariantCulture)} -> {light.intensity.ToString("R", CultureInfo.InvariantCulture)}."
                });

                var transformSerializedObject = new SerializedObject(gameObject.transform);
                var localPosition = transformSerializedObject.FindProperty("m_LocalPosition");
                ApplyBridgeValue(localPosition, new BridgeValue
                {
                    kind = "vector3",
                    x = 1f,
                    y = 2f,
                    z = 3f
                });
                transformSerializedObject.ApplyModifiedPropertiesWithoutUndo();
                var vectorPassed = gameObject.transform.localPosition == new Vector3(1f, 2f, 3f);
                response.selfTestSteps.Add(new BridgeSelfTestStep
                {
                    name = "Vector3 serialized property",
                    passed = vectorPassed,
                    details = $"Transform localPosition is {gameObject.transform.localPosition}."
                });

                var inspection = InspectSerializedTarget(light, false, 500);
                var inspectionPassed = false;
                for (var index = 0; index < inspection.properties.Count; index++)
                {
                    if (inspection.properties[index].propertyPath == "m_Intensity" &&
                        inspection.properties[index].value == "4.25")
                    {
                        inspectionPassed = true;
                        break;
                    }
                }

                response.selfTestSteps.Add(new BridgeSelfTestStep
                {
                    name = "Serialized inspection",
                    passed = inspectionPassed,
                    details = $"Inspected {inspection.properties.Count} visible Light properties."
                });
            }
            catch (Exception exception)
            {
                response.selfTestSteps.Add(new BridgeSelfTestStep
                {
                    name = "Self-test execution",
                    passed = false,
                    details = exception.ToString()
                });
            }
            finally
            {
                if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
                {
                    EditorSceneManager.SetActiveScene(originalActiveScene);
                }

                if (testScene.IsValid() && testScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(testScene, true);
                }
            }
        }

        private static bool AllSelfTestsPassed(List<BridgeSelfTestStep> steps)
        {
            if (steps.Count == 0)
            {
                return false;
            }

            for (var index = 0; index < steps.Count; index++)
            {
                if (!steps[index].passed)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
