using System;
using Newtonsoft.Json;
using Google.Apis.Services;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Compute.beta;
using CloudStartService.Models;
using System.Collections.Generic;
using Data = Google.Apis.Compute.beta.Data;
using System.Net.NetworkInformation;
using System.Threading;

namespace CloudStartService.Services
{
    // TODO : if each service has different implementation, please use polymorphism
    public class CloudService
    {
        private const string SCOPE_URL = "https://www.googleapis.com/auth/cloud-platform";

        private readonly Ping _pinger;
        private readonly ComputeService _service;
        private readonly string _zone;
        private readonly string _project;
        private readonly string _targetHub;
        private readonly Dictionary<string, List<string>> _hubs;

        public CloudService(Configs configs)
        {
            var client = new BaseClientService.Initializer() { HttpClientInitializer = InitCredential() };

            // initialize setting
            _pinger = new Ping();
            _service = new ComputeService(client);
            _zone = configs.Zone;
            _project = configs.Project;
            _hubs = configs.Hubs;
            _targetHub = configs.TargetHub;
            Console.WriteLine("Settings: " + JsonConvert.SerializeObject(configs) + "\n");
        }

        private GoogleCredential InitCredential()
        {
            var credential = Task.Run(() => GoogleCredential.GetApplicationDefaultAsync()).Result;
            if (credential.IsCreateScopedRequired)
            {
                credential = credential.CreateScoped(SCOPE_URL);
            }
            return credential;
        }

        public void Start()
        {
            foreach (var hub in _hubs)
            {
                //1) If hub is running, skip
                var hubName = hub.Key;
                var HubIP = string.Empty;
                if (IsInstanceRunning(hubName)) { continue; }

                DisplayTimer(hubName, () =>
                {
                    //2) Run hub
                    StartInstance(hubName);

                    //3) Run all nodes that belong to this hub
                    foreach (var node in hub.Value) { StartInstance(node); }

                    //4) Everything need to be done before running next hub
                    Thread.Sleep(5000);
                    HubIP = WaitUntilHubIsFinised(hubName);
                });

                //5) Set parameter to TC
                SetTeamcityConfig(HubIP, hubName);

                //6) Forcing terminate program
                break;
            }
        }

        public void Stop()
        {
            DisplayTimer(_targetHub, () =>
            {
                //1) Stop hub
                StopInstance(_targetHub);

                //2) Stop all nodes that belong to this hub
                foreach (var node in _hubs[_targetHub]) { StopInstance(node); }
            });
        }

        //private string WaitUntilHubIsFinised(string name)
        //{
        //    int retry = 0;
        //    bool isSuccess = false;
        //    var ipAddress = GetInstance(name).NetworkInterfaces[0].AccessConfigs[0].NatIP;

        //    while (!isSuccess && retry < 5)
        //    {
        //        retry++;
        //        Thread.Sleep(5000);
        //        isSuccess = _pinger.Send(ipAddress).Status == IPStatus.Success;
        //    }

        //    if (!isSuccess) { Console.WriteLine("cannot ping this machine: " + name); }

        //    return ipAddress;
        //}

        private string WaitUntilHubIsFinised(string name)
        {
            int retry = 0;
            bool isSuccess = false;
            var ipAddress = GetInstance(name).NetworkInterfaces[0].AccessConfigs[0].NatIP;

            string url = "http://" + ipAddress + ":80/grid/console";

            while (!isSuccess && retry < 5)
            {
                retry++;
                Thread.Sleep(5000);

                Uri uri = new Uri(url);
                isSuccess = _pinger.Send(uri.Host).Status == IPStatus.Success;
            }

            if (!isSuccess) { Console.WriteLine("cannot ping to Hub console on this machine: " + name); }

            return ipAddress;
        }

        private Data.Instance GetInstance(string name)
        {
            var instanceGet = _service.Instances.Get(_project, _zone, name);
            return instanceGet.Execute();
        }

        private void StartInstance(string name)
        {
            Console.WriteLine("START --> " + name);
            var instanceRequest = _service.Instances.Start(_project, _zone, name);
            instanceRequest.Execute();
        }

        private void StopInstance(string name)
        {
            Console.WriteLine("STOP --> " + name);
            var instanceRequest = _service.Instances.Stop(_project, _zone, name);
            instanceRequest.Execute();
        }

        private bool IsInstanceRunning(string name)
        {
            return GetInstance(name).Status == "RUNNING";
        }

        private void DisplayTimer(string name, Action codeBlock)
        {
            Console.WriteLine("init opteration: " + DateTime.Now);
            codeBlock.Invoke();
            Console.WriteLine("finish: " + DateTime.Now);
        }

        private void SetTeamcityConfig(string hubIP, string hubName)
        {
            //pass hub url to TC
            Console.WriteLine("##teamcity[setParameter name='CloudIPAddress' value='" + hubIP + "']");
            //pass hub name for stop service
            Console.WriteLine("##teamcity[setParameter name='CloudHubName' value='"+ hubName +"']");
        }
    }
}
