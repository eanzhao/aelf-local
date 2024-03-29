using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Client.Service;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Newtonsoft.Json;

namespace AElfChain.Common
{
    public class Node
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("endpoint")] public string Endpoint { get; set; }
        [JsonProperty("account")] public string Account { get; set; }
        [JsonProperty("password")] public string Password { get; set; }
        [JsonIgnore] public string PublicKey { get; set; }
        [JsonIgnore] public bool Status { get; set; }
        [JsonIgnore] public AElfClient ApiClient { get; set; }
    }

    public class BasicAuth
    {
        [JsonProperty("userName")] public string UserName { get; set; }
        [JsonProperty("password")] public string NodePassword { get; set; }
    }

    public class NodesInfo
    {
        [JsonProperty("Nodes")] public List<Node> Nodes { get; set; }
        [JsonProperty("NativeTokenSymbol")] public string NativeTokenSymbol { get; set; }
        [JsonProperty("DefaultPassword")] public string DefaultPassword { get; set; }
        [JsonProperty("BasicAuth")] public BasicAuth BasicAuth {get; set; }
        public void CheckNodesAccount()
        {
            var keyStore = AElfKeyStore.GetKeyStore();
            var accountManager = new AccountManager(keyStore);
            foreach (var node in Nodes)
            {
                //check account exist
                var exist = accountManager.AccountIsExist(node.Account);
                if (!exist)
                {
                    $"Account {node.Account} not exist, please copy account file into folder: {CommonHelper.GetCurrentDataDir()}/keys"
                        .WriteErrorLine();
                    throw new FileNotFoundException();
                }

                //get public key
                var publicKey = accountManager.GetPublicKey(node.Account, node.Password);
                node.PublicKey = publicKey;
            }
        }

        public List<Node> GetMinerNodes(ConsensusContract consensus)
        {
            var miners = consensus.GetCurrentMinersPubkey();
            return Nodes.Where(o => miners.Contains(o.PublicKey)).ToList();
        }
    }

    public static class NodeInfoHelper
    {
        private static NodesInfo _instance;
        private static string _jsonContent;
        private static readonly object LockObj = new object();

        public static string ConfigFile = CommonHelper.MapPath("config/nodes.json");

        public static NodesInfo Config => GetConfigInfo();

        public static void SetConfig(string? name)
        {
            if (!name.Contains(".json"))
                name += ".json";
            _instance = null;
            ConfigFile = CommonHelper.MapPath($"config/{name}");
        }

        public static List<string> GetAccounts()
        {
            return _instance.Nodes.Select(o => o.Account).ToList();
        }

        public static NodesInfo ReadConfigInfo(string configFile)
        {
            var content = File.ReadAllText(configFile);
            return JsonConvert.DeserializeObject<NodesInfo>(content);
        }

        private static NodesInfo GetConfigInfo()
        {
            lock (LockObj)
            {
                if (_instance != null) return _instance;

                _jsonContent = File.ReadAllText(ConfigFile);
                _instance = JsonConvert.DeserializeObject<NodesInfo>(_jsonContent);
            }

            return _instance;
        }
    }
}