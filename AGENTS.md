# GSheet AI 작업 안내

이 문서는 GSheet API를 다루는 AI를 위한 진입점이다. 작업에 필요한 문서와 소스만 읽는다.

GSheet는 [LWSerializer](https://github.com/hoguma9x/LWSerializer.git)를 필수로 사용한다. LWSerializer가 없으면 컴파일, 시트 데이터 생성, 생성 데이터 로드가 정상 동작하지 않으므로 작업 전에 Unity 프로젝트에 설치되어 있는지 확인한다.

- API 설정, 시트 설계, 데이터 생성, 런타임 조회, 로컬라이징, 커스텀 파서 작업: [SKILL.md](SKILL.md)를 먼저 읽는다.
- 기능 설명과 사용 예제: [README.ko.md](README.ko.md), 영문 원문: [README.md](README.md).
- 구현 확인: SKILL.md의 작업별 소스 표에서 관련 파일을 선택한다. README와 구현이 다르면 현재 소스의 시그니처·메뉴·경로를 확인하고 차이를 알린다.

시트 데이터와 스키마는 Google Sheet에서 관리하고 코드와 바이너리는 재생성한다. 생성 파일을 직접 고쳐 기능을 추가하지 않는다. 프로젝트의 실제 설정과 생성된 타입을 확인한 뒤 API를 사용한다.
