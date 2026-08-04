# NalApps Macro

무료·무인증 Windows 데스크톱 키보드 및 마우스 매크로 프로그램입니다.

## 정식 버전

- 현재 버전: `1.0.0`
- 지원 운영체제: Windows 10/11 64비트
- 런타임: .NET 8 self-contained
- 이용 정책: 회원가입, 시리얼, 서버 인증, 사용 횟수 제한 없음

## 주요 기능

- 키보드 문자열 입력 및 단일키·조합키 입력
- 마우스 이동, 좌클릭, 우클릭, 더블클릭, 휠
- 화면을 직접 클릭해 마우스 위치 지정
- 현재 마우스 위치 저장 (`Ctrl + Alt + F8`)
- 밀리초 단위 시간 지연
- 지정 횟수 및 무한 반복
- 시작·재개 (`Ctrl + Alt + F9`)
- 일시정지 (`Ctrl + Alt + F10`)
- 즉시 중지 (`Ctrl + Alt + F12`)
- 단계 편집, 위·아래 이동, 삭제
- JSON 저장 및 불러오기
- 멀티모니터 및 DPI-aware 좌표 처리
- 실행 종료 시 Ctrl, Alt, Shift, Win 키 해제

## 사용 방법

1. `화면에서 위치 찍기`를 누릅니다.
2. 원하는 화면 위치를 클릭합니다. Esc를 누르면 취소됩니다.
3. 클릭, 문자 입력, 키 입력, 시간 대기 등의 단계를 순서대로 추가합니다.
4. 각 단계를 선택해 좌표, 문자, 키 또는 숫자 값을 수정합니다.
5. 반복 횟수를 정한 뒤 `실행` 또는 `Ctrl + Alt + F9`를 누릅니다.
6. 긴급 중지는 언제든지 `Ctrl + Alt + F12`입니다.

## 배포 파일

정식 배포 파일은 `Eoingtilab/nalapps-releases` 저장소의 Releases에서 제공합니다.

- `NalApps-Macro-v1.0.0-Portable-win-x64.zip`
- `SHA256SUMS.txt`

압축을 해제한 뒤 `NalApps.Macro.exe`를 실행하면 됩니다.

## 직접 빌드

```powershell
dotnet restore src/NalApps.Macro/NalApps.Macro.csproj
dotnet build src/NalApps.Macro/NalApps.Macro.csproj -c Release -warnaserror
dotnet publish src/NalApps.Macro/NalApps.Macro.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/NalApps-Macro-v1.0.0-win-x64
```

## 릴리즈 자동화

`v1.0.0` 형식의 태그를 푸시하면 `.github/workflows/release.yml`이 빌드, ZIP 압축, SHA-256 생성 후 `Eoingtilab/nalapps-releases`에 릴리즈를 생성합니다.

교차 저장소 릴리즈를 위해 소스 저장소 Actions secret에 `NALAPPS_RELEASES_TOKEN`이 필요합니다. 이 토큰은 `Eoingtilab/nalapps-releases`에 릴리즈를 생성할 수 있는 최소 권한만 부여해야 합니다.

## 주의사항

- 다른 프로그램이 동일한 전역 단축키를 선점한 경우 등록에 실패할 수 있으며 앱에서 충돌을 알립니다.
- 관리자 권한 프로그램에 입력하려면 NalApps Macro도 동일한 권한 수준으로 실행해야 할 수 있습니다.
- 화면 해상도, 모니터 배치 또는 배율이 변경되면 저장된 좌표를 다시 확인해야 합니다.
- 온라인 게임, 금융 서비스, 티켓 예매 등 자동화가 제한된 서비스에서는 각 서비스의 약관과 정책을 준수해야 합니다.

## 라이선스

MIT License
