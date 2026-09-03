<#
.SYNOPSIS
  Construit le zip de distribution beta Skylightning (Revit 2024/2025/2026), avec
  obfuscation du DLL avant packaging. Cf. memoire "skylightning-code-protection".

.USAGE
  powershell -File scripts\build-beta-release.ps1
#>

param(
    [string[]] $Versions = @("R24", "R25", "R26")
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$revitInfo = @{
    R24 = @{ Year = "2024"; RefTfm = "net48";               ApiVersion = "2024.3.60" }
    R25 = @{ Year = "2025"; RefTfm = "net8.0-windows7.0";    ApiVersion = "2025.4.60" }
    R26 = @{ Year = "2026"; RefTfm = "net8.0-windows7.0";    ApiVersion = "2026.4.10" }
}

$nugetPackages = Join-Path $env:USERPROFILE ".nuget\packages"
$stagingRoot   = Join-Path $repoRoot "bin\beta-staging"
$tempXmlDir    = Join-Path $env:TEMP "skylightning-obfuscar"

# Assemblies WPF/WinForms (PresentationCore, WindowsBase, System.Drawing...) : pas copiees
# dans le dossier de build (references uniquement), il faut leur emplacement pour qu'Obfuscar
# puisse resoudre les types utilises par le plugin (fenetres, icones vectorielles...).
$net48RefDir = "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"

$windowsDesktopRefPack = Get-ChildItem "C:\Program Files\dotnet\packs\Microsoft.WindowsDesktop.App.Ref" -Directory |
    Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$net8RefDir = (Get-ChildItem (Join-Path $windowsDesktopRefPack.FullName "ref") -Directory |
    Select-Object -First 1).FullName

if (Test-Path $stagingRoot) { Remove-Item $stagingRoot -Recurse -Force }
New-Item -ItemType Directory -Path $stagingRoot | Out-Null
New-Item -ItemType Directory -Path $tempXmlDir -Force | Out-Null

dotnet tool restore | Out-Null

foreach ($v in $Versions) {
    $info   = $revitInfo[$v]
    $config = "Release $v"

    Write-Host "=== Build $config ===" -ForegroundColor Cyan
    dotnet build -c $config
    if ($LASTEXITCODE -ne 0) { throw "Echec du build $config" }

    $inPath  = Join-Path $repoRoot "bin\x64\$config"
    $outPath = Join-Path $repoRoot "bin\x64\Obfuscated $v"
    if (Test-Path $outPath) { Remove-Item $outPath -Recurse -Force }

    $refApi     = Join-Path $nugetPackages "nice3point.revit.api.revitapi\$($info.ApiVersion)\ref\$($info.RefTfm)"
    $refApiUi   = Join-Path $nugetPackages "nice3point.revit.api.revitapiui\$($info.ApiVersion)\ref\$($info.RefTfm)"
    $frameworkRefDir = if ($v -eq "R24") { $net48RefDir } else { $net8RefDir }

    $obfuscarXml = @"
<?xml version='1.0'?>
<Obfuscator>
  <Var name='InPath' value='$inPath' />
  <Var name='OutPath' value='$outPath' />
  <Var name='KeepPublicApi' value='false' />
  <Var name='HidePrivateApi' value='true' />
  <Var name='SuppressIldasmAttribute' value='true' />
  <AssemblySearchPath path='$inPath' />
  <AssemblySearchPath path='$refApi' />
  <AssemblySearchPath path='$refApiUi' />
  <AssemblySearchPath path='$frameworkRefDir' />
  <Module file='$inPath\RevitLightingPlugin.dll'>
    <SkipType name='RevitLightingPlugin.Application' />
    <SkipType name='RevitLightingPlugin.Commands.ParametresCommand' />
    <SkipType name='RevitLightingPlugin.Commands.CalculCommand' />
    <SkipType name='RevitLightingPlugin.Commands.AboutCommand' />
  </Module>
</Obfuscator>
"@
    $xmlPath = Join-Path $tempXmlDir "Obfuscar.$v.xml"
    Set-Content -Path $xmlPath -Value $obfuscarXml -Encoding utf8

    Write-Host "=== Obfuscation $v ===" -ForegroundColor Cyan
    dotnet obfuscar.console $xmlPath
    if ($LASTEXITCODE -ne 0) { throw "Echec de l'obfuscation $v" }

    $obfuscatedDll = Join-Path $outPath "RevitLightingPlugin.dll"
    if (-not (Test-Path $obfuscatedDll)) { throw "DLL obfusque introuvable pour $v : $obfuscatedDll" }

    # Dossier de version dans le zip : copie du Release complet, DLL remplacee par la version obfusquee
    $versionStage = Join-Path $stagingRoot "Revit$($info.Year)"
    Copy-Item -Path $inPath -Destination $versionStage -Recurse
    Get-ChildItem -Path $versionStage -Filter "*.pdb" -Recurse | Remove-Item -Force
    Copy-Item -Path $obfuscatedDll -Destination (Join-Path $versionStage "RevitLightingPlugin.dll") -Force

    Write-Host "OK -> $versionStage" -ForegroundColor Green
}

$readme = @"
Skylightning - Plugin Revit (Beta)
====================================

Installation :
1. Fermez Revit s'il est ouvert.
2. Copiez le contenu du dossier correspondant a votre version de Revit
   (Revit2024, Revit2025 ou Revit2026) dans :
   %ProgramData%\Autodesk\Revit\Addins\<annee>\
   (ex: C:\ProgramData\Autodesk\Revit\Addins\2025\)
3. Relancez Revit. L'onglet "Skylightning" apparait dans le ruban.

Support / feedback : skylightning.support@gmail.com
"@
Set-Content -Path (Join-Path $stagingRoot "LISEZMOI.txt") -Value $readme -Encoding utf8

$zipPath = Join-Path $repoRoot "bin\skylightning-beta.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $stagingRoot "*") -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host "`nZip beta pret : $zipPath" -ForegroundColor Green
