Use CreateAppContainerProfile (via P/Invoke) to create a sandboxed environment.
Auditing and Logging:
Mechanism: Log all API calls, script executions, and snapshot operations to Windows Event Log.
Use System.Diagnostics.EventLog for tamper-resistant logging.
Implementation:
Log gRPC requests:
