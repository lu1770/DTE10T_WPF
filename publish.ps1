param(
    [string]$Configuration = "Release",
    [string[]]$RuntimeIdentifiers = @("win-x86", "win-x64"),
    [string]$TargetFramework = "net8.0-windows10.0.17763.0",
    [switch]$NoZip,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

$projectPath = "$PSScriptRoot\DTE10T_WPF.csproj"
$basePublishDir = "$PSScriptRoot\publish"

if (-not (Test-Path $projectPath)) {
    Write-Error "Project file not found: $projectPath"
    exit 1
}

if ($Clean) {
    Write-Host "Cleaning publish directory..." -ForegroundColor Cyan
    if (Test-Path $basePublishDir) {
        Remove-Item -Path $basePublishDir -Recurse -Force
        Write-Host "Cleaned: $basePublishDir" -ForegroundColor Green
    }
    exit 0
}

$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo((Get-ChildItem -Path "$PSScriptRoot\bin\$Configuration\$TargetFramework" -Filter "DTE10T_WPF.dll" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1).FullName).ProductVersion 2>$null
if (-not $version) {
    $version = Get-Date -Format "yyyyMMdd"
}
$version = $version.Replace(".", "_")

foreach ($rid in $RuntimeIdentifiers) {
    $publishDir = "$basePublishDir\$rid"
    $zipPath = "$basePublishDir\DTE10T_WPF_${rid}_v${version}.zip"

    Write-Host "`nPublishing $rid version..." -ForegroundColor Cyan
    Write-Host "Configuration: $Configuration"
    Write-Host "Target Framework: $TargetFramework"
    Write-Host "Publish Directory: $publishDir"

    if (Test-Path $publishDir) {
        Remove-Item -Path $publishDir -Recurse -Force
    }

    $publishArgs = @(
        "publish",
        $projectPath,
        "-c", $Configuration,
        "-f", $TargetFramework,
        "-r", $rid,
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-o", $publishDir
    )

    Write-Host "Executing: dotnet $($publishArgs -join ' ')" -ForegroundColor DarkGray

    try {
        dotnet @publishArgs

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Publish failed with exit code: $LASTEXITCODE"
            exit $LASTEXITCODE
        }

        Write-Host "Publish succeeded!" -ForegroundColor Green

        if (-not $NoZip) {
            Write-Host "Creating zip package..." -ForegroundColor Cyan

            if (Test-Path $zipPath) {
                Remove-Item -Path $zipPath -Force
            }

            Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -Force
            Write-Host "Zip package created: $zipPath" -ForegroundColor Green
        }
    }
    catch {
        Write-Error "Error during publish: $_"
        exit 1
    }
}

Write-Host "`nAll publish tasks completed!" -ForegroundColor Green