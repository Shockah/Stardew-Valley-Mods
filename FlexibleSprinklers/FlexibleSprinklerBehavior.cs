using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.FlexibleSprinklers
{
    public class FlexibleSprinklerBehavior: ISprinklerBehavior
    {
        public enum TileWaterBalanceMode
        {
            Relaxed, Exact, Restrictive
        }

        private readonly TileWaterBalanceMode tileWaterBalanceMode;
        private readonly ISprinklerBehavior vanillaBehavior = new VanillaSprinklerBehavior();

        public FlexibleSprinklerBehavior(TileWaterBalanceMode tileWaterBalanceMode)
        {
            this.tileWaterBalanceMode = tileWaterBalanceMode;
        }

        private int GetSprinklerRange(SprinklerInfo info)
        {
            return (int)Math.Floor(Math.Pow(info.Power, 0.62) + 1);
        }

        public ISet<IntPoint> GetSprinklerTiles(IMap map, IntPoint sprinklerPosition, SprinklerInfo info)
        {
            var wateredTiles = new HashSet<IntPoint>();
            var unwateredTileCount = info.Power;

            void WaterTile(IntPoint tilePosition)
            {
                unwateredTileCount--;
                wateredTiles.Add(tilePosition);
            }

            void WaterTiles(IEnumerable<IntPoint> tilePositions)
            {
                foreach (var tilePosition in tilePositions)
                {
                    WaterTile(tilePosition);
                }
            }

            if (!FlexibleSprinklers.Instance.SkipVanillaBehavior)
            {
                foreach (var tileToWater in vanillaBehavior.GetSprinklerTiles(map, sprinklerPosition, info))
                {
                    switch (map[tileToWater])
                    {
                        case SoilType.Dry:
                        case SoilType.Wet:
                            WaterTile(tileToWater);
                            break;
                        case SoilType.NonWaterable:
                        case SoilType.Sprinkler:
                        case SoilType.NonSoil:
                            break;
                    }
                }
            }
            if (unwateredTileCount <= 0)
                return wateredTiles;

            var sprinklerRange = GetSprinklerRange(info);
            var waterableTiles = new HashSet<IntPoint>();
            var otherSprinklers = new HashSet<IntPoint>();
            var @checked = new HashSet<IntPoint>();
            var toCheck = new Queue<IntPoint>();
            var costMap = new Dictionary<IntPoint, double>();

            toCheck.Enqueue(sprinklerPosition);
            costMap[sprinklerPosition] = 0;
            foreach (var wateredTile in wateredTiles)
            {
                toCheck.Enqueue(sprinklerPosition);
                costMap[wateredTile] = Math.Sqrt(Math.Pow(wateredTile.X - sprinklerPosition.X, 2) + Math.Pow(wateredTile.Y - sprinklerPosition.Y, 2));
            }

            while (toCheck.Count > 0)
            {
                var tilePosition = toCheck.Dequeue();
                @checked.Add(tilePosition);

                var tilePathLength = costMap[tilePosition];
                var newTilePathLength = tilePathLength + 1;

                if (tilePathLength > 0)
                {
                    if (tilePathLength > sprinklerRange)
                        continue;
                    if (waterableTiles.Count >= unwateredTileCount && costMap[waterableTiles.Last()] > newTilePathLength)
                        continue;
                    switch (map[tilePosition])
                    {
                        case SoilType.Dry:
                        case SoilType.Wet:
                            if (!wateredTiles.Contains(tilePosition))
                                waterableTiles.Add(tilePosition);
                            break;
                        case SoilType.Sprinkler:
                            otherSprinklers.Add(tilePosition);
                            continue;
                        case SoilType.NonSoil:
                        case SoilType.NonWaterable:
                            continue;
                    }
                }

                foreach (var neighbor in tilePosition.Neighbors)
                {
                    if (@checked.Contains(neighbor))
                        continue;
                    toCheck.Enqueue(neighbor);
                    costMap[neighbor] = costMap.ContainsKey(neighbor) ? Math.Min(costMap[neighbor], newTilePathLength) : newTilePathLength;
                }
            }

            var otherSprinklerDetectionRange = (int)Math.Sqrt(sprinklerRange - 1);

            IEnumerable<IntPoint> DetectSprinklers(IntPoint singleDirection)
            {
                int[] directions = { -1, 1 };
                
                for (int i = 1; i <= otherSprinklerDetectionRange; i++)
                {
                    foreach (var direction in directions)
                    {
                        var position = sprinklerPosition + singleDirection * direction * i;
                        if (!otherSprinklers.Contains(position))
                            continue;
                        if (map[position] == SoilType.Sprinkler)
                            yield return position;
                    }
                }
            }

            int? ClosestSprinkler(IntPoint singleDirection)
            {
                return DetectSprinklers(singleDirection)
                    .Select(p => (Math.Abs(p.X - sprinklerPosition.X) + Math.Abs(p.Y - sprinklerPosition.Y)) as int?)
                    .FirstOrDefault();
            }

            var sprinklerNeighbors = sprinklerPosition.Neighbors;
            var horizontalSprinklerDistance = ClosestSprinkler(IntPoint.Right);
            var verticalSprinklerDistance = ClosestSprinkler(IntPoint.Bottom);

            var sortedWaterableTiles = waterableTiles
                .Select(e => {
                    var dx = Math.Abs(e.X - sprinklerPosition.X) * ((horizontalSprinklerDistance ?? 0) * sprinklerRange + 1);
                    var dy = Math.Abs(e.Y - sprinklerPosition.Y) * ((verticalSprinklerDistance ?? 0) * sprinklerRange + 1);
                    return (
                        tilePosition: e,
                        distance: Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2))
                    );
                })
                .OrderBy(e => e.distance)
                .ToList();

            while (unwateredTileCount > 0 && sortedWaterableTiles.Count > 0)
            {
                var reachable = sortedWaterableTiles
                    .Where(e => sprinklerNeighbors.Contains(e.tilePosition) || wateredTiles.SelectMany(t => t.Neighbors).Contains(e.tilePosition))
                    .ToList();
                var currentDistance = reachable.First().distance;
                var tileEntries = reachable.TakeWhile(e => e.distance == currentDistance).ToList();
                if (tileEntries.Count == 0)
                {
                    FlexibleSprinklers.Instance.Monitor.Log($"Could not find all tiles to water for sprinkler at {sprinklerPosition}.", StardewModdingAPI.LogLevel.Warn);
                    break;
                }

                foreach (var tileEntry in tileEntries)
                {
                    sortedWaterableTiles.Remove(tileEntry);
                }

                if (unwateredTileCount >= tileEntries.Count)
                {
                    WaterTiles(tileEntries.Select(e => e.tilePosition));
                }
                else
                {
                    switch (tileWaterBalanceMode)
                    {
                        case TileWaterBalanceMode.Relaxed:
                            WaterTiles(tileEntries.Select(e => e.tilePosition));
                            break;
                        case TileWaterBalanceMode.Restrictive:
                            unwateredTileCount = 0;
                            break;
                        case TileWaterBalanceMode.Exact:
                            // TODO: more fair implementation
                            WaterTiles(tileEntries.Take(unwateredTileCount).Select(e => e.tilePosition));
                            break;
                    }
                }
            }

            return wateredTiles;
        }
    }
}