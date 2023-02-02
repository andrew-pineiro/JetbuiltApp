<h1><p align="center">Jetbuilt API Connectivity Application</p></h1>

## About This Project

Jetbuilt is a service that allows manufacturers and distributors to advertise their products and allows customers to create leads or quotes on those products. To manage Tower's products as a manufacturer communications need to be made to their API Endpoints that allows up to manage our products at an automated level. This project will complete just that.

## Usage

The application currently is self contained and can be run simply by executing the .exe file associated with the project. It uses a hard-coded list of vendors to run through each process. In the future (as apart of the roadmap) this will be enabled to dynamically run for certain vendors.

```powershell
Invoke-Expression .\JetbuiltAPI.exe
```

## Roadmap

- [x] Initial Connectivity
- [x] Full Functionality
- [ ] Deploy application to production
- [ ] Add ability to add vendors on the fly
- [ ] Add process logging
- [ ] Add email notifications for failure notices
 
## Contributors

* <a href="https://github.com/andrew-pineiro">Andrew Pineiro</a>