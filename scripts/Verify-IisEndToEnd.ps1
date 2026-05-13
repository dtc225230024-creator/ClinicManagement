param(
    [string]$BaseUrl = "http://clinic.local:8081",
    [string]$AdminUsername = "admin",
    [string]$AdminPassword = "123456",
    [string]$ReceptionUsername = "letan",
    [string]$ReceptionPassword = "123456",
    [string]$TemporaryNewPassword = "654321"
)

$ErrorActionPreference = "Stop"

function Decode-HtmlText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return [System.Net.WebUtility]::HtmlDecode($Value)
}

function Get-AntiForgeryToken {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Html
    )

    $match = [regex]::Match($Html, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"')
    if (-not $match.Success) {
        throw "Khong tim thay anti-forgery token trong form."
    }

    return $match.Groups[1].Value
}

function New-WebSessionObject {
    return New-Object Microsoft.PowerShell.Commands.WebRequestSession
}

function Invoke-ClinicRequest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [Microsoft.PowerShell.Commands.WebRequestSession]$Session,
        [string]$Method = "Get",
        [hashtable]$Body
    )

    $uri = if ($Path.StartsWith("http", [StringComparison]::OrdinalIgnoreCase)) { $Path } else { $BaseUrl.TrimEnd("/") + $Path }
    if ($PSBoundParameters.ContainsKey("Body")) {
        return Invoke-WebRequest -UseBasicParsing $uri -Method $Method -WebSession $Session -Body $Body
    }

    return Invoke-WebRequest -UseBasicParsing $uri -Method $Method -WebSession $Session
}

function Login-ClinicUser {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Username,
        [Parameter(Mandatory = $true)]
        [string]$Password
    )

    $session = New-WebSessionObject
    $loginPage = Invoke-ClinicRequest -Path "/Auth/Login" -Session $session
    $token = Get-AntiForgeryToken -Html $loginPage.Content
    $response = Invoke-ClinicRequest -Path "/Auth/Login" -Session $session -Method Post -Body @{
        "__RequestVerificationToken" = $token
        Username = $Username
        Password = $Password
    }

    return [pscustomobject]@{
        Session = $session
        Response = $response
    }
}

function Invoke-PostFormFromPage {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PagePath,
        [Parameter(Mandatory = $true)]
        [Microsoft.PowerShell.Commands.WebRequestSession]$Session,
        [Parameter(Mandatory = $true)]
        [hashtable]$Body
    )

    $page = Invoke-ClinicRequest -Path $PagePath -Session $Session
    $token = Get-AntiForgeryToken -Html $page.Content
    $payload = @{}
    foreach ($key in $Body.Keys) {
        $payload[$key] = $Body[$key]
    }
    $payload["__RequestVerificationToken"] = $token
    return Invoke-ClinicRequest -Path $PagePath -Session $Session -Method Post -Body $payload
}

function Ensure-ExpectedPath {
    param(
        [Parameter(Mandatory = $true)]
        $Response,
        [Parameter(Mandatory = $true)]
        [string]$ExpectedPath,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $actualPath = $Response.BaseResponse.ResponseUri.AbsolutePath
    if ($actualPath -ne $ExpectedPath) {
        throw "$Label khong dung. Expected: $ExpectedPath, actual: $actualPath"
    }

    Write-Host "[OK] $Label -> $actualPath"
}

function Get-PatientIdFromList {
    param(
        [Parameter(Mandatory = $true)]
        [Microsoft.PowerShell.Commands.WebRequestSession]$Session,
        [Parameter(Mandatory = $true)]
        [string]$Query
    )

    $page = Invoke-ClinicRequest -Path ("/Reception/Patients?q=" + [uri]::EscapeDataString($Query)) -Session $Session
    $match = [regex]::Match($page.Content, 'href="/Reception/EditPatient/(\d+)"')
    if (-not $match.Success) {
        throw "Khong tim thay benh nhan voi query: $Query"
    }

    return [int]$match.Groups[1].Value
}

function Get-SuggestionKey {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Html
    )

    $match = [regex]::Match($Html, '<input[^>]*name="SelectedSuggestionKey"[^>]*value="([^"]+)"|<input[^>]*value="([^"]+)"[^>]*name="SelectedSuggestionKey"', 'Singleline')
    if (-not $match.Success) {
        throw "Khong tim thay lich goi y hoan chinh."
    }

    return ($match.Groups[1].Value, $match.Groups[2].Value | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1)
}

function Get-DepartmentId {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Html
    )

    $checkedMatch = [regex]::Match($Html, '<input[^>]*name="DepartmentId"[^>]*value="(\d+)"[^>]*checked="checked"|<input[^>]*checked="checked"[^>]*value="(\d+)"[^>]*name="DepartmentId"', 'Singleline')
    if ($checkedMatch.Success) {
        return ($checkedMatch.Groups[1].Value, $checkedMatch.Groups[2].Value | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1)
    }

    $firstMatch = [regex]::Match($Html, '<input[^>]*name="DepartmentId"[^>]*value="(\d+)"|<input[^>]*value="(\d+)"[^>]*name="DepartmentId"', 'Singleline')
    if (-not $firstMatch.Success) {
        throw "Khong tim thay DepartmentId trong form dat lich."
    }

    return ($firstMatch.Groups[1].Value, $firstMatch.Groups[2].Value | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1)
}

function Get-AppointmentIdForPatient {
    param(
        [Parameter(Mandatory = $true)]
        [Microsoft.PowerShell.Commands.WebRequestSession]$Session,
        [Parameter(Mandatory = $true)]
        [string]$PatientName
    )

    $page = Invoke-ClinicRequest -Path ("/Reception/Appointments?q=" + [uri]::EscapeDataString($PatientName)) -Session $Session
    $match = [regex]::Match($page.Content, '<td>#(\d+)</td>\s*<td>' + [regex]::Escape($PatientName), 'Singleline')
    if (-not $match.Success) {
        throw "Khong tim thay lich kham vua tao cho benh nhan $PatientName."
    }

    return [int]$match.Groups[1].Value
}

function Get-DoctorNameForAppointment {
    param(
        [Parameter(Mandatory = $true)]
        [Microsoft.PowerShell.Commands.WebRequestSession]$Session,
        [Parameter(Mandatory = $true)]
        [int]$AppointmentId
    )

    $page = Invoke-ClinicRequest -Path ("/Reception/Appointments?q=" + $AppointmentId) -Session $Session
    $pattern = '<td>#' + [regex]::Escape($AppointmentId.ToString()) + '</td>\s*<td>.*?</td>\s*<td>(.*?)</td>'
    $match = [regex]::Match($page.Content, $pattern, 'Singleline')
    if (-not $match.Success) {
        throw "Khong tim thay bac si cua lich #$AppointmentId."
    }

    $doctorName = ($match.Groups[1].Value -replace '<.*?>', '').Trim()
    return Decode-HtmlText -Value $doctorName
}

function Get-DoctorUserForName {
    param(
        [Parameter(Mandatory = $true)]
        [Microsoft.PowerShell.Commands.WebRequestSession]$Session,
        [Parameter(Mandatory = $true)]
        [string]$DoctorName
    )

    $page = Invoke-ClinicRequest -Path "/Admin/Doctors" -Session $Session
    $decodedContent = Decode-HtmlText -Value $page.Content
    $escapedName = [regex]::Escape($DoctorName)
    $pattern = $escapedName + '</td>\s*<td>.*?</td>\s*<td>.*?</td>\s*<td>\s*<div>([^<]+)</div>'
    $match = [regex]::Match($decodedContent, $pattern, 'Singleline')
    if (-not $match.Success) {
        throw "Khong tim thay tai khoan cua bac si $DoctorName."
    }

    return $match.Groups[1].Value.Trim()
}

function Get-ServiceIdsFromInvoicePage {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Html
    )

    $matches = [regex]::Matches($Html, 'name="SelectedServiceIds"\s+value="(\d+)"')
    if ($matches.Count -lt 1) {
        throw "Khong tim thay danh sach dich vu tren hoa don."
    }

    return $matches | Select-Object -First 2 | ForEach-Object { $_.Groups[1].Value }
}

function Confirm-Contains {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Html,
        [Parameter(Mandatory = $true)]
        [string]$Needle,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    if ($Html -notmatch [regex]::Escape($Needle)) {
        throw "$Label khong tim thay noi dung mong doi: $Needle"
    }

    Write-Host "[OK] $Label"
}

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$testPatientName = "BN E2E IIS $timestamp"
$testPhone = "0977" + (Get-Date -Format "HHmmss")
$testIdentity = "9900" + (Get-Date -Format "ddHHmmss")
$desiredDate = (Get-Date).AddDays(1).ToString("yyyy-MM-dd")
$testReason = "dau hong e2e iis"

Write-Host "=== Bat dau smoke test IIS end-to-end ==="
Write-Host "BaseUrl: $BaseUrl"

$receptionLogin = Login-ClinicUser -Username $ReceptionUsername -Password $ReceptionPassword
Ensure-ExpectedPath -Response $receptionLogin.Response -ExpectedPath "/" -Label "Dang nhap le tan"
$receptionSession = $receptionLogin.Session

$patientResponse = Invoke-PostFormFromPage -PagePath "/Reception/EditPatient" -Session $receptionSession -Body @{
    PatientId = "0"
    FullName = $testPatientName
    DateOfBirth = "1998-05-10"
    Gender = "Nam"
    Phone = $testPhone
    Address = "Thai Nguyen"
    IdentityNumber = $testIdentity
}
Ensure-ExpectedPath -Response $patientResponse -ExpectedPath "/Reception/Patients" -Label "Tao benh nhan moi"

$patientId = Get-PatientIdFromList -Session $receptionSession -Query $testPhone
Write-Host "[OK] Tim thay benh nhan moi voi PatientId=$patientId"

$suggestPage = Invoke-PostFormFromPage -PagePath "/Reception/CreateAppointment" -Session $receptionSession -Body @{
    Reason = $testReason
    DepartmentId = ""
    PatientId = $patientId
    DesiredDate = $desiredDate
    SelectedSuggestionKey = ""
    PatientSearch = $testPhone
    command = "suggest"
}
Confirm-Contains -Html $suggestPage.Content -Needle "Lich goi y hoan chinh" -Label "Sinh goi y AI"

$departmentId = Get-DepartmentId -Html $suggestPage.Content
$selectedSuggestionKey = Get-SuggestionKey -Html $suggestPage.Content
Write-Host "[OK] Chon goi y AI: $selectedSuggestionKey"

$bookResponse = Invoke-PostFormFromPage -PagePath "/Reception/CreateAppointment" -Session $receptionSession -Body @{
    Reason = $testReason
    DepartmentId = $departmentId
    PatientId = $patientId
    DesiredDate = $desiredDate
    SelectedSuggestionKey = $selectedSuggestionKey
    PatientSearch = $testPhone
    command = "book"
}
Ensure-ExpectedPath -Response $bookResponse -ExpectedPath "/Reception/Appointments" -Label "Dat lich kham"

$appointmentId = Get-AppointmentIdForPatient -Session $receptionSession -PatientName $testPatientName
$doctorName = Get-DoctorNameForAppointment -Session $receptionSession -AppointmentId $appointmentId
Write-Host "[OK] Tao duoc lich #$appointmentId voi $doctorName"

$adminLogin = Login-ClinicUser -Username $AdminUsername -Password $AdminPassword
Ensure-ExpectedPath -Response $adminLogin.Response -ExpectedPath "/" -Label "Dang nhap admin"
$doctorUsername = Get-DoctorUserForName -Session $adminLogin.Session -DoctorName $doctorName
Write-Host "[OK] Tim duoc tai khoan bac si: $doctorUsername"

$doctorPassword = "123456"
$doctorLogin = Login-ClinicUser -Username $doctorUsername -Password $doctorPassword
if ($doctorLogin.Response.BaseResponse.ResponseUri.AbsolutePath -eq "/Auth/ChangePassword") {
    $changeResponse = Invoke-PostFormFromPage -PagePath "/Auth/ChangePassword?required=1" -Session $doctorLogin.Session -Body @{
        CurrentPassword = $doctorPassword
        NewPassword = $TemporaryNewPassword
        ConfirmPassword = $TemporaryNewPassword
    }
    Ensure-ExpectedPath -Response $changeResponse -ExpectedPath "/" -Label "Bac si doi mat khau tam thoi"
    $doctorPassword = $TemporaryNewPassword
    $doctorLogin = Login-ClinicUser -Username $doctorUsername -Password $doctorPassword
}
Ensure-ExpectedPath -Response $doctorLogin.Response -ExpectedPath "/" -Label "Dang nhap bac si"

$medicalRecordResponse = Invoke-PostFormFromPage -PagePath ("/Doctor/MedicalRecord?appointmentId=" + $appointmentId) -Session $doctorLogin.Session -Body @{
    "Record.RecordId" = "0"
    "Record.AppointmentId" = $appointmentId
    "Record.Symptoms" = "Ho, dau hong, met moi"
    "Record.Diagnosis" = "Viem hong cap"
    "Record.ExaminationResult" = "Amidan sung do, khong co dau hieu bien chung"
    "Record.Note" = "Hen tai kham neu sot cao hoac kho tho"
}
Ensure-ExpectedPath -Response $medicalRecordResponse -ExpectedPath "/Doctor/Schedule" -Label "Bac si luu ket qua kham"

$invoicePage = Invoke-ClinicRequest -Path ("/Reception/Invoice?appointmentId=" + $appointmentId) -Session $receptionSession
Ensure-ExpectedPath -Response $invoicePage -ExpectedPath "/Reception/Invoice" -Label "Le tan mo hoa don"
$serviceId = (Get-ServiceIdsFromInvoicePage -Html $invoicePage.Content | Select-Object -First 1)

$invoiceResponse = Invoke-PostFormFromPage -PagePath ("/Reception/Invoice?appointmentId=" + $appointmentId) -Session $receptionSession -Body @{
    AppointmentId = $appointmentId
    SelectedServiceIds = $serviceId
}
Ensure-ExpectedPath -Response $invoiceResponse -ExpectedPath "/Reception/Invoice" -Label "Le tan xac nhan thanh toan"
Confirm-Contains -Html $invoiceResponse.Content -Needle "Ma hoa don: HD-" -Label "Hoa don da duoc tao"

$printResponse = Invoke-ClinicRequest -Path ("/Reception/PrintInvoice?appointmentId=" + $appointmentId) -Session $receptionSession
Ensure-ExpectedPath -Response $printResponse -ExpectedPath "/Reception/PrintInvoice" -Label "Mo ban in hoa don"
Confirm-Contains -Html $printResponse.Content -Needle $testPatientName -Label "Ban in hoa don dung benh nhan"

$reportsResponse = Invoke-ClinicRequest -Path ("/Reports?fromDate=$desiredDate&toDate=$desiredDate&departmentId=$departmentId") -Session $adminLogin.Session
Ensure-ExpectedPath -Response $reportsResponse -ExpectedPath "/Reports" -Label "Admin mo bao cao"
Confirm-Contains -Html $reportsResponse.Content -Needle ('value="' + $desiredDate + '"') -Label "Bao cao ap dung dung khoang ngay loc"
Confirm-Contains -Html $reportsResponse.Content -Needle "Xu huong lich kham" -Label "Bao cao render bieu do lich kham"
Confirm-Contains -Html $reportsResponse.Content -Needle "Tong lich kham" -Label "Bao cao render thong ke tong hop"

if ($doctorPassword -ne "123456") {
    $adminUsersPage = Invoke-ClinicRequest -Path "/Admin/Users" -Session $adminLogin.Session
    $usersToken = Get-AntiForgeryToken -Html $adminUsersPage.Content
    $doctorRow = [regex]::Match($adminUsersPage.Content, '<tr>\s*<td>(\d+)</td>\s*<td>' + [regex]::Escape($doctorUsername) + '</td>', 'Singleline')
    if ($doctorRow.Success) {
        $resetResponse = Invoke-ClinicRequest -Path "/Admin/ResetPassword" -Session $adminLogin.Session -Method Post -Body @{
            "__RequestVerificationToken" = $usersToken
            id = $doctorRow.Groups[1].Value
        }
        Ensure-ExpectedPath -Response $resetResponse -ExpectedPath "/Admin/Users" -Label "Reset lai mat khau bac si sau test"
    }
}

Write-Host ""
Write-Host "Smoke test IIS end-to-end da dat."
Write-Host "Patient:     $testPatientName"
Write-Host "Appointment: #$appointmentId"
Write-Host "Doctor user: $doctorUsername"
