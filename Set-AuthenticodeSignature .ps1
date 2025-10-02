$cert = Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert
Set-AuthenticodeSignature -FilePath "C:\Windows\Temp\ps_wrapper.ps1" -Certificate $cert