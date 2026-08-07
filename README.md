# NalaApps Macro

무료·무인증 Windows 데스크톱 키보드 및 마우스 매크로 프로그램입니다.

## 정식 버전

- 현재 버전: `1.1.0`
- 지원 운영체제: Windows 10/11 64비트
- 런타임: .NET 8 Desktop Runtime 필요 (framework-dependent 경량 배포)
- 이용 정책: 회원가입, 시리얼, 서버 인증, 사용 횟수 제한 없음

## v1.1.0 주요 기능

### 키보드

- A~Z, 0~9, F1~F24 및 주요 특수키 입력
- `Ctrl+C`, `Ctrl+V`, `Ctrl+Shift+S`, `Alt+Tab`, `Win+D` 등 조합키
- Space 등 키를 지정 시간 동안 누르고 있기
- 누르고 있기 시간: 1~86,400초
- 취소·중지 시 눌린 키를 역순으로 안전하게 해제

### 마우스

- 현재 위치 또는 지정 좌표에서 동작
- 마우스 이동, 왼쪽 클릭, 오른쪽 클릭, 더블클릭
- 휠 위·아래
- 횟수 지정 반복 클릭
- 지정 시간 동안 연속 클릭
- 반복 간격: 10~60,000ms
- 위치 선택창과 저장 좌표 테스트

### 문자 및 시간

- `문자` 버튼을 누르면 별도 입력창에서 키보드로 직접 입력
- 여러 줄 문장, 한글·영문·숫자·유니코드 입력
- Enter와 Tab을 실제 키 입력으로 실행
- 글자 사이 입력 간격 설정
- 대기 시간: 1~86,400초
- 1초 단위 증감, 직접 숫자 입력, 빠른 프리셋

### 편집·실행

- 모든 주요 버튼에 명시적인 클릭 동작 연결
- `동작` 버튼에서 마우스 이동, 클릭, 연속 클릭, 휠, 키 누르기, 문자, 대기 메뉴 제공
- 단계 추가·상세 편집·빠른 설정·위아래 이동·삭제
- 지정 횟수 반복 및 무한 반복
- JSON 저장·불러오기, 기존 스키마 v1 파일 호환
- 전역 단축키:
  - `Ctrl + Alt + F8`: 현재 마우스 위치 단계 추가
  - `Ctrl + Alt + F9`: 실행
  - `Ctrl + Alt + F10`: 일시정지·재개
  - `Ctrl + Alt + F12`: 즉시 중지
- 파란색·빨간색 버튼의 글자를 흰색으로 고정

## 사용 방법

1. `키보드`, `마우스`, `시간`, `문자` 중 필요한 동작을 누릅니다.
2. 열린 설정창에서 키·좌표·문자·시간·반복 조건을 지정합니다.
3. `동작` 버튼에서는 연속 클릭이나 휠 같은 세부 동작을 바로 선택할 수 있습니다.
4. 실행 순서에서 단계를 선택해 `편집`하거나 위아래로 이동합니다.
5. 반복 횟수를 정한 뒤 `실행` 또는 `Ctrl + Alt + F9`를 누릅니다.
6. 긴급 중지는 언제든지 `Ctrl + Alt + F12`입니다.

## 예시

### Ctrl+C

1. `키보드` 클릭
2. `Ctrl + C` 선택
3. `한 번 누르기`
4. `동작 적용`

### 스페이스바 10초 누르기

1. `키보드` 클릭
2. `Space` 선택
3. `누르고 있기`
4. `10초` 입력
5. `동작 적용`

### 50초 기다리기

1. `시간` 클릭
2. `50` 직접 입력 또는 `50초` 프리셋 선택
3. `시간 추가`

### 10초간 연속 왼쪽 클릭

1. `동작` 클릭
2. `연속 왼쪽 클릭` 선택
3. 실행 시간 `10초`
4. 클릭 간격 설정
5. `마우스 동작 적용`

## 품질 검증

이 프로젝트는 ISO/IEC/IEEE 29119의 테스트 프로세스 개념과 ISO/IEC 25010 품질 특성을 참고하여 다음 검증을 자동화합니다.

- 기능·경계값·오류 처리·회귀 테스트
- A~Z, 숫자, F키, 조합키 파싱
- 마우스 클릭·반복·연속 클릭 실행 순서
- 문자·Enter·Tab·유니코드 입력
- 키 누르고 있기 및 취소 시 안전 해제
- 저장 형식 v1 호환과 v2 왕복 직렬화
- 모든 주요 UI 버튼 이벤트 연결
- 색상 버튼 흰색 글자 계약
- 경고를 오류로 처리하는 Release 빌드
- framework-dependent 단일 EXE publish와 시작 확인

관련 문서:

- `docs/TEST_PLAN_ISO_29119.md`
- `docs/QUALITY_EVALUATION_ISO_25010.md`
- `docs/TEST_REPORT_V1.1.0.md`

이는 국제표준의 요구사항을 참고한 프로젝트 수준의 적용이며 제3자 공식 인증을 의미하지 않습니다.

## 배포 파일

정식 배포 파일은 GitHub Actions 검증을 통과한 뒤 생성됩니다.

- `NallaMacro-v1.1.0-win-x64.zip`
- `SHA256SUMS.txt`

압축을 해제한 뒤 `NallaMacro.exe`를 실행하면 됩니다.

현재 경량 배포본은 .NET 8 Desktop Runtime이 설치된 Windows 10/11 64비트 환경을 대상으로 합니다. 런타임이 없는 PC를 위한 설치 안내 또는 별도 self-contained 패키지는 정식 배포 정책에서 별도로 결정합니다.

## 직접 빌드

```powershell
dotnet restore src/NalApps.Macro/NalApps.Macro.csproj
dotnet restore tests/NalApps.Macro.Tests/NalApps.Macro.Tests.csproj
dotnet build src/NalApps.Macro/NalApps.Macro.csproj -c Release --no-restore -warnaserror
dotnet build tests/NalApps.Macro.Tests/NalApps.Macro.Tests.csproj -c Release --no-restore -warnaserror
dotnet run --project tests/NalApps.Macro.Tests/NalApps.Macro.Tests.csproj -c Release --no-build
dotnet publish src/NalApps.Macro/NalApps.Macro.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=embedded -o artifacts/NallaMacro-v1.1.0-win-x64
```

## 주의사항

- 관리자 권한으로 실행 중인 프로그램에 입력하려면 NalaApps Macro도 동일한 권한 수준으로 실행해야 할 수 있습니다.
- 다른 프로그램이 동일한 전역 단축키를 선점한 경우 앱에서 충돌을 알립니다.
- 화면 해상도, 모니터 배치 또는 배율이 변경되면 저장된 좌표를 다시 확인해야 합니다.
- 무한 반복이나 연속 클릭 전에는 긴급 중지 단축키 `Ctrl + Alt + F12`를 확인하십시오.
- Windows 보안 정책상 `Ctrl + Alt + Delete`는 자동 입력할 수 없습니다.
- 온라인 게임, 금융 서비스, 티켓 예매 등 자동화가 제한된 서비스에서는 각 서비스의 약관과 정책을 준수해야 합니다.

## 라이선스

MIT License
