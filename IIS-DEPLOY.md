# IIS Deploy

1. Mo PowerShell bang quyen Administrator.
2. Bat prerequisites IIS:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
D:\OneDrive\Dev\DoAn\ClinicManagement\scripts\Enable-IISPrerequisites.ps1
```

3. Neu script bao thieu `AspNetCoreModuleV2`, cai ASP.NET Core Hosting Bundle phu hop voi .NET 8.
4. Deploy site IIS:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
D:\OneDrive\Dev\DoAn\ClinicManagement\scripts\Deploy-ClinicManagementIIS.ps1
```

Mac dinh script se:
- publish ban `Release` vao `D:\OneDrive\Dev\DoAn\ClinicManagement\artifacts\publish\ClinicManagement-IIS`
- tao app pool `ClinicManagementPool`
- tao site `ClinicManagement`
- bind `http://clinic.local:8081`

Neu dung host name `clinic.local`, them dong sau vao file `C:\Windows\System32\drivers\etc\hosts`:

```text
127.0.0.1 clinic.local
```
