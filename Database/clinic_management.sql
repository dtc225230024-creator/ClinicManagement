CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    ALTER DATABASE CHARACTER SET utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    CREATE TABLE `Departments` (
        `DepartmentId` int NOT NULL AUTO_INCREMENT,
        `DepartmentName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Description` longtext CHARACTER SET utf8mb4 NULL,
        `IsActive` tinyint(1) NOT NULL,
        CONSTRAINT `PK_Departments` PRIMARY KEY (`DepartmentId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    CREATE TABLE `Patients` (
        `PatientId` int NOT NULL AUTO_INCREMENT,
        `FullName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `DateOfBirth` datetime(6) NULL,
        `Gender` varchar(10) CHARACTER SET utf8mb4 NULL,
        `Phone` varchar(15) CHARACTER SET utf8mb4 NOT NULL,
        `Address` varchar(255) CHARACTER SET utf8mb4 NULL,
        `IdentityNumber` varchar(20) CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_Patients` PRIMARY KEY (`PatientId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    CREATE TABLE `Services` (
        `ServiceId` int NOT NULL AUTO_INCREMENT,
        `ServiceName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Price` decimal(12,2) NOT NULL,
        `Description` longtext CHARACTER SET utf8mb4 NULL,
        `IsActive` tinyint(1) NOT NULL,
        CONSTRAINT `PK_Services` PRIMARY KEY (`ServiceId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    CREATE TABLE `Users` (
        `UserId` int NOT NULL AUTO_INCREMENT,
        `Username` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `Password` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Role` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `DoctorId` int NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_Users` PRIMARY KEY (`UserId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    CREATE TABLE `Doctors` (
        `DoctorId` int NOT NULL AUTO_INCREMENT,
        `FullName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Gender` varchar(10) CHARACTER SET utf8mb4 NULL,
        `Phone` varchar(15) CHARACTER SET utf8mb4 NULL,
        `Email` varchar(100) CHARACTER SET utf8mb4 NULL,
        `DepartmentId` int NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        CONSTRAINT `PK_Doctors` PRIMARY KEY (`DoctorId`),
        CONSTRAINT `FK_Doctors_Departments_DepartmentId` FOREIGN KEY (`DepartmentId`) REFERENCES `Departments` (`DepartmentId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    CREATE TABLE `Appointments` (
        `AppointmentId` int NOT NULL AUTO_INCREMENT,
        `PatientId` int NOT NULL,
        `DoctorId` int NOT NULL,
        `DepartmentId` int NOT NULL,
        `AppointmentDate` datetime(6) NOT NULL,
        `TimeSlot` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `Reason` longtext CHARACTER SET utf8mb4 NULL,
        `Status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_Appointments` PRIMARY KEY (`AppointmentId`),
        CONSTRAINT `FK_Appointments_Departments_DepartmentId` FOREIGN KEY (`DepartmentId`) REFERENCES `Departments` (`DepartmentId`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Appointments_Doctors_DoctorId` FOREIGN KEY (`DoctorId`) REFERENCES `Doctors` (`DoctorId`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Appointments_Patients_PatientId` FOREIGN KEY (`PatientId`) REFERENCES `Patients` (`PatientId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    CREATE TABLE `WorkSchedules` (
        `ScheduleId` int NOT NULL AUTO_INCREMENT,
        `DoctorId` int NOT NULL,
        `WorkDate` datetime(6) NOT NULL,
        `StartTime` time(6) NOT NULL,
        `EndTime` time(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        CONSTRAINT `PK_WorkSchedules` PRIMARY KEY (`ScheduleId`),
        CONSTRAINT `FK_WorkSchedules_Doctors_DoctorId` FOREIGN KEY (`DoctorId`) REFERENCES `Doctors` (`DoctorId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    CREATE TABLE `Invoices` (
        `InvoiceId` int NOT NULL AUTO_INCREMENT,
        `AppointmentId` int NOT NULL,
        `TotalAmount` decimal(12,2) NOT NULL,
        `PaymentStatus` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_Invoices` PRIMARY KEY (`InvoiceId`),
        CONSTRAINT `FK_Invoices_Appointments_AppointmentId` FOREIGN KEY (`AppointmentId`) REFERENCES `Appointments` (`AppointmentId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    CREATE TABLE `MedicalRecords` (
        `RecordId` int NOT NULL AUTO_INCREMENT,
        `AppointmentId` int NOT NULL,
        `Symptoms` longtext CHARACTER SET utf8mb4 NULL,
        `Diagnosis` longtext CHARACTER SET utf8mb4 NULL,
        `ExaminationResult` longtext CHARACTER SET utf8mb4 NULL,
        `Note` longtext CHARACTER SET utf8mb4 NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_MedicalRecords` PRIMARY KEY (`RecordId`),
        CONSTRAINT `FK_MedicalRecords_Appointments_AppointmentId` FOREIGN KEY (`AppointmentId`) REFERENCES `Appointments` (`AppointmentId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    CREATE INDEX `IX_Appointments_DepartmentId` ON `Appointments` (`DepartmentId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    CREATE INDEX `IX_Appointments_DoctorId_AppointmentDate_TimeSlot` ON `Appointments` (`DoctorId`, `AppointmentDate`, `TimeSlot`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    CREATE INDEX `IX_Appointments_PatientId` ON `Appointments` (`PatientId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    CREATE INDEX `IX_Doctors_DepartmentId` ON `Doctors` (`DepartmentId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Invoices_AppointmentId` ON `Invoices` (`AppointmentId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_MedicalRecords_AppointmentId` ON `MedicalRecords` (`AppointmentId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Patients_Phone` ON `Patients` (`Phone`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Users_Username` ON `Users` (`Username`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    CREATE INDEX `IX_WorkSchedules_DoctorId` ON `WorkSchedules` (`DoctorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420184901_InitialCreate') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260420184901_InitialCreate', '8.0.13');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420190822_AddInvoiceDetails') THEN

    CREATE TABLE `InvoiceDetails` (
        `InvoiceDetailId` int NOT NULL AUTO_INCREMENT,
        `InvoiceId` int NOT NULL,
        `ServiceId` int NOT NULL,
        `ServiceName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `UnitPrice` decimal(12,2) NOT NULL,
        `Quantity` int NOT NULL,
        `LineTotal` decimal(12,2) NOT NULL,
        CONSTRAINT `PK_InvoiceDetails` PRIMARY KEY (`InvoiceDetailId`),
        CONSTRAINT `FK_InvoiceDetails_Invoices_InvoiceId` FOREIGN KEY (`InvoiceId`) REFERENCES `Invoices` (`InvoiceId`) ON DELETE CASCADE,
        CONSTRAINT `FK_InvoiceDetails_Services_ServiceId` FOREIGN KEY (`ServiceId`) REFERENCES `Services` (`ServiceId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420190822_AddInvoiceDetails') THEN

    CREATE INDEX `IX_InvoiceDetails_InvoiceId` ON `InvoiceDetails` (`InvoiceId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420190822_AddInvoiceDetails') THEN

    CREATE INDEX `IX_InvoiceDetails_ServiceId` ON `InvoiceDetails` (`ServiceId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260420190822_AddInvoiceDetails') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260420190822_AddInvoiceDetails', '8.0.13');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

