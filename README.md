# MediView
```
Booking, Diagnosis, DICOM viewport
```

## Logging rule (non-negotiable)

Log identifiers, never protected health information.

- OK: `StudyId`, `PatientId`, `DoctorId`, `UserId`, `CorrelationId`, status transitions, timings
- NO: patient name, date of birth, MRN, blood type, report text, medication notes, DICOM pixel data

Every service logs through `builder.Host.UseMediViewLogging()` (console + Seq) and
`app.UseMediViewRequestLogging()` (correlation id, then one summary line per request). Each line
carries `ServiceName`, `CorrelationId` and — once the caller is authenticated — `UserId`.

Seq runs at http://localhost:5341 (`docker compose up -d seq`). To follow one request end to end,
filter on `CorrelationId = '...'`; callers can pin the id themselves by sending an
`X-Correlation-Id` header, and it is echoed back on every response.
