using System;
using System.Linq;
using Space_Engineers_LCD_MOD.Graph.Config;
using VRage.Game;

namespace Space_Engineers_LCD_MOD.Graph.Sys
{
    /// <summary>
    /// Token for Caching Search Query
    /// </summary>
    public struct SearchQueryToken : IEquatable<SearchQueryToken>
    {
        static readonly SearchQueryToken Empty = new SearchQueryToken();

        readonly long[] _storages;
        readonly string[] _groups;
        readonly MyDefinitionId[] _items;
        readonly string[] _categories;

        readonly int _storagesHash;
        readonly int _groupsHash;
        readonly int _itemsHash;
        readonly int _categoriesHash;

        SearchQueryToken(ScreenConfig config)
        {
            _storages = config.SelectedBlocks;
            _groups = config.SelectedGroups;
            _items = config.SelectedItems;
            _categories = config.SelectedCategories;
            _storagesHash = ComputeArrayHash(_storages);
            _groupsHash = ComputeArrayHash(_groups);
            _itemsHash = ComputeArrayHash(_items);
            _categoriesHash = ComputeArrayHash(_categories);
        }

        static int ComputeArrayHash<T>(T[] array)
        {
            if (array == null) return 0;
            unchecked
            {
                int hash = 17;
                foreach (var item in array)
                    hash = hash * 31 + (item?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public bool Equals(SearchQueryToken other)
        {
            if (!FastEquality(other))
                return false;

            return SequenceEqualSafe(_storages, other._storages)
                   && SequenceEqualSafe(_groups, other._groups)
                   && SequenceEqualSafe(_items, other._items)
                   && SequenceEqualSafe(_categories, other._categories);
        }

        bool FastEquality(SearchQueryToken other)
        {
            return _storagesHash == other._storagesHash
                   && _groupsHash == other._groupsHash
                   && _itemsHash == other._itemsHash
                   && _categoriesHash == other._categoriesHash;
        }
    
        static bool SequenceEqualSafe<T>(T[] a, T[] b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            return a.SequenceEqual(b);
        }

        public override bool Equals(object obj) => obj is SearchQueryToken && Equals((SearchQueryToken)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = _storagesHash;
                hash = (hash * 397) ^ _groupsHash;
                hash = (hash * 397) ^ _itemsHash;
                hash = (hash * 397) ^ _categoriesHash;
                return hash;
            }
        }

        public static SearchQueryToken GetToken(ScreenConfig config)
        {
            if (!config.SelectedBlocks.Any()
                && !config.SelectedGroups.Any()
                && !config.SelectedItems.Any()
                && !config.SelectedCategories.Any())
                return Empty;

            return new SearchQueryToken(config);
        }
    }
}