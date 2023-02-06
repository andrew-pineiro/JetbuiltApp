$BrandList = @("CAMPLE", "SESCOM", "LAIRD", "MCS", "DELV", "OMX")
$basePath = "E:\Scripts\JetbuiltApp"
#Input Arguments
$Brand = $args[0]
if ($Brand -eq "" -or $Brand -eq $null -or -not($Brand -in $BrandList)) {
    Write-Host "Invalid Brand argument supplied. Program is exiting."
    exit
}
#Logging Variables
$logDate = Get-Date -Format MM-yyyy
$logFile = "$basePath\Scripts\.Logs\PSProcessLog.txt"

#Logging Function
function WriteLog {
    Param ([string]$logText)
    $timeStamp = (Get-Date).toString("M/d/yyyy H:mm:ss tt")
    $LogMessage = "[$timeStamp] $logText"
    Add-Content $logFile -value $LogMessage
}
$serverName = "QA-RESP"
$databaseName = "RESPUTIL"
$Query = "exec sp_JetBuilt_BuildFullProducts_ByVendor '$Brand'"
$jetbuiltFile = "$basePath\Output\$Brand\JBProducts.json"
$responseFile = "$basePath\Output\$Brand\ResponseProducts.json"
$DifferentFile = "$basePath\Output\$Brand\UpdateProducts.json"
$NotOnJetbuiltFile = "$basePath\Output\$Brand\AddProducts.json"
$NotOnResponseFile = "$basePath\Output\$Brand\DeleteProducts.txt"
WriteLog("Application Starting for $Brand...")
if (Test-Path -Path $responseFile) {
    rm $responseFile
}

WriteLog("Fetching Response SQL Data.")

Try {
    
    $results = Invoke-Sqlcmd -Query $Query -OutputAs DataRows -ConnectionString "Data Source=$serverName;Initial Catalog=$databaseName;Integrated Security=True;"
    $json = $results | Select-Object * -ExcludeProperty ItemArray, Table, RowError, RowState, HasErrors | ConvertTo-Json | Out-File $responseFile
    
} Catch { 
    WriteLog("Error Fetching Response SQL Data. Exception: " + $_.Exception) 
    exit 1
    }

WriteLog("Starting to Compare Data...")

Try {

    $jetbuiltData = Get-Content $jetbuiltFile | ConvertFrom-Json 
    $jetbuiltData = $jetbuiltData | Where-Object {$_.discontinued -ne "True"}
    $responseData = Get-Content $responseFile | ConvertFrom-Json
    $result1 = Compare-Object -ReferenceObject $jetbuiltData -DifferenceObject $responseData -Property model -PassThru | Where-Object {$_.SideIndicator -eq "=>"} | Select-Object * -ExcludeProperty SideIndicator, category_name, manufacturer
    ConvertTo-Json @($result1) | Out-File $NotOnJetbuiltFile 
    Compare-Object -ReferenceObject $responseData -DifferenceObject $jetbuiltData -Property model -PassThru | Where-Object {$_.SideIndicator -eq "=>"} | Select-Object id | Out-File $NotOnResponseFile
    $result2 = Compare-Object -ReferenceObject $jetbuiltData -DifferenceObject $responseData -Property short_description, long_description, part_number, msrp, mapp -PassThru | Where-Object {$_.SideIndicator -eq "=>" -and $_.model -ne $null} | Select-Object model, short_description, long_description, part_number, msrp,  mapp -ExcludeProperty SideIndicator
    ConvertTo-Json @($result2) | Out-File $DifferentFile
} Catch { 
    WriteLog("Error Comparing Data. Exception: " + $_.Exception) 
    exit 1
    }
WriteLog("Script complete. Exiting...")