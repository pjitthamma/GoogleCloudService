using System;
using CloudStartService.Models;
using CloudStartService.Helper;
using CloudStartService.Services;

namespace CloudStartService
{
    class Program
    {
        static void Main(string[] userInput)
        {
            var setting = ConfigHelper.GetConfiguration(userInput);

            if (setting.Operation == OperationType.Start)
            {
                new CloudService(setting).Start();
            }
            else if (setting.Operation == OperationType.Stop)
            {
                new CloudService(setting).Stop();
            }

            //Console.ReadLine();
        }
    }
}