using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.Utils;
using IMyCubeGrid = VRage.Game.ModAPI.IMyCubeGrid;
using IMySlimBlock = VRage.Game.ModAPI.IMySlimBlock;
using IngameItem = VRage.Game.ModAPI.Ingame.MyInventoryItem;

namespace Graph.Data.Scripts.Graph.Sys
{
    /// <summary>
    /// Logic attached to <see cref="Grid"/>
    /// </summary>
    public class GridLogic
    {
        const int DELAY = 600; // 120 ticks means 2 seconds delay
        long _clock;

        public readonly IMyCubeGrid Grid;
        List<IMySlimBlock> _blocks = new List<IMySlimBlock>();
        List<IMyTerminalBlock> _invBlocks = new List<IMyTerminalBlock>();

        IMyGridTerminalSystem GridTerminalSystem => MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(Grid);

        public Dictionary<MyItemType, double> // Dictionary for Specific Category of Items
            Components = new Dictionary<MyItemType, double>(),
            Ingots = new Dictionary<MyItemType, double>(),
            Ores = new Dictionary<MyItemType, double>(),
            Ammo = new Dictionary<MyItemType, double>(),
            Consumables = new Dictionary<MyItemType, double>(),
            Seeds = new Dictionary<MyItemType, double>();

        public Dictionary<SearchQuery, Dictionary<MyItemType, double>> Cache =
            new Dictionary<SearchQuery, Dictionary<MyItemType, double>>();

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

            Cache.Clear();
            _blocks.Clear();

            Grid.GetBlocks(_blocks, a => a.FatBlock?.InventoryCount != 0 && a.FatBlock is IMyTerminalBlock);

            _invBlocks.Clear();
            _invBlocks.AddRange(_blocks.Where(a =>
            {
                var block = a?.FatBlock as IMyTerminalBlock;
                return block != null && block.HasInventory;
            }).Select(a => (IMyTerminalBlock)a.FatBlock));

            AggregateByType(_invBlocks, Components, "Component");
            AggregateByType(_invBlocks, Ingots, "Ingot");
            AggregateByType(_invBlocks, Ores, "Ore");
            AggregateByType(_invBlocks, Ammo, "AmmoMagazine");
            AggregateByType(_invBlocks, Consumables, "ConsumableItem");
            AggregateByType(_invBlocks, Seeds, "SeedItem");
        }

        /// <summary>
        /// Collect items from <see cref="blocks"/> with specific <see cref="suffix"/>> and add to <see cref="dictionary"/>
        /// </summary>
        /// <param name="blocks">Blocks to collect from</param>
        /// <param name="dictionary">Dictionary to store item Type/Ammount</param>
        /// <param name="suffix">Suffix of the item to be collected</param>
        void AggregateByType(List<IMyTerminalBlock> blocks, Dictionary<MyItemType, double> dictionary, string suffix)
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

                        MyItemType type = it.Type;

                        double amount = (double)it.Amount;
                        if (amount <= 0) continue;

                        double acc;
                        if (dictionary.TryGetValue(type, out acc)) dictionary[type] = acc + amount;
                        else dictionary[type] = amount;
                    }
                }
            }
        }

        public Dictionary<MyItemType, double> GetItems(ScreenConfig config, IMyTerminalBlock referenceBlock)
        {
            Dictionary<MyItemType, double> dictionary;

            try
            {
                SearchQuery query;
                if (!config.SelectedBlocks.Any() && !config.SelectedGroups.Any() && !config.SelectedItems.Any())
                    query = SearchQuery.Empty;
                else
                    query = new SearchQuery(config.SelectedBlocks, config.SelectedItems, config.SelectedGroups);

                if (!Cache.TryGetValue(query, out dictionary))
                {
                    dictionary = new Dictionary<MyItemType, double>();

                    List<IMyTerminalBlock> blocks =
                        config.SelectedBlocks.Length == 0 && config.SelectedGroups.Length == 0
                            ? _invBlocks
                            : new List<IMyTerminalBlock>();

                    foreach (var groupName in config.SelectedGroups)
                    {
                        GridTerminalSystem.GetBlockGroupWithName(groupName)?
                            .GetBlocks(blocks, b => b.HasInventory &&
                                                    b.GetUserRelationToOwner(referenceBlock.OwnerId)
                                                    <= MyRelationsBetweenPlayerAndBlock.FactionShare &&
                                                    !blocks.Contains(b));
                    }
                    
                    blocks.AddRange(config.SelectedBlocks.Select(id => MyAPIGateway.Entities.GetEntityById(id))
                        .Select(entity => entity as IMyTerminalBlock)
                        .Where(block => block != null && block.HasInventory && block.CubeGrid.IsInSameLogicalGroupAs(referenceBlock.CubeGrid)));

                    AggregateByType(blocks, dictionary, "");

                    Cache[query] = dictionary;
                }

                return dictionary;
            }
            catch (Exception ex)
            {
                MyLog.Default.Log(MyLogSeverity.Error, ex.ToString());
                MyAPIGateway.Utilities.ShowNotification("ERROR on updating LCD, Check log!");
                return new Dictionary<MyItemType, double>();
            }
        }

        public struct SearchQuery : IEquatable<SearchQuery>
        {
            public static readonly SearchQuery Empty = new SearchQuery();

            public long[] Storages;
            public string[] Groups;
            public string[] Names;

            public SearchQuery(long[] storages, string[] names, string[] groups)
            {
                Storages = storages;
                Names = names;
                Groups = groups;
            }

            public bool Equals(SearchQuery other)
            {
                return Equals(Storages, other.Storages) && Equals(Groups, other.Groups) && Equals(Names, other.Names);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is SearchQuery && Equals((SearchQuery)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (Storages != null ? Storages.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Groups != null ? Groups.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Names != null ? Names.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }
    }
}