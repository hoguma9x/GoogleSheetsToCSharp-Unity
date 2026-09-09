[English](README.md) | [한국어](README.ko.md) | [日本語](README.jp.md)

# GSheet (GoogleSheet to Data)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
![.NET Standard 2.1](https://img.shields.io/badge/.NET%20Standard-2.1-orange)
![.NET](https://img.shields.io/badge/.NET-8.0%2B-blueviolet)

GSheet は、Google スプレッドシートで作成した企画データを Unity ですぐに使用できる C# 型とバイナリデータに変換するプラグインです。
Google スプレッドシートで作業したデータはエディタとランタイムの両方で使用できます。
ダウンロードされたデータは自動的に型を生成し、`Gsheet.Instance` を通じて値にアクセスできます。

## 制作理由
Google スプレッドシートをベースに企画データを管理し、Unity と連携するパイプラインは業界で非常に一般的です。
しかし公式プラグインが存在しないため、プロジェクトごとに実装方法が異なり、機能的な限界がありました。
このプラグインは実際のライブサービス開発過程で必要とされるコア機能を集約し、生産性を最大化するために開発されました。

## 主な機能
1. __変更点 Viewer サポート__
    - データダウンロード後に以前のバージョンと比較し、変更されたデータを視覚的に確認できる `DiffViewer` を提供
2. __LWSerialize（高性能シリアライズライブラリの適用）__
    - DirectMemoryCopy メカニズムベースのシリアライズライブラリを使用し、データ容量とロード速度を大幅に改善
3. __UnityDOTS サポート__
   - ECS アーキテクチャ向けの `NativeContainer`・`FixedString` などの形式をサポート
4. __List / Dictionary / struct / class / external の選択肢提供__
   - Google スプレッドシートのデータを構成する際、上記キーワードを設定可能
   - `external` を使用すると、シート型の宣言を生成せず既存の型を使用
5. __高い拡張性__
   - 配列形式またはユーザー定義型（CustomType）をサポート
   - `IGSheetParser` インターフェースを適用したすべての型が使用可能
6. __ローカライズサポート__
   - Google スプレッドシートを活用したローカライズ機能をサポート
   - `TextMeshProLocalizeUGUI` TextMeshPro への多言語サポートとフォントマッピング機能を提供
   - `LocalizeString` 現在設定されている言語に合わせて文字列を返す機能を提供

## 設定方法
<img width="650" height="505" alt="image" src="https://github.com/user-attachments/assets/d3fee405-affa-4ab1-949e-aab4ed5c0835" />

設定は基本的に `Assets/Resources/GsheetSetting.asset` にあります。アセットが存在しない場合、スクリプトリロード時に自動生成されます。
主な設定項目は以下の通りです。

| 項目 | 説明 |
| --- | --- |
| `Code Generation Path` | 生成された C# コードが保存されるパスです。デフォルトは `Scripts/Generator` です。 |
| `Sheet ID` | Google スプレッドシート URL の `/d/{spreadsheetId}/` の部分です。 |
| `Localize Setting / Sheet Name` | ローカライズデータとして使用するシート名です。例：`Localize` |
| `Localize Setting / Font Sets` | 言語ごとの TMP フォントマッピングです。 |

設定アセットの `Code Generation Path` が `Scripts/Generator` の場合、生成コードの名前空間は `Generator` になります。

## Google スプレッドシートの記述ルール

### 1. A1 セルに型キーワードを記述
各シートの A1 セルには、生成する型の種類を記述します。

| 値 | 動作 |
| --- | --- |
| `class` | シート型を partial class として生成します。 |
| `struct` | シート型を partial struct として生成します。 |
| `external` | シート型の生成をスキップし、既存の外部型を使用します。 |

`external` は C# の型キーワードではなく、生成指示です。シートのバイナリデータと `Gsheet` コレクションは引き続き生成されますが、`{シート名}.cs` は生成されません。生成コードの名前空間に、シート名と同名の public 型をあらかじめ定義してください。この型は `ILwSerializable` と `IDisposable` を実装する必要があり、class の場合は public な引数なしコンストラクターも必要です。シリアライズの順序と型は、B1 以降で宣言したカラムと一致させてください。

たとえば `Unit` シートで既存の `Unit` 型を使用する場合は、A1 に `external` を記述します。

| external | string name | int hp |
| --- | --- | --- |
| unit_100 | Soldier | 100 |

Generate を実行すると、`Unit.cs` を生成せず、`Gsheet.Unit` で既存の `Unit` 型を使用します。

### 2. 先頭行に型と変数名を記述
先頭行の各カラムには、以下の形式でフィールドを宣言します。

| struct   |string name|int hp| Vector3 pos | Color color | int[] rewards |
|----------| --- | --- |-------------|------------|---------------|
| unit_100 |Soldier|100| 0.2,0.11    |#0000FF| 1,2,3         |
| unit_101 |Archer|70| 1.22,-5     |#FF0000| 2,2,6         |

最初のカラムはデータキーとして使用されるため、実際のデータフィールドは2番目のカラムから生成されます。

### 3. 最初のカラムで List / Dictionary を区別
データ行の最初のカラムに Key 値がある場合、そのシートは `Dictionary<string, SheetType>` として生成されます。

| class|string name|int hp|
| --- | --- | --- |
| unit_100|Soldier|100|
| unit_101|Archer|70|

生成結果：

```csharp
public static Dictionary<string, Unit> Unit => Instance._Unit;
```

最初のカラムが空の場合、そのシートは `List<SheetType>` として生成されます。

| class|string name|int hp|
| --- | --- | --- |
| |Soldier|100|
| |Archer|70|

生成結果：

```csharp
public static List<Unit> Unit => Instance._Unit;
```

### 4. コメント行と無視カラム

- 行全体が `//` で始まる場合、コメント行として扱われ無視されます。
- 先頭行でカラムヘッダーが `//` で始まる場合、そのカラム全体が無視されます。

### 5. ExampleImage
<img width="1862" height="1031" alt="ex_2" src="https://github.com/user-attachments/assets/278672e9-3092-43d7-a5bd-f3b479d9cf19" />
<img width="2013" height="941" alt="ex_1" src="https://github.com/user-attachments/assets/dce97536-d5c4-4560-947b-969f817bc0d2" />


## サポートされる型

基本実装では以下の型を使用できます。

- C# プリミティブ型：`int`、`float`、`long`、`bool` など
- `string`
- enum
- 配列：`int[]`、`string[]` など
- Unity 型：`Vector2`、`Vector3`、`Vector2Int`、`Vector3Int`、`Color`
- DOTS / Unity.Collections 型：
  - `NativeArray<T>`
  - `NativeReference<T>`
  - `FixedString32Bytes`
  - `FixedString64Bytes`
  - `FixedString128Bytes`
  - `FixedString512Bytes`
  - `FixedString4096Bytes`
- `IParserFormatter` または `IGSheetParser` でパース方法を提供したカスタム型
- `ILwSerializable` でバイナリシリアライズが可能なユーザー定義型

配列と NativeArray は内部文字列を `StringArray.Convert` で分解し、各要素型のフォーマッタで再パースします。

## 生成の実行

Unity の上部メニューから以下の項目を使用できます。

| メニュー | 説明 |
| --- | --- |
| `Tools/- Gsheet -/Generate` | Google スプレッドシートのダウンロード、バイナリ保存、コード生成を実行します。 |
| `Tools/- Gsheet -/View GoogleSheet` | 現在設定されている Google スプレッドシートをブラウザで開きます。 |
| `Tools/- Gsheet -/View Setting` | `GsheetSetting` インスペクターウィンドウを開きます。 |
| `Tools/- Gsheet -/View Log-Diff` | 生成前後のデータ差分を確認する Diff Viewer を開きます。 |

生成に成功すると、以下のファイルが更新されます。

```text
Assets/Resources/GsheetData.bytes
Assets/Scripts/Generator/GSheet.cs
Assets/Scripts/Generator/{SheetName}.cs
```

最後の生成情報は `GsheetSetting` インスペクターの `LastUpdateInfo` に表示されます。

## ランタイムでの使用方法

生成されたデータは `Gsheet` クラスの static プロパティからアクセスします。

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

`Gsheet.Instance` に最初にアクセスした際、`Resources/GsheetData.bytes` を読み込んでシート全体のデータを復元します。

Dictionary として生成されたシートのキーは、`Gsheet` partial クラスに定数としても生成されます。

```csharp
Gsheet.UNIT_100
Gsheet.UI_OK
```

## ローカライズの使用方法

`GsheetSetting.asset` の `Localize Setting / Sheet Name` にローカライズシート名を指定します。現在のサンプル設定では `Localize` シートを使用しています。

ローカライズシートは最初のカラムをキーとして使用し、以降のカラム名を言語コードとして使用します。

```text
class,string EN,string KR,string JP
ui_ok,OK,확인,確認
ui_cancel,Cancel,취소,キャンセル
```

生成時に `LangCode.cs` がローカライズシートの言語カラムに合わせて更新されます。

コードで言語を変更するには `LocalizeManager` を使用します。

```csharp
using SheetData;
using SheetData.Localize;

LocalizeManager.Instance.SetLanguage(LangCode.KR);
```

文字列のみ取得する場合は以下のように使用できます。

```csharp
string text = LocalizeManager.Instance.Localize(Gsheet.UI_OK);
```

`TextMeshProLocalizeUGUI` コンポーネントは `LocalizeString` キーを保持し、言語が変更されると自動的にテキストとフォントを更新します。

## カスタム型パーサー

シートの文字列を自作の型に変換するには `IGSheetParser` を実装します。`IGSheetParser` は `IParserFormatter` と `ILwSerializable` の両方を要求するため、文字列パースとバイナリシリアライズの方法を両方提供する必要があります。

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

パーサー型と実際の対象型が異なる場合は、`ParserTriggerAttribute` を使用できます。

```csharp
[ParserTrigger(typeof(MyTargetType))]
public class MyTargetTypeFormatter : IParserFormatter
{
    public object ToData(string content)
    {
        // 文字列を MyTargetType に変換
    }

    public void Write(string content, SheetBinaryWriter writer)
    {
        writer.Write((MyTargetType)ToData(content));
    }
}
```

## 動作フロー

1. `Assets/Resources/GsheetSetting.asset` で Google スプレッドシート ID とコード生成パスを設定します。
2. Unity メニューから `Tools/- Gsheet -/Generate` を実行します。
3. プラグインが Google スプレッドシートの各シート名と gid を更新します。
4. 各シートを CSV としてダウンロードします。
5. 先頭行のヘッダーを読み込んで型情報を解析します。
6. シートデータを `Assets/Resources/GsheetData.bytes` にバイナリとして保存します。
7. `Assets/Scripts/Generator` 以下にシートごとの型と `Gsheet.cs` を生成します。
8. ランタイムでは `Gsheet` シングルトンが `Resources.Load<TextAsset>` でバイナリを読み込みデータを復元します。

## 変更点の確認
<img width="525" height="735" alt="image" src="https://github.com/user-attachments/assets/8cfa128d-3d28-4636-9c72-6e311006af0b" />

最後の Generate 時に発生した Add・Remove・Insert の記録が表示されます。
ただし、`NativeArray` など `IDisposable` インターフェースが実装されたオブジェクトの比較機能はサポートされていません。

## 注意事項
- Google スプレッドシートはエディタからアクセス可能な公開設定または権限設定が必要です。（リンクを知っているユーザーへの共有を推奨します）
- `NativeArray<T>`・`NativeReference<T>` のように Dispose が必要なフィールドは、生成型の `Dispose()` でクリーンアップされます。ユーザー定義型で Dispose を実装する場合は、必ず `IDisposable` インターフェースを実装してください。
- 生成コードは partial クラスを別ファイルに記述して拡張してください。

## サンプルシート
プロジェクトのデフォルト設定には、以下のサンプル用 Google スプレッドシートが設定されています。

```text
https://docs.google.com/spreadsheets/d/1188AKPfAl2taqn6G-JDENJF-WeO_YA_gE4SRYzMRZBc/edit
```


