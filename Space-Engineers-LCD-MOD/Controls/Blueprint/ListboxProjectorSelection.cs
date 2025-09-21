using Sandbox.ModAPI;
using Space_Engineers_LCD_MOD.Controls.Generic;

namespace Space_Engineers_LCD_MOD.Controls.Blueprint
{
    public class ListboxProjectorSelection : ListboxSingleBlockSelection<IMyProjector>
    {
        protected override string[] VisibleForScripts { get; } = { "BlueprintDiagram" };

        public ListboxProjectorSelection()
        {
            CreateListbox("ProjectorSelection", "DisplayName_BlockGroup_Projectors");
        }
    }
}