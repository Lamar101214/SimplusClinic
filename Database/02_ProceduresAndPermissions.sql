USE SimplusClinic;
GO
CREATE OR ALTER PROCEDURE dbo.SavePatient
 @Id int,
 @Version binary(8),
 @FirstName nvarchar(80),
 @MiddleName nvarchar(80),
 @LastName nvarchar(80),
 @HouseNo nvarchar(30),
 @Street nvarchar(120),
 @Municipality nvarchar(100),
 @CityProvince nvarchar(100),
 @ZipCode nvarchar(15),
 @DateOfBirth date,
 @Gender nvarchar(30),
 @MobileNumber nvarchar(30),
 @LandlineNumber nvarchar(30),
 @EmailAddress nvarchar(254),
 @GuardianName nvarchar(180),
 @GuardianAddress nvarchar(400),
 @GuardianMobileNumber nvarchar(30),
 @GuardianLandlineNumber nvarchar(30),
 @IsArchived bit
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @LockResult int;
 EXEC @LockResult = sys.sp_getapplock @Resource=N'ClinicScheduling', @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
 IF @LockResult < 0 THROW 51004, 'The clinic is busy. Please retry.', 1;
 IF @Id <> 0 AND NOT EXISTS(SELECT 1 FROM dbo.Patients WHERE Id=@Id AND Version=@Version) THROW 51002, 'This item was changed by another user. Refresh and try again.', 1;
 IF @DateOfBirth < '19000101' OR @DateOfBirth > CONVERT(date,GETDATE()) THROW 51006, 'Invalid date of birth.', 1;
 IF @IsArchived=1 AND EXISTS(SELECT 1 FROM dbo.Appointments WHERE PatientId=@Id AND Status=N'Scheduled') THROW 51005, 'Complete or cancel all scheduled appointments before archiving.', 1;
 IF @Id=0
 BEGIN
 INSERT dbo.Patients (FirstName,MiddleName,LastName,HouseNo,Street,Municipality,CityProvince,ZipCode,DateOfBirth,Gender,MobileNumber,LandlineNumber,EmailAddress,GuardianName,GuardianAddress,GuardianMobileNumber,GuardianLandlineNumber,IsArchived)
 VALUES (@FirstName,@MiddleName,@LastName,@HouseNo,@Street,@Municipality,@CityProvince,@ZipCode,@DateOfBirth,@Gender,@MobileNumber,@LandlineNumber,@EmailAddress,@GuardianName,@GuardianAddress,@GuardianMobileNumber,@GuardianLandlineNumber,@IsArchived);
 SET @Id=CONVERT(int,SCOPE_IDENTITY());
 END
 ELSE
 BEGIN
 UPDATE dbo.Patients SET FirstName=@FirstName,MiddleName=@MiddleName,LastName=@LastName,HouseNo=@HouseNo,Street=@Street,Municipality=@Municipality,CityProvince=@CityProvince,ZipCode=@ZipCode,DateOfBirth=@DateOfBirth,Gender=@Gender,MobileNumber=@MobileNumber,LandlineNumber=@LandlineNumber,EmailAddress=@EmailAddress,GuardianName=@GuardianName,GuardianAddress=@GuardianAddress,GuardianMobileNumber=@GuardianMobileNumber,GuardianLandlineNumber=@GuardianLandlineNumber,IsArchived=@IsArchived WHERE Id=@Id AND Version=@Version;
 END
 COMMIT;
 SELECT @Id;
 END TRY
 BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
 END CATCH
END;
GO
CREATE OR ALTER PROCEDURE dbo.SaveDoctor
 @Id int,
 @Version binary(8),
 @FirstName nvarchar(80),
 @MiddleName nvarchar(80),
 @LastName nvarchar(80),
 @HouseNo nvarchar(30),
 @Street nvarchar(120),
 @Municipality nvarchar(100),
 @CityProvince nvarchar(100),
 @ZipCode nvarchar(15),
 @DateOfBirth date,
 @Gender nvarchar(30),
 @MobileNumber nvarchar(30),
 @LandlineNumber nvarchar(30),
 @EmailAddress nvarchar(254),
 @Specialization nvarchar(120),
 @LicenseNumber nvarchar(60),
 @IsArchived bit
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @LockResult int;
 EXEC @LockResult = sys.sp_getapplock @Resource=N'ClinicScheduling', @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
 IF @LockResult < 0 THROW 51004, 'The clinic is busy. Please retry.', 1;
 IF @Id <> 0 AND NOT EXISTS(SELECT 1 FROM dbo.Doctors WHERE Id=@Id AND Version=@Version) THROW 51002, 'This item was changed by another user. Refresh and try again.', 1;
 IF @DateOfBirth < '19000101' OR @DateOfBirth > CONVERT(date,GETDATE()) THROW 51006, 'Invalid date of birth.', 1;
 IF @IsArchived=1 AND EXISTS(SELECT 1 FROM dbo.Appointments WHERE DoctorId=@Id AND Status=N'Scheduled') THROW 51005, 'Complete or cancel all scheduled appointments before archiving.', 1;
 IF @Id=0
 BEGIN
 INSERT dbo.Doctors (FirstName,MiddleName,LastName,HouseNo,Street,Municipality,CityProvince,ZipCode,DateOfBirth,Gender,MobileNumber,LandlineNumber,EmailAddress,Specialization,LicenseNumber,IsArchived)
 VALUES (@FirstName,@MiddleName,@LastName,@HouseNo,@Street,@Municipality,@CityProvince,@ZipCode,@DateOfBirth,@Gender,@MobileNumber,@LandlineNumber,@EmailAddress,@Specialization,@LicenseNumber,@IsArchived);
 SET @Id=CONVERT(int,SCOPE_IDENTITY());
 END
 ELSE
 BEGIN
 UPDATE dbo.Doctors SET FirstName=@FirstName,MiddleName=@MiddleName,LastName=@LastName,HouseNo=@HouseNo,Street=@Street,Municipality=@Municipality,CityProvince=@CityProvince,ZipCode=@ZipCode,DateOfBirth=@DateOfBirth,Gender=@Gender,MobileNumber=@MobileNumber,LandlineNumber=@LandlineNumber,EmailAddress=@EmailAddress,Specialization=@Specialization,LicenseNumber=@LicenseNumber,IsArchived=@IsArchived WHERE Id=@Id AND Version=@Version;
 END
 COMMIT;
 SELECT @Id;
 END TRY
 BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
 END CATCH
END;
GO
CREATE OR ALTER PROCEDURE dbo.SaveMedicalRecord
 @Id int,
 @Version binary(8),
 @PatientId int,
 @DoctorId int,
 @Diagnosis nvarchar(4000),
 @Treatment nvarchar(4000),
 @Prescriptions nvarchar(4000),
 @TreatmentDate date,
 @IsArchived bit
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @LockResult int;
 EXEC @LockResult = sys.sp_getapplock @Resource=N'ClinicScheduling', @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
 IF @LockResult < 0 THROW 51004, 'The clinic is busy. Please retry.', 1;
 IF @Id <> 0 AND NOT EXISTS(SELECT 1 FROM dbo.MedicalRecords WHERE Id=@Id AND Version=@Version) THROW 51002, 'This item was changed by another user. Refresh and try again.', 1;
 IF (@Id=0 OR @PatientId <> (SELECT PatientId FROM dbo.MedicalRecords WHERE Id=@Id)) AND NOT EXISTS(SELECT 1 FROM dbo.Patients WHERE Id=@PatientId AND IsArchived=0) THROW 51003, 'Select an active patient.', 1;
 IF (@Id=0 OR @DoctorId <> (SELECT DoctorId FROM dbo.MedicalRecords WHERE Id=@Id)) AND NOT EXISTS(SELECT 1 FROM dbo.Doctors WHERE Id=@DoctorId AND IsArchived=0) THROW 51003, 'Select an active doctor.', 1;
 IF @TreatmentDate < '19000101' OR @TreatmentDate > CONVERT(date,GETDATE()) THROW 51006, 'Invalid treatment date.', 1;
 IF @Id=0
 BEGIN
 INSERT dbo.MedicalRecords (PatientId,DoctorId,Diagnosis,Treatment,Prescriptions,TreatmentDate,IsArchived)
 VALUES (@PatientId,@DoctorId,@Diagnosis,@Treatment,@Prescriptions,@TreatmentDate,@IsArchived);
 SET @Id=CONVERT(int,SCOPE_IDENTITY());
 END
 ELSE
 BEGIN
 UPDATE dbo.MedicalRecords SET PatientId=@PatientId,DoctorId=@DoctorId,Diagnosis=@Diagnosis,Treatment=@Treatment,Prescriptions=@Prescriptions,TreatmentDate=@TreatmentDate,IsArchived=@IsArchived WHERE Id=@Id AND Version=@Version;
 END
 COMMIT;
 SELECT @Id;
 END TRY
 BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
 END CATCH
END;
GO
CREATE OR ALTER PROCEDURE dbo.SaveAppointment
 @Id int,
 @Version binary(8),
 @PatientId int,
 @DoctorId int,
 @StartAt datetime2(0),
 @EndAt datetime2(0),
 @Status nvarchar(20)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @LockResult int;
 EXEC @LockResult = sys.sp_getapplock @Resource=N'ClinicScheduling', @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
 IF @LockResult < 0 THROW 51004, 'The clinic is busy. Please retry.', 1;
 IF @Id <> 0 AND NOT EXISTS(SELECT 1 FROM dbo.Appointments WHERE Id=@Id AND Version=@Version) THROW 51002, 'This item was changed by another user. Refresh and try again.', 1;
 IF @Status <> N'Cancelled' AND NOT EXISTS(SELECT 1 FROM dbo.Patients WHERE Id=@PatientId AND IsArchived=0) THROW 51003, 'Select an active patient.', 1;
 IF @Status <> N'Cancelled' AND NOT EXISTS(SELECT 1 FROM dbo.Doctors WHERE Id=@DoctorId AND IsArchived=0) THROW 51003, 'Select an active doctor.', 1;
 IF @EndAt <= @StartAt OR DATEDIFF_BIG(second,@StartAt,@EndAt)>86400 THROW 51006, 'Invalid appointment interval.', 1;
 IF @Status <> N'Cancelled' AND EXISTS (
  SELECT 1 FROM dbo.Appointments WHERE Id<>@Id AND Status<>N'Cancelled'
  AND StartAt < @EndAt AND EndAt > @StartAt AND (DoctorId=@DoctorId OR PatientId=@PatientId))
  THROW 51001, 'Schedule conflict: this doctor or patient already has an overlapping appointment.', 1;
 IF @Id=0
 BEGIN
 INSERT dbo.Appointments (PatientId,DoctorId,StartAt,EndAt,Status)
 VALUES (@PatientId,@DoctorId,@StartAt,@EndAt,@Status);
 SET @Id=CONVERT(int,SCOPE_IDENTITY());
 END
 ELSE
 BEGIN
 UPDATE dbo.Appointments SET PatientId=@PatientId,DoctorId=@DoctorId,StartAt=@StartAt,EndAt=@EndAt,Status=@Status WHERE Id=@Id AND Version=@Version;
 END
 COMMIT;
 SELECT @Id;
 END TRY
 BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
 END CATCH
END;
GO
-- Procedure-only writes prevent bypassing the scheduling lock and audit trail.
IF DATABASE_PRINCIPAL_ID(N'clinic_staff') IS NULL CREATE ROLE clinic_staff;
GRANT SELECT ON dbo.Patients TO clinic_staff;
GRANT SELECT ON dbo.Doctors TO clinic_staff;
GRANT SELECT ON dbo.MedicalRecords TO clinic_staff;
GRANT SELECT ON dbo.Appointments TO clinic_staff;
GRANT EXECUTE ON dbo.SavePatient TO clinic_staff;
GRANT EXECUTE ON dbo.SaveDoctor TO clinic_staff;
GRANT EXECUTE ON dbo.SaveMedicalRecord TO clinic_staff;
GRANT EXECUTE ON dbo.SaveAppointment TO clinic_staff;
DENY INSERT, UPDATE, DELETE ON dbo.Patients TO clinic_staff;
DENY INSERT, UPDATE, DELETE ON dbo.Doctors TO clinic_staff;
DENY INSERT, UPDATE, DELETE ON dbo.MedicalRecords TO clinic_staff;
DENY INSERT, UPDATE, DELETE ON dbo.Appointments TO clinic_staff;
DENY INSERT, UPDATE, DELETE ON dbo.AuditLog TO clinic_staff;
GO
-- DBA: CREATE USER [DOMAIN\ClinicStaff] FOR LOGIN [DOMAIN\ClinicStaff];
-- ALTER ROLE clinic_staff ADD MEMBER [DOMAIN\ClinicStaff];
