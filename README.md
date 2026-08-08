# NalaApps Macro

무료·무인증 Windows 데스크톱 키보드 및 마우스 매크로 프로그램입니다.

## 정식 버전

- 현재 버전: `1.1.1`
- GitHub 태그: `v1.1.1`
- 지원 운영체제: Windows 10/11 64비트
- 런타임: .NET 8 Desktop Runtime 필요 (framework-dependent 경량 배포)
- 이용 정책: 회원가입, 시리얼, 서버 인증, 사용 횟수 제한 없음
- 배포 파일: `NallaMacro-v1.1.1-win-x64.zip`

## v1.1.1 변경 사항

- 시작 인트로를 제거하고 실행 즉시 메인 창이 열리도록 안정화
- Pretendard Regular / Medium / SemiBold를 WPF 리소스로 내장
- 사용자 PC에 Pretendard가 설치되어 있지 않아도 동일한 UI 폰트 사용
- 일반 버튼 `SemiBold → Medium`, 주요 버튼 `Bold → SemiBold`로 한 단계 경량화
- 주요 제목 `Bold → SemiBold`, 상태 강조 `SemiBold → Medium`으로 조정
- Pretendard SIL Open Font License 1.1 고지를 배포 패키지에 포함
- 폰트 파일은 외부 설치 파일로 배포하지 않고 실행 파일 리소스로 포함
- 기존 키보드·마우스·문자·시간·반복·단축키 기능 유지

## 주요 기능

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

- `동작` 버튼에서 마우스 이동, 클릭, 연속 클릭, 휠, 키 누르기, 문자, 대기 메뉴 제공
- 단계 추가·상세 편집·복사·위아래 이동·삭제
- `선택 단계 아래 추가` 선택 시 현재 단계 바로 아래 삽입
- 지정 횟수 반복 및 무한 반복
- JSON 저장·불러오기, 기존 스키마 v1 파일 호환
- 전역 단축키:
  - `Ctrl + Alt + F8`: 현재 마우스 위치 단계 추가
  - `Ctrl + Alt + F9`: 실행
  - `Ctrl + Alt + F10`: 일시정지·재개
  - `Ctrl + Alt + F12`: 즉시 중지

## 사용 방법

1. `키보드`, `마우스`, `시간`, `문자` 중 필요한 동작을 누릅니다.
2. 열린 설정창에서 키·좌표·문자·시간·반복 조건을 지정합니다.
3. `동작` 버튼에서는 연속 클릭이나 휠 같은 세부 동작을 바로 선택할 수 있습니다.
4. 실행 순서에서 단계를 선택해 `편집`하거나 위아래로 이동합니다.
5. 반복 횟수를 정한 뒤 `실행` 또는 `Ctrl + Alt + F9`를 누릅니다.
6. 긴급 중지는 언제든지 `Ctrl + Alt + F12`입니다.

## 폰트 및 라이선스

NalaApps Macro는 Pretendard 1.3.9의 다음 굵기를 앱 리소스로 내장합니다.

- Pretendard Regular 400
- Pretendard Medium 500
- Pretendard SemiBold 600

Pretendard는 SIL Open Font License 1.1에 따라 앱과 함께 임베드·재배포되며, 배포 ZIP 안의 `THIRD-PARTY-NOTICES/Pretendard-OFL.txt`에서 라이선스 전문을 확인할 수 있습니다.

## 품질 검증

이 프로젝트는 ISO/IEC/IEEE 29119의 테스트 프로세스 개념과 ISO/IEC 25010 품질 특성을 참고하여 자동 검증합니다.

- 기능·경계값·오류 처리·회귀 테스트
- A~Z, 숫자, F키, 조합키 파싱
- 마우스 클릭·반복·연속 클릭 실행 순서
- 문자·Enter·Tab·유니코드 입력
- 키 누르고 있기 및 취소 시 안전 해제
- 저장 형식 v1 호환과 v2 왕복 직렬화
- WPF 창 로딩 및 주요 UI 동작 smoke test
- Pretendard 내장 리소스 및 Pack URI 검증
- 외부 `.otf`/`.ttf` 파일 미노출 검증
- 경고를 오류로 처리하는 Release 빌드
- framework-dependent 단일 EXE publish와 실제 시작 확인
- ZIP 및 SHA256 생성

관련 문서:

- `docs/TEST_PLAN_ISO_29119.md`
- `docs/QUALITY_EVALUATION_ISO_25010.md`
- `docs/TEST_REPORT_V1.1.0.md` (이전 정식 검증 기준 보고서)

이는 국제표준 요구사항을 참고한 프로젝트 수준의 적용이며 제3자 공식 인증을 의미하지 않습니다.

## 정식 배포 파일

EDD와 GitHub Release에는 EXE 단독 파일이 아니라 다음 ZIP 패키지를 사용합니다.

- `NallaMacro-v1.1.1-win-x64.zip`
- `SHA256SUMS.txt`

ZIP을 해제한 뒤 `NallaMacro.exe`를 실행합니다. 배포 패키지에는 Pretendard 라이선스 고지 파일도 함께 포함됩니다.

현재 정식판은 .NET 8 Desktop Runtime이 설치된 Windows 10/11 64비트 환경을 대상으로 합니다.

## 직접 빌드

```powershell
dotnet restore src/NalApps.Macro/NalApps.Macro.csproj
dotnet restore tests/NalApps.Macro.Tests/NalApps.Macro.Tests.csproj
dotnet build src/NalApps.Macro/NalApps.Macro.csproj -c Release --no-restore -warnaserror
dotnet build tests/NalApps.Macro.Tests/NalApps.Macro.Tests.csproj -c Release --no-restore -warnaserror
dotnet run --project tests/NalApps.Macro.Tests/NalApps.Macro.Tests.csproj -c Release --no-build
dotnet publish src/NalApps.Macro/NalApps.Macro.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=embedded -o artifacts/NallaMacro-v1.1.1-win-x64
```

첫 Restore/Build 시 공식 Pretendard v1.3.9 릴리스에서 필요한 폰트 리소스를 자동 준비합니다. 이후 빌드에서는 준비된 리소스를 재사용합니다.

## 주의사항

- 관리자 권한으로 실행 중인 프로그램에 입력하려면 NalaApps Macro도 동일한 권한 수준으로 실행해야 할 수 있습니다.
- 다른 프로그램이 동일한 전역 단축키를 선점한 경우 앱에서 충돌을 알립니다.
- 화면 해상도, 모니터 배치 또는 배율이 변경되면 저장된 좌표를 다시 확인해야 합니다.
- 무한 반복이나 연속 클릭 전에는 긴급 중지 단축키 `Ctrl + Alt + F12`를 확인하십시오.
- Windows 보안 정책상 `Ctrl + Alt + Delete`는 자동 입력할 수 없습니다.
- 온라인 게임, 금융 서비스, 티켓 예매 등 자동화가 제한된 서비스에서는 각 서비스의 약관과 정책을 준수해야 합니다.

## 라이선스

NalaApps Macro: MIT License

Pretendard: SIL Open Font License 1.1
