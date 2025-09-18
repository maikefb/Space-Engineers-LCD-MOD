using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Space_Engineers_LCD_MOD.Graph.Config;
using Space_Engineers_LCD_MOD.Networking;
using VRage;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Utils;
using VRageMath;


namespace Graph.Data.Scripts.Graph.Sys
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class TerminalSessionComponent : MySessionComponentBase
    {
        MyEasyNetworkManager networkManager;
        
        public override void BeforeStart()
        {
            networkManager = new MyEasyNetworkManager(46541);
            
            networkManager.OnReceivedPacket += OnReceivedPacket;
            MyAPIGateway.TerminalControls.CustomControlGetter += CustomControlGetter;
        }

        void OnReceivedPacket(MyEasyNetworkManager.PacketIn packetRaw)
        {
            if (packetRaw.PacketId == 1)
            {
                var packet = packetRaw.UnWrap<PacketSyncScreenConfig>();
                var block = MyEntities.GetEntityById(packet.BlockId) as IMyFunctionalBlock;
                
                if(block == null)
                    return;
                
                MyTuple<int, ScreenProviderConfig> settings;
                if (ItemCharts.ActiveScreens.TryGetValue(block, out settings))
                {
                    if(settings.Item2.Screens.Count != packet.Config.Screens.Count)
                        return;

                    settings.Item2.Dirty = false;
                    for (var index = 0; index < settings.Item2.Screens.Count; index++) 
                        settings.Item2.Screens[index].CopyFrom(packet.Config.Screens[index]);
                }
            }

        }

        protected override void UnloadData() =>
            MyAPIGateway.TerminalControls.CustomControlGetter -= CustomControlGetter;

        public override void SaveData()
        {
            foreach (var screen in ItemCharts.ActiveScreens)
                ItemCharts.Save(screen.Key, screen.Value.Item2);

            base.SaveData();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            foreach (var screen in ItemCharts.ActiveScreens)
            {
                if(!screen.Value.Item2.Dirty)
                    return;
                
                networkManager.TransmitToServer(new PacketSyncScreenConfig(screen.Key.EntityId, screen.Value.Item2));
            }
        }

        IMyTerminalControlColor _colorPicker;
        IMyTerminalControlColor _colorPickerLcd;
        IMyTerminalControlColor _originalColorPicker;

        void CustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (controls == null)
                return;

            if (block is IMyTextPanel)
                SetupLcdTerminal(block, controls);
            else if (block is IMyTextSurfaceProvider)
                SetupProviderTerminal(block, controls);
        }

        void SetupLcdTerminal(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (_colorPickerLcd == null)
            {
                _colorPickerLcd =
                    MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlColor, IMyTextPanel>(
                        "ItemChartHeaderLcd");
                _colorPickerLcd.Getter = GetterPanelColorPicker;
                _colorPickerLcd.Setter = SetterPanelColorPicker;
                _colorPickerLcd.Visible = VisiblePanelColorPicker;
                _colorPickerLcd.Title = MyStringId.GetOrCompute("BlockPropertyTitle_TextPanelPublicTitle");
            }

            controls.Add(_colorPickerLcd);
        }

        void SetupProviderTerminal(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            var provider = block as IMyTextSurfaceProvider;
            if (_colorPicker == null)
            {
                _originalColorPicker =
                    controls.Find(a => a.Id == "ScriptForegroundColor") as IMyTerminalControlColor;

                _colorPicker =
                    MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlColor, IMyTerminalBlock>(
                        "ItemChartHeaderPanel");
                _colorPicker.Getter = ColorPickerGetter;
                _colorPicker.Setter = ColorPickerSetter;
                _colorPicker.Visible = ColorPickerVisible;
                _colorPicker.Title = MyStringId.GetOrCompute("BlockPropertyTitle_TextPanelPublicTitle");
            }

            if (provider.SurfaceCount > 0)
            {
                var index = controls.FindIndex(p => p.Id == "Script");
                controls.AddOrInsert(_colorPicker, index + 3);
            }
        }

        bool ColorPickerVisible(IMyTerminalBlock b)
        {
            var sf = ((IMyTextSurfaceProvider)b).GetSurface(GetThisSurfaceIndex(b));
            return !string.IsNullOrEmpty(sf?.Script) && sf.Script.Contains("Charts") &&
                   sf.ContentType == ContentType.SCRIPT;
        }

        void ColorPickerSetter(IMyTerminalBlock b, Color c)
        {
            var index = GetThisSurfaceIndex(b);
            if (index == -1) return;
            MyTuple<int, ScreenProviderConfig> settings;
            if (ItemCharts.ActiveScreens.TryGetValue(b, out settings) && settings.Item2.Screens.Count > index)
                settings.Item2.Screens[index].HeaderColor = c;
        }

        Color ColorPickerGetter(IMyTerminalBlock b)
        {
            var index = GetThisSurfaceIndex(b);
            if (index != -1)
            {
                MyTuple<int, ScreenProviderConfig> settings;
                if (ItemCharts.ActiveScreens.TryGetValue(b, out settings) && settings.Item2.Screens.Count > index)
                    return settings.Item2.Screens[index].HeaderColor;
            }

            return Color.White;
        }

        int GetThisSurfaceIndex(IMyTerminalBlock block)
        {
            if (block == null)
                return 0;

            var provider = block as IMyTextSurfaceProvider;

            var original = _originalColorPicker.Getter.Invoke(block);
            _originalColorPicker.Setter.Invoke(block, Color.Transparent);

            for (int i = 0; i < provider.SurfaceCount; i++)
            {
                if (provider.GetSurface(i).ScriptForegroundColor == Color.Transparent)
                {
                    _originalColorPicker.Setter.Invoke(block, original);
                    return i;
                }
            }

            return 0;
        }

        bool VisiblePanelColorPicker(IMyTerminalBlock b)
        {
            var sf = (IMyTextSurface)b;
            return !string.IsNullOrEmpty(sf?.Script) && sf.Script.Contains("Charts") &&
                   sf.ContentType == ContentType.SCRIPT;
        }

        void SetterPanelColorPicker(IMyTerminalBlock b, Color c)
        {
            MyTuple<int, ScreenProviderConfig> settings;
            if (ItemCharts.ActiveScreens.TryGetValue(b, out settings) && settings.Item2.Screens.Count > 0)
                settings.Item2.Screens[0].HeaderColor = c;
        }

        Color GetterPanelColorPicker(IMyTerminalBlock b)
        {
            MyTuple<int, ScreenProviderConfig> settings;
            if (ItemCharts.ActiveScreens.TryGetValue(b, out settings) && settings.Item2.Screens.Count > 0)
                return settings.Item2.Screens[0].HeaderColor;
            return Color.White;
        }
    }
}