using System;
using System.Collections.Generic;
using System.Linq;
using Graph.Charts;
using Graph.Helpers;
using Graph.Networking;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.Components;

namespace Graph.System
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class TerminalSessionComponent : MySessionComponentBase
    {
    }
}