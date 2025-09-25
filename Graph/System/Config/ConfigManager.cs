using System;
using System.Linq;
using Graph.Charts;
using Graph.Helpers;
using Graph.Networking;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace Graph.System.Config
{
    /// <summary>
    /// Ensures settings is correctly Saved/Loaded and Synced between clients
    /// </summary>
    public static class ConfigManager
    {
        public static MyEasyNetworkManager NetworkManager;

        public static void Init()
        {
            MyLog.Default.Log(MyLogSeverity.Info, $"{nameof(Graph)}: Setting up Network Manager using port {Constants.PORT}");
            NetworkManager = new MyEasyNetworkManager(Constants.PORT);
            NetworkManager.Register();
        }

        public static void Close()
        {
            MyLog.Default.Log(MyLogSeverity.Info, $"{nameof(Graph)}: Closing Network Manager");
            NetworkManager?.UnRegister();
            NetworkManager?.Clear();
            NetworkManager = null;
        }

        public static void SaveAll()
        {
            try
            {
                foreach (var screen in ChartBase.Instances)
                    Save((IMyEntity)screen.Block, screen.ProviderConfig);
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, typeof(ConfigManager));
            }
        }

        public static void SyncAll()
        {
            try
            {
                foreach (var screen in ChartBase.Instances)
                    Sync((IMyEntity)screen.Block, screen.ProviderConfig);
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, typeof(ConfigManager));
            }
        }

        public static void Save(IMyEntity storageEntity, ScreenProviderConfig providerConfig)
        {
            try
            {
                if (storageEntity.Storage == null)
                    return;

                var base64 = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(providerConfig));

                if (string.IsNullOrEmpty(base64))
                    throw new Exception("Invalid storage config");

                storageEntity.Storage[Constants.STORAGE_GUID] = base64;
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, typeof(ConfigManager));
            }
        }

        public static void Sync(IMyEntity storageEntity, ScreenProviderConfig providerConfig)
        {
            GetAppForBlock(storageEntity as IMyTerminalBlock).RequestRedraw();
            NetworkManager.TransmitToServer(new PacketSyncScreenConfig(storageEntity.EntityId, providerConfig));
            Save(storageEntity, providerConfig);
        }

        public static void Sync(IMyTerminalBlock storageEntity) =>
            Sync(storageEntity, GetConfigForBlock(storageEntity));

        public static void LoadSettings(IMyCubeBlock block, int index, ref ScreenProviderConfig provider,
            out ScreenConfig screen)
        {
            try
            {
                provider = GetConfigForBlock((IMyTerminalBlock)block);
                if (provider != null && provider.Screens.Count > index)
                {
                    screen = provider.Screens[index];
                    return;
                }

                var storageEntity = (IMyEntity)block;

                if (storageEntity.Storage == null)
                    storageEntity.Storage = new MyModStorageComponent();

                provider = TryLoad(block);
                if (provider != null)
                {
                    screen = provider.Screens[index];
                    return;
                }

                CreateSettings(block, index, out provider, out screen);
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification($"Fail to Load Settings for block {block.DisplayNameText}\n{e.Message}");
                ErrorHandlerHelper.LogError(e, typeof(ConfigManager));
                CreateSettings(block, index, out provider, out screen);
            }
        }

        public static ScreenProviderConfig TryLoad(IMyCubeBlock block)
        {
            string value;
            if (block.Storage.TryGetValue(Constants.STORAGE_GUID, out value) && !string.IsNullOrEmpty(value))
            {
                var provider =
                    MyAPIGateway.Utilities.SerializeFromBinary<ScreenProviderConfig>(Convert.FromBase64String(value));

                if (provider.ParentGrid != block.CubeGrid.EntityId)
                    provider.ParentGrid = block.CubeGrid.EntityId;

                return provider;
            }

            return null;
        }

        public static void CreateSettings(IMyCubeBlock block, int index, out ScreenProviderConfig provider, out ScreenConfig screen)
        {
            provider = CreateSettings(block);
            Save(block, provider);
            screen = provider.Screens[index];
        }

        public static ScreenProviderConfig CreateSettings(IMyCubeBlock block) => new ScreenProviderConfig(block is IMyTextPanel ? 1 : ((IMyTextSurfaceProvider)block).SurfaceCount, block as IMyTerminalBlock);

        public static ChartBase GetAppForBlock(IMyTerminalBlock block) =>
            ChartBase.Instances.FirstOrDefault(a => a.Block.Equals(block));

        public static ScreenProviderConfig GetConfigForBlock(IMyTerminalBlock block) =>
            GetAppForBlock(block)?.ProviderConfig;

        public static ScreenConfig GetConfigForScreen(IMyTerminalBlock block, int index)
        {
            var settings = GetConfigForBlock(block);

            if (settings?.Screens == null
                || settings.Screens.Count <= index
                || index < 0)
                return null;

            return settings.Screens[index];
        }

        public static ScreenConfig GetConfigForCurrentScreen(IMyTerminalBlock block) =>
            GetConfigForScreen(block, GetThisSurfaceIndex(block));

        public static int GetThisSurfaceIndex(IMyTerminalBlock block)
        {
            var multiTextPanel = block.Components.Get<MyMultiTextPanelComponent>();
            return multiTextPanel?.SelectedPanelIndex ?? 0;
        }
    }
}