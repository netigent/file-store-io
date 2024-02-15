using Netigent.Utils.FileStoreIO.Clients;
using Netigent.Utils.FileStoreIO.Enums;
using System;
using System.Linq;
using System.Reflection;

namespace Netigent.Utils.FileStoreIO.Models
{
    public class StoreProviderDef
    {
        public FileStorageProvider StoreType =>
            Config.StoreType;

        public int StoreTypeId
            => (int)Config.StoreType;

        public bool IsAvailable =>
            StartupState.Success;

        public string ErrorMessage =>
            string.Join("; ", StartupState?.Messages) ?? string.Empty;

        public IClient? GetClient()
        {
            if (!IsAvailable)
            {
                // Get all classes that implement IClient
                var clientTypes = from t in Assembly.GetExecutingAssembly().GetTypes()
                                  where t.GetInterfaces().Contains(typeof(IClient))
                                           && t.GetConstructor(Type.EmptyTypes) != null
                                  select (Activator.CreateInstance(t)) as IClient;

                // Loop through each until we find what we need
                foreach (var client in clientTypes)
                {
                    if (client.ProviderType == StoreType)
                    {
                        Client = client;
                        break;
                    }
                }

                if (Client != null)
                {
                    StartupState = Client.Init(Config, MaxVersions, AppCodePrefix);
                }
            }

            if (IsAvailable)
            {
                return Client;
            }

            return null;
        }

        public IConfig Config { private get; set; }

        public StoreProviderDef() { }

        public StoreProviderDef(IConfig config, int maxVersions, string appCodePrefix)
        {
            Config = config;
            MaxVersions = maxVersions;
            AppCodePrefix = appCodePrefix ?? string.Empty;
            StartupState = new ResultModel { Success = false, Messages = null };
        }

        // Internal Items
        private IClient? Client { get; set; }

        private ResultModel StartupState { get; set; } = new ResultModel { Success = false, Messages = null };

        private readonly int MaxVersions;

        private readonly string AppCodePrefix;

    }

}
