using System.Configuration;
using System.Net.Mail;

namespace JetbuiltApp.Data
{
    public static class HelperFunctions
    {
        public static string GetAPIKey(string vendor)
        {
            string apiKey =
                ConfigurationManager.AppSettings[string.Format("{0}_apiKey", vendor.ToLower())] ?? string.Empty;
            return apiKey;
        }
        public static Uri GetBaseURI()
        {
            string baseURI =
                ConfigurationManager.AppSettings["baseURI"] ?? string.Empty;
            if (string.IsNullOrEmpty(baseURI))
            {
                Console.WriteLine($"[{DateTime.Now}] Base URI Not Found.");
                throw new Exception("Base URI for Jetbuilt API not found");
            }
            return new Uri(baseURI);
        }
        public static void SendFailureEmail()
        {
            string emailServer = ConfigurationManager.AppSettings["failureEmailServer"] ?? string.Empty;
            string emailTo = ConfigurationManager.AppSettings["failureEmailTo"] ?? string.Empty;
            string emailFrom = ConfigurationManager.AppSettings["failureEmailFrom"] ?? string.Empty;

            if (string.IsNullOrEmpty(emailServer) || string.IsNullOrEmpty(emailTo) || string.IsNullOrEmpty(emailFrom))
            {
                Console.WriteLine($"[{DateTime.Now}] Email variables not present in config. Failure email not sent.");
                throw new Exception("Error in email process; Config variables not present");
            }

            MailMessage mail = new()
            {
                From = new MailAddress(emailFrom),
                Subject = "Jetbuilt Application Failure",
                Body = "Check log file for details"
            };
            mail.To.Add(emailTo);

            SmtpClient mailClient = new(){ Host = emailServer, Port = 25 };

            try
            {
                mailClient.Send(mail);
                Console.WriteLine($"[{DateTime.Now}] Failure email sent to {emailTo}");

            } catch (Exception ex) { 

                Console.WriteLine($"[{DateTime.Now}] {ex.Message}");
                throw;
            }
        }
        public static string GetOutputFile(string vendor, string fileName)
        {
            string path =
                ConfigurationManager.AppSettings["productsFilePath"] ?? string.Empty;
            if (string.IsNullOrEmpty(path))
            {
                Console.WriteLine($"[{DateTime.Now}] Output path does not exist. {vendor}\\{fileName}");
                throw new Exception("Product file path not found in config");
            }
            return string.Format("{0}\\{1}\\{2}", path, vendor, fileName);
        }
    }
}
