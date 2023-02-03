$basePath = "E:\Scripts\JetbuiltApp"
Invoke-Expression $basePath\JetbuiltApp.exe | tee $basePath\Scripts\.Logs\AppProcessLog.txt -append