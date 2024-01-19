using JetbuiltApp.Data;
using static JetbuiltApp.Data.HelperFunctions;
using System.Configuration;

internal class Program
{
    private static void Main()
    {
        string vendors = ConfigurationManager.AppSettings["vendors"] ?? string.Empty;
        JetbuiltFunctions Jetbuilt = new();

        foreach (string vendor in vendors.Split(';'))
        {
            var _jetbuilt = Jetbuilt;
            string apiKey = GetAPIKey(vendor); 

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine($"[{DateTime.Now}] ApiKey not found for {vendor}, skipping vendor.");
                continue;
            }

            Console.WriteLine($"[{DateTime.Now}] START {vendor.ToUpper()}");
            int errNo;
            while (true)
            {
                errNo = _jetbuilt.GetProducts(apiKey, vendor);
                if (errNo != 0) { break; }
                errNo = JetbuiltFunctions.RunCompareFiles(vendor);
                if (errNo != 0) { break; }
                errNo = _jetbuilt.DeleteProducts(apiKey, vendor);
                if (errNo != 0) { break; }
                errNo = _jetbuilt.AddProducts(apiKey, vendor);
                if (errNo != 0) { break; }
                errNo = _jetbuilt.UpdateProducts(apiKey, vendor);
                break;
            }

            if(errNo != 0)
            {
                SendFailureEmail();
                if(errNo == 2)
                {
                    Environment.Exit(errNo);
                }
                continue;
            }

            Console.WriteLine($"[{DateTime.Now}] END {vendor.ToUpper()}");
        }
    }
}