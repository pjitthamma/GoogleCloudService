using System.Collections.Generic;

namespace CloudStartService.Models
{
    public enum OperationType
    {
        Idle =  -1,
        Stop = 0,
        Start = 1
    }

    public class Configs
    {
        public Configs(OperationType op, string targetMachine, string zone, string project, Dictionary<string, List<string>> hubs)
        {
            Operation = op;
            TargetHub = targetMachine;
            Zone = zone;
            Project = project;
            Hubs = hubs;
        }
        public OperationType Operation { get; private set; }
        public string TargetHub { get; private set; } // only for stop operation
        public string Zone { get; private set; }
        public string Project { get; private set; }
        public Dictionary<string, List<string>> Hubs { get; private set; }  // store name of hub & list of node
    }
}
