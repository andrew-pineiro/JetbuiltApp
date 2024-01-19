# Update to root folder of application
$basePath = ""

Invoke-Expression "$basePath\bin\Publish\net7.0\win-x86\JetbuiltApp.exe" | 
    Tee-Object $basePath\Scripts\.Logs\AppProcessLog_$(Get-Date -Format MM-yyyy).txt -append