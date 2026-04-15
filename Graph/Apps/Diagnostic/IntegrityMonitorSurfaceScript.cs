using System;
using System.Collections.Generic;
using System.Linq;
using Graph.Apps.Abstract;
using Graph.Extensions;
using Graph.Helpers;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using IMyCubeBlock = VRage.Game.ModAPI.IMyCubeBlock;
using IMyLargeTurretBase = Sandbox.ModAPI.Ingame.IMyLargeTurretBase;

namespace Graph.Apps.Diagnostic
{
    [MyTextSurfaceScript(ID, TITLE)]
    public class IntegrityMonitorSurfaceScript : SurfaceScriptBase
    {
        const int MAP_BUILD_BATCH_SIZE = 256;

        static readonly Dictionary<GridPairKey, CachedGridMapEntry> GridMapCache = new Dictionary<GridPairKey, CachedGridMapEntry>();

        public const string ID = "PreviewCharts";
        public const string TITLE = "BroadcastStatus_IsPreviewGrid";

        public virtual string Title => LocHelper.GetLoc(TITLE);

        IMyProjector _projector;

        public IntegrityMonitorSurfaceScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface,
            block, size)
        {
        }

        public override void Run()
        {
            base.Run();

            if (Config == null)
                return;

            if (Config.ReferenceBlock == 0)
                return;

            IMyCubeGrid grid = Block?.CubeGrid as IMyCubeGrid;
            FindProjector(grid, ref _projector);

            if (_projector?.ProjectedGrid == null || _projector.Closed)
            {
                using (var frame = Surface.DrawFrame())
                {
                    var sprites = new List<MySprite>();
                    DrawTitle(sprites);
                    sprites.Add(MakeText((IMyTextSurface)Surface, LocHelper.Empty, ViewBox.Center, Scale,
                        TextAlignment.CENTER));
                    DrawFooter(sprites);
                    frame.AddRange(sprites);
                }

                return;
            }

            try
            {
                var map3D = GetOrUpdate3DMap(_projector);

                DepthMap2D depth = BuildDepthMap(map3D.Cells, map3D.DamagedCells, map3D.CellTypes, (View)Config.DisplayInternal);

                using (var frame = Surface.DrawFrame())
                {
                    var sprites = new List<MySprite>();
                    DrawTitle(sprites);
                    DrawDepthMap(sprites, depth, MathHelper.ToRadians(Config.Rotation), Config.Scale);
                    DrawFooter(sprites);
                    frame.AddRange(sprites);
                }
            }
            catch (Exception e)
            {
            }
        }

        static CachedGridMap GetOrUpdate3DMap(IMyProjector projector)
        {
            if (projector == null || projector.Closed || projector.ProjectedGrid == null || projector.CubeGrid == null)
                return CachedGridMap.Empty;

            var key = new GridPairKey(projector.ProjectedGrid.EntityId, projector.CubeGrid.EntityId);
            CachedGridMapEntry entry;
            if (!GridMapCache.TryGetValue(key, out entry))
            {
                entry = new CachedGridMapEntry();
                GridMapCache[key] = entry;
            }

            if (entry.Updater == null)
                entry.Updater = Generate3DMapCoroutine(projector, entry).GetEnumerator();

            // Advance one chunk per Run() so map building happens progressively.
            bool hasMore;
            try
            {
                hasMore = entry.Updater.MoveNext();
            }
            catch
            {
                hasMore = false;
            }

            if (!hasMore)
                entry.Updater = null;

            return entry.Current;
        }

        static IEnumerable<bool> Generate3DMapCoroutine(IMyProjector projector, CachedGridMapEntry entry)
        {
            if (projector == null || projector.Closed || projector.ProjectedGrid == null || projector.CubeGrid == null)
                yield break;

            var cells = new List<Vector3I>();
            var damagedCells = new List<Vector3I>();
            var cellTypes = new Dictionary<Vector3I, CellKind>();

            Vector3I a = projector.ProjectedGrid.Min;
            Vector3I b = projector.ProjectedGrid.Max;

            int startX = Math.Min(a.X, b.X);
            int startY = Math.Min(a.Y, b.Y);
            int startZ = Math.Min(a.Z, b.Z);

            int endX = Math.Max(a.X, b.X);
            int endY = Math.Max(a.Y, b.Y);
            int endZ = Math.Max(a.Z, b.Z);

            int processed = 0;

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    for (int z = startZ; z <= endZ; z++)
                    {
                        var projectedPos = new Vector3I(x, y, z);

                        bool projectedExists = projector.ProjectedGrid.CubeExists(projectedPos);
                        if (projectedExists)
                        {
                            bool isBroken;
                            Vector3I realPos =
                                projector.CubeGrid.WorldToGridInteger(
                                    projector.ProjectedGrid.GridIntegerToWorld(projectedPos));
                            bool realExists = projector.CubeGrid.CubeExists(realPos);

                            if (!realExists)
                            {
                                isBroken = true;
                            }
                            else
                            {
                                var reference = projector.ProjectedGrid.GetCubeBlock(projectedPos);
                                var real = projector.CubeGrid.GetCubeBlock(realPos);

                                isBroken = reference.GetType() != real.GetType();

                                if (reference.FatBlock != null)
                                    cellTypes[realPos] = GetCellKind(reference.FatBlock.GetType());
                            }

                            cells.Add(realPos);
                            if (isBroken)
                                damagedCells.Add(realPos);
                        }

                        processed++;
                        if (processed >= MAP_BUILD_BATCH_SIZE)
                        {
                            processed = 0;
                            yield return true;
                        }
                    }
                }
            }

            entry.Current = new CachedGridMap(cells, damagedCells, cellTypes);
        }

        void DrawDepthMap(List<MySprite> sprites, DepthMap2D depthMap, float rotation = 0f, float scale = 1f)
        {
            if (depthMap.Width <= 0 || depthMap.Height <= 0)
            {
                var center = new Vector2(ViewBox.Center.X, (CaretY + ViewBox.Bottom - FooterHeight) * 0.5f);
                sprites.Add(MakeText((IMyTextSurface)Surface, LocHelper.Empty, center, Scale, TextAlignment.CENTER));
                return;
            }

            float margin = ViewBox.Width * Margin;
            float contentStart = ViewBox.X + margin;
            float contentEnd = ViewBox.X + ViewBox.Width - margin;
            float contentTop = CaretY;
            float contentBottom = ViewBox.Bottom - FooterHeight;

            float contentWidth = Math.Max(0f, contentEnd - contentStart);
            float contentHeight = Math.Max(0f, contentBottom - contentTop);
            if (contentWidth <= 0f || contentHeight <= 0f)
                return;

            float baseCellSize = Math.Min(contentWidth / depthMap.Width, contentHeight / depthMap.Height);
            float baseMapWidth = depthMap.Width * baseCellSize;
            float baseMapHeight = depthMap.Height * baseCellSize;

            float mapScale = Math.Max(0.05f, scale);
            float mapWidth = baseMapWidth * mapScale;
            float mapHeight = baseMapHeight * mapScale;
            float mapX = contentStart + (contentWidth - mapWidth) * 0.5f;
            float mapY = contentTop + (contentHeight - mapHeight) * 0.5f;
            var mapCenter = new Vector2(mapX + mapWidth * 0.5f, mapY + mapHeight * 0.5f);
            float cellWidth = mapWidth / depthMap.Width;
            float cellHeight = mapHeight / depthMap.Height;

            for (int x = 0; x < depthMap.Width; x++)
            {
                for (int y = 0; y < depthMap.Height; y++)
                {
                    var percent = depthMap.Percent[x, y];
                    if (!percent.HasValue)
                        continue;

                    float shade = MathHelper.Clamp(percent.Value / 100f, 0f, 1f);
                    float value = (255 - (60f + shade * 195f)) / 255;

                    var color =
                        (depthMap.Damaged[x, y] ? Color.Red : GetColorForType(depthMap.CellType[x, y])).MulValue(value);
                    float drawX = mapX + x * cellWidth;
                    float drawY = mapY + (depthMap.Height - 1 - y) * cellHeight;
                    var cellCenter = new Vector2(drawX + cellWidth * 0.5f, drawY + cellHeight * 0.5f);

                    sprites.Add(new MySprite
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = RotateAround(cellCenter, mapCenter, rotation),
                        Size = new Vector2(cellWidth * 1.02f, cellHeight * 1.02f),
                        Color = color,
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = rotation
                    });
                }
            }
        }

        static Vector2 RotateAround(Vector2 point, Vector2 origin, float rotation)
        {
            if (Math.Abs(rotation) < 0.0001f)
                return point;

            float sin = (float)Math.Sin(rotation);
            float cos = (float)Math.Cos(rotation);
            var delta = point - origin;

            return new Vector2(
                delta.X * cos - delta.Y * sin,
                delta.X * sin + delta.Y * cos
            ) + origin;
        }

        private static CellKind GetCellKind(Type topType)
        {
            if (topType == null)
                return CellKind.None;

            if (MyAPIGateway.Reflection.IsAssignableFrom(typeof(IMyThrust), topType))
                return CellKind.Thruster;

            if (MyAPIGateway.Reflection.IsAssignableFrom(typeof(IMyPowerProducer), topType))
                return CellKind.Power;

            if (MyAPIGateway.Reflection.IsAssignableFrom(typeof(IMyGyro), topType))
                return CellKind.Gyro;

            if (MyAPIGateway.Reflection.IsAssignableFrom(typeof(IMyWarhead), topType) ||
                MyAPIGateway.Reflection.IsAssignableFrom(typeof(IMyUserControllableGun), topType))
                return CellKind.Weapons;

            if (MyAPIGateway.Reflection.IsAssignableFrom(typeof(IMyGravityGenerator), topType) ||
                MyAPIGateway.Reflection.IsAssignableFrom(typeof(IMyArtificialMassBlock), topType))
                return CellKind.Gravity;

            if (MyAPIGateway.Reflection.IsAssignableFrom(typeof(IMyProductionBlock), topType))
                return CellKind.Production;

            return CellKind.None;
        }

        Color GetColorForType(CellKind topType)
        {
            Color color;
            if (Colors.TryGetValue(topType, out color))
                return color;

            return Color.Gray;
        }

        Dictionary<CellKind, Color> Colors = new Dictionary<CellKind, Color>()
        {
            { CellKind.Thruster, new Color(0, 0, 255, 127) },
            { CellKind.Power, new Color(0, 255, 0, 127) },
            { CellKind.Gyro, new Color(255, 255, 0, 127) },
            { CellKind.Weapons, new Color(255, 127, 0, 127) },
            { CellKind.Gravity, new Color(127, 0, 127, 127) },
            { CellKind.Production, new Color(0, 127, 127, 127) }
        };


        private static DepthMap2D BuildDepthMap(
            IEnumerable<Vector3I> cells,
            IEnumerable<Vector3I> damagedCells,
            IReadOnlyDictionary<Vector3I, CellKind> cellKinds,
            View view)
        {
            var axes = GetAxes(view);
            var damagedSet = new HashSet<Vector3I>(damagedCells);

            var points = cells
                .Select(p =>
                {
                    CellKind kind;
                    if (!cellKinds.TryGetValue(p, out kind))
                        kind = CellKind.None;

                    return new
                    {
                        Pos = p,
                        U = axes.U(p),
                        V = axes.V(p),
                        D = axes.D(p),
                        Damaged = damagedSet.Contains(p),
                        Kind = kind
                    };
                })
                .ToList();

            if (points.Count == 0)
            {
                return new DepthMap2D
                {
                    Percent = new float?[0, 0],
                    Damaged = new bool[0, 0],
                    CellType = new CellKind[0, 0],
                    Width = 0,
                    Height = 0,
                    UMin = 0,
                    VMin = 0
                };
            }

            int uMin = points.Min(p => p.U);
            int uMax = points.Max(p => p.U);
            int vMin = points.Min(p => p.V);
            int vMax = points.Max(p => p.V);
            int dMin = points.Min(p => p.D);
            int dMax = points.Max(p => p.D);

            int width = uMax - uMin + 1;
            int height = vMax - vMin + 1;

            float?[,] percent = new float?[width, height];
            bool[,] damaged = new bool[width, height];
            CellKind[,] topKind = new CellKind[width, height];

            int[,] bestDepth = new int[width, height];
            bool[,] filled = new bool[width, height];

            int[,] bestKindDepth = new int[width, height];
            bool[,] kindFilled = new bool[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bestDepth[x, y] = int.MaxValue;
                    bestKindDepth[x, y] = int.MaxValue;
                    topKind[x, y] = CellKind.None;
                }
            }

            foreach (var p in points)
            {
                int x = p.U - uMin;
                int y = p.V - vMin;

                // Damaged scans the whole column
                if (p.Damaged)
                    damaged[x, y] = true;

                // Front-most cell overall for Percent
                if (!filled[x, y] || p.D < bestDepth[x, y])
                {
                    bestDepth[x, y] = p.D;
                    filled[x, y] = true;
                }

                // First front-most cell that actually has a kind
                if (p.Kind != CellKind.None &&
                    (!kindFilled[x, y] || p.D < bestKindDepth[x, y]))
                {
                    bestKindDepth[x, y] = p.D;
                    kindFilled[x, y] = true;
                    topKind[x, y] = p.Kind;
                }
            }

            int depthRange = dMax - dMin;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!filled[x, y])
                        continue;

                    percent[x, y] = depthRange == 0
                        ? 0f
                        : 100f * (bestDepth[x, y] - dMin) / depthRange;
                }
            }

            return new DepthMap2D
            {
                Percent = percent,
                Damaged = damaged,
                CellType = topKind,
                Width = width,
                Height = height,
                UMin = uMin,
                VMin = vMin
            };
        }

        void FindProjector(IMyCubeGrid grid, ref IMyProjector projector)
        {
            if (Config.ReferenceBlock == 0)
            {
                projector = null;
                return;
            }

            if (projector != null && projector.EntityId == Config.ReferenceBlock)
                return;

            var entity = MyAPIGateway.Entities.GetEntityById(Config.ReferenceBlock) as IMyProjector;
            projector = entity?.CubeGrid.IsInSameLogicalGroupAs(grid) ?? false ? entity : null;
        }


        private static ViewAxes GetAxes(View view)
        {
            switch (view)
            {
                // These choose a stable 2D plane.
                // Depth is signed so 0% = front-most, 100% = back-most.

                case View.Xpos: // looking from +X toward -X
                    return new ViewAxes
                    {
                        U = p => p.Z,
                        V = p => p.Y,
                        D = p => -p.X
                    };

                case View.Xneg: // looking from -X toward +X
                    return new ViewAxes
                    {
                        U = p => p.Z,
                        V = p => p.Y,
                        D = p => p.X
                    };

                case View.Ypos: // looking from +Y toward -Y
                    return new ViewAxes
                    {
                        U = p => p.X,
                        V = p => p.Z,
                        D = p => -p.Y
                    };

                case View.Yneg: // looking from -Y toward +Y
                    return new ViewAxes
                    {
                        U = p => p.X,
                        V = p => p.Z,
                        D = p => p.Y
                    };

                case View.Zpos: // looking from +Z toward -Z
                    return new ViewAxes
                    {
                        U = p => p.X,
                        V = p => p.Y,
                        D = p => -p.Z
                    };

                case View.Zneg: // looking from -Z toward +Z
                    return new ViewAxes
                    {
                        U = p => p.X,
                        V = p => p.Y,
                        D = p => p.Z
                    };

                default:
                    throw new Exception("Invalid view");
            }
        }
    }

    public struct DepthMap2D
    {
        public float?[,] Percent;
        public bool[,] Damaged;
        public CellKind[,] CellType;
        public int Width;
        public int Height;
        public int UMin;
        public int VMin;
    }

    public struct ViewAxes
    {
        public Func<Vector3I, int> U; // 2D X
        public Func<Vector3I, int> V; // 2D Y
        public Func<Vector3I, int> D; // depth, normalized later
    }


    public enum CellKind
    {
        None,
        Thruster,
        Power,
        Gyro,
        Weapons,
        Gravity,
        Production
    }

    struct GridPairKey : IEquatable<GridPairKey>
    {
        public readonly long ProjectedGridId;
        public readonly long RealGridId;

        public GridPairKey(long projectedGridId, long realGridId)
        {
            ProjectedGridId = projectedGridId;
            RealGridId = realGridId;
        }

        public bool Equals(GridPairKey other)
        {
            return ProjectedGridId == other.ProjectedGridId && RealGridId == other.RealGridId;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GridPairKey))
                return false;

            return Equals((GridPairKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ProjectedGridId.GetHashCode() * 397) ^ RealGridId.GetHashCode();
            }
        }
    }

    struct CachedGridMap
    {
        public static readonly CachedGridMap Empty = new CachedGridMap(
            new List<Vector3I>(),
            new List<Vector3I>(),
            new Dictionary<Vector3I, CellKind>());

        public readonly List<Vector3I> Cells;
        public readonly List<Vector3I> DamagedCells;
        public readonly Dictionary<Vector3I, CellKind> CellTypes;

        public CachedGridMap(
            List<Vector3I> cells,
            List<Vector3I> damagedCells,
            Dictionary<Vector3I, CellKind> cellTypes)
        {
            Cells = cells;
            DamagedCells = damagedCells;
            CellTypes = cellTypes;
        }
    }

    class CachedGridMapEntry
    {
        public CachedGridMap Current = CachedGridMap.Empty;
        public IEnumerator<bool> Updater;
    }
}
