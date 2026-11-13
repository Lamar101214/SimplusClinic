USE SimplusClinic;
GO

-- Create the missing table without duplicate constraint names.
IF OBJECT_ID(N'dbo.Appointments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Appointments
    (
        Id int IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Appointments PRIMARY KEY,

        PatientId int NOT NULL,
        DoctorId int NOT NULL,
        StartAt datetime2(0) NOT NULL,
        EndAt datetime2(0) NOT NULL,
        Status nvarchar(20) NOT NULL,
        Version rowversion NOT NULL,

        CONSTRAINT FK_Appointments_Patients
            FOREIGN KEY (PatientId)
            REFERENCES dbo.Patients(Id),

        CONSTRAINT FK_Appointments_Doctors
            FOREIGN KEY (DoctorId)
            REFERENCES dbo.Doctors(Id),

        CONSTRAINT CK_Appointments_Status
            CHECK (Status IN
                (N'Scheduled', N'Completed', N'Cancelled')),

        CONSTRAINT CK_Appointments_Interval
            CHECK
            (
                EndAt > StartAt
                AND DATEDIFF_BIG(second, StartAt, EndAt) <= 86400
            )
    );
END;
GO

CREATE OR ALTER TRIGGER dbo.tr_Appointments_Audit
ON dbo.Appointments
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT dbo.AuditLog
        (EntityName, EntityId, Operation)
    SELECT
        N'Appointments',
        i.Id,
        CASE
            WHEN d.Id IS NULL THEN N'INSERT'
            ELSE N'UPDATE'
        END
    FROM inserted AS i
    LEFT JOIN deleted AS d ON d.Id = i.Id;
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Appointments')
      AND name = N'IX_Appointments_Doctor_Interval'
)
BEGIN
    CREATE INDEX IX_Appointments_Doctor_Interval
    ON dbo.Appointments(DoctorId, StartAt)
    INCLUDE (EndAt, Status, PatientId);
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Appointments')
      AND name = N'IX_Appointments_Patient_Interval'
)
BEGIN
    CREATE INDEX IX_Appointments_Patient_Interval
    ON dbo.Appointments(PatientId, StartAt)
    INCLUDE (EndAt, Status, DoctorId);
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Appointments')
      AND name = N'IX_Appointments_Date'
)
BEGIN
    CREATE INDEX IX_Appointments_Date
    ON dbo.Appointments(StartAt)
    INCLUDE (EndAt, Status, PatientId, DoctorId);
END;
GO

-- Confirm that the required tables exist.
SELECT name AS TableName
FROM sys.tables
WHERE name IN
(
    N'Patients',
    N'Doctors',
    N'MedicalRecords',
    N'Appointments',
    N'AuditLog'
)
ORDER BY name;