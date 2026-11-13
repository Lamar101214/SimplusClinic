# Windows / SQL Server acceptance checklist

Run on a disposable database and repeat with the restricted `clinic_staff` identity.

1. Build Release using `build.ps1`; run rule tests and live integration tests. Expected: exit code 0.
2. Add patient and doctor with all fields. Reopen View, edit each field, refresh, and verify persistence. Blank required fields, malformed supplied email, oversized input and future birth/treatment dates must fail; blank optional contact/guardian fields must work.
3. Search IDs and first, middle, last and full names. Combine doctor name, specialization and license filters. Search apostrophes and literal `%`, `_`, `[` and `~`. Confirm no SQL syntax errors or unexpected wildcard matches.
4. Create a diagnosis with treatment/prescription. Change it, archive/restore it, and have a DBA inspect `dbo.MedicalRecords FOR SYSTEM_TIME ALL` and `dbo.AuditLog`.
5. Create 10:00–10:30. Same doctor/different patient at 10:15 must fail. Same patient/different doctor at 10:15 must fail. Different patient and doctor must succeed. 10:30 must succeed. Containment and midnight crossings must be caught. Cancelled visits free slots; completed visits block overlaps.
6. In two app instances, choose the same available doctor/time and save simultaneously. Exactly one save must succeed. Repeat for the same patient with different doctors and for concurrent edits moving appointments into the same interval.
7. Edit one row in two instances. Save the first; saving the second must show a stale-data error. Refresh and re-edit to continue. Test the same with archive and restore.
8. Attempt to archive a patient/doctor with a scheduled visit; reject. Complete/cancel the visits; archive succeeds without losing history. Exclude archived participants from new lookups; retain them in linked historical medical records.
9. Verify all four reports. A patient linked by both a visit and medical record must appear once per doctor. Verify data beyond page 1 with >100 synthetic records and the empty-page Previous control.
10. Rapidly type search terms and switch pages; no stale results or disposed-control errors. Stop SQL Server temporarily; show a helpful error and allow refresh after reconnecting.
11. Check at 100%, 125%, 150% and 200% DPI; test 1100×740 minimum window, maximize/restore, resizing, keyboard Tab order, enter/cancel, scrollable guardian fields and full diagnosis text in View.
12. Restricted staff identity: SELECT and authorized procedure writes succeed; direct INSERT/UPDATE/DELETE fail; audit/history deletion fails. Verify ORIGINAL_LOGIN correctly identifies the user. Never deploy with sysadmin/db_owner membership.
13. Verify trusted TLS, operational backup and successful restore, time zone consistency and workstation access controls before adding real clinical data.

## History inspection (DBA / auditor)

```sql
USE SimplusClinic;
SELECT * FROM dbo.MedicalRecords FOR SYSTEM_TIME ALL
WHERE Id = 1 ORDER BY ValidFrom;
SELECT * FROM dbo.AuditLog ORDER BY AuditId DESC;
```

Temporal history shows database transaction time in UTC. New revisions do not overwrite retained history. Do not disable SYSTEM_VERSIONING during routine application use.
