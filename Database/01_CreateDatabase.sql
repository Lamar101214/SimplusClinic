:ON ERROR EXIT
-- Run once on a fresh instance using SSMS or sqlcmd. Never drops an existing database.
USE master;
GO
IF DB_ID(N'SimplusClinic') IS NOT NULL THROW 50000, 'SimplusClinic already exists. Use versioned migrations, not this initial script.', 1;
GO
CREATE DATABASE SimplusClinic;
GO
USE SimplusClinic;
GO
CREATE TABLE dbo.AuditLog (
 AuditId bigint IDENTITY PRIMARY KEY, EntityName sysname NOT NULL, EntityId int NOT NULL,
 Operation nvarchar(10) NOT NULL, Actor nvarchar(128) NOT NULL DEFAULT ORIGINAL_LOGIN(),
 OccurredAtUtc datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME());
GO
CREATE TABLE dbo.Patients (
 Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Patients PRIMARY KEY,
 FirstName nvarchar(80) NOT NULL,
 MiddleName nvarchar(80) NOT NULL DEFAULT N'',
 LastName nvarchar(80) NOT NULL,
 HouseNo nvarchar(30) NOT NULL DEFAULT N'',
 Street nvarchar(120) NOT NULL DEFAULT N'',
 Municipality nvarchar(100) NOT NULL DEFAULT N'',
 CityProvince nvarchar(100) NOT NULL,
 ZipCode nvarchar(15) NOT NULL DEFAULT N'',
 DateOfBirth date NOT NULL,
 Gender nvarchar(30) NOT NULL,
 MobileNumber nvarchar(30) NOT NULL DEFAULT N'',
 LandlineNumber nvarchar(30) NOT NULL DEFAULT N'',
 EmailAddress nvarchar(254) NOT NULL DEFAULT N'',
 GuardianName nvarchar(180) NOT NULL DEFAULT N'',
 GuardianAddress nvarchar(400) NOT NULL DEFAULT N'',
 GuardianMobileNumber nvarchar(30) NOT NULL DEFAULT N'',
 GuardianLandlineNumber nvarchar(30) NOT NULL DEFAULT N'',
 IsArchived bit NOT NULL DEFAULT 0,
 Version rowversion NOT NULL,
 CONSTRAINT CK_Patients_FirstName CHECK (LEN(LTRIM(RTRIM(FirstName))) > 0),
 CONSTRAINT CK_Patients_LastName CHECK (LEN(LTRIM(RTRIM(LastName))) > 0),
 CONSTRAINT CK_Patients_CityProvince CHECK (LEN(LTRIM(RTRIM(CityProvince))) > 0),
 CONSTRAINT CK_Patients_Gender CHECK (LEN(LTRIM(RTRIM(Gender))) > 0),
 CONSTRAINT CK_Patients_Birth CHECK (DateOfBirth >= '19000101')
);
GO
CREATE OR ALTER TRIGGER dbo.tr_Patients_Audit ON dbo.Patients AFTER INSERT, UPDATE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditLog(EntityName,EntityId,Operation)
 SELECT N'Patients',i.Id,CASE WHEN d.Id IS NULL THEN N'INSERT' ELSE N'UPDATE' END
 FROM inserted i LEFT JOIN deleted d ON d.Id=i.Id;
END;
GO
CREATE TABLE dbo.Doctors (
 Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Doctors PRIMARY KEY,
 FirstName nvarchar(80) NOT NULL,
 MiddleName nvarchar(80) NOT NULL DEFAULT N'',
 LastName nvarchar(80) NOT NULL,
 HouseNo nvarchar(30) NOT NULL DEFAULT N'',
 Street nvarchar(120) NOT NULL DEFAULT N'',
 Municipality nvarchar(100) NOT NULL DEFAULT N'',
 CityProvince nvarchar(100) NOT NULL,
 ZipCode nvarchar(15) NOT NULL DEFAULT N'',
 DateOfBirth date NOT NULL,
 Gender nvarchar(30) NOT NULL,
 MobileNumber nvarchar(30) NOT NULL DEFAULT N'',
 LandlineNumber nvarchar(30) NOT NULL DEFAULT N'',
 EmailAddress nvarchar(254) NOT NULL DEFAULT N'',
 Specialization nvarchar(120) NOT NULL,
 LicenseNumber nvarchar(60) NOT NULL,
 IsArchived bit NOT NULL DEFAULT 0,
 Version rowversion NOT NULL,
 CONSTRAINT CK_Doctors_FirstName CHECK (LEN(LTRIM(RTRIM(FirstName))) > 0),
 CONSTRAINT CK_Doctors_LastName CHECK (LEN(LTRIM(RTRIM(LastName))) > 0),
 CONSTRAINT CK_Doctors_CityProvince CHECK (LEN(LTRIM(RTRIM(CityProvince))) > 0),
 CONSTRAINT CK_Doctors_Gender CHECK (LEN(LTRIM(RTRIM(Gender))) > 0),
 CONSTRAINT CK_Doctors_Specialization CHECK (LEN(LTRIM(RTRIM(Specialization))) > 0),
 CONSTRAINT CK_Doctors_LicenseNumber CHECK (LEN(LTRIM(RTRIM(LicenseNumber))) > 0),
 CONSTRAINT CK_Doctors_Birth CHECK (DateOfBirth >= '19000101'),
 CONSTRAINT UQ_Doctors_LicenseNumber UNIQUE (LicenseNumber)
);
GO
CREATE OR ALTER TRIGGER dbo.tr_Doctors_Audit ON dbo.Doctors AFTER INSERT, UPDATE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditLog(EntityName,EntityId,Operation)
 SELECT N'Doctors',i.Id,CASE WHEN d.Id IS NULL THEN N'INSERT' ELSE N'UPDATE' END
 FROM inserted i LEFT JOIN deleted d ON d.Id=i.Id;
END;
GO
CREATE TABLE dbo.MedicalRecords (
 Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MedicalRecords PRIMARY KEY,
 PatientId int NOT NULL,
 DoctorId int NOT NULL,
 Diagnosis nvarchar(4000) NOT NULL,
 Treatment nvarchar(4000) NOT NULL,
 Prescriptions nvarchar(4000) NOT NULL DEFAULT N'',
 TreatmentDate date NOT NULL,
 IsArchived bit NOT NULL DEFAULT 0,
 Version rowversion NOT NULL,
 ValidFrom datetime2 GENERATED ALWAYS AS ROW START NOT NULL,
 ValidTo datetime2 GENERATED ALWAYS AS ROW END NOT NULL,
 PERIOD FOR SYSTEM_TIME (ValidFrom,ValidTo),
 CONSTRAINT CK_MedicalRecords_Diagnosis CHECK (LEN(LTRIM(RTRIM(Diagnosis))) > 0),
 CONSTRAINT CK_MedicalRecords_Treatment CHECK (LEN(LTRIM(RTRIM(Treatment))) > 0),
 CONSTRAINT FK_MedicalRecords_Patients FOREIGN KEY (PatientId) REFERENCES dbo.Patients(Id),
 CONSTRAINT FK_MedicalRecords_Doctors FOREIGN KEY (DoctorId) REFERENCES dbo.Doctors(Id)
) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.MedicalRecordHistory));
GO
CREATE OR ALTER TRIGGER dbo.tr_MedicalRecords_Audit ON dbo.MedicalRecords AFTER INSERT, UPDATE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditLog(EntityName,EntityId,Operation)
 SELECT N'MedicalRecords',i.Id,CASE WHEN d.Id IS NULL THEN N'INSERT' ELSE N'UPDATE' END
 FROM inserted i LEFT JOIN deleted d ON d.Id=i.Id;
END;
GO
CREATE TABLE dbo.Appointments (
 Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Appointments PRIMARY KEY,
 PatientId int NOT NULL,
 DoctorId int NOT NULL,
 StartAt datetime2(0) NOT NULL,
 EndAt datetime2(0) NOT NULL,
 Status nvarchar(20) NOT NULL,
 Version rowversion NOT NULL,
 CONSTRAINT CK_Appointments_StatusNotBlank CHECK (LEN(LTRIM(RTRIM(Status))) > 0),
 CONSTRAINT FK_Appointments_Patients FOREIGN KEY (PatientId) REFERENCES dbo.Patients(Id),
 CONSTRAINT FK_Appointments_Doctors FOREIGN KEY (DoctorId) REFERENCES dbo.Doctors(Id),
 CONSTRAINT CK_Appointments_Status CHECK (Status IN ('Scheduled','Completed','Cancelled')),
 CONSTRAINT CK_Appointments_Interval CHECK (EndAt > StartAt AND DATEDIFF_BIG(second,StartAt,EndAt) <= 86400)
);
GO
CREATE OR ALTER TRIGGER dbo.tr_Appointments_Audit ON dbo.Appointments AFTER INSERT, UPDATE AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.AuditLog(EntityName,EntityId,Operation)
 SELECT N'Appointments',i.Id,CASE WHEN d.Id IS NULL THEN N'INSERT' ELSE N'UPDATE' END
 FROM inserted i LEFT JOIN deleted d ON d.Id=i.Id;
END;
GO
CREATE INDEX IX_Patients_Name ON dbo.Patients(LastName,FirstName,MiddleName) INCLUDE (IsArchived,MobileNumber);
CREATE INDEX IX_Doctors_Name ON dbo.Doctors(LastName,FirstName,MiddleName) INCLUDE (IsArchived);
CREATE INDEX IX_Doctors_Specialization ON dbo.Doctors(Specialization,LastName) INCLUDE (FirstName,IsArchived);
-- The UNIQUE license constraint creates the license search index.
CREATE INDEX IX_MedicalRecords_Patient ON dbo.MedicalRecords(PatientId,TreatmentDate) INCLUDE(DoctorId,IsArchived);
CREATE INDEX IX_MedicalRecords_Doctor ON dbo.MedicalRecords(DoctorId,PatientId) INCLUDE(TreatmentDate,IsArchived);
CREATE INDEX IX_Appointments_Doctor_Interval ON dbo.Appointments(DoctorId,StartAt) INCLUDE(EndAt,Status,PatientId);
CREATE INDEX IX_Appointments_Patient_Interval ON dbo.Appointments(PatientId,StartAt) INCLUDE(EndAt,Status,DoctorId);
CREATE INDEX IX_Appointments_Date ON dbo.Appointments(StartAt) INCLUDE(EndAt,Status,PatientId,DoctorId);
GO
