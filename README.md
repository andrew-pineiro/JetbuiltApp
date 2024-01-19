<h1><p align="center">Jetbuilt API Connectivity Application</p></h1>

## About This Project

Jetbuilt is a service that allows manufacturers and distributors to advertise their products and allows customers to create leads or quotes on those products. To manage products, as a manufacturer, communications need to be made to their API Endpoints that allows management of products at an automated level. 

## Usage

The application currently is self contained and can be run simply by executing the .exe file associated with the project. It uses a hard-coded list of vendors to run through each process. In the future (as apart of the roadmap) this will be enabled to dynamically run for certain vendors.

Since the .NET console application writes to the standard output of the invoking script. It can be tee'd out to a log file for ease of logging.

The application is initiated with a powershell script that looks something like this:
```powershell
$basePath = ""
Invoke-Expression -Command $basePath\JetbuiltApp.exe | tee $basePath\Scripts\.Logs\ProcessLog.txt
```
This currently uses a SQL connection, via stored procedure, to gather live data to compare to Jetbuilt's data.
## Requirements

This application, as a .NET assembly, only requires a Windows operating system and the <a href="https://dotnet.microsoft.com/en-us/download/dotnet/7.0">.NET 7 <b>Desktop</b> Runtime</a> installed. 

It also uses Powershell which is built in to most Windows operating systems.

Below is a sample `App.Config` that will need to be populated before this application can run. `{vendor}` needs to be replaced with the name of your vendors.
```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <appSettings>
    <add key="{vendor}_apiKey" value=""/>
    <add key="{vendor}_apiKey" value=""/>
    <add key="{vendor}_apiKey" value=""/>
    <add key="{vendor}_apiKey" value=""/>
    <add key="{vendor}_apiKey" value=""/>
    <add key="{vendor}_apiKey" value=""/>
    <add key="baseURI" value=""/>
    <add key="psScriptFilePath" value=""/>
    <add key="psScriptFileName" value=""/>
    <add key="productsFilePath" value=""/>
    <add key="vendors" value=""/>
    <add key="failureEmailServer" value=""/>
    <add key="failureEmailTo" value=""/>
    <add key="failureEmailFrom" value=""/>
  </appSettings>
</configuration>
```

Sample `Jetbuilt_PSConfig.txt` file that is used for the `Jetbuilt_CompareProducts.ps1` script. This way they are stored outside of the script.

```txt
VENDOR1,VENDOR2,VENDOR3...
root\path\to\jetbuilt\app
Database Server
Database
Stored Procedure Name
```
Each one of the above fields will need to be updated for your specific instance.
 
## Contributors

* <a href="https://github.com/andrew-pineiro">Andrew Pineiro</a>