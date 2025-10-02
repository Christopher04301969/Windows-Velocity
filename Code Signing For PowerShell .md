Use System.Security.Cryptography to encrypt sensitive data (e.g., snapshot manifests) stored on disk.
Code Signing for PowerShell:
Mechanism: Require all PowerShell scripts to be digitally signed with a trusted certificate.
Use a code-signing certificate from a Windows CA or a trusted provider.
Implementation:
Set PowerShell execution policy to AllSigned: