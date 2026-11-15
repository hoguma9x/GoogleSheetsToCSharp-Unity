param(
    [string]$ProjectRoot = (Resolve-Path "$PSScriptRoot/../../../../..").Path,
    [string]$LWSerializerSource,
    [string]$UnityEditor,
    [switch]$Offline
)
$ErrorActionPreference = 'Stop'
if (!$UnityEditor) {
    $version = (Select-String -Path "$ProjectRoot/ProjectSettings/ProjectVersion.txt" -Pattern '^m_EditorVersion: (.+)$').Matches.Groups[1].Value
    $UnityEditor = "$env:ProgramFiles/Unity/Hub/Editor/$version/Editor/Unity.exe"
}
if (!$LWSerializerSource) {
    $resolution = Get-Content "$ProjectRoot/Library/PackageManager/projectResolution.json" -Raw | ConvertFrom-Json
    $package = @($resolution.outputs.PSObject.Properties.Value | Where-Object { $_.name -eq 'com.hoguma.lwserializer' })
    if ($package.Count -ne 1) { throw 'Pass -LWSerializerSource pointing to the resolved LWSerializer Source directory.' }
    $LWSerializerSource = Join-Path $package[0].resolvedPath 'Source'
}
$plugin = (Resolve-Path "$PSScriptRoot/../..").Path
$build = "$PSScriptRoot/obj/unity"
$generated = "$PSScriptRoot/obj/Generated"
New-Item -ItemType Directory -Force $build, $generated | Out-Null
$editorData = Join-Path (Split-Path $UnityEditor -Parent) 'Data'
$compiler = "$editorData/DotNetSdkRoslyn/csc.dll"
if (!(Test-Path $compiler)) {
    $compiler = (Get-ChildItem "$editorData/DotNetSdk/sdk/*/Roslyn/bincore/csc.dll" | Select-Object -First 1).FullName
}
$references = @(Get-ChildItem "$editorData/NetStandard/ref/2.1.0/*.dll") +
    @(Get-ChildItem "$editorData/NetStandard/compat/2.1.0/shims/netstandard/*.dll") +
    @(Get-ChildItem "$editorData/Managed/UnityEngine/*.dll") +
    @(Get-ChildItem "$editorData/Managed/UnityEditor*.dll" | Where-Object { $_.Name -ne 'UnityEditor.dll' }) +
    @(Get-ChildItem "$editorData/Managed/UnityEditor/*.dll" -ErrorAction SilentlyContinue) +
    @(Get-ChildItem "$ProjectRoot/Library/ScriptAssemblies/*.dll" | Where-Object { $_.Name -notlike 'Gsheets*' -and $_.Name -notlike 'Assembly-CSharp*' }) +
    @(Get-ChildItem "$ProjectRoot/Library/PackageCache/com.unity.collab-proxy@*/Lib/Editor/Unity.Plastic.Newtonsoft.Json.dll") +
    @(Get-ChildItem "$(Split-Path $LWSerializerSource -Parent)/Plugins" -Recurse -Filter '*.dll') +
    @((Get-Item "$plugin/Plugin/Scriban.6.5.2/lib/netstandard2.0/Scriban.dll"))
$referencePaths = @($references.FullName | Select-Object -Unique)
function Compile([string]$name, [string[]]$sources, [string[]]$extra, [string]$target = 'library') {
    $response = @('-nologo', "-target:$target", '-unsafe+', '-langversion:9', '-nostdlib+',
        '-define:UNITY_5_3_OR_NEWER,UNITY_2017_1_OR_NEWER,UNITY_EDITOR', "-out:`"$build/$name`"")
    $response += @($referencePaths + $extra | ForEach-Object { '-reference:"' + $_ + '"' })
    $response += @($sources | ForEach-Object { '"' + $_ + '"' })
    $response | Set-Content "$build/$name.rsp" -Encoding UTF8
    & "$editorData/NetCoreRuntime/dotnet.exe" $compiler "@$build/$name.rsp"
    if ($LASTEXITCODE -ne 0) { throw "Compilation failed: $name" }
}
Compile 'Gsheets.dll' @(Get-ChildItem "$plugin/Scripts" -Recurse -Filter '*.cs' | ForEach-Object FullName) @()
Compile 'Gsheets.Editor.dll' @(Get-ChildItem "$plugin/Editor" -Recurse -Filter '*.cs' | ForEach-Object FullName) @("$build/Gsheets.dll")
Compile 'GenerateSmoke.exe' @("$PSScriptRoot/GenerateSmoke.cs") @("$build/Gsheets.dll", "$build/Gsheets.Editor.dll") 'exe'
$previousMonoPath = $env:MONO_PATH
try {
    $env:MONO_PATH = (@($build, "$ProjectRoot/Library/ScriptAssemblies", "$editorData/Managed", "$editorData/Managed/UnityEngine",
        "$editorData/MonoBleedingEdge/lib/mono/unityjit-win32/Facades", "$plugin/Plugin/Scriban.6.5.2/lib/netstandard2.0") +
        @($referencePaths | ForEach-Object { Split-Path $_ -Parent } | Select-Object -Unique)) -join ';'
    & "$editorData/MonoBleedingEdge/bin/mono.exe" "$build/GenerateSmoke.exe" "$ProjectRoot/Library/ScriptAssemblies/Assembly-CSharp.dll" $generated
    if ($LASTEXITCODE -ne 0) { throw 'Unity template/wire generation failed.' }
}
finally { $env:MONO_PATH = $previousMonoPath }
Compile 'GeneratedModels.dll' (@(Get-ChildItem "$generated/*.cs" | ForEach-Object FullName) +
    @(Get-ChildItem "$ProjectRoot/Assets/Scripts/Examples/*.cs" | ForEach-Object FullName)) @("$build/Gsheets.dll", "$build/Gsheets.Editor.dll")

$restoreArgs = @('-p:NuGetAudit=false')
if ($Offline) { $restoreArgs += @('--source', "$env:USERPROFILE/.nuget/packages") }
& dotnet build "$PSScriptRoot/Gsheets.Smoke.csproj" "-p:LWSerializerSource=$LWSerializerSource" -p:UseSharedCompilation=false -m:1 @restoreArgs
if ($LASTEXITCODE -ne 0) { throw '.NET build failed.' }
& dotnet "$PSScriptRoot/bin/Debug/net8.0/Gsheets.Smoke.dll" "$ProjectRoot/Assets/Resources/GsheetData.bytes" "$generated/wire.bytes"
if ($LASTEXITCODE -ne 0) { throw 'Cross-runtime smoke failed.' }
