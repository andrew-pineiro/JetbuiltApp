# Specify path to PSConfig file
$Parameters = Get-Content ".\Jetbuilt_PSConfig.txt"
$ParameterCount = 5
if($Parameters.Count -lt $ParameterCount) {
    Write-Host "Not enough parameters provided in PSConfig file"
    exit 1
}
$BrandList = $Parameters[0]  
$BasePath = $Parameters[1]                         
$SQLServer = $Parameters[2]
$SQLDatabase = $Parameters[3]                                           
$SQLStoredProc = $Parameters[4]

#Input Arguments
$Brand = $args[0]
if ($Brand -eq "" -or $Brand -eq $null -or -not($Brand -in $BrandList.Split(','))) {
    Write-Host "Invalid Brand argument supplied. Program is exiting."
    exit 1
}

#Logging Variables
$logDate = Get-Date -Format MM-yyyy
$logFile = "$BasePath\Scripts\.Logs\PSProcessLog_$logDate.txt"

#Logging Function
function WriteLog {
    Param ([string]$logText)
    $timeStamp = (Get-Date).toString("M/d/yyyy H:mm:ss tt")
    $LogMessage = "[$timeStamp] $logText"
    Add-Content $logFile -value $LogMessage
}

$SQLQuery = "exec $SQLStoredProc '$Brand'"
$SQLConnectionString = "Data Source=$SQLServer;Initial Catalog=$SQLDatabase;Integrated Security=True;TrustServerCertificate=true;"

$JetbuiltProducts = "$BasePath\Output\$Brand\JetbuiltProducts.json"
$DatabaseProducts = "$BasePath\Output\$Brand\DatabaseProducts.json"
$ProductsToUpdate = "$BasePath\Output\$Brand\UpdateProducts.json"
$ProductsToAdd = "$BasePath\Output\$Brand\AddProducts.json"
$ProductsToDelete = "$BasePath\Output\$Brand\DeleteProducts.txt"

WriteLog("START $($Brand.ToUpper())")

if (Test-Path -Path $DatabaseProducts) {
    Remove-Item $DatabaseProducts
}

WriteLog("SQL START")

try {
    
    Invoke-Sqlcmd -Query $SQLQuery -OutputAs DataRows -ConnectionString $SQLConnectionString | 
        Select-Object * -ExcludeProperty ItemArray, Table, RowError, RowState, HasErrors | 
            ConvertTo-Json | Out-File $DatabaseProducts
    WriteLog("SQL SUCCESS")
} catch { 
    WriteLog("SQL ERROR. Exception: " + $_.Exception) 
    exit 1
}

try {
    WriteLog("COMPARE START")
    $jetbuiltData = Get-Content $JetbuiltProducts | ConvertFrom-Json 
    $jetbuiltData = $jetbuiltData | Where-Object {$_.discontinued -ne "True"}

    $dbData = Get-Content $DatabaseProducts | ConvertFrom-Json

    # Products to add to Jetbuilt
    $Compare1 = Compare-Object -ReferenceObject $jetbuiltData -DifferenceObject $dbData -Property model -PassThru |
        Where-Object {$_.SideIndicator -eq "=>"} | 
            Select-Object * -ExcludeProperty SideIndicator, category_name, manufacturer
    ConvertTo-Json @($Compare1) | Out-File $ProductsToAdd 

    # Products to delete from Jetbuilt
    Compare-Object -ReferenceObject $dbData -DifferenceObject $jetbuiltData -Property model -PassThru | 
        Where-Object {$_.SideIndicator -eq "=>"} | 
            Select-Object id -ExpandProperty id | Out-File $ProductsToDelete
    
    # Products to update on Jetbuilt
    $Compare2 = Compare-Object -ReferenceObject $jetbuiltData -DifferenceObject $dbData -Property short_description, long_description, part_number, msrp, mapp -PassThru | 
        Where-Object {$_.SideIndicator -eq "=>" -and $_.model -ne $null} | 
            Select-Object model, short_description, long_description, part_number, msrp,  mapp -ExcludeProperty SideIndicator
    ConvertTo-Json @($Compare2) | Out-File $ProductsToUpdate

    WriteLog("COMPARE SUCCESS")
} Catch { 
    WriteLog("COMPARE ERROR. Exception: " + $_.Exception) 
    exit 1
    }
WriteLog("END $($Brand.ToUpper())")
