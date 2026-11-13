# Run the login upgrade

Your existing SQL Express connection and your edited dashboard/module layouts are preserved.

1. Extract this updated project into its own folder. Stop the old running app.
2. Open SSMS and connect to `.\SQLEXPRESS` with the Windows account you used to create the clinic database.
3. Open **Database/03_LoginUpgrade.sql** and execute the entire file. Standard SSMS mode works. It adds only authentication tables/procedures; it does not drop or recreate clinic tables. It is safe to rerun this upgrade to update its procedures.
4. Open **SimplusClinic.slnx** from this updated folder in Visual Studio.
5. Set **SimplusClinic.WinForms** as the startup project, rebuild, and press F5.
6. On the first launch, create your administrator: display name, username, password and confirmation. Use a 12–128 character password/passphrase. There is no default username or password. Then sign in.
7. As an Admin, open **User accounts** in the sidebar to create Staff or additional Admin accounts. Staff have access to the six existing clinic sections; only Admins can manage accounts.
8. Use **Log out** to return to login. Closing the main window exits the app. Password fields have Show password controls.

**Do not rerun 01_CreateDatabase.sql on your working database.** Scripts 01 and 02 are for a fresh installation; a fresh installation then also needs script 03. Script 01's earlier SQLCMD syntax and duplicate appointment-constraint errors are corrected in this copy for future fresh installs. The repair script you uploaded is retained.

If startup says a procedure is missing, check that script 03 was executed on the same `.\SQLEXPRESS` / `SimplusClinic` configured in App.config.

First-admin setup requires the current Windows SQL login to be db_owner or sysadmin. This prevents a restricted clinic workstation account from claiming first administration. After setup, use appropriately restricted Windows SQL identities in `clinic_staff` for ordinary operation. A database owner remains able to change all database data regardless of application roles.

## Included login behavior

- Salted PBKDF2-HMAC-SHA256, 600,000 iterations, independent 16-byte salts, 32-byte derived values. SQL stores a SHA-256 verifier of the derived value, so the stored verifier cannot be submitted directly as the login proof.
- Passwords are never stored as plain text; the database never returns the stored password hash to the UI. Login derives a proof from the salt and submits it over the configured SQL connection. Treat that proof as sensitive; it is not logged.
- Case-insensitive usernames normalized to lowercase; passwords remain case-sensitive and are not trimmed.
- Five consecutive failed attempts lock an account for 15 minutes. Unknown, disabled, locked and incorrect-password responses share a generic failure message.
- Random in-memory session tokens; only SHA-256 token hashes are stored in SQL. Eight-hour session lifetime. Logout revokes the token; local token memory is cleared after the main form closes.
- Admin account operations validate role, expiry and active status on the SQL server as well as in the service layer. Staff cannot obtain the account list or create users by calling those procedures with a Staff token.
- Disabling an account revokes its server sessions. Own-account disabling and disabling the last active administrator are blocked.
- Authentication audit records track setup, logins, user creation, enabling/disabling and logout. Existing clinic-operation auditing remains based on the Windows SQL identity, not the new application username.

## Boundaries and recovery

This is a direct-to-SQL desktop application. Windows SQL permissions remain the security boundary for the existing clinical tables and procedures. Application login gates this app's screens; it does not prevent a person already authorized in SQL from using SSMS. Both app roles have the existing clinic permissions; their difference is account administration. Do not grant db_owner to ordinary staff.

Disabling an account prevents new logins and revokes privileged operations immediately. Existing clinical screens are not polled for remote account changes; close that user's app when disabling their account. An eight-hour expiration returns the main window to login once any modal editor closes. This is not an inactivity timer.

If SQL is unreachable during logout, the local app still signs out after a short attempt; the server token expires at its original expiry. Tokens are never persisted to disk. There is no password-recovery/reset feature in this increment. Keep administrator credentials safely; do not delete AppUsers to reset setup. Account administration currently supports create and enable/disable, not renaming, role changes or password resets.

The inherited local-development connection uses TrustServerCertificate=True. Use a trusted SQL certificate and False for a deployed environment because derived login proofs and session tokens require a trusted encrypted connection.

## Validation

Run `build.ps1` to restore, build and run the rule/service tests. Authentication tests are included in the default console test run. On a disposable SQL instance initialized with scripts 01, 02 and 03, set SIMPLUS_TEST_CONNECTION and run:

```powershell
dotnet run --project tests/SimplusClinic.Tests -- --auth-integration
```

This test requires no existing AppUsers, creates synthetic accounts, and tests concurrent first-admin creation, role checks, duplicate usernames, disabling, lockout and logout revocation. Do not run against your working clinic database.

Manual Windows checks: first-admin setup; incorrect password; password visibility; Enter to submit; successful login; Staff sees no User accounts item; Admin creates Staff; disable/enable; logout; close app; reopen; retry after stopping SQL temporarily; check layout at your normal display scaling. Do not treat syntax checks as a successful Windows UI or SQL test.

Reference for the chosen built-in password derivation settings: https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html
