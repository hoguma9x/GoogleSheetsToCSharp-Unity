[English](README.md) | [한국어](README.ko.md) | [日本語](README.jp.md)

# GSheet (Google Sheets to strongly-typed C# data pipeline for Unity)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
![.NET Standard 2.1](https://img.shields.io/badge/.NET%20Standard-2.1-orange)
![.NET](https://img.shields.io/badge/.NET-8.0%2B-blueviolet)

GSheet는 Google Sheets에 작성한 기획 데이터를 Unity에서 바로 사용할 수 있는 C# 타입과 바이너리 데이터로 변환하는 플러그인입니다.
구글시트에서 작업한 데이터는 에디터와 런타임 모두 사용가능합니다
다운로드 된 데이터는 자동으로 타입을 생성시켜줘 Gsheet.Instance를 통해 값에 접근할 수 있습니다.

## 제작이유
Google Sheets를 기반으로 기획 데이터를 관리하고 이를 Unity 연동하는 파이프라인은 업계에서 매우 보편적입니다. 
그러나 공식적으로 제공하는 플러그인의 부재로 인해 프로젝트마다 구현 방식이 상이하고 기능적 한계가 존재했습니다. 
이 플러그인은 실제 라이브 서비스 개발 과정에서 필요로 하는 핵심 기능들을 집약하여 생산성을 극대화하고자 개발되었습니다.

## 주요기능
1. __변경사항 Viewer 지원__
    - 데이터 다운로드 이후 이전버전과 비교하여 변경된 데이터에 대한 시각적으로 확인할 수 있는 `DiffViewer` 제공 
2. __LWSerialize (고성능 직렬화 라이브러리 적용)__
    - DirectMemoryCopy 메커니즘 기반의 직렬화 라이브러리를 사용해서 데이터 용량과 로드속도 비약적 개선
3. __UnityDOTS 지원__
   - ECS 아키텍처를 위한 `NativeContainer`, `FixedString` 등의 형식 지원
4. __List/Dictionary/struct/class/external에 대한 선택지 제공__
   - GoogleSheet 데이터를 구성할 때 위 키워드 설정 가능
   - `external`을 사용하면 시트 타입 선언을 생성하지 않고 기존 타입을 사용
5. __넓은 확장성__
   - 배열형식 또는 사용자 정의 타입(CustomType) 지원
   - `IGSheetParser` 인터페이스를 적용한 모든 타입 사용가능
6. __로컬라이제이션 지원__
   - 구글시트를 활용해 로컬라이징을 할수있도록 기능지원
   - `TextMeshProLocalizeUGUI` TextMeshPro에 다국어 지원기능과 폰트 매핑기능 제공
   - `LocalizeString`) 현재 설정된 언어에맞추어 문자열을 반환해주는 기능제공
 
## 설정 방법
<img width="650" height="505" alt="image" src="https://github.com/user-attachments/assets/d3fee405-affa-4ab1-949e-aab4ed5c0835" />

설정은 기본적으로 `Assets/Resources/GsheetSetting.asset`에 있습니다. 에셋이 없으면 스크립트 리로드 시 자동 생성합니다.
주요 설정값은 다음과 같습니다.

| 항목 | 설명 |
| --- | --- |
| `Code Generation Path` | 생성된 C# 코드가 저장될 경로입니다. 기본값은 `Scripts/Generator`입니다. |
| `Sheet ID` | Google Spreadsheet URL의 `/d/{spreadsheetId}/` 부분입니다. |
| `Localize Setting / Sheet Name` | 로컬라이징 데이터로 사용할 시트 이름입니다. 예: `Localize` |
| `Localize Setting / Font Sets` | 언어별 TMP 폰트 매핑입니다. |

설정 에셋의 `Code Generation Path`가 `Scripts/Generator`이면 생성 코드의 네임스페이스는 `Generator`가 됩니다.

## Google Sheet 작성 규칙

### 1. A1 셀에 타입 키워드 작성
각 시트의 A1 셀에는 생성할 타입 종류를 적습니다.

| 값 | 동작 |
| --- | --- |
| `class` | 시트 타입을 partial class로 생성합니다. |
| `struct` | 시트 타입을 partial struct로 생성합니다. |
| `external` | 시트 타입 생성을 건너뛰고 기존 외부 타입을 사용합니다. |

`external`은 C# 타입 키워드가 아니라 생성 지시자입니다. 시트의 바이너리 데이터와 `Gsheet` 컬렉션은 계속 생성되지만 `{시트 이름}.cs`는 생성하지 않습니다. 생성 코드의 네임스페이스에 시트 이름과 같은 public 타입을 미리 정의해야 합니다. 이 타입은 `ILwSerializable`과 `IDisposable`을 구현해야 하며, class라면 public 매개변수 없는 생성자도 필요합니다. 직렬화 순서와 타입은 B1 이후에 선언한 컬럼과 일치해야 합니다.

예를 들어 `Unit` 시트에서 기존 `Unit` 타입을 사용하려면 A1에 `external`을 작성합니다.

| external | string name | int hp |
| --- | --- | --- |
| unit_100 | Soldier | 100 |

Generate를 실행하면 `Unit.cs`를 생성하지 않고 `Gsheet.Unit`에서 기존 `Unit` 타입을 사용합니다.


### 2. 첫 행에 타입과 변수명 작성
첫 행의 각 컬럼에는 다음 형식으로 필드를 선언합니다.

| struct   |string name|int hp| Vector3 pos | Color color | int[] rewards |
|----------| --- | --- |-------------|------------|---------------|
| unit_100 |Soldier|100| 0.2,0.11    |#0000FF| 1,2,3         |
| unit_101 |Archer|70| 1.22,-5     |#FF0000| 2,2,6         |


첫 번째 컬럼은 데이터 키로 사용될 수 있으므로, 실제 데이터 필드는 두 번째 컬럼부터 생성됩니다.

### 3. 첫 번째 컬럼으로 List / Dictionary 구분
데이터 행의 첫 번째 컬럼에 Key 값이 있으면 해당 시트는 `Dictionary<string, SheetType>`로 생성됩니다.

| class|string name|int hp|
| --- | --- | --- |
| unit_100|Soldier|100|
| unit_101|Archer|70|

생성 결과:

```csharp
public static Dictionary<string, Unit> Unit => Instance._Unit;
```

첫 번째 컬럼이 비어 있으면 해당 시트는 `List<SheetType>`로 생성됩니다.

| class|string name|int hp|
| --- | --- | --- |
| |Soldier|100|
| |Archer|70|

생성 결과:

```csharp
public static List<Unit> Unit => Instance._Unit;
```

### 4. 주석 행과 무시 컬럼

- 행 전체가 `//`로 시작하면 주석 행으로 취급되어 무시됩니다.
- 첫 행에서 컬럼 헤더가 `//`로 시작하면 해당 컬럼 전체가 무시됩니다.

### 5. 예제이미지
<img width="1862" height="1031" alt="ex_2" src="https://github.com/user-attachments/assets/278672e9-3092-43d7-a5bd-f3b479d9cf19" />
<img width="2013" height="941" alt="ex_1" src="https://github.com/user-attachments/assets/dce97536-d5c4-4560-947b-969f817bc0d2" />

## 지원 타입

기본 구현 기준으로 다음 타입을 사용할 수 있습니다.

- C# primitive: `int`, `float`, `long`, `bool` 등
- `string`
- enum
- 배열: `int[]`, `string[]` 등
- Unity 타입: `Vector2`, `Vector3`, `Vector2Int`, `Vector3Int`, `Color`
- DOTS / Unity.Collections 타입:
  - `NativeArray<T>`
  - `NativeReference<T>`
  - `FixedString32Bytes`
  - `FixedString64Bytes`
  - `FixedString128Bytes`
  - `FixedString512Bytes`
  - `FixedString4096Bytes`
- `IParserFormatter` 또는 `IGSheetParser`로 파싱 방법을 제공한 커스텀 타입
- `ILwSerializable`로 바이너리 직렬화가 가능한 사용자 타입

배열과 NativeArray는 내부 문자열을 `StringArray.Convert`로 분해해 각 원소 타입의 포매터로 다시 파싱합니다.

## 생성 실행

Unity 상단 메뉴에서 다음 항목을 사용할 수 있습니다.

| 메뉴 | 설명 |
| --- | --- |
| `Tools/- Gsheet -/Generate` | Google Sheet 다운로드, 바이너리 저장, 코드 생성을 실행합니다. |
| `Tools/- Gsheet -/View GoogleSheet` | 현재 설정된 Google Sheet를 브라우저로 엽니다. |
| `Tools/- Gsheet -/View Setting` | `GsheetSetting` 인스펙터 창을 엽니다. |
| `Tools/- Gsheet -/View Log-Diff` | 생성 전후 데이터 차이를 확인하는 Diff Viewer를 엽니다. |

생성에 성공하면 다음 파일이 갱신됩니다.

```text
Assets/Resources/GsheetData.bytes
Assets/Scripts/Generator/GSheet.cs
Assets/Scripts/Generator/{SheetName}.cs
```

마지막 생성 정보는 `GsheetSetting` 인스펙터의 `LastUpdateInfo`에 표시됩니다.

## 런타임 사용법

생성된 데이터는 `Gsheet` 클래스의 static property로 접근합니다.

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

`Gsheet.Instance`가 처음 접근될 때 `Resources/GsheetData.bytes`를 읽고 전체 시트 데이터를 복원합니다.

Dictionary로 생성된 시트의 key는 `Gsheet` partial class에 상수로도 생성됩니다.

```csharp
Gsheet.UNIT_100
Gsheet.UI_OK
```

## 로컬라이징 사용법

`GsheetSetting.asset`의 `Localize Setting / Sheet Name`에 로컬라이징 시트 이름을 지정합니다. 현재 예제 설정은 `Localize` 시트를 사용합니다.

로컬라이징 시트는 첫 번째 컬럼을 key로 사용하고, 이후 컬럼명을 언어 코드로 사용합니다.

```text
class,string EN,string KR,string JP
ui_ok,OK,확인,確認
ui_cancel,Cancel,취소,キャンセル
```

생성 시 `LangCode.cs`가 로컬라이징 시트의 언어 컬럼에 맞춰 갱신됩니다.

코드에서 언어를 변경하려면 `LocalizeManager`를 사용합니다.

```csharp
using SheetData;
using SheetData.Localize;

LocalizeManager.Instance.SetLanguage(LangCode.KR);
```

문자열만 가져올 때는 다음처럼 사용할 수 있습니다.

```csharp
string text = LocalizeManager.Instance.Localize(Gsheet.UI_OK);
```

`TextMeshProLocalizeUGUI` 컴포넌트는 `LocalizeString` key를 가지고 있다가 언어가 변경되면 자동으로 텍스트와 폰트를 갱신합니다.

## 커스텀 타입 파서

시트 문자열을 직접 만든 타입으로 변환하려면 `IGSheetParser`를 구현합니다. `IGSheetParser`는 `IParserFormatter`와 `ILwSerializable`을 함께 요구하므로, 문자열 파싱과 바이너리 직렬화 방식을 모두 제공해야 합니다.

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

파서 타입과 실제 대상 타입이 다를 경우에는 `ParserTriggerAttribute`를 사용할 수 있습니다.

```csharp
[ParserTrigger(typeof(MyTargetType))]
public class MyTargetTypeFormatter : IParserFormatter
{
    public object ToData(string content)
    {
        // 문자열을 MyTargetType으로 변환
    }

    public void Write(string content, SheetBinaryWriter writer)
    {
        writer.Write((MyTargetType)ToData(content));
    }
}
```

## 동작 흐름

1. `Assets/Resources/GsheetSetting.asset`에서 Google Sheet ID와 코드 생성 경로를 설정합니다.
2. Unity 메뉴에서 `Tools/- Gsheet -/Generate`를 실행합니다.
3. 플러그인이 Google Sheet의 각 시트 이름과 gid를 갱신합니다.
4. 각 시트를 CSV로 다운로드합니다.
5. 첫 행의 헤더를 읽어 타입 정보를 해석합니다.
6. 시트 데이터를 `Assets/Resources/GsheetData.bytes`에 바이너리로 저장합니다.
7. `Assets/Scripts/Generator` 아래에 시트별 타입과 `Gsheet.cs`를 생성합니다.
8. 런타임에서는 `Gsheet` 싱글톤이 `Resources.Load<TextAsset>`로 바이너리를 읽고 데이터를 복원합니다.

## 변경사항 View 
<img width="525" height="735" alt="image" src="https://github.com/user-attachments/assets/8cfa128d-3d28-4636-9c72-6e311006af0b" />

마지막 Generator 당시에 발생 한 Add,Remove,Insert에 대한 기록이표시됩니다.
단, NativeArray등 IDisposable 인터페이스가 구현된 객체에대한 비교기능은 지원하지않습니다.

## 주의사항
- Google Sheet는 에디터에서 접근 가능한 공개 또는 권한 설정이 필요합니다. (링크가있는 사용자에게 공유를 추천합니다)
- `NativeArray<T>`, `NativeReference<T>`처럼 dispose가 필요한 필드는 생성 타입의 `Dispose()`에서 정리됩니다. 사용자 지정 형식중 Dispose를 구현하면 반드시 IDisposable 인터페이스를 구현하세요
- 생성 코드는 partial class를 별도 파일에 작성해 확장하세요

## 예제 시트
프로젝트 기본 설정은 아래 예제용 Google Sheet 가 설정되어있습니다

```text
https://docs.google.com/spreadsheets/d/1188AKPfAl2taqn6G-JDENJF-WeO_YA_gE4SRYzMRZBc/edit
```

