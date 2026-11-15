# Unity / ASP.NET Core shared data

`Gsheets.Core/Gsheets.Core.csproj` builds the shared reader and collection loader for .NET 8 or later. It compiles the same source files as Unity; Unity keeps its existing assembly, Resources loading, generation menus, serialization attributes and native containers. `DotNet~` is ignored by Unity's asset importer. No binary format conversion is needed.

## Build and share generated types

1. Run **Generate** in Unity after updating GSheet. The updated templates emit source that compiles in Unity and .NET. Do not edit previously generated files by hand.
2. Keep the generated C# files and `Assets/Resources/GsheetData.bytes` from the same generation together.
3. Reference `Gsheets.Core.csproj` from a .NET data project and link the generated C# files. Include any enums and external/custom runtime contracts used by those files.
4. Build with `LWSerializerSource` set to the **Source** directory of the same LWSerializer revision used in Unity. The bridge project compiles the upstream source; it does not download or vendor a second serializer implementation. Its NuGet dependency is `System.IO.Hashing` 8.0.0.

For example, create an application-owned `GameData.csproj` outside Unity's Assets folder (adjust paths):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Unity/Assets/Plugins/GSheet/DotNet~/Gsheets.Core/Gsheets.Core.csproj" />
    <Compile Include="../Unity/Assets/Scripts/Generator/*.cs" Link="Generated/%(Filename)%(Extension)" />
  </ItemGroup>
</Project>
```

Place application-owned runtime contracts under this data project, or link shared contracts separately. Then build your server with:

```powershell
dotnet build Server.csproj -p:LWSerializerSource="D:/Dependencies/LWSerializer/Source"
```

Use `-p:UseSharedCompilation=false` if the compiler server is unavailable in a restricted build environment. The same MSBuild property is needed for restore, build and publish; it can also be set in an application-owned `Directory.Build.props`.

## ASP.NET Core usage

Reference the data project from the ASP.NET Core project. Copy `GsheetData.bytes` to `Data/` in the server project and include it in published output:

```xml
<ItemGroup>
  <Content Include="Data/GsheetData.bytes" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />
</ItemGroup>
```

The following startup example uses the default generated namespace `Generator`. `LoadFromFile` returns an independent instance; access its instance fields rather than the Unity static properties:

```csharp
using Generator;

var builder = WebApplication.CreateBuilder(args);
var path = Path.Combine(builder.Environment.ContentRootPath, "Data", "GsheetData.bytes");
builder.Services.AddSingleton(Gsheet.LoadFromFile(path));
var app = builder.Build();

app.MapGet("/units/{key}", (string key, Gsheet data) =>
    data._ExampleStruct.TryGetValue(key, out var unit)
        ? Results.Ok(new { unit.speed, text = unit.testfxStr.ToString() })
        : Results.NotFound());

app.Run();
```

`LoadFromBytes(byte[])` is also available. In Unity, existing `Gsheet.Instance` and static sheet properties still auto-load from Resources. In .NET, `Gsheet.Instance` throws with instructions to use an explicit loader. The returned instance implements `IDisposable`, so DI disposes its owned generated rows on shutdown. As before, the generated aggregate does not dispose `external` rows.

Alternatively, load without the generated `Gsheet` aggregate:

```csharp
using Gsheets.IO;
using Generator;
using Examples;

var data = GSheetData.FromFile(path,
    typeof(ExampleClass), typeof(TestExternalData), typeof(ExampleStruct),
    typeof(CustomTypes), typeof(Localize));
var units = data.GetDictionary<ExampleStruct>("ExampleStruct");
var rows = data.GetList<ExampleClass>("ExampleClass");
```

Register a public `ILwSerializable` row type for **every** sheet in the file, including external sheets. Class row types need a public parameterless constructor. The explicit map uses sheet/type names and does not depend on Unity settings or assembly scanning. Duplicate type names and missing registrations are rejected.

## Types and compatibility

| Unity field | .NET field |
| --- | --- |
| Primitives, strings, enums, managed arrays | Same type |
| `Vector2/3/4`, `Vector2Int/3Int`, `Quaternion`, `Color` | Matching value layouts in `Gsheets.Compatibility` |
| `NativeArray<T>` | `T[]`, with the same length and unmanaged block encoding |
| `NativeReference<T>` | `T` |
| `FixedString32/64/128/512/4096Bytes` | Fixed-size layouts in `Gsheets.Compatibility`; call `ToString()` for text |
| Custom/external type | Application-owned compatible runtime contract |

The generated Unity branch retains its original field types and native disposal. The .NET value types provide fields and wire layouts, not Unity math/engine behavior. `Vector4` and `Quaternion` layouts can be read when the project supplies their sheet parsers; this change does not add new Unity parsers.

Custom contracts must preserve field types, serialization order, enum underlying types and unmanaged struct layout. Separate `IGSheetParser` / `IParserFormatter` implementations into Editor-only code, or conditionally exclude their parsing members and Editor interfaces in .NET while keeping `ILwSerializable`. Do not reference `Gsheets.Editor`, Unity UI, TMP or ScriptableObjects from server contracts. Custom contracts containing Unity fields need matching conditional aliases/types too; the generator only adapts the fields it generates. `Tests/SampleContracts.cs` demonstrates server contracts for this repository's examples.

Load a snapshot once at server startup and treat it as read-only while handling requests. Do not dispose or mutate it while requests use it. No hot-reload service, server-side Google Sheet generator, NuGet publication, or Unity UI portability is included.

LWSerializer uses unsafe memory reads and does not validate payload bounds. Load only trusted, intact build artifacts; the added header/registration checks are not a validator for arbitrary uploaded files. Use matching serializer versions, little-endian platforms and compatible unmanaged layouts.

## Verification

From the plugin directory, on Windows with this Unity project and its dependencies already compiled:

```powershell
./DotNet~/Tests/Verify.ps1
# With all NuGet dependencies cached:
./DotNet~/Tests/Verify.ps1 -Offline
```

The script resolves the installed LWSerializer path from Unity's package resolution. Optional overrides: `-ProjectRoot`, `-UnityEditor`, `-LWSerializerSource`.

It compiles the Unity runtime and Editor assemblies, renders the actual templates using this project's sample schema, compiles the generated sources for both Unity and .NET, then loads the existing Unity `.bytes` in .NET. Checks cover List/Dictionary, external/custom contracts, localization, portable layouts, a Unity-compiled known wire fixture, parallel independent loads, schema count/registration errors, and ASP.NET Core singleton registration. Generated test files live in ignored `obj/` folders and do not overwrite the project's generated code or data.

This is a standalone compiler/Mono/.NET check, not an Editor Play Mode, Player, IL2CPP, HTTP hosting or cross-architecture test.
