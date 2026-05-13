param(
    [switch]$IncludeIisExpress
)

$ErrorActionPreference = "Stop"

function Assert-Administrator {
    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Hay chay script bang PowerShell voi quyen Administrator."
    }
}

function Enable-FeatureIfNeeded {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FeatureName
    )

    $feature = Get-WindowsOptionalFeature -Online -FeatureName $FeatureName
    if ($feature.State -eq "Enabled") {
        Write-Host "[OK] $FeatureName da bat."
        return
    }

    Write-Host "[..] Dang bat $FeatureName ..."
    Enable-WindowsOptionalFeature -Online -FeatureName $FeatureName -All -NoRestart | Out-Null
}

Assert-Administrator

$features = @(
    "IIS-WebServerRole",
    "IIS-WebServer",
    "IIS-CommonHttpFeatures",
    "IIS-DefaultDocument",
    "IIS-StaticContent",
    "IIS-HttpErrors",
    "IIS-HttpRedirect",
    "IIS-ApplicationDevelopment",
    "IIS-ISAPIExtensions",
    "IIS-ISAPIFilter",
    "IIS-HealthAndDiagnostics",
    "IIS-HttpLogging",
    "IIS-Security",
    "IIS-RequestFiltering",
    "IIS-Performance",
    "IIS-WebServerManagementTools",
    "IIS-ManagementConsole"
)

if ($IncludeIisExpress) {
    $features += "IIS-HostableWebCore"
}

foreach ($featureName in $features) {
    Enable-FeatureIfNeeded -FeatureName $featureName
}

$aspNetCoreModulePath = "C:\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
if (Test-Path $aspNetCoreModulePath) {
    Write-Host "[OK] AspNetCoreModuleV2 da co san."
}
else {
    Write-Warning "Chua tim thay AspNetCoreModuleV2. Can cai ASP.NET Core Hosting Bundle truoc khi site IIS co the chay ung dung nay."
}

Write-Host "Hoan tat kiem tra prerequisites IIS. Neu co yeu cau restart, hay khoi dong lai may truoc khi deploy."
