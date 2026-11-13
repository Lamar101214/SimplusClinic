-- RUN THIS FILE ONLY to upgrade an existing working clinic database.
-- Standard SSMS mode is sufficient. No SQLCMD directives and no clinic data deletion.
USE SimplusClinic;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.AppUsers',N'U') IS NULL
 CREATE TABLE dbo.AppUsers(
 Id int IDENTITY CONSTRAINT PK_AppUsers PRIMARY KEY,
 Username nvarchar(50) COLLATE Latin1_General_100_CI_AS NOT NULL CONSTRAINT UQ_AppUsers_Username UNIQUE,
 DisplayName nvarchar(100) NOT NULL,
 Role nvarchar(10) NOT NULL CONSTRAINT CK_AppUsers_Role CHECK(Role IN(N'Admin',N'Staff')),
 PasswordSalt varbinary(16) NOT NULL CONSTRAINT CK_AppUsers_Salt CHECK(DATALENGTH(PasswordSalt)=16),
 PasswordHash varbinary(32) NOT NULL CONSTRAINT CK_AppUsers_Hash CHECK(DATALENGTH(PasswordHash)=32),
 Iterations int NOT NULL CONSTRAINT CK_AppUsers_Iterations CHECK(Iterations BETWEEN 600000 AND 2000000),
 IsActive bit NOT NULL DEFAULT 1, FailedAttempts int NOT NULL DEFAULT 0,
 LockedUntilUtc datetime2 NULL, CreatedAtUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), LastLoginUtc datetime2 NULL);
IF OBJECT_ID(N'dbo.AppSessions',N'U') IS NULL
 CREATE TABLE dbo.AppSessions(
 TokenHash binary(32) CONSTRAINT PK_AppSessions PRIMARY KEY,
 UserId int NOT NULL CONSTRAINT FK_AppSessions_AppUsers REFERENCES dbo.AppUsers(Id),
 ExpiresAtUtc datetime2 NOT NULL);
IF OBJECT_ID(N'dbo.AuthAudit',N'U') IS NULL
 CREATE TABLE dbo.AuthAudit(
 Id bigint IDENTITY PRIMARY KEY, UserId int NULL, EventName nvarchar(40) NOT NULL,
 ActorUserId int NULL, WindowsLogin nvarchar(128) NOT NULL DEFAULT ORIGINAL_LOGIN(),
 OccurredAtUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AppSessions') AND name=N'IX_AppSessions_User')
 CREATE INDEX IX_AppSessions_User ON dbo.AppSessions(UserId) INCLUDE(ExpiresAtUtc);
COMMIT;
GO
CREATE OR ALTER PROCEDURE dbo.AuthNeedsSetup AS
BEGIN
 SET NOCOUNT ON;
 SELECT CONVERT(bit,CASE WHEN EXISTS(SELECT 1 FROM dbo.AppUsers) THEN 0 ELSE 1 END);
END;
GO
CREATE OR ALTER PROCEDURE dbo.AuthChallenge @Username nvarchar(50) AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM dbo.AppUsers WHERE Username=@Username)
  SELECT PasswordSalt,Iterations FROM dbo.AppUsers WHERE Username=@Username;
 ELSE SELECT CRYPT_GEN_RANDOM(16),600000;
END;
GO
CREATE OR ALTER PROCEDURE dbo.AuthBootstrap
 @Username nvarchar(50),@DisplayName nvarchar(100),@Salt varbinary(16),@Hash varbinary(32),@Iterations int
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF ISNULL(IS_MEMBER(N'db_owner'),0)<>1 AND ISNULL(IS_SRVROLEMEMBER(N'sysadmin'),0)<>1
  THROW 51100,'First administrator setup must be performed by the database owner on a trusted workstation.',1;
 BEGIN TRY
  BEGIN TRANSACTION;
  DECLARE @lock int;
  EXEC @lock=sys.sp_getapplock @Resource=N'ClinicAccounts',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000;
  IF @lock<0 THROW 51100,'Account setup is busy. Retry.',1;
  IF EXISTS(SELECT 1 FROM dbo.AppUsers) THROW 51100,'Setup is already complete. Sign in with an existing account.',1;
  IF LEN(LTRIM(RTRIM(@Username)))<3 OR LEN(LTRIM(RTRIM(@DisplayName)))=0 THROW 51100,'Enter a valid username and display name.',1;
  INSERT dbo.AppUsers(Username,DisplayName,Role,PasswordSalt,PasswordHash,Iterations)
  VALUES(LOWER(LTRIM(RTRIM(@Username))),@DisplayName,N'Admin',@Salt,HASHBYTES('SHA2_256',@Hash),@Iterations);
  DECLARE @id int=CONVERT(int,SCOPE_IDENTITY());
  INSERT dbo.AuthAudit(UserId,EventName,ActorUserId) VALUES(@id,N'ADMIN_SETUP',@id);
  COMMIT; SELECT @id;
 END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH
END;
GO
CREATE OR ALTER PROCEDURE dbo.AuthSignIn @Username nvarchar(50),@Proof varbinary(32),@Token varbinary(32)
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF DATALENGTH(@Token)<>32 THROW 51100,'Invalid login request.',1;
 BEGIN TRY
  BEGIN TRANSACTION;
  DECLARE @id int,@hash varbinary(32),@active bit,@failures int,@locked datetime2,@now datetime2=SYSUTCDATETIME();
  SELECT @id=Id,@hash=PasswordHash,@active=IsActive,@failures=FailedAttempts,@locked=LockedUntilUtc
  FROM dbo.AppUsers WITH(UPDLOCK,HOLDLOCK) WHERE Username=@Username;
  IF @id IS NULL OR @active=0 OR @locked>@now
  BEGIN
   INSERT dbo.AuthAudit(UserId,EventName) VALUES(@id,N'LOGIN_REJECTED');
   COMMIT; RETURN;
  END;
  IF @locked IS NOT NULL AND @locked<=@now SET @failures=0;
  IF @hash<>HASHBYTES('SHA2_256',@Proof) OR @Proof IS NULL OR DATALENGTH(@Proof)<>32
  BEGIN
   SET @failures=@failures+1;
   UPDATE dbo.AppUsers SET FailedAttempts=@failures,
    LockedUntilUtc=CASE WHEN @failures>=5 THEN DATEADD(minute,15,@now) ELSE NULL END WHERE Id=@id;
   INSERT dbo.AuthAudit(UserId,EventName) VALUES(@id,N'LOGIN_FAILED');
   COMMIT; RETURN;
  END;
  UPDATE dbo.AppUsers SET FailedAttempts=0,LockedUntilUtc=NULL,LastLoginUtc=@now WHERE Id=@id;
  DELETE dbo.AppSessions WHERE ExpiresAtUtc<=@now;
  DECLARE @expires datetime2=DATEADD(hour,8,@now);
  INSERT dbo.AppSessions(TokenHash,UserId,ExpiresAtUtc) VALUES(HASHBYTES('SHA2_256',@Token),@id,@expires);
  INSERT dbo.AuthAudit(UserId,EventName,ActorUserId) VALUES(@id,N'LOGIN_SUCCESS',@id);
  COMMIT;
  SELECT Id,Username,DisplayName,Role,IsActive,@expires ExpiresAtUtc FROM dbo.AppUsers WHERE Id=@id;
 END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH
END;
GO
CREATE OR ALTER PROCEDURE dbo.AuthListUsers @Token varbinary(32) AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM dbo.AppSessions s JOIN dbo.AppUsers u ON u.Id=s.UserId
 WHERE s.TokenHash=HASHBYTES('SHA2_256',@Token) AND s.ExpiresAtUtc>SYSUTCDATETIME() AND u.IsActive=1 AND u.Role=N'Admin')
 THROW 51100,'An active administrator session is required. Sign in again.',1;
 SELECT Id,Username,DisplayName,Role,IsActive FROM dbo.AppUsers ORDER BY Username;
END;
GO
CREATE OR ALTER PROCEDURE dbo.AuthCreateUser
 @Token varbinary(32),@Username nvarchar(50),@DisplayName nvarchar(100),@Role nvarchar(10),
 @Salt varbinary(16),@Hash varbinary(32),@Iterations int
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @lock int,@actor int;
 EXEC @lock=sys.sp_getapplock @Resource=N'ClinicAccounts',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000;
 IF @lock<0 THROW 51100,'Account management is busy. Retry.',1;
 SELECT @actor=u.Id FROM dbo.AppSessions s JOIN dbo.AppUsers u ON u.Id=s.UserId
 WHERE s.TokenHash=HASHBYTES('SHA2_256',@Token) AND s.ExpiresAtUtc>SYSUTCDATETIME() AND u.IsActive=1 AND u.Role=N'Admin';
 IF @actor IS NULL THROW 51100,'An active administrator session is required. Sign in again.',1;
 IF LEN(LTRIM(RTRIM(@Username)))<3 OR LEN(LTRIM(RTRIM(@DisplayName)))=0 THROW 51100,'Enter a valid username and display name.',1;
 IF EXISTS(SELECT 1 FROM dbo.AppUsers WHERE Username=@Username) THROW 51100,'That username already exists.',1;
 INSERT dbo.AppUsers(Username,DisplayName,Role,PasswordSalt,PasswordHash,Iterations)
 VALUES(LOWER(LTRIM(RTRIM(@Username))),@DisplayName,@Role,@Salt,HASHBYTES('SHA2_256',@Hash),@Iterations);
 DECLARE @id int=CONVERT(int,SCOPE_IDENTITY());
 INSERT dbo.AuthAudit(UserId,EventName,ActorUserId) VALUES(@id,N'USER_CREATED',@actor);
 COMMIT; SELECT @id;
 END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH
END;
GO
CREATE OR ALTER PROCEDURE dbo.AuthSetActive @Token varbinary(32),@Id int,@Active bit AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @lock int,@actor int;
 EXEC @lock=sys.sp_getapplock @Resource=N'ClinicAccounts',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000;
 IF @lock<0 THROW 51100,'Account management is busy. Retry.',1;
 SELECT @actor=u.Id FROM dbo.AppSessions s JOIN dbo.AppUsers u ON u.Id=s.UserId
 WHERE s.TokenHash=HASHBYTES('SHA2_256',@Token) AND s.ExpiresAtUtc>SYSUTCDATETIME() AND u.IsActive=1 AND u.Role=N'Admin';
 IF @actor IS NULL THROW 51100,'An active administrator session is required. Sign in again.',1;
 IF @Id=@actor THROW 51100,'You cannot disable your own account.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.AppUsers WHERE Id=@Id) THROW 51100,'Account no longer exists.',1;
 IF @Active=0 AND EXISTS(SELECT 1 FROM dbo.AppUsers WHERE Id=@Id AND Role=N'Admin')
 AND (SELECT COUNT(*) FROM dbo.AppUsers WHERE Role=N'Admin' AND IsActive=1)<=1
 THROW 51100,'At least one active administrator must remain.',1;
 UPDATE dbo.AppUsers SET IsActive=@Active WHERE Id=@Id;
 IF @Active=0 DELETE dbo.AppSessions WHERE UserId=@Id;
 INSERT dbo.AuthAudit(UserId,EventName,ActorUserId) VALUES(@Id,CASE WHEN @Active=1 THEN N'USER_ENABLED' ELSE N'USER_DISABLED' END,@actor);
 COMMIT; SELECT @Id;
 END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH
END;
GO
CREATE OR ALTER PROCEDURE dbo.AuthSignOut @Token varbinary(32) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRANSACTION;
 DECLARE @id int;
 SELECT @id=UserId FROM dbo.AppSessions WHERE TokenHash=HASHBYTES('SHA2_256',@Token);
 DELETE dbo.AppSessions WHERE TokenHash=HASHBYTES('SHA2_256',@Token);
 IF @id IS NOT NULL INSERT dbo.AuthAudit(UserId,EventName,ActorUserId) VALUES(@id,N'LOGOUT',@id);
 COMMIT; SELECT 1;
END;
GO
IF DATABASE_PRINCIPAL_ID(N'clinic_staff') IS NULL CREATE ROLE clinic_staff;
GRANT EXECUTE ON dbo.AuthNeedsSetup TO clinic_staff;
GRANT EXECUTE ON dbo.AuthChallenge TO clinic_staff;
GRANT EXECUTE ON dbo.AuthSignIn TO clinic_staff;
GRANT EXECUTE ON dbo.AuthListUsers TO clinic_staff;
GRANT EXECUTE ON dbo.AuthCreateUser TO clinic_staff;
GRANT EXECUTE ON dbo.AuthSetActive TO clinic_staff;
GRANT EXECUTE ON dbo.AuthSignOut TO clinic_staff;
DENY SELECT,INSERT,UPDATE,DELETE ON dbo.AppUsers TO clinic_staff;
DENY SELECT,INSERT,UPDATE,DELETE ON dbo.AppSessions TO clinic_staff;
DENY INSERT,UPDATE,DELETE ON dbo.AuthAudit TO clinic_staff;
GO
PRINT 'Login upgrade installed. Start the app and create your first administrator.';
