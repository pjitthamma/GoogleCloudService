using System;
using System.Linq;
using System.Configuration;
using System.Collections.Generic;
using CloudStartService.Models;
using System.Collections.Specialized;

namespace CloudStartService.Helper
{
    public static class ConfigHelper
    {
        // NEED TO BE SAME KEY AS App.config
        private static readonly Dictionary<string, string> CONFIG_SECTION = new Dictionary<string, string>()
        {
            { "google", "CloudService/GoogleSetting" }
        };
        private static readonly string OPERATIONAL_KEY = "Operational";
        private static readonly string TARGET_KEY = "TargetHub";
        private static readonly string ZONE_KEY = "Zone";
        private static readonly string PROJECT_KEY = "Project";
        private static readonly string N_HUB_KEY = "NumberOfHub";
        private static readonly string HUB_KEY = "Hub";
        private static readonly string NODE_KEY = "NodeList";

        public static Configs GetConfiguration(string[] param)
        {
            var conf = ReadAppConfig(param);
            var configuration = MapConfigObject(conf);

            return configuration;
        }

        private static NameValueCollection ReadAppConfig(string[] param)
        {
            string serviceType = "";
            CONFIG_SECTION.TryGetValue(param[0]?.ToLower() ?? "", out serviceType);
            var conf = ConfigurationManager.GetSection(serviceType) as NameValueCollection;

            // if cannot find configuration, throw exception
            if (conf.Count == 0)
            {
                ThrowException("No configuration found for this provider: " + param);
            }

            return conf;
        }

        private static Configs MapConfigObject(NameValueCollection conf)
        {
            var zone = conf[ZONE_KEY];
            var project = conf[PROJECT_KEY];
            var targetMachine = conf[TARGET_KEY];
            var machineList = BuildMachineList(conf);
            var operational = (OperationType) Enum.Parse(typeof(OperationType), conf[OPERATIONAL_KEY]);

            // if some of config are not in well-formed, throw exception
            if (string.IsNullOrEmpty(zone) ||
                string.IsNullOrEmpty(project) ||
                !machineList.Any() ||
                !Enum.IsDefined(typeof(OperationType), operational) ||
                !IsValidStopConfig(operational, targetMachine, machineList))
            {
                ThrowException("Invalid configuration values \n" +
                                "Operational: " + operational +
                                "\n TargetMachine: " + targetMachine +
                                "\n Zone: " + zone +
                                "\n Project: " + project +
                                "\n MachineList: " + machineList);
            }

            return new Configs(operational, targetMachine, zone, project, machineList);
        }

        private static Dictionary<string, List<string>> BuildMachineList(NameValueCollection conf)
        {
            Dictionary<string, List<string>> machineList = new Dictionary<string, List<string>>();

            int numberOfHub = int.Parse(conf[N_HUB_KEY]);
            for (var i = 1; i <= numberOfHub; i++)
            {
                var hub = conf[HUB_KEY + i];
                var nodeList = conf[NODE_KEY + i].Split(',').ToList();
                machineList.Add(hub, nodeList);
            }
            return machineList;
        }

        private static bool IsValidStopConfig(OperationType op, string targetMachine, Dictionary<string, List<string>> machineList)
        {
            if (op == OperationType.Stop && !machineList.ContainsKey(targetMachine))
            {
                return false;
            }

            return true;
        }

        private static void ThrowException(string message) { throw new ArgumentException(message); }
    }
}
