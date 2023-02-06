<h1><p align="center">Jetbuilt API Connectivity Application</p></h1>

## About This Project

Jetbuilt is a service that allows manufacturers and distributors to advertise their products and allows customers to create leads or quotes on those products. To manage Tower's products as a manufacturer communications need to be made to their API Endpoints that allows up to manage our products at an automated level. This project will complete just that.

## Usage

The application currently is self contained and can be run simply by executing the .exe file associated with the project. It uses a hard-coded list of vendors to run through each process. In the future (as apart of the roadmap) this will be enabled to dynamically run for certain vendors.

Since the .NET console application writes to the standard output of the invoking script. It can be tee'd out to a log file for ease of logging.

The application is initiated with a powershell script that looks something like this:
```powershell
$basePath = ""
Invoke-Expression -Command $basePath\JetbuiltApp.exe | tee $basePath\Scripts\.Logs\ProcessLog.txt
```

## Requirements

This application, as a .NET assembly, only requires a Windows operating system and the <a href="https://dotnet.microsoft.com/en-us/download/dotnet/7.0">.NET 7 <b>Desktop</b> Runtime</a> installed. 

It also uses Powershell which is built in to most Windows operating systems.

## Roadmap

- [x] Complete initial connectivity
- [x] Complete full functionality
- [x] Add process logging
- [ ] Deploy application to production
- [ ] Add ability to add vendors on the fly
- [ ] Add email notifications for failure notices
 
## Contributors

* <a href="https://github.com/andrew-pineiro">Andrew Pineiro</a>