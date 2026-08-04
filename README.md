# NalApps Macro

무료·무인증 Windows 데스크톱 매크로 프로그램입니다.

## V1 기능

- 키 입력 및 문자열 입력
- 마우스 이동, 좌/우/더블 클릭, 휠
- 화면에서 위치 찍기 (`Ctrl + Alt + F8`)
- 시간 지연
- 지정 횟수 및 무한 반복
- 시작/재개 (`Ctrl + Alt + F9`)
- 일시정지 (`Ctrl + Alt + F10`)
- 즉시 중지 (`Ctrl + Alt + F12`)
- JSON 저장/불러오기
- 시리얼, 로그인, 서버 인증 없음

## 개발 환경

- Windows 10/11
- .NET 8
- WPF

## 빌드

```powershell
dotnet restore
dotnet build -c Release
dotnet publish src/NalApps.Macro/NalApps.Macro.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

> 다른 프로그램이 같은 전역 단축키를 선점한 경우 등록에 실패할 수 있습니다. 앱은 해당 상황을 사용자에게 표시하도록 설계합니다.
