[English](README.md) | [한국어](README.ko.md) | [日本語](README.jp.md)

# GSheet (Google Sheets to strongly-typed C# data pipeline for Unity)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
![.NET Standard 2.1](https://img.shields.io/badge/.NET%20Standard-2.1-orange)
![.NET](https://img.shields.io/badge/.NET-8.0%2B-blueviolet)

GSheet is a plugin that converts game design data written in Google Sheets into C# types and binary data ready for use in Unity.
Data created in Google Sheets can be used in both the Editor and at runtime.
Downloaded data automatically generates types, allowing you to access values via `Gsheet.Instance`.

## Required Dependency

GSheet requires [LWSerializer](https://github.com/hoguma9x/LWSerializer.git). Install LWSerializer in the Unity project before using GSheet. GSheet cannot compile, generate sheet data, or load generated data correctly without it.

For Unity, add the following Git URL through the Unity Package Manager (UPM): [https://github.com/hoguma9x/LWSerializer.git](https://github.com/hoguma9x/LWSerializer.git)

## Motivation
Managing game design data in Google Sheets and integrating it with Unity is a very common pipeline in the industry.
However, due to the lack of an official plugin, each project had a different implementation with varying functional limitations.
This plugin was developed to consolidate the core features required in real live-service development and maximize productivity.

## Key Features
1. __Change Diff Viewer__
    - Provides a `DiffViewer` to visually inspect data changes by comparing the downloaded data against the previous version.
2. __LWSerialize (High-Performance Serialization Library)__
    - Uses a DirectMemoryCopy-based serialization library to dramatically reduce data size and improve load speed.
3. __Unity DOTS Support__
   - Supports formats such as `NativeContainer` and `FixedString` for ECS architecture.
4. __List / Dictionary / struct / class / external Options__
   - These keywords can be configured when structuring Google Sheet data.
   - `external` uses an existing type without generating a sheet type declaration.
5. __High Extensibility__
   - Supports array types and user-defined types (CustomType).
   - Any type implementing the `IGSheetParser` interface can be used.
6. __Localization Support__
   - Provides localization functionality using Google Sheets.
   - `TextMeshProLocalizeUGUI` offers multi-language support and font mapping for TextMeshPro.
   - `LocalizeString` returns a string according to the currently configured language.

## Configuration
<img width="650" height="505" alt="image" src="https://github.com/user-attachments/assets/d3fee405-affa-4ab1-949e-aab4ed5c0835" />

Settings are located at `Assets/Resources/GsheetSetting.asset` by default. If the asset does not exist, it will be automatically created on script reload.
The main configuration options are as follows.

| Field | Description |
| --- | --- |
| `Code Generation Path` | The path where generated C# code will be saved. Default is `Scripts/Generator`. |
| `Sheet ID` | The `/d/{spreadsheetId}/` portion of the Google Spreadsheet URL. |
| `Localize Setting / Sheet Name` | The sheet name to use as localization data. e.g. `Localize` |
| `Localize Setting / Font Sets` | TMP font mapping per language. |

If `Code Generation Path` in the settings asset is `Scripts/Generator`, the namespace of the generated code will be `Generator`.

## Google Sheet Writing Rules

### 1. Write the Type Keyword in Cell A1
In cell A1 of each sheet, write the type to be generated.

| Value | Behavior |
| --- | --- |
| `class` | Generates the sheet type as a partial class. |
| `struct` | Generates the sheet type as a partial struct. |
| `external` | Skips generation of the sheet type and uses an existing external type. |

`external` is a generation directive, not a C# type keyword. The sheet's binary data and its `Gsheet` collection are still generated, but `{SheetName}.cs` is not. Define a public type whose name matches the sheet name in the generated-code namespace. The type must implement `ILwSerializable` and `IDisposable`; classes also need a public parameterless constructor. Its serialization order and types must match the columns declared from B1 onward.

For example, a sheet named `Unit` can use an existing `Unit` type by writing `external` in A1:

| external | string name | int hp |
| --- | --- | --- |
| unit_100 | Soldier | 100 |

After generation, `Gsheet.Unit` uses the existing `Unit` type without generating `Unit.cs`.

### 2. Write Types and Variable Names in the First Row
Declare fields in each column of the first row using the following format.

| struct   |string name|int hp| Vector3 pos | Color color | int[] rewards |
|----------| --- | --- |-------------|------------|---------------|
| unit_100 |Soldier|100| 0.2,0.11    |#0000FF| 1,2,3         |
| unit_101 |Archer|70| 1.22,-5     |#FF0000| 2,2,6         |

Since the first column may be used as the data key, actual data fields are generated starting from the second column.

### 3. Distinguish List / Dictionary by the First Column
If a data row has a value in the first column, the sheet is generated as `Dictionary<string, SheetType>`.

| class|string name|int hp|
| --- | --- | --- |
| unit_100|Soldier|100|
| unit_101|Archer|70|

Generated result:

```csharp
public static Dictionary<string, Unit> Unit => Instance._Unit;
```

If the first column is empty, the sheet is generated as `List<SheetType>`.

| class|string name|int hp|
| --- | --- | --- |
| |Soldier|100|
| |Archer|70|

Generated result:

```csharp
public static List<Unit> Unit => Instance._Unit;
```

### 4. Comment Rows and Ignored Columns

- If an entire row starts with `//`, it is treated as a comment row and ignored.
- If a column header in the first row starts with `//`, that entire column is ignored.

### 5. ExampleImage
<img width="1862" height="1031" alt="ex_2" src="https://github.com/user-attachments/assets/278672e9-3092-43d7-a5bd-f3b479d9cf19" />
<img width="2013" height="941" alt="ex_1" src="https://github.com/user-attachments/assets/dce97536-d5c4-4560-947b-969f817bc0d2" />

## Supported Types

The following types are available with the default implementation.

- C# primitives: `int`, `float`, `long`, `bool`, etc.
- `string`
- enum
- Arrays: `int[]`, `string[]`, etc.
- Unity types: `Vector2`, `Vector3`, `Vector2Int`, `Vector3Int`, `Color`
- DOTS / Unity.Collections types:
  - `NativeArray<T>`
  - `NativeReference<T>`
  - `FixedString32Bytes`
  - `FixedString64Bytes`
  - `FixedString128Bytes`
  - `FixedString512Bytes`
  - `FixedString4096Bytes`
- Custom types that provide a parsing method via `IParserFormatter` or `IGSheetParser`
- User-defined types that support binary serialization via `ILwSerializable`

Arrays and NativeArrays decompose the internal string using `StringArray.Convert` and re-parse each element with the appropriate type formatter.

## Running Generation

The following items are available from the Unity top menu.

| Menu | Description |
| --- | --- |
| `Tools/- Gsheet -/Generate` | Downloads the Google Sheet, saves binary data, and runs code generation. |
| `Tools/- Gsheet -/View GoogleSheet` | Opens the currently configured Google Sheet in a browser. |
| `Tools/- Gsheet -/View Setting` | Opens the `GsheetSetting` Inspector window. |
| `Tools/- Gsheet -/View Log-Diff` | Opens the Diff Viewer to inspect data changes before and after generation. |

On successful generation, the following files are updated.

```text
Assets/Resources/GsheetData.bytes
Assets/Scripts/Generator/GSheet.cs
Assets/Scripts/Generator/{SheetName}.cs
```

The last generation info is displayed in `LastUpdateInfo` in the `GsheetSetting` Inspector.

## Runtime Usage

Generated data is accessed via the static properties of the `Gsheet` class.

```csharp
using Generator;
using UnityEngine;

public class UnitSample : MonoBehaviour
{
    private void Start()
    {
        var unit = Gsheet.ExampleStruct[Gsheet.UNIT_100];
        Debug.Log(unit.speed);

        foreach (var item in Gsheet.ExampleClass)
        {
            Debug.Log(item.localizeName);
        }
    }
}
```

When `Gsheet.Instance` is first accessed, it reads `Resources/GsheetData.bytes` and restores all sheet data.

Keys for sheets generated as Dictionary are also generated as constants in the `Gsheet` partial class.

```csharp
Gsheet.UNIT_100
Gsheet.UI_OK
```

## Localization Usage

Specify the localization sheet name in `Localize Setting / Sheet Name` within `GsheetSetting.asset`. The current sample configuration uses the `Localize` sheet.

The localization sheet uses the first column as the key and subsequent column names as language codes.

```text
class,string EN,string KR,string JP
ui_ok,OK,확인,確認
ui_cancel,Cancel,취소,キャンセル
```

On generation, `LangCode.cs` is updated to match the language columns of the localization sheet.

To change the language in code, use `LocalizeManager`.

```csharp
using SheetData;
using SheetData.Localize;

LocalizeManager.Instance.SetLanguage(LangCode.KR);
```

To retrieve a string directly, use the following.

```csharp
string text = LocalizeManager.Instance.Localize(Gsheet.UI_OK);
```

The `TextMeshProLocalizeUGUI` component holds a `LocalizeString` key and automatically updates its text and font whenever the language changes.

## Custom Type Parser

To convert a sheet string into a custom type, implement `IGSheetParser`. Since `IGSheetParser` requires both `IParserFormatter` and `ILwSerializable`, you must provide both a string parsing method and a binary serialization method.

```csharp
using LWSerializer;
using SheetData.IO;
using SheetData.Scripts.Parsing;

public class CustomClass : IGSheetParser
{
    private string _unitName;
    private int _unitNumber;

    public object ToData(string content)
    {
        var datas = StringArray.Convert(content);
        return new CustomClass
        {
            _unitName = datas[0],
            _unitNumber = int.Parse(datas[1])
        };
    }

    public void Write(string content, SheetBinaryWriter writer)
    {
        writer.Write((CustomClass)ToData(content));
    }

    public void OnNativeWrite(LwBinaryWriter writer)
    {
        writer.Write(_unitName, _unitNumber);
    }

    public void OnNativeRead(LwBinaryReader reader)
    {
        reader.Read(out _unitName, out _unitNumber);
    }
}
```

If the parser type and the actual target type differ, you can use `ParserTriggerAttribute`.

```csharp
[ParserTrigger(typeof(MyTargetType))]
public class MyTargetTypeFormatter : IParserFormatter
{
    public object ToData(string content)
    {
        // Convert string to MyTargetType
    }

    public void Write(string content, SheetBinaryWriter writer)
    {
        writer.Write((MyTargetType)ToData(content));
    }
}
```

## Workflow

1. Set the Google Sheet ID and code generation path in `Assets/Resources/GsheetSetting.asset`.
2. Run `Tools/- Gsheet -/Generate` from the Unity menu.
3. The plugin refreshes each sheet name and gid from Google Sheets.
4. Each sheet is downloaded as a CSV.
5. The first row header is read to interpret type information.
6. Sheet data is saved as binary to `Assets/Resources/GsheetData.bytes`.
7. Per-sheet types and `Gsheet.cs` are generated under `Assets/Scripts/Generator`.
8. At runtime, the `Gsheet` singleton reads the binary via `Resources.Load<TextAsset>` and restores the data.

## Viewing Changes
<img width="525" height="735" alt="image" src="https://github.com/user-attachments/assets/8cfa128d-3d28-4636-9c72-6e311006af0b" />

Records of Add, Remove, and Insert operations that occurred during the last generation are displayed.
Note that comparison for objects implementing `IDisposable` (such as `NativeArray`) is not supported.

## Notes
- Google Sheets must be set to public or have appropriate sharing permissions accessible from the Editor. (Sharing with anyone who has the link is recommended.)
- Fields that require disposal, such as `NativeArray<T>` and `NativeReference<T>`, are cleaned up in the generated type's `Dispose()`. If you implement `Dispose` in a custom type, make sure to also implement the `IDisposable` interface.
- Extend generated code by writing additional logic in a separate `partial class` file.

## Sample Sheet
The project's default configuration points to the following sample Google Sheet.

```text
https://docs.google.com/spreadsheets/d/1188AKPfAl2taqn6G-JDENJF-WeO_YA_gE4SRYzMRZBc/edit
```

