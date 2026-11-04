[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet(
        'status',
        'scene_summary',
        'find_objects',
        'inspect_object',
        'set_property',
        'ensure_asset_folders',
        'create_scene',
        'open_scene',
        'save_scene',
        'create_game_object',
        'add_component',
        'reparent_object',
        'self_test', 'starter_validate', 'starter_capture', 'starter_enter_play', 'starter_exit_play', 'starter_reset_demo')]
    [string]$Action,

    [string]$GlobalObjectId,
    [string]$SessionToken,
    [string]$ProjectPath = (Join-Path $PSScriptRoot '..\UnityXRTemplate'),
    [string]$PropertyPath,
    [string]$ExpectedCurrentValue,
    [string]$ValueJson,
    [string]$NameContains,
    [string]$ComponentTypeContains,
    [string[]]$AssetPaths = @(),
    [string]$ScenePath,
    [switch]$SaveCurrentScenes,
    [string]$ObjectName,
    [string]$PrimitiveType = 'Empty',
    [string]$ParentGlobalObjectId,
    [string]$ComponentType,
    [string]$ExpectedParentGlobalObjectId,
    [string]$NewParentGlobalObjectId,
    [switch]$IncludeHidden,
    [switch]$DryRun,
    [switch]$Save,
    [int]$MaxObjects = 5000,
    [int]$MaxProperties = 2000,
    [ValidateRange(1, 300)]
    [int]$TimeoutSeconds = 60,
    [switch]$KeepExchangeFiles
)

$ErrorActionPreference = 'Stop'

$projectRoot = [System.IO.Path]::GetFullPath($ProjectPath)
if (-not (Test-Path -LiteralPath (Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt'))) { throw 'ProjectPath is not a Unity project.' }
$exchangeRoot = Join-Path $projectRoot 'UserSettings\CodexSceneBridge'
$requestDirectory = Join-Path $exchangeRoot 'requests'
$responseDirectory = Join-Path $exchangeRoot 'responses'
$processedDirectory = Join-Path $exchangeRoot 'processed'

[System.IO.Directory]::CreateDirectory($requestDirectory) | Out-Null
[System.IO.Directory]::CreateDirectory($responseDirectory) | Out-Null
[System.IO.Directory]::CreateDirectory($processedDirectory) | Out-Null

$requestId = [System.Guid]::NewGuid().ToString('N')
$request = [ordered]@{
    requestId = $requestId
    expectedProjectRoot = $projectRoot
    expiresUtc = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds).ToString('o')
    sessionToken = $SessionToken
    action = $Action
    globalObjectId = $GlobalObjectId
    propertyPath = $PropertyPath
    expectedCurrentValue = $ExpectedCurrentValue
    hasExpectedCurrentValue = $PSBoundParameters.ContainsKey('ExpectedCurrentValue')
    includeHidden = [bool]$IncludeHidden
    dryRun = [bool]$DryRun
    save = [bool]$Save
    maxObjects = $MaxObjects
    maxProperties = $MaxProperties
    nameContains = $NameContains
    componentTypeContains = $ComponentTypeContains
    assetPaths = $AssetPaths
    scenePath = $ScenePath
    saveCurrentScenes = [bool]$SaveCurrentScenes
    objectName = $ObjectName
    primitiveType = $PrimitiveType
    parentGlobalObjectId = $ParentGlobalObjectId
    componentType = $ComponentType
    expectedParentGlobalObjectId = $ExpectedParentGlobalObjectId
    hasExpectedParentGlobalObjectId = $PSBoundParameters.ContainsKey('ExpectedParentGlobalObjectId')
    newParentGlobalObjectId = $NewParentGlobalObjectId
}

if ($Action -in @('inspect_object', 'set_property', 'add_component', 'reparent_object') -and [string]::IsNullOrWhiteSpace($GlobalObjectId)) {
    throw "Action '$Action' requires -GlobalObjectId. Obtain it from scene_summary or find_objects."
}

if ($Action -eq 'ensure_asset_folders' -and $AssetPaths.Count -eq 0) {
    throw "Action 'ensure_asset_folders' requires one or more -AssetPaths below Assets/."
}

if ($Action -in @('create_scene', 'open_scene', 'save_scene', 'create_game_object', 'add_component', 'reparent_object') -and
    [string]::IsNullOrWhiteSpace($ScenePath)) {
    throw "Action '$Action' requires an exact -ScenePath below Assets/."
}

if ($Action -eq 'create_game_object' -and [string]::IsNullOrWhiteSpace($ObjectName)) {
    throw "Action 'create_game_object' requires -ObjectName."
}

if ($Action -eq 'add_component' -and [string]::IsNullOrWhiteSpace($ComponentType)) {
    throw "Action 'add_component' requires -ComponentType, preferably a full type name."
}

if ($Action -eq 'reparent_object' -and -not $PSBoundParameters.ContainsKey('ExpectedParentGlobalObjectId')) {
    throw "Action 'reparent_object' requires -ExpectedParentGlobalObjectId; pass an empty string for a current root object."
}

if ($Action -eq 'set_property') {
    if ([string]::IsNullOrWhiteSpace($PropertyPath)) {
        throw "Action 'set_property' requires -PropertyPath."
    }

    if (-not $PSBoundParameters.ContainsKey('ExpectedCurrentValue')) {
        throw "Action 'set_property' requires -ExpectedCurrentValue from a fresh inspect_object response."
    }

    if ([string]::IsNullOrWhiteSpace($ValueJson)) {
        throw 'Action set_property requires -ValueJson, for example {"kind":"bool","boolValue":true}.'
    }

    $request['value'] = $ValueJson | ConvertFrom-Json
}

$requestPath = Join-Path $requestDirectory "$requestId.request.json"
$temporaryRequestPath = "$requestPath.tmp"
$responsePath = Join-Path $responseDirectory "$requestId.response.json"
$processedPath = Join-Path $processedDirectory "$requestId.request.json"
$requestJson = $request | ConvertTo-Json -Depth 20
$utf8WithoutBom = [System.Text.UTF8Encoding]::new($false)

[System.IO.File]::WriteAllText($temporaryRequestPath, $requestJson, $utf8WithoutBom)
[System.IO.File]::Move($temporaryRequestPath, $requestPath)

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
while (-not [System.IO.File]::Exists($responsePath)) {
    if ($stopwatch.Elapsed.TotalSeconds -ge $TimeoutSeconds) {
        if ([System.IO.File]::Exists($requestPath)) { [System.IO.File]::Delete($requestPath) }
        throw "Timed out after $TimeoutSeconds seconds waiting for Unity. Confirm this exact project is open and its bridge compiles. Inspect status before retrying a mutation. Request ID: $requestId"
    }

    Start-Sleep -Milliseconds 100
}

$responseJson = [System.IO.File]::ReadAllText($responsePath, $utf8WithoutBom)
$response = $responseJson | ConvertFrom-Json
$responseJson

if (-not $KeepExchangeFiles) {
    [System.IO.File]::Delete($responsePath)
    for ($attempt = 0; $attempt -lt 10 -and -not [System.IO.File]::Exists($processedPath); $attempt++) {
        Start-Sleep -Milliseconds 50
    }

    if ([System.IO.File]::Exists($processedPath)) {
        [System.IO.File]::Delete($processedPath)
    }
}

if (-not $response.success) {
    exit 2
}
