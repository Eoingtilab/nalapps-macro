# NalaApps Design System

공통 리소스 파일:

`src/NalApps.Macro/Themes/NalaApps.DesignSystem.xaml`

다른 WPF 유틸리티에서는 이 파일을 프로젝트의 `Themes/` 폴더로 복사한 뒤 `App.xaml`에서 병합합니다.

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source="Themes/NalaApps.DesignSystem.xaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

## 공통 색상

- `PrimaryBrush`: 주요 실행 및 강조 버튼
- `DangerBrush`: 삭제 및 중지 버튼
- `BackgroundBrush`: 앱 배경
- `SurfaceBrush`: 카드 배경
- `TextBrush`: 기본 텍스트
- `MutedBrush`: 보조 텍스트
- `LineBrush`: 구분선과 입력 테두리
- `SoftBrush`: 보조 버튼과 리스트 배경

## 공통 컴포넌트 스타일

- `NalaCard`: 흰색 카드, 16px 라운드, 얇은 테두리, 약한 그림자
- `NalaButtonBase`: 기본 회색 버튼
- `PrimaryButtonStyle`: 파란색 배경과 흰색 글자
- `DangerButtonStyle`: 빨간색 배경과 흰색 글자
- `IconButtonStyle`: 34×34 아이콘 버튼

기본 `Button`, `TextBox`, `ListBox`, `ListBoxItem`, `Window`, `TextBlock` 스타일도 리소스에서 공통 적용됩니다.

## 사용 원칙

- 작은 데스크톱 유틸리티를 우선합니다.
- 기본 창은 430~560px 폭을 권장합니다.
- 주요 행동은 파란색 버튼 하나로 강조합니다.
- 삭제와 긴급 중지만 빨간색을 사용합니다.
- 파란색 및 빨간색 버튼의 글자는 항상 흰색입니다.
- 카드와 버튼의 라운드는 각각 16px, 11px를 기본으로 사용합니다.
- 한 화면에서 주요 기능을 실행할 수 있도록 불필요한 패널을 줄입니다.
