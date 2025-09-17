using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using IngameItem = VRage.Game.ModAPI.Ingame.MyInventoryItem;

namespace Graph.Data.Scripts.Graph.Sys
{
    /// <summary>
    /// Logic attached to <see cref="Grid"/>
    /// </summary>
    public class GridLogic
    {
        const int DELAY = 120; // 120 ticks means 2 seconds delay
        long _clock;

        public readonly IMyCubeGrid Grid;
        List<IMySlimBlock> _blocks = new List<IMySlimBlock>();

        public Dictionary<string, double> // Dictionary for Specific Category of Items
            Components = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase),
            Ingots = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase),
            Ores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase),
            Ammo = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase),
            Consumables = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase),
            Seeds = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Logic attached to <see cref="grid"/>
        /// </summary>
        /// <param name="grid"></param>
        public GridLogic(IMyCubeGrid grid)
        {
            Grid = grid;
            _clock = new Random().Next(DELAY);
            // Initial Randomization so not every single grid ticks on the same time
        }

        /// <summary>
        /// Update Grid component after specific <see cref="DELAY"/>, Called every tick
        /// </summary>
        public void Update()
        {
            _clock++;
            if (_clock % DELAY != 0)
                return; // skip update by {DELAY} ticks

            _blocks.Clear();
            Grid.GetBlocks(_blocks, a => a.FatBlock?.InventoryCount != 0 && a.FatBlock is IMyTerminalBlock);
            var invBlocks = _blocks.Select(a => a.FatBlock as IMyTerminalBlock).ToList();

            AggregateByType(invBlocks, Components, "Component");
            AggregateByType(invBlocks, Ingots, "Ingot");
            AggregateByType(invBlocks, Ores, "Ore");
            AggregateByType(invBlocks, Ammo, "AmmoMagazine");
            AggregateByType(invBlocks, Consumables, "ConsumableItem");
            AggregateByType(invBlocks, Seeds, "SeedItem");
        }

        /// <summary>
        /// Collect items from <see cref="blocks"/> with specific <see cref="suffix"/>> and add to <see cref="dictionary"/>
        /// </summary>
        /// <param name="blocks">Blocks to collect from</param>
        /// <param name="dictionary">Dictionary to store item Type/Ammount</param>
        /// <param name="suffix">Suffix of the item to be collected</param>
        void AggregateByType(List<IMyTerminalBlock> blocks, Dictionary<string, double> dictionary, string suffix)
        {
            dictionary.Clear();

            for (int b = 0; b < blocks.Count; b++)
            {
                var tb = blocks[b];

                if (!tb.HasInventory) // should *NOT* happen
                    continue;

                int invCount = tb.InventoryCount;
                for (int i = 0; i < invCount; i++)
                {
                    var inv = tb.GetInventory(i);
                    if (inv == null) continue;

                    var items = new List<IngameItem>();
                    inv.GetItems(items);
                    for (int k = 0; k < items.Count; k++)
                    {
                        var it = items[k];

                        var typeIdStr = it.Type.TypeId != null ? it.Type.TypeId.ToString() : "";
                        if (!typeIdStr.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) continue;

                        string subtype = it.Type.SubtypeId ?? "";
                        string display = subtype;

                        double amount = (double)it.Amount;
                        if (amount <= 0) continue;

                        double acc;
                        if (dictionary.TryGetValue(display, out acc)) dictionary[display] = acc + amount;
                        else dictionary[display] = amount;
                    }
                }
            }
        }
    }
}