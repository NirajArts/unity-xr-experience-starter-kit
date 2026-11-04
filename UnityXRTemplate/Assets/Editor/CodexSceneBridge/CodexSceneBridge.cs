using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace XRTest.EditorTools.CodexSceneBridge
{
    [InitializeOnLoad]
    internal static class CodexSceneBridge
    {
        private const double PollIntervalSeconds = 0.25d;
        private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);
        private static readonly string ExchangeDirectory = Path.GetFullPath(
            Path.Combine(Application.dataPath, "..", "UserSettings", "CodexSceneBridge"));
        private static readonly string RequestDirectory = Path.Combine(ExchangeDirectory, "requests");
        private static readonly string ResponseDirectory = Path.Combine(ExchangeDirectory, "responses");
        private static readonly string ProcessedDirectory = Path.Combine(ExchangeDirectory, "processed");
        private static double _nextPollTime;

        static CodexSceneBridge()
        {
            EnsureDirectories();
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
            Debug.Log($"[CodexSceneBridge] Ready (v{BridgeProtocol.Version}) at '{ExchangeDirectory}'.");
        }

        [MenuItem("Tools/Codex/Scene Bridge/Process Pending Requests")]
        private static void ProcessPendingRequestsFromMenu()
        {
            ProcessPendingRequests();
        }

        [MenuItem("Tools/Codex/Scene Bridge/Log Status")]
        private static void LogStatus()
        {
            Debug.Log(
                $"[CodexSceneBridge] v{BridgeProtocol.Version}; exchange='{ExchangeDirectory}'; " +
                $"playing={EditorApplication.isPlaying}; compiling={EditorApplication.isCompiling}; updating={EditorApplication.isUpdating}.");
        }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _nextPollTime)
            {
                return;
            }

            _nextPollTime = EditorApplication.timeSinceStartup + PollIntervalSeconds;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            ProcessPendingRequests();
        }

        private static void ProcessPendingRequests()
        {
            EnsureDirectories();
            var requestPaths = Directory.GetFiles(RequestDirectory, "*.request.json", SearchOption.TopDirectoryOnly);
            Array.Sort(requestPaths, StringComparer.Ordinal);

            for (var requestIndex = 0; requestIndex < requestPaths.Length; requestIndex++)
            {
                ProcessRequestFile(requestPaths[requestIndex]);
            }
        }

        private static void ProcessRequestFile(string requestPath)
        {
            var fallbackRequestId = Path.GetFileName(requestPath).Replace(".request.json", string.Empty);
            BridgeRequest request = null;
            BridgeResponse response;

            try
            {
                var requestJson = File.ReadAllText(requestPath, Utf8WithoutBom);
                request = JsonUtility.FromJson<BridgeRequest>(requestJson);
                if (request == null)
                {
                    throw new InvalidDataException("Request JSON did not deserialize to a bridge request.");
                }

                if (string.IsNullOrWhiteSpace(request.requestId))
                {
                    request.requestId = fallbackRequestId;
                }

                response = CodexSceneBridgeProcessor.Process(request, ExchangeDirectory);
            }
            catch (Exception exception)
            {
                response = CodexSceneBridgeProcessor.CreateFailure(
                    request != null ? request.requestId : fallbackRequestId,
                    request != null ? request.action : string.Empty,
                    exception.Message,
                    ExchangeDirectory);
            }

            var requestId = SanitizeFileName(response.requestId);
            var processedPath = Path.Combine(ProcessedDirectory, requestId + ".request.json");
            var responsePath = Path.Combine(ResponseDirectory, requestId + ".response.json");

            MoveReplacing(requestPath, processedPath);
            WriteJsonAtomically(responsePath, JsonUtility.ToJson(response, true));
        }

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(RequestDirectory);
            Directory.CreateDirectory(ResponseDirectory);
            Directory.CreateDirectory(ProcessedDirectory);
        }

        private static void MoveReplacing(string sourcePath, string destinationPath)
        {
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            File.Move(sourcePath, destinationPath);
        }

        private static void WriteJsonAtomically(string path, string json)
        {
            var temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, json, Utf8WithoutBom);
            MoveReplacing(temporaryPath, path);
        }

        private static string SanitizeFileName(string value)
        {
            var candidate = string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("N") : value;
            var invalidCharacters = Path.GetInvalidFileNameChars();
            for (var index = 0; index < invalidCharacters.Length; index++)
            {
                candidate = candidate.Replace(invalidCharacters[index], '_');
            }

            return candidate;
        }
    }
}
