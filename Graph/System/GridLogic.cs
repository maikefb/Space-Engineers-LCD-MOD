using System;
using System.Collections.Generic;
using System.Linq;
using Graph.Helpers;
using Graph.System.Config;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using IMyCubeGrid = VRage.Game.ModAPI.IMyCubeGrid;
using IMySlimBlock = VRage.Game.ModAPI.IMySlimBlock;
using IngameItem = VRage.Game.ModAPI.Ingame.MyInventoryItem;

namespace Graph.System
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
        List<IMyTerminalBlock> _invBlocks = new List<IMyTerminalBlock>();

        IMyGridTerminalSystem GridTerminalSystem => MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(Grid);

        public Dictionary<MyItemType, double> Components
        {
            get
            {
                if (!_compCache.Any())
                    AggregateItems(GetAllInventories(), _compCache, new[] { "Component" },
                        Array.Empty<MyDefinitionId>());

                return _compCache;
            }
        }

        readonly Dictionary<SearchQueryToken, Dictionary<MyItemType, double>> _queryCache =
            new Dictionary<SearchQueryToken, Dictionary<MyItemType, double>>();

        readonly Dictionary<MyItemType, double> _compCache = new Dictionary<MyItemType, double>();

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

            try
            {
                _blocks.Clear();
                _compCache.Clear();
                _invBlocks.Clear();
                _queryCache.Clear();
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        /// <summary>
        /// Collect items from <see cref="blocks"/> with specific <see cref="categories"/>> or specific <see cref="idWhiteList"/> and add to <see cref="dictionary"/>
        /// </summary>
        /// <param name="blocks">Blocks to collect from</param>
        /// <param name="dictionary">Dictionary to store item Type/Ammount</param>
        /// <param name="categories">Suffix of the item to be collected</param>
        /// <param name="idWhiteList">Items to be collected</param>
        void AggregateItems(List<IMyTerminalBlock> blocks, Dictionary<MyItemType, double> dictionary,
            string[] categories, MyDefinitionId[] idWhiteList)
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

                        var typeIdStr = it.Type.TypeId;

                        var filter = categories.Length > 0 || idWhiteList.Length > 0;

                        if (filter)
                        {
                            var match =
                                categories.Any(category =>
                                    typeIdStr.EndsWith(category, StringComparison.OrdinalIgnoreCase)) ||
                                idWhiteList.Any(definition => definition.Equals(it.Type));

                            if (!match)
                                continue;
                        }


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
            try
            {
                SearchQueryToken queryToken = SearchQueryToken.GetToken(config);
                Dictionary<MyItemType, double> dictionary;
                if (!_queryCache.TryGetValue(queryToken, out dictionary))
                {
                    dictionary = new Dictionary<MyItemType, double>();

                    List<IMyTerminalBlock> blocks =
                        config.SelectedBlocks.Length == 0 && config.SelectedGroups.Length == 0
                            ? GetAllInventories()
                            : new List<IMyTerminalBlock>();

                    blocks.AddRange(config.SelectedBlocks.Select(id => MyAPIGateway.Entities.GetEntityById(id))
                        .Select(entity => entity as IMyTerminalBlock)
                        .Where(block =>
                            block != null && block.HasInventory &&
                            block.CubeGrid.IsInSameLogicalGroupAs(referenceBlock.CubeGrid)));

                    if (config.SelectedGroups.Any())
                    {
                        List<IMyTerminalBlock> blockFromGroups = new List<IMyTerminalBlock>();
                        foreach (var groupName in config.SelectedGroups)
                        {
                            blockFromGroups.Clear();
                            GridTerminalSystem.GetBlockGroupWithName(groupName)?
                                .GetBlocks(blockFromGroups, b => b.HasInventory &&
                                                                 b.GetUserRelationToOwner(referenceBlock.OwnerId)
                                                                 <= MyRelationsBetweenPlayerAndBlock.FactionShare &&
                                                                 !blocks.Contains(b));
                            blocks.AddRange(blockFromGroups);
                        }
                    }

                    AggregateItems(blocks, dictionary, config.SelectedCategories, config.SelectedItems);

                    _queryCache[queryToken] = dictionary;
                }

                return dictionary;
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
                return new Dictionary<MyItemType, double>();
            }
        }

        public List<IMyTerminalBlock> GetAllInventories()
        {
            if (_invBlocks.Any())
                return _invBlocks;

            Grid.GetBlocks(_blocks, a => a.FatBlock?.InventoryCount != 0 && a.FatBlock is IMyTerminalBlock);
            _invBlocks = _blocks.Where(a =>
            {
                var block = a?.FatBlock as IMyTerminalBlock;
                return block != null && block.HasInventory;
            }).Select(a => (IMyTerminalBlock)a.FatBlock).ToList();

            return _invBlocks;
        }
    }
}