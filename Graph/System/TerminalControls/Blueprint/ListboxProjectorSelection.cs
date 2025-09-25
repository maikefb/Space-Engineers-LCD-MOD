using Graph.System.TerminalControls.Generic;
using Sandbox.ModAPI;

namespace Graph.System.TerminalControls.Blueprint
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