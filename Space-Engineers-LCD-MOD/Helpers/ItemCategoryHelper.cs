using VRage;
using VRage.Utils;

namespace Space_Engineers_LCD_MOD.Helpers
{
    public class ItemCategoryHelper
    {
        public static string[] Groups = new[] { "AmmoMagazine", "Component", "PhysicalGun", "Ingot", "Ore", "ConsumableItem", "SeedItem" };
        
        public static string GetGroupName(string groupName)
        {
            switch (groupName)
            {
                case "AmmoMagazine":
                    return MyTexts.GetString(MyStringId.GetOrCompute("DisplayName_ConvSorterTypes_Ammo"));
                case "Component":
                    return MyTexts.GetString(MyStringId.GetOrCompute("DisplayName_ConvSorterTypes_Component"));
                case "PhysicalGun":
                    return MyTexts.GetString(MyStringId.GetOrCompute("DisplayName_ConvSorterTypes_HandTool"));    
                case "Ingot":
                    return MyTexts.GetString(MyStringId.GetOrCompute("DisplayName_ConvSorterTypes_Ingot"));
                case "Ore":
                    return MyTexts.GetString(MyStringId.GetOrCompute("DisplayName_ConvSorterTypes_Ore"));
                case "ConsumableItem":
                    return $"*{MyTexts.GetString(MyStringId.GetOrCompute("DisplayName_BlueprintClass_Consumables")).ToLower()}*";
                case "SeedItem":
                    return $"*{MyTexts.GetString(MyStringId.GetOrCompute("DisplayName_BlueprintClass_GardenItems")).ToLower()}*";
            }

            return groupName;
        }
        
        public static string GetGroupDisplayName(string groupName)
        {
            switch (groupName)
            {
                case "AmmoMagazine":
                    return MyTexts.GetString(MyStringId.GetOrCompute("DisplayName_BlueprintClass_Ammo"));
                case "Component":
                    return MyTexts.GetString(MyStringId.GetOrCompute("DisplayName_BlueprintClass_Components"));
                case "PhysicalGun":
                    return MyTexts.GetString(MyStringId.GetOrCompute("DisplayName_BlueprintClass_Tools"));    
                case "Ingot":
                    return MyTexts.GetString(MyStringId.GetOrCompute("Description_BlueprintClass_Ingots"));
                case "Ore":
                    return MyTexts.GetString(MyStringId.GetOrCompute("RadialMenuGroupTitle_VoxelOres"));
                case "ConsumableItem":
                    return MyTexts.GetString(MyStringId.GetOrCompute("DisplayName_BlueprintClass_Consumables"));
                case "SeedItem":
                    return MyTexts.GetString(MyStringId.GetOrCompute("DisplayName_BlueprintClass_GardenItems"));
            }

            return groupName;
        }
    }
}