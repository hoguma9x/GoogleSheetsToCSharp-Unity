---
name: gsheet-api
description: Unity GSheet 플러그인의 시트 스키마 설계, 설정, C# 및 바이너리 생성, 런타임 데이터 조회, 로컬라이징, 커스텀 타입 파서를 구현하거나 진단할 때 사용한다. 일반 Google Sheets 문서 편집만 하는 작업에는 적용하지 않는다.
---

# GSheet API 사용 스킬

[README.md](README.md)와 [README.ko.md](README.ko.md)를 기반으로 한다. 아래의 `Assets/...`는 이 플러그인 폴더가 아니라 사용하는 Unity 프로젝트 루트를 기준으로 한다. 문서의 샘플 이름을 프로젝트에 존재하는 API로 가정하지 않는다.

## 필수 의존성

GSheet는 [LWSerializer](https://github.com/hoguma9x/LWSerializer.git)를 필수로 사용한다. LWSerializer가 없으면 GSheet가 컴파일되지 않으며 시트 데이터 생성과 생성 데이터 로드도 정상 동작하지 않는다. GSheet 관련 작업을 시작하기 전에 Unity 프로젝트에 LWSerializer가 설치되어 있고 참조가 정상적으로 해석되는지 확인한다.

## 작업 시작

1. 사용하는 Unity 프로젝트와 GSheet 설치 위치를 확인한다.
2. `Assets/Resources/GsheetSetting.asset`에서 Sheet ID, Code Generation Path, 로컬라이징 시트 이름과 Font Sets를 확인한다. 설정 에셋은 없으면 스크립트 리로드 시 자동 생성된다.
3. 작업에 필요한 생성 코드에서 네임스페이스, 시트 프로퍼티, 필드, 키 상수와 컬렉션 타입을 확인한다. 기본 경로 `Scripts/Generator`의 네임스페이스는 `Generator`지만 실제 설정을 우선한다.
4. 아래에서 해당 작업 절차만 수행한다. 구현을 수정하거나 문서와 동작이 다르면 마지막 표의 관련 소스를 읽는다.

## 시트 설계와 데이터 변경

- A1에 `class` 또는 `struct`를 쓴다. 첫 행 B열부터 `int hp`, `string name`처럼 `타입 필드명`을 선언한다.
- 첫 열은 데이터 키다. 키가 있는 시트는 `Dictionary<string, SheetType>`, 모든 데이터 행의 첫 열이 빈 시트는 `List<SheetType>`가 된다. 현재 구현은 데이터 행 중 하나라도 첫 열에 값이 있으면 Dictionary로 판정하므로 빈 키와 유효 키를 혼합하지 않는다.
- Dictionary 키는 중복 없이 작성한다. 시트 이름과 필드 이름은 생성할 C# 식별자로 사용할 수 있어야 하며, 키로 생성되는 상수 이름도 생성 코드에서 확인한다.
- `//`로 시작하는 행은 주석으로 제외하고 첫 행의 헤더가 `//`로 시작하는 열은 제외한다.
- 기본 지원 타입은 primitive, string, enum, 배열, `Vector2/3`, `Vector2Int/3Int`, `Color`, `NativeArray<T>`, `NativeReference<T>`, `FixedString32/64/128/512/4096Bytes`다. 구체적인 타입과 셀 표기는 해당 포매터를 확인한다.
- 배열 셀은 `1,2,3`처럼 작성할 수 있다. 배열·복합 값 파싱은 `StringArray.Convert`를 사용하므로 중첩 값이나 구분자 포함 문자열을 임의의 `Split(',')`로 대체하지 않는다.
- 데이터와 스키마의 원본은 Google Sheet다. 요청된 시트 변경 후 재생성하며, `.bytes`나 생성 C#을 직접 수정해 원본과 다른 데이터를 만들지 않는다.

## 설정과 생성

Sheet ID는 Google Sheets URL의 `/d/{spreadsheetId}/` 부분이다. README의 샘플 Sheet ID를 실제 프로젝트 설정에 덮어쓰지 않는다. 다운로드하려면 Editor에서 해당 시트에 접근할 수 있어야 한다. 접근 오류가 나면 현재 공유 권한과 URL을 확인한다.

| Unity 메뉴 | 용도 |
| --- | --- |
| `Tools/- Gsheet -/Generate` | 시트 다운로드, 바이너리 저장, C# 생성 |
| `Tools/- Gsheet -/View GoogleSheet` | 설정된 시트 열기 |
| `Tools/- Gsheet -/GsheetSetting` | 현재 구현의 설정 창 열기 |
| `Tools/- Gsheet -/View Log-Diff` | 생성 전후 데이터 차이 확인 |

README의 `View Setting`은 현재 소스에서 `GsheetSetting`으로 등록되어 있다. 자동화할 때 설치된 버전의 `MenuItem`을 확인한다.

생성 흐름은 시트 이름·gid 갱신 → CSV 다운로드 → 헤더 해석 → 바이너리 저장 → C# 생성 → AssetDatabase 갱신이다. 생성 시 다음 산출물을 함께 확인한다.

- `Assets/Resources/GsheetData.bytes`
- `Assets/{Code Generation Path}/Gsheet.cs`와 `{SheetName}.cs` — 기본 경로는 `Assets/Scripts/Generator`. README는 파일명을 `GSheet.cs`로 표기하지만 현재 생성기는 `Gsheet.cs`를 쓴다.
- 로컬라이징 언어 열을 변경했다면 `LangCode.cs`

Editor 코드에서 실행할 필요가 있으면 `SheetData.Editor.Generator.GsheetGenerator.Run(SheetDataSettingScriptable.Instance)`가 반환하는 Task를 기다린다. `Menu_Generate()`는 내부 Task를 기다리지 않고 반환한다. `Run`도 예외를 로그로 처리하므로 호출 반환만으로 성공을 판단하지 않는다.

생성 후 Console 오류, 컴파일 결과, 설정 Inspector의 `LastUpdateInfo`, 산출물 변경과 Diff Viewer를 함께 확인한다. `IDisposable` 구현 객체(예: NativeArray)는 Diff Viewer의 비교 대상에서 제외되므로 표시가 없다고 값이 같다고 판단하지 않는다.

## 런타임 조회와 확장

생성된 `Gsheet` 클래스의 static property로 접근한다. 첫 `Gsheet.Instance` 접근 시 Resources의 바이너리 데이터를 읽어 복원하므로 생성된 코드와 데이터가 함께 준비되어 있어야 한다.

아래는 `Unit` Dictionary, `UNIT_100` 상수, `hp` 필드가 생성된 프로젝트의 예제다. 적용 전 실제 이름으로 맞춘다.

```csharp
using Generator;

var unit = Gsheet.Unit[Gsheet.UNIT_100];
int hp = unit.hp;
```

List 시트는 해당 static property를 순회하거나 인덱스로 조회한다. 실행 중 키가 없을 수 있다면 Dictionary의 `TryGetValue`로 처리한다.

추가 로직은 생성 파일 밖의 별도 partial 파일에 작성한다. 생성된 선언과 네임스페이스, 타입명, `class`/`struct`를 맞춘다. NativeArray·NativeReference 필드는 생성 타입의 `Dispose()`에서 정리된다. 공유 시트 데이터의 소비자가 임의로 해제하지 않도록 실제 소유권과 사용 수명을 확인한다. 사용자 타입에 Dispose를 구현하면 `IDisposable`도 구현한다.

## 로컬라이징

설정의 Localize Setting / Sheet Name을 실제 로컬라이징 시트와 맞춘다. 첫 열은 키, B열 이후의 필드명은 언어 코드다.

```text
class,string EN,string KR,string JP
ui_ok,OK,확인,確認
ui_cancel,Cancel,취소,キャンセル
```

언어 열 변경 후 재생성된 `LangCode`를 확인한다. `KR`과 `UI_OK`가 생성된 경우 다음처럼 사용한다.

```csharp
using Generator;
using SheetData;
using SheetData.Localize;

LocalizeManager.Instance.SetLanguage(LangCode.KR);
string text = LocalizeManager.Instance.Localize(Gsheet.UI_OK);
```

TMP UI에는 `TextMeshProLocalizeUGUI`를 사용하고 `LocalizeString` 키와 언어별 Font Sets를 설정한다. `LocalizeString`의 현재 네임스페이스는 `Localize.Elements`다. 언어 전환 시 문자열과 폰트가 함께 갱신되는지 확인한다. 누락 문자열은 시트 이름·키·언어 열·재생성 여부를 확인하고, 폰트 문제는 Font Sets와 해당 TMP 폰트의 글리프를 확인한다.

## 커스텀 타입 파서

README의 Custom Type Parser 예제를 참고하되 사용 버전의 어셈블리 경계를 먼저 확인한다.

- `IGSheetParser`는 `IParserFormatter`와 `ILwSerializable`을 함께 요구한다.
- `object ToData(string content)`는 문자열을 실제 대상 타입으로 변환한다.
- `void Write(string content, SheetBinaryWriter writer)`는 파싱된 값을 바이너리로 기록한다.
- `OnNativeWrite(LwBinaryWriter writer)`와 `OnNativeRead(LwBinaryReader reader)`의 필드 순서와 타입을 일치시킨다.
- 파서와 대상 타입을 분리할 때는 `IParserFormatter` 구현에 `[ParserTrigger(typeof(MyTargetType))]`를 붙인다. 대상 타입의 바이너리 직렬화 지원도 별도로 확보한다.

현재 `IGSheetParser`, `IParserFormatter`, `ParserTriggerAttribute`는 `SheetData.Editor` 어셈블리에 있다. 런타임 타입이 Editor 전용 인터페이스를 직접 참조하도록 예제를 그대로 복사하지 않는다. Player에서도 필요한 타입은 런타임 직렬화 타입과 Editor 전용 포매터를 분리하는 방식을 우선 검토하고, 실제 생성·복원 및 Player 컴파일을 확인한다.

## 작업별 소스

다음 경로는 이 SKILL.md가 있는 플러그인 루트 기준이다.

| 확인할 내용 | 읽을 파일 |
| --- | --- |
| 설정·경로·네임스페이스 | [SheetDataSettingScriptable.cs](Scripts/SheetDataSettingScriptable.cs) |
| 메뉴·설정 자동 생성 | [SheetDataSettingScriptableEditor.cs](Editor/SheetDataSettingScriptableEditor.cs) |
| 다운로드·생성·언어 코드 갱신 | [GsheetGenerator.cs](Editor/Generator/GsheetGenerator.cs), [SheetLoader.cs](Editor/DownLoader/SheetLoader.cs) |
| 시트 헤더·컬렉션 판정 | [SheetRawData.cs](Editor/DownLoader/SheetRawData.cs), [HeaderType.cs](Editor/DownLoader/HeaderType.cs) |
| 생성 API·직렬화·Dispose | [GSheetModel.cs](Editor/Generator/GSheetModel.cs), [TypeModel.cs](Editor/Generator/TypeModel.cs) |
| 언어·키·TMP 연동 | [LocalizeManager.cs](Scripts/Localize/LocalizeManager.cs), [LocalizeSheetBinder.cs](Scripts/Localize/LocalizeSheetBinder.cs), [TextMeshProLocalizeUGUI.cs](Scripts/Localize/TextMeshProLocalizeUGUI.cs) |
| 파서 계약·검색·문자열 분해 | [IParserFormatter.cs](Editor/Parsing/Formatter/Base/IParserFormatter.cs), [ParserFormatter.cs](Editor/Parsing/Formatter/ParserFormatter.cs), [StringArray.cs](Editor/Parsing/Formatter/Base/StringArray.cs) |

## 완료 확인

검증은 변경 범위에 맞춘다. 데이터·스키마 변경은 재생성 및 대표 값 복원, 조회 코드 변경은 컴파일 및 해당 조회, 로컬라이징은 언어 전환과 폰트, 커스텀 파서는 문자열 변환·바이너리 왕복·필요한 Dispose를 확인한다. 문서만 변경했다면 링크와 API 명칭을 확인한다. 실행한 검증과 Editor·시트 접근 등의 제약으로 실행하지 못한 검증을 구분해 보고한다.
