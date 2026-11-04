[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('Open','Validate','EditTests','PlayTests','Capture','BuildWindows','BuildAndroid')][string]$Action,
    [string]$ProjectPath=(Join-Path $PSScriptRoot '..\UnityXRTemplate'),
    [string]$UnityPath=$env:UNITY_EDITOR_PATH
)
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath($ProjectPath)
$versionFile=Join-Path $project 'ProjectSettings\ProjectVersion.txt'
if(-not(Test-Path -LiteralPath $versionFile)){throw 'Not a Unity project.'}
$version=((Get-Content -LiteralPath $versionFile | Select-String '^m_EditorVersion:').Line -split ': ')[1]
$onWindows=[Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Windows)
if(-not $UnityPath){
    $candidates=@("$env:ProgramFiles\Unity\Hub\Editor\$version\Editor\Unity.exe","$env:ProgramFiles\Unity\$version\Editor\Unity.exe")
    $UnityPath=$candidates | Where-Object {Test-Path -LiteralPath $_} | Select-Object -First 1
}
if(-not $UnityPath -or -not(Test-Path -LiteralPath $UnityPath)){throw "Pass -UnityPath for Unity $version or set UNITY_EDITOR_PATH. No Editor is installed automatically."}
if($onWindows){
    $live=Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Replace('/','\').IndexOf($project.Replace('/','\'),[StringComparison]::OrdinalIgnoreCase)-ge0 }
    if($live){throw 'This project has a live Unity process. Use its bridge or close it yourself.'}
}
$instance=Join-Path $project 'Library\EditorInstance.json'
if(Test-Path -LiteralPath $instance){
    $info=Get-Content -LiteralPath $instance -Raw | ConvertFrom-Json
    if($info.process_id -and (Get-Process -Id $info.process_id -ErrorAction SilentlyContinue)){throw 'This project already has a live Editor. Use its project-local bridge or close it yourself; no existing session will be interrupted.'}
}
$repo=[IO.Path]::GetFullPath((Join-Path $project '..'))
$raw=Join-Path $repo 'Reports\Raw';[IO.Directory]::CreateDirectory($raw)|Out-Null
$log=Join-Path $raw ($Action+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.log')
$arguments=@('-projectPath',('"'+$project+'"'),'-logFile',('"'+$log+'"'))
if($Action-eq'BuildAndroid'){$arguments+=@('-buildTarget','Android')}
if($Action-eq'BuildWindows'){$arguments+=@('-buildTarget','Win64')}
if($Action-ne'Open'){
    $arguments+= '-batchmode'
    if($Action-in@('EditTests','PlayTests')){
        $mode=if($Action-eq'EditTests'){'EditMode'}else{'PlayMode'}
        $arguments+=@('-runTests','-testPlatform',$mode,'-testResults',('"'+(Join-Path $repo ('Reports\'+$mode+'-results.xml'))+'"'))
    }else{
        $method=@{Validate='BatchValidate';Capture='BatchCapture';BuildWindows='BuildWindows';BuildAndroid='BuildAndroid'}[$Action]
        $arguments+=@('-executeMethod',('XRStarter.Editor.StarterValidation.'+$method),'-quit')
    }
}
$launch=@{FilePath=$UnityPath;ArgumentList=$arguments;PassThru=$true}
if($onWindows){$launch.WindowStyle='Hidden'}
$started=Start-Process @launch
Write-Output "Unity ${version}: PID $($started.Id), action $Action, project $project"
if($Action-eq'Open'){return}
$started.WaitForExit();$started.Refresh()
Write-Output "Exit: $($started.ExitCode). Log: $log"
Get-Content -LiteralPath $log -Tail 12
if($started.ExitCode-ne0){throw "Unity $Action failed; inspect $log"}
