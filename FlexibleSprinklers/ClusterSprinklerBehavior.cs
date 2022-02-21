using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.FlexibleSprinklers
{
	internal enum ClusterSprinklerBehaviorClusterOrdering { SmallerFirst, BiggerFirst, All }
	internal enum ClusterSprinklerBehaviorBetweenClusterBalanceMode { Relaxed, Restrictive }
	internal enum ClusterSprinklerBehaviorInClusterBalanceMode { Relaxed, Exact, Restrictive }

	internal class ClusterSprinklerBehavior: ISprinklerBehavior.Collective
	{
		private class Cluster
		{
			public readonly ISet<IntPoint> Tiles = new HashSet<IntPoint>();
			public readonly ISet<(IntPoint position, SprinklerInfo info)> Sprinklers = new HashSet<(IntPoint position, SprinklerInfo info)>();

			public Cluster()
			{
			}
		}

		private readonly ClusterSprinklerBehaviorClusterOrdering ClusterOrdering;
		private readonly ClusterSprinklerBehaviorBetweenClusterBalanceMode BetweenClusterBalanceMode;
		private readonly ClusterSprinklerBehaviorInClusterBalanceMode InClusterBalanceMode;

		public ClusterSprinklerBehavior(
			ClusterSprinklerBehaviorClusterOrdering clusterOrdering,
			ClusterSprinklerBehaviorBetweenClusterBalanceMode betweenClusterBalanceMode,
			ClusterSprinklerBehaviorInClusterBalanceMode inClusterBalanceMode
		)
		{
			this.ClusterOrdering = clusterOrdering;
			this.BetweenClusterBalanceMode = betweenClusterBalanceMode;
			this.InClusterBalanceMode = inClusterBalanceMode;
		}

		public ISet<IntPoint> GetSprinklerTiles(IMap map, IEnumerable<(IntPoint position, SprinklerInfo info)> sprinklers)
		{
			ICollection<(IntPoint position, SprinklerInfo info)> sprinklerCollection = sprinklers.ToList();

			ICollection<Cluster> GetClusters()
			{
				IList<Cluster> clusters = new List<Cluster>();
				ISet<IntPoint> @checked = new HashSet<IntPoint>();
				var toCheck = new LinkedList<(IntPoint point, Cluster cluster)>();

				Cluster? GetClusterContainingTile(IntPoint point)
				{
					foreach (var cluster in clusters)
						if (cluster.Tiles.Contains(point))
							return cluster;
					return null;
				}

				Cluster? GetClusterContainingSprinklerPosition(IntPoint point)
				{
					foreach (var cluster in clusters)
						if (cluster.Sprinklers.Where(e => e.position == point).Any())
							return cluster;
					return null;
				}

				Cluster? GetClusterContainingTileOrSprinklerPosition(IntPoint point)
				{
					return GetClusterContainingTile(point) ?? GetClusterContainingSprinklerPosition(point);
				}

				void CombineClusters(Cluster clusterToRemove, Cluster clusterToMergeWith)
				{
					clusters.Remove(clusterToRemove);
					clusterToMergeWith.Tiles.UnionWith(clusterToRemove.Tiles);
					clusterToMergeWith.Sprinklers.UnionWith(clusterToRemove.Sprinklers);

					var current = toCheck.First;
					while (current is not null)
					{
						if (ReferenceEquals(current.Value.cluster, clusterToRemove))
							current.Value = (current.Value.point, clusterToMergeWith);
						current = current.Next;
					}
				}

				IDictionary<IntPoint, SprinklerInfo> sprinklerDictionary = new Dictionary<IntPoint, SprinklerInfo>();
				foreach (var (sprinklerPosition, info) in sprinklerCollection)
				{
					sprinklerDictionary[sprinklerPosition] = info;
					foreach (var point in info.Layout)
					{
						if ((Math.Abs((int)point.X) == 1 && (int)point.Y == 0) || (Math.Abs((int)point.Y) == 1 && (int)point.X == 0))
						{
							var clusterStartingPoint = sprinklerPosition + new IntPoint((int)point.X, (int)point.Y);
							var cluster = GetClusterContainingTile(clusterStartingPoint);
							if (cluster is null)
							{
								cluster = new Cluster();
								cluster.Tiles.Add(clusterStartingPoint);
								clusters.Add(cluster);
							}
							else
							{
								toCheck.AddLast((clusterStartingPoint, cluster));
							}
							cluster.Sprinklers.Add((sprinklerPosition, info));
						}
					}
				}

				while (toCheck.Count != 0)
				{
					var (point, cluster) = toCheck.First!.Value;
					toCheck.RemoveFirst();
					if (@checked.Contains(point))
						continue;
					@checked.Add(point);

					foreach (var neighbor in point.Neighbors)
					{
						var mapTile = map[neighbor];
						switch (mapTile)
						{
							case SoilType.Dry:
							case SoilType.Wet:
							case SoilType.Sprinkler:
								var existingCluster = GetClusterContainingTileOrSprinklerPosition(neighbor);
								if (existingCluster is not null)
								{
									CombineClusters(cluster, existingCluster);
									continue;
								}

								if (mapTile == SoilType.Sprinkler)
									cluster.Sprinklers.Add((neighbor, sprinklerDictionary[neighbor]));
								else
									cluster.Tiles.Add(neighbor);
								break;
							case SoilType.NonWaterable:
							case SoilType.NonSoil:
								break;
						}
					}
				}

				return clusters;
			}

			IEnumerable<Cluster> GetClustersForSprinkler(IntPoint sprinklerPosition, IEnumerable<Cluster> clusters)
			{
				foreach (var cluster in clusters)
					foreach (var (clusterSprinklerPosition, _) in cluster.Sprinklers)
						if (clusterSprinklerPosition == sprinklerPosition)
							yield return cluster;
			}

			ICollection<(IntPoint position, SprinklerInfo info)>?[,] GetTileSprinklersGridForCluster(Cluster cluster, IEnumerable<Cluster> allClusters)
			{
				int minX = cluster.Tiles.Min(p => p.X);
				int maxX = cluster.Tiles.Max(p => p.X);
				int minY = cluster.Tiles.Min(p => p.Y);
				int maxY = cluster.Tiles.Max(p => p.Y);
				var grid = new ICollection<(IntPoint position, SprinklerInfo info)>?[maxX - minX + 1, maxY - minY + 1];

				foreach (var (sprinklerPosition, info) in cluster.Sprinklers)
				{
					var sprinklerClusterCount = GetClustersForSprinkler(sprinklerPosition, allClusters).Count();
					var sprinklerRange = FlexibleSprinklers.Instance.GetFloodFillSprinklerRange((int)Math.Ceiling(1.0 * info.Power / sprinklerClusterCount));

					var pathLengthGrid = new int?[grid.GetLength(0), grid.GetLength(1)];
					ISet<IntPoint> @checked = new HashSet<IntPoint>();
					var toCheck = new LinkedList<(IntPoint point, int pathLength)>();

					foreach (var layoutPoint in info.Layout)
					{
						if ((Math.Abs((int)layoutPoint.X) == 1 && (int)layoutPoint.Y == 0) || (Math.Abs((int)layoutPoint.Y) == 1 && (int)layoutPoint.X == 0))
						{
							var point = sprinklerPosition + new IntPoint((int)layoutPoint.X, (int)layoutPoint.Y);
							if (cluster.Tiles.Contains(point))
							{
								toCheck.AddLast((point, 1));
								pathLengthGrid[point.X - minX, point.Y - minY] = 1;
							}
						}
					}

					while (toCheck.Count != 0)
					{
						var (point, pathLength) = toCheck.First!.Value;
						toCheck.RemoveFirst();
						if (@checked.Contains(point))
							continue;
						@checked.Add(point);
						pathLengthGrid[point.X - minX, point.Y - minY] = Math.Min(pathLengthGrid[point.X - minX, point.Y - minY] ?? int.MaxValue, pathLength);

						var tileSprinklers = grid[point.X - minX, point.Y - minY] ?? new List<(IntPoint position, SprinklerInfo info)>();
						tileSprinklers.Add((sprinklerPosition, info));
						grid[point.X - minX, point.Y - minY] = tileSprinklers;

						if (pathLength >= sprinklerRange)
							continue;
						foreach (var neighbor in point.Neighbors)
						{
							if (!cluster.Tiles.Contains(neighbor))
								continue;
							toCheck.AddLast((neighbor, pathLength + 1));
						}
					}
				}

				return grid;
			}

			var clusters = GetClusters();

			IDictionary<Cluster, IDictionary<IntPoint, int>> sprinklerTileCountToWaterPerCluster = new Dictionary<Cluster, IDictionary<IntPoint, int>>();
			foreach (var (sprinklerPosition, info) in sprinklerCollection)
			{
				int tileCountToWaterLeft = info.Power;
				IList<Cluster> sprinklerClusters = clusters.Where(c => c.Sprinklers.Where(s => s.position == sprinklerPosition).Any()).ToList();
				if (sprinklerClusters.Count == 0)
					continue;

				void AddTileCountToWaterToCluster(int tileCount, Cluster cluster)
				{
					if (!sprinklerTileCountToWaterPerCluster.TryGetValue(cluster, out var sprinklerTileCountsToWater))
					{
						sprinklerTileCountsToWater = new Dictionary<IntPoint, int>();
						sprinklerTileCountToWaterPerCluster[cluster] = sprinklerTileCountsToWater;
					}

					if (!sprinklerTileCountsToWater.TryGetValue(sprinklerPosition, out int existingTileCountToWater))
						existingTileCountToWater = 0;
					sprinklerTileCountsToWater[sprinklerPosition] = existingTileCountToWater + tileCount;
					tileCountToWaterLeft -= tileCount;
				}

				int addEquallyPerCluster = tileCountToWaterLeft / sprinklerClusters.Count;
				if (addEquallyPerCluster > 0)
					foreach (var cluster in sprinklerClusters)
						AddTileCountToWaterToCluster(addEquallyPerCluster, cluster);

				while (tileCountToWaterLeft > 0)
				{
					IEnumerable<Cluster> nextClustersEnumerable = sprinklerClusters;
					switch (ClusterOrdering)
					{
						case ClusterSprinklerBehaviorClusterOrdering.SmallerFirst:
							nextClustersEnumerable = nextClustersEnumerable.OrderBy(c => c.Tiles.Count);
							break;
						case ClusterSprinklerBehaviorClusterOrdering.BiggerFirst:
							nextClustersEnumerable = nextClustersEnumerable.OrderByDescending(c => c.Tiles.Count);
							break;
						case ClusterSprinklerBehaviorClusterOrdering.All:
							break;
					}

					var nextClusters = nextClustersEnumerable.ToList();
					addEquallyPerCluster = BetweenClusterBalanceMode == ClusterSprinklerBehaviorBetweenClusterBalanceMode.Relaxed
						? (int)Math.Ceiling(1.0 * tileCountToWaterLeft / nextClusters.Count)
						: (int)Math.Floor(1.0 * tileCountToWaterLeft / nextClusters.Count);
					if (addEquallyPerCluster > 0)
						foreach (var cluster in nextClusters)
							AddTileCountToWaterToCluster(addEquallyPerCluster, cluster);
					tileCountToWaterLeft = 0;
				}
			}

			ISet<IntPoint> tilesToWater = new HashSet<IntPoint>();
			foreach (var cluster in clusters)
			{
				int minX = cluster.Tiles.Min(p => p.X);
				int minY = cluster.Tiles.Min(p => p.Y);
				var grid = GetTileSprinklersGridForCluster(cluster, clusters);
				var totalTileCountToWater = sprinklerTileCountToWaterPerCluster[cluster].Values.Sum();
				var totalTileCount = cluster.Tiles.Count;
				var totalReachableTileCount = cluster.Tiles.Where(p => (grid[p.X - minX, p.Y - minY]?.Count ?? 0) != 0).Count();

				if (totalTileCountToWater >= totalTileCount && totalReachableTileCount == totalTileCount)
				{
					tilesToWater.UnionWith(cluster.Tiles);
					continue;
				}

				var averageSprinklerX = cluster.Sprinklers.Select(e => e.position.X).Average();
				var averageSprinklerY = cluster.Sprinklers.Select(e => e.position.Y).Average();
				var averageSprinklerPosition = new IntPoint((int)Math.Round(averageSprinklerX), (int)Math.Round(averageSprinklerY));
				var sortedReachableTiles = cluster.Tiles
					.Select(p => (
						tilePosition: p,
						sprinklerCount: grid[p.X - minX, p.Y - minY]?.Count ?? 0,
						distanceFromSprinklerCenter: Math.Sqrt(Math.Pow(p.X - averageSprinklerX, 2) + Math.Pow(p.Y - averageSprinklerY, 2)))
					)
					.Where(e => e.sprinklerCount != 0)
					.OrderByDescending(e => e.sprinklerCount)
					.ThenBy(e => e.distanceFromSprinklerCenter)
					.ToList();
				while (totalTileCountToWater > 0 && sortedReachableTiles.Count > 0)
				{
					var (_, firstSprinklerCount, firstDistanceFromSprinklerCenter) = sortedReachableTiles.First();
					var stepTiles = sortedReachableTiles
						.TakeWhile(e => e.sprinklerCount == firstSprinklerCount && e.distanceFromSprinklerCenter == firstDistanceFromSprinklerCenter)
						.ToList();

					foreach (var stepTile in stepTiles)
						sortedReachableTiles.Remove(stepTile);

					if (totalTileCountToWater >= stepTiles.Count)
					{
						foreach (var (tilePosition, _, _) in stepTiles)
							tilesToWater.Add(tilePosition);
					}
					else
					{
						switch (InClusterBalanceMode)
						{
							case ClusterSprinklerBehaviorInClusterBalanceMode.Relaxed:
								foreach (var (tilePosition, _, _) in stepTiles)
									tilesToWater.Add(tilePosition);
								break;
							case ClusterSprinklerBehaviorInClusterBalanceMode.Restrictive:
								totalTileCountToWater = 0;
								break;
							case ClusterSprinklerBehaviorInClusterBalanceMode.Exact:
								var minD = stepTiles.Min(e => Math.Max(Math.Abs(e.tilePosition.X - averageSprinklerPosition.X), Math.Abs(e.tilePosition.Y - averageSprinklerPosition.Y)));
								var maxD = stepTiles.Max(e => Math.Max(Math.Abs(e.tilePosition.X - averageSprinklerPosition.X), Math.Abs(e.tilePosition.Y - averageSprinklerPosition.Y)));
								foreach (var spiralingTile in averageSprinklerPosition.GetSpiralingTiles(minD, maxD))
								{
									foreach (var (tilePosition, _, _) in stepTiles)
									{
										if (tilePosition == spiralingTile)
										{
											tilesToWater.Add(tilePosition);
											totalTileCountToWater--;
											if (totalTileCountToWater <= 0)
												goto done;
											break;
										}
									}
								}
								done:;
								break;
						}
					}
				}
			}
			return tilesToWater;
		}
	}
}
