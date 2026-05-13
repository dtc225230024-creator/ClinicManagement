param(
    [string]$BaseUrl = "http://localhost:5080",
    [string]$MySqlExePath = "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe",
    [string]$Database = "clinic_management",
    [string]$DbUser = "root",
    [string]$DbPassword = "admin",
    [string]$AdminUsername = "admin",
    [string]$ReceptionUsername = "letan",
    [string]$DoctorUsername = "bacsi",
    [string]$SharedPassword = "123456"
)

$ErrorActionPreference = "Stop"

function Assert-Dependency {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    if (-not (Test-Path $Path)) {
        throw "Khong tim thay $Label tai: $Path"
    }
}

function Invoke-MySqlQuery {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Query
    )

    $env:MYSQL_PWD = $DbPassword
    try {
        return & $MySqlExePath "-u$DbUser" "-D" $Database "-N" "-e" $Query
    }
    finally {
        $env:MYSQL_PWD = $null
    }
}

function Get-FirstRow {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Query
    )

    $rows = Invoke-MySqlQuery -Query $Query | Where-Object { $_ }
    return $rows | Select-Object -First 1
}

function Get-AntiForgeryToken {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Html
    )

    $match = [regex]::Match($Html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (-not $match.Success) {
        throw "Khong tim thay anti-forgery token."
    }

    return $match.Groups[1].Value
}

function New-LoggedInSession {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Username,
        [Parameter(Mandatory = $true)]
        [string]$Password
    )

    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginPage = Invoke-WebRequest -UseBasicParsing "$BaseUrl/Auth/Login" -WebSession $session
    $token = Get-AntiForgeryToken -Html $loginPage.Content

    Invoke-WebRequest -UseBasicParsing "$BaseUrl/Auth/Login" -Method Post -WebSession $session -Body @{
        "__RequestVerificationToken" = $token
        Username = $Username
        Password = $Password
    } | Out-Null

    return $session
}

function Invoke-AppRequest {
    param(
        [Parameter(Mandatory = $true)]
        [Microsoft.PowerShell.Commands.WebRequestSession]$Session,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    try {
        $response = Invoke-WebRequest -UseBasicParsing ($BaseUrl + $Path) -WebSession $Session
        return [pscustomobject]@{
            StatusCode = [int]$response.StatusCode
            Url = $response.BaseResponse.ResponseUri.AbsoluteUri
            Content = $response.Content
        }
    }
    catch [System.Net.WebException] {
        $webResponse = $_.Exception.Response
        if ($null -eq $webResponse) {
            throw
        }

        $reader = New-Object System.IO.StreamReader($webResponse.GetResponseStream())
        try {
            $content = $reader.ReadToEnd()
        }
        finally {
            $reader.Close()
        }

        return [pscustomobject]@{
            StatusCode = [int]$webResponse.StatusCode
            Url = $webResponse.ResponseUri.AbsoluteUri
            Content = $content
        }
    }
}

function Test-AllowedRoute {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [Microsoft.PowerShell.Commands.WebRequestSession]$Session,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $response = Invoke-AppRequest -Session $Session -Path $Path
    $passed = -not $response.Url.Contains("/Auth/Denied")

    return [pscustomobject]@{
        Name = $Name
        Passed = $passed
        FinalUrl = $response.Url
    }
}

function Test-DeniedRoute {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [Microsoft.PowerShell.Commands.WebRequestSession]$Session,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $response = Invoke-AppRequest -Session $Session -Path $Path
    $passed = $response.Url.Contains("/Auth/Denied")

    return [pscustomobject]@{
        Name = $Name
        Passed = $passed
        FinalUrl = $response.Url
    }
}

function Test-MenuLink {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$Html,
        [Parameter(Mandatory = $true)]
        [string]$Token,
        [Parameter(Mandatory = $true)]
        [bool]$Expected
    )

    $actual = $Html.Contains($Token)
    return [pscustomobject]@{
        Name = $Name
        Passed = ($actual -eq $Expected)
        FinalUrl = "MenuContains=$actual"
    }
}

Assert-Dependency -Path $MySqlExePath -Label "mysql.exe"

$doctorClaimRow = Get-FirstRow -Query @"
SELECT UserId, DoctorId
FROM Users
WHERE Username = '$DoctorUsername' AND IsActive = 1
LIMIT 1;
"@

if (-not $doctorClaimRow) {
    throw "Khong tim thay tai khoan bac si '$DoctorUsername'."
}

$doctorClaimParts = $doctorClaimRow -split "`t"
$doctorId = [int]$doctorClaimParts[1]

$doctorScheduledRow = Get-FirstRow -Query @"
SELECT AppointmentId
FROM Appointments
WHERE DoctorId = $doctorId AND Status = 'Scheduled'
ORDER BY AppointmentDate, AppointmentId
LIMIT 1;
"@

if (-not $doctorScheduledRow) {
    throw "Khong tim thay lich dang cho cua bac si '$DoctorUsername'."
}

$doctorScheduledAppointmentId = [int]$doctorScheduledRow

$doctorRecordRow = Get-FirstRow -Query @"
SELECT mr.AppointmentId
FROM MedicalRecords mr
JOIN Appointments a ON a.AppointmentId = mr.AppointmentId
WHERE a.DoctorId = $doctorId
ORDER BY mr.RecordId
LIMIT 1;
"@

if (-not $doctorRecordRow) {
    throw "Khong tim thay ho so kham cua bac si '$DoctorUsername'."
}

$doctorRecordAppointmentId = [int]$doctorRecordRow

$otherDoctorRecordRow = Get-FirstRow -Query @"
SELECT mr.AppointmentId
FROM MedicalRecords mr
JOIN Appointments a ON a.AppointmentId = mr.AppointmentId
WHERE a.DoctorId <> $doctorId
ORDER BY mr.RecordId
LIMIT 1;
"@

if (-not $otherDoctorRecordRow) {
    throw "Khong tim thay ho so kham cua bac si khac de kiem thu gioi han truy cap."
}

$otherDoctorRecordAppointmentId = [int]$otherDoctorRecordRow

$invoiceRow = Get-FirstRow -Query @"
SELECT AppointmentId
FROM Invoices
ORDER BY InvoiceId
LIMIT 1;
"@

if (-not $invoiceRow) {
    throw "Khong tim thay hoa don de kiem thu."
}

$invoiceAppointmentId = [int]$invoiceRow

$adminSession = New-LoggedInSession -Username $AdminUsername -Password $SharedPassword
$receptionSession = New-LoggedInSession -Username $ReceptionUsername -Password $SharedPassword
$doctorSession = New-LoggedInSession -Username $DoctorUsername -Password $SharedPassword

$adminHome = Invoke-AppRequest -Session $adminSession -Path "/"
$receptionHome = Invoke-AppRequest -Session $receptionSession -Path "/"
$doctorHome = Invoke-AppRequest -Session $doctorSession -Path "/"

$results = @(
    Test-MenuLink -Name "Admin thay menu lich kham" -Html $adminHome.Content -Token 'Reception/Appointments' -Expected $true
    Test-MenuLink -Name "Admin khong thay menu benh nhan" -Html $adminHome.Content -Token 'Reception/Patients' -Expected $false
    Test-MenuLink -Name "Admin khong thay menu dat lich AI" -Html $adminHome.Content -Token 'Reception/CreateAppointment' -Expected $false
    Test-MenuLink -Name "Admin khong thay menu lich ca nhan" -Html $adminHome.Content -Token 'Doctor/Schedule' -Expected $false
    Test-MenuLink -Name "Le tan thay menu benh nhan" -Html $receptionHome.Content -Token 'Reception/Patients' -Expected $true
    Test-MenuLink -Name "Le tan thay menu dat lich AI" -Html $receptionHome.Content -Token 'Reception/CreateAppointment' -Expected $true
    Test-MenuLink -Name "Le tan khong thay menu thong ke" -Html $receptionHome.Content -Token 'Reports/Index' -Expected $false
    Test-MenuLink -Name "Le tan khong thay menu lich ca nhan" -Html $receptionHome.Content -Token 'Doctor/Schedule' -Expected $false
    Test-MenuLink -Name "Bac si thay menu lich ca nhan" -Html $doctorHome.Content -Token 'Doctor/Schedule' -Expected $true
    Test-MenuLink -Name "Bac si khong thay menu benh nhan" -Html $doctorHome.Content -Token 'Reception/Patients' -Expected $false
    Test-MenuLink -Name "Bac si khong thay menu dat lich AI" -Html $doctorHome.Content -Token 'Reception/CreateAppointment' -Expected $false
    Test-MenuLink -Name "Bac si khong thay menu thong ke" -Html $doctorHome.Content -Token 'Reports/Index' -Expected $false

    Test-AllowedRoute -Name "Admin vao Users" -Session $adminSession -Path "/Admin/Users"
    Test-AllowedRoute -Name "Admin vao Doctors" -Session $adminSession -Path "/Admin/Doctors"
    Test-AllowedRoute -Name "Admin vao Departments" -Session $adminSession -Path "/Admin/Departments"
    Test-AllowedRoute -Name "Admin vao Services" -Session $adminSession -Path "/Admin/Services"
    Test-AllowedRoute -Name "Admin vao Appointments read-only" -Session $adminSession -Path "/Reception/Appointments"
    Test-AllowedRoute -Name "Admin vao Reports" -Session $adminSession -Path "/Reports"
    Test-AllowedRoute -Name "Admin vao Records" -Session $adminSession -Path "/Records"
    Test-DeniedRoute -Name "Admin bi chan Patients" -Session $adminSession -Path "/Reception/Patients"
    Test-DeniedRoute -Name "Admin bi chan CreateAppointment" -Session $adminSession -Path "/Reception/CreateAppointment"
    Test-DeniedRoute -Name "Admin bi chan Invoice" -Session $adminSession -Path "/Reception/Invoice?appointmentId=$invoiceAppointmentId"
    Test-DeniedRoute -Name "Admin bi chan Doctor Schedule" -Session $adminSession -Path "/Doctor/Schedule"
    Test-DeniedRoute -Name "Admin bi chan Nhap ket qua kham" -Session $adminSession -Path "/Doctor/MedicalRecord?appointmentId=$doctorScheduledAppointmentId"

    Test-AllowedRoute -Name "Le tan vao Patients" -Session $receptionSession -Path "/Reception/Patients"
    Test-AllowedRoute -Name "Le tan vao CreateAppointment" -Session $receptionSession -Path "/Reception/CreateAppointment"
    Test-AllowedRoute -Name "Le tan vao Appointments" -Session $receptionSession -Path "/Reception/Appointments"
    Test-AllowedRoute -Name "Le tan vao Reschedule" -Session $receptionSession -Path "/Reception/Reschedule?id=$doctorScheduledAppointmentId"
    Test-AllowedRoute -Name "Le tan vao Invoice" -Session $receptionSession -Path "/Reception/Invoice?appointmentId=$invoiceAppointmentId"
    Test-AllowedRoute -Name "Le tan vao Records" -Session $receptionSession -Path "/Records"
    Test-DeniedRoute -Name "Le tan bi chan Users" -Session $receptionSession -Path "/Admin/Users"
    Test-DeniedRoute -Name "Le tan bi chan Reports" -Session $receptionSession -Path "/Reports"
    Test-DeniedRoute -Name "Le tan bi chan Doctor Schedule" -Session $receptionSession -Path "/Doctor/Schedule"
    Test-DeniedRoute -Name "Le tan bi chan Nhap ket qua kham" -Session $receptionSession -Path "/Doctor/MedicalRecord?appointmentId=$doctorScheduledAppointmentId"

    Test-AllowedRoute -Name "Bac si vao Doctor Schedule" -Session $doctorSession -Path "/Doctor/Schedule"
    Test-AllowedRoute -Name "Bac si vao Doctor Details" -Session $doctorSession -Path "/Doctor/Details?appointmentId=$doctorScheduledAppointmentId"
    Test-AllowedRoute -Name "Bac si vao Doctor MedicalRecord" -Session $doctorSession -Path "/Doctor/MedicalRecord?appointmentId=$doctorScheduledAppointmentId"
    Test-AllowedRoute -Name "Bac si vao Records" -Session $doctorSession -Path "/Records"
    Test-AllowedRoute -Name "Bac si xem Records cua minh" -Session $doctorSession -Path "/Records/Details?id=$doctorRecordAppointmentId"
    Test-DeniedRoute -Name "Bac si bi chan Appointments le tan" -Session $doctorSession -Path "/Reception/Appointments"
    Test-DeniedRoute -Name "Bac si bi chan CreateAppointment" -Session $doctorSession -Path "/Reception/CreateAppointment"
    Test-DeniedRoute -Name "Bac si bi chan Invoice" -Session $doctorSession -Path "/Reception/Invoice?appointmentId=$invoiceAppointmentId"
    Test-DeniedRoute -Name "Bac si bi chan Users" -Session $doctorSession -Path "/Admin/Users"
    Test-DeniedRoute -Name "Bac si bi chan Reports" -Session $doctorSession -Path "/Reports"
    Test-DeniedRoute -Name "Bac si bi chan Records cua bac si khac" -Session $doctorSession -Path "/Records/Details?id=$otherDoctorRecordAppointmentId"
)

$results | Format-Table -AutoSize

$failed = $results | Where-Object { -not $_.Passed }
if ($failed.Count -gt 0) {
    Write-Host ""
    Write-Host "Chi tiet cac test chua dat:" -ForegroundColor Red
    $failed | Format-Table -AutoSize
    exit 1
}

Write-Host ""
Write-Host "Tat ca test phan quyen deu dat." -ForegroundColor Green
