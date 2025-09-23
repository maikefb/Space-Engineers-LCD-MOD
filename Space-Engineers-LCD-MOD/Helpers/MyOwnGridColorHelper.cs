using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRageMath;

namespace Space_Engineers_LCD_MOD.Helpers
{
    public class MyOwnGridColorHelper
    {
        Dictionary<IMyCubeGrid, Color?> _colors = new Dictionary<IMyCubeGrid, Color?>();
        int _lastColorIndex;

        public MyOwnGridColorHelper(IMyCubeGrid mainGrid = null)
        {
            _lastColorIndex = 0;
            _colors.Clear();
            if (mainGrid == null)
                return;
            _colors.Add(mainGrid, new Color?());
        }

        public Color? GetGridColor(IMyCubeGrid grid)
        {
            Color? gridColor;
            if (!this._colors.TryGetValue(grid, out gridColor))
            {
                do
                {
                    gridColor = new Vector3(_lastColorIndex++ % 20 / 20f, 0.75f, 1f).HSVtoColor();
                }
                while ((double) gridColor.Value.HueDistance(Color.Red) < 0.03999999910593033 || (double) gridColor.Value.HueDistance(0.65f) < 0.07000000029802322);
                this._colors[grid] = gridColor;
            }
            return gridColor;
        }
    }
}