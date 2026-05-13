# Deploy ClinicManagement len Railway

Cap nhat: 2026-05-13

## 1. Ket luan kha thi

Project da duoc chuan bi de deploy len Railway:

- Web app ASP.NET Core MVC chay bang `Dockerfile`.
- App doc `PORT` tu Railway va bind vao `0.0.0.0`.
- App doc MySQL qua cac bien `MYSQLHOST`, `MYSQLPORT`, `MYSQLUSER`, `MYSQLPASSWORD`, `MYSQLDATABASE` hoac `MYSQL_URL`.
- Seed demo va buoc chuan hoa du lieu demo se tao/cap nhat du lieu tieng Viet co dau.

## 2. Cac file da them/sua

- `Dockerfile`: build/publish ASP.NET Core 8 bang multi-stage Docker.
- `.dockerignore`: loai bo thu muc tam, artifact, yeu-cau va file khong can deploy.
- `.env.railway.example`: mau bien moi truong Railway.
- `ClinicManagement/Program.cs`: doc `PORT`, doc MySQL env Railway, bat forwarded headers.
- `ClinicManagement/Data/ClinicDbInitializer.cs`: chuan hoa seed demo tieng Viet co dau va cap nhat du lieu demo cu.

## 3. Cac buoc thao tac online tren Railway

1. Dua project len GitHub.
2. Tao project moi tren Railway.
3. Add service MySQL trong cung Railway project.
4. Add web service tu GitHub repo cua project.
5. Railway se tu nhan `Dockerfile` va build app.
6. Trong web service, them/cau hinh bien moi truong:
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `MYSQLHOST`
   - `MYSQLPORT`
   - `MYSQLUSER`
   - `MYSQLPASSWORD`
   - `MYSQLDATABASE`
   - hoac dung `MYSQL_URL`
7. Neu Railway co chuc nang reference variable, dung gia tri tu MySQL service, vi du:
   - `MYSQLHOST=${{MySQL.MYSQLHOST}}`
   - `MYSQLPORT=${{MySQL.MYSQLPORT}}`
   - `MYSQLUSER=${{MySQL.MYSQLUSER}}`
   - `MYSQLPASSWORD=${{MySQL.MYSQLPASSWORD}}`
   - `MYSQLDATABASE=${{MySQL.MYSQLDATABASE}}`
8. Vao tab Networking cua web service va Generate Domain.
9. Mo public URL de app khoi dong. Lan khoi dong dau se migrate DB va tao du lieu demo.

## 4. Luu y bao mat truoc khi public

- Khong nen demo public voi mat khau `123456`.
- Nen dang nhap Admin va reset/chot lai bo tai khoan demo truoc khi gui link.
- Neu repo GitHub public, can can nhac xoa thong tin connection string local trong `appsettings.json` hoac chuyen repo sang private.

## 5. Kiem tra sau deploy

Kiem tra nhanh cac URL:

- `/Auth/Login`
- `/`
- `/Admin/Users`
- `/Reception/CreateAppointment`
- `/Reception/Appointments`
- `/Doctor/Schedule`
- `/Reports`
- `/Records`

Kiem tra du lieu:

- Ten benh nhan/bac si demo hien thi co dau.
- Ly do `Bao cao demo` cu duoc chuan hoa thanh `Báo cáo demo`.
- AI rules va cac chuyen khoa co dau.

## 6. Neu Railway bao loi

- Loi app khong respond: kiem tra app co doc dung `PORT` khong.
- Loi DB connection: kiem tra bien MySQL co gan vao web service chua.
- Loi migration: xem logs cua web service, thu Restart service.
- Loi tieng Viet: kiem tra MySQL service dung charset `utf8mb4`; app da set `CharacterSet=utf8mb4` khi tao connection string tu Railway env.
