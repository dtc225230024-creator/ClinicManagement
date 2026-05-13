param(
    [string]$ProjectPath = "D:\OneDrive\Dev\DoAn\ClinicManagement\ClinicManagement\ClinicManagement.csproj",
    [string]$PublishPath = "D:\OneDrive\Dev\DoAn\ClinicManagement\artifacts\publish\ClinicManagement-IIS",
    [string]$SiteName = "ClinicManagement",
    [string]$AppPoolName = "ClinicManagementPool",
    [string]$HostName = "clinic.local",
    [int]$Port = 8081
)

$ErrorActionPreference = "Stop"

function Assert-Administrator {
    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Hay chay script bang PowerShell voi quyen Administrator."
    }
}

function Assert-IisInstalled {
    $webAdministrationAvailable = Get-Module -ListAvailable -Name WebAdministration
    if (-not $webAdministrationAvailable) {
        throw "Khong tim thay module WebAdministration. Hay bat IIS truoc bang script Enable-IISPrerequisites.ps1."
    }

    $aspNetCoreModulePath = "C:\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
    if (-not (Test-Path $aspNetCoreModulePath)) {
        throw "Khong tim thay AspNetCoreModuleV2. Hay cai ASP.NET Core Hosting Bundle truoc khi deploy."
    }
}

Assert-Administrator
Assert-IisInstalled

Write-Host "[1/5] Publish ban Release..."
dotnet publish $ProjectPath -c Release -o $PublishPath

Import-Module WebAdministration

if (-not (Test-Path $PublishPath)) {
    throw "Khong tim thay thu muc publish: $PublishPath"
}

Write-Host "[2/5] Tao hoac cap nhat App Pool..."
if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
    New-Item "IIS:\AppPools\$AppPoolName" | Out-Null
}
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ""
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name processModel.identityType -Value "ApplicationPoolIdentity"
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name autoStart -Value "True"

Write-Host "[3/5] Gan quyen thu muc cho App Pool..."
$appPoolIdentity = "IIS AppPool\$AppPoolName"
icacls $PublishPath /grant "${appPoolIdentity}:(OI)(CI)(RX)" | Out-Null

Write-Host "[4/5] Tao hoac cap nhat Website..."
if (-not (Test-Path "IIS:\Sites\$SiteName")) {
    New-Website -Name $SiteName -PhysicalPath $PublishPath -ApplicationPool $AppPoolName -Port $Port -HostHeader $HostName | Out-Null
}
else {
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath -Value $PublishPath
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name applicationPool -Value $AppPoolName

    $existingBindings = Get-WebBinding -Name $SiteName
    foreach ($binding in $existingBindings) {
        Remove-WebBinding -Name $SiteName -Protocol $binding.protocol -Port $binding.bindingInformation.Split(":")[1] -HostHeader $binding.HostHeader
    }

    New-WebBinding -Name $SiteName -Protocol "http" -Port $Port -HostHeader $HostName | Out-Null
}

Write-Host "[5/5] Khoi dong lai IIS site..."
Start-WebAppPool -Name $AppPoolName
if ((Get-Website -Name $SiteName).State -ne "Started") {
    Start-Website -Name $SiteName
}

Write-Host ""
Write-Host "Deploy hoan tat."
Write-Host "Site:   $SiteName"
Write-Host "URL:    http://${HostName}:$Port/"
Write-Host "Folder: $PublishPath"
Write-Host ""
Write-Host "Neu dung host name moi, hay them vao file hosts:"
Write-Host "127.0.0.1 $HostName"
