# Cau hinh MySQL cho ClinicManagement

Ung dung da duoc chuyen sang EF Core + MySQL. Khi chay app, he thong se tu tao database schema va seed du lieu demo neu database dang trong.

## 1. Tao database va user

Dang nhap MySQL bang tai khoan quan tri cua may ban, sau do chay:

```sql
CREATE DATABASE IF NOT EXISTS clinic_management
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

CREATE USER IF NOT EXISTS 'clinic_user'@'localhost' IDENTIFIED BY 'clinic_password';
GRANT ALL PRIVILEGES ON clinic_management.* TO 'clinic_user'@'localhost';
FLUSH PRIVILEGES;
```

Ban co the doi `clinic_user` va `clinic_password` theo y muon.

## 2. Sua connection string

Mo file:

```text
ClinicManagement/appsettings.Development.json
```

Doi connection string thanh:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=clinic_management;user=clinic_user;password=clinic_password;TreatTinyAsBoolean=true;"
  }
}
```

Neu muon dung tai khoan `root`, chi can thay `user` va `password` bang thong tin MySQL tren may ban.

## 3. Chay ung dung

```bash
dotnet run --project ClinicManagement/ClinicManagement.csproj --launch-profile http
```

Mo trinh duyet:

```text
http://localhost:5080/Auth/Login
```

Tai khoan seed mac dinh:

- admin / 123456
- letan / 123456
- bacsi / 123456

## 4. Ghi chu

- Neu database da co du lieu trong bang `Users`, he thong se khong seed lai.
- Neu doi model sau nay, nen chuyen tu `EnsureCreated` sang EF Core migrations de quan ly thay doi schema chuyen nghiep hon.
