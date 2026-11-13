# Verification record — login update

## Executed
- Cross-compiled the complete five-project solution in Release using .NET SDK 10.0.100 on Linux.
- Ran the console rule/service suite: all 42 checks passed, including 18 authentication checks.
- Passed 311 structural checks during implementation, including C# grammar, field mappings, references and transaction guards.
- Compared the uploaded source: preserved the user's Theme.cs, DashboardPage.cs, EntityPage.cs and SQL Express App.config.
- Reviewed the additive authentication migration and packaged source without build output.

## Still required on Windows / SQL Server
- Run Database/03_LoginUpgrade.sql against the existing database.
- Exercise first-admin setup, sign-in, account management, lockout and logout.
- Run the supplied SQL integration tests only against a disposable database.
- Check keyboard navigation, resizing, scaling and dark-theme layout on Windows.

Cross-compilation and service tests do not establish live database or Windows UI correctness. Follow ACCEPTANCE.md and START_HERE_LOGIN.md before deployment.
