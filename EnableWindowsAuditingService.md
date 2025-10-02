Enable Windows auditing for service access via secpol.msc.
Anti-Tampering:
Mechanism: Protect the .NET service binary and configuration files using Windows Defender Application Control (WDAC).
Define a WDAC policy to allow only signed binaries.
Implementation:
Create a WDAC policy XML:
