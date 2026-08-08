param(
    [Parameter(Mandatory = $true)]
    [string]$Destination
)

$ErrorActionPreference = "Stop"
$Version = "1.3.9"
$ReleaseUrl = "https://github.com/orioncactus/pretendard/releases/download/v$Version/Pretendard-$Version.zip"
$Fonts = @(
    "Pretendard-Regular.otf",
    "Pretendard-Medium.otf",
    "Pretendard-SemiBold.otf"
)

New-Item -ItemType Directory -Force -Path $Destination | Out-Null
$missing = $Fonts | Where-Object { -not (Test-Path (Join-Path $Destination $_)) }
if (-not $missing) {
    Write-Host "Pretendard font resources already exist."
    exit 0
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("nallamacro-pretendard-" + [guid]::NewGuid().ToString("N"))
$zip = Join-Path $tempRoot "Pretendard.zip"
$expanded = Join-Path $tempRoot "expanded"

try {
    New-Item -ItemType Directory -Force -Path $tempRoot, $expanded | Out-Null
    Write-Host "Downloading Pretendard $Version from the official release..."
    Invoke-WebRequest -Uri $ReleaseUrl -OutFile $zip -UseBasicParsing
    Expand-Archive -Path $zip -DestinationPath $expanded -Force

    foreach ($font in $Fonts) {
        $source = Get-ChildItem $expanded -Recurse -File -Filter $font | Select-Object -First 1
        if (-not $source) {
            throw "Pretendard font not found in official archive: $font"
        }

        $target = Join-Path $Destination $font
        Copy-Item $source.FullName $target -Force
        if ((Get-Item $target).Length -lt 100KB) {
            throw "Pretendard font file is unexpectedly small: $font"
        }
    }

    Write-Host "Pretendard $Version font resources prepared."
}
finally {
    Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
