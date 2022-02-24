using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.FlexibleSprinklers
{
	internal enum ClusterSprinklerBehaviorClusterOrdering { SmallerFirst, BiggerFirst, Equally }
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

			public override string ToString()
				=> $"Cluster{{Tiles = {Tiles.Count}, Sprinklers = {Sprinklers.Count}}}";
		}

		private readonly ClusterSprinklerBehaviorClusterOrdering ClusterOrdering;
		private readonly ClusterSprinklerBehaviorBetweenClusterBalanceMode BetweenClusterBalanceMode;
		private readonly ClusterSprinklerBehaviorInClusterBalanceMode InClusterBalanceMode;
		private readonly ISprinklerBehavior.Independent? PriorityBehavior;

		private readonly IDictionary<IMap, (ISet<(IntPoint position, SprinklerInfo info)> sprinklers, IList<(ISet<IntPoint>, float)> tilesToWater)> Cache
			= new Dictionary<IMap, (ISet<(IntPoint position, SprinklerInfo info)> sprinklers, IList<(ISet<IntPoint>, float)> tilesToWater)>();

		public ClusterSprinklerBehavior(
			ClusterSprinklerBehaviorClusterOrdering clusterOrdering,
			ClusterSprinklerBehaviorBetweenClusterBalanceMode betweenClusterBalanceMode,
			ClusterSprinklerBehaviorInClusterBalanceMode inClusterBalanceMode,
			ISprinklerBehavior.Independent? priorityBehavior
		)
		{
			this.ClusterOrdering = clusterOrdering;
			this.BetweenClusterBalanceMode = betweenClusterBalanceMode;
			this.InClusterBalanceMode = inClusterBalanceMode;
			this.PriorityBehavior = priorityBehavior;
		}

		void ISprinklerBehavior.ClearCache()
		{
			Cache.Clear();
		}

		void ISprinklerBehavior.ClearCacheForMap(IMap map)
		{
			Cache.Remove(map);
		}

		public IList<(ISet<IntPoint>, float)> GetSprinklerTilesWithSteps(IMap map, IEnumerable<(IntPoint position, SprinklerInfo info)> sprinklers)
		{
			var sprinklersSet = sprinklers.ToHashSet();
			if (!Cache.TryGetValue(map, out var cachedInfo))
				return GetUncachedSprinklerTilesWithSteps(map, sprinklersSet);
			if (!cachedInfo.sprinklers.SetEquals(sprinklersSet))
				return GetUncachedSprinklerTilesWithSteps(map, sprinklersSet);
			return cachedInfo.tilesToWater;
		}

		private IList<(ISet<IntPoint>, float)> GetUncachedSprinklerTilesWithSteps(IMap map, ISet<(IntPoint position, SprinklerInfo info)> sprinklers)
		{
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
				IDictionary<IntPoint, ISet<IntPoint>> sprinklerStartingPoints = new Dictionary<IntPoint, ISet<IntPoint>>();
				foreach (var (sprinklerPosition, info) in sprinklers)
				{
					sprinklerDictionary[sprinklerPosition] = info;
					var thisSprinklerStartingPoints = info.Layout
					   .Select(p => new IntPoint((int)p.X, (int)p.Y))
					   .Where(p => (Math.Abs(p.X) == 1 && p.Y == 0) || (Math.Abs(p.Y) == 1 && p.X == 0))
					   .Select(p => sprinklerPosition + p)
					   .Where(p => map[p] == SoilType.Waterable)
					   .ToHashSet();
					sprinklerStartingPoints[sprinklerPosition] = thisSprinklerStartingPoints;

					foreach (var sprinklerStartingPoint in thisSprinklerStartingPoints)
					{
						var cluster = GetClusterContainingTile(sprinklerStartingPoint);
						if (cluster is null)
						{
							cluster = new Cluster();
							cluster.Tiles.Add(sprinklerStartingPoint);
							clusters.Add(cluster);
							toCheck.AddLast((sprinklerStartingPoint, cluster));
						}
					}
				}

				while (toCheck.Count != 0)
				{
					var (point, cluster) = toCheck.First!.Value;
					toCheck.RemoveFirst();
					@checked.Add(point);

					foreach (var neighbor in point.Neighbors)
					{
						switch (map[neighbor])
						{
							case SoilType.Waterable:
								var existingCluster = GetClusterContainingTile(neighbor);
								if (existingCluster is not null && !ReferenceEquals(cluster, existingCluster))
								{
									CombineClusters(cluster, existingCluster);
									continue;
								}
								if (@checked.Contains(neighbor))
									continue;
								cluster.Tiles.Add(neighbor);
								toCheck.AddLast((neighbor, cluster));
								break;
							case SoilType.Sprinkler:
							case SoilType.NonWaterable:
								break;
						}
					}
				}

				foreach (var (sprinklerPosition, thisSprinklerStartingPoints) in sprinklerStartingPoints)
				{
					foreach (var sprinklerStartingPoint in thisSprinklerStartingPoints)
					{
						var cluster = GetClusterContainingTile(sprinklerStartingPoint);
						cluster?.Sprinklers.Add((sprinklerPosition, sprinklerDictionary[sprinklerPosition]));
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
			IList<(ISet<IntPoint>, float)> priorityTilesToWaterSteps = new List<(ISet<IntPoint>, float)>();
			IList<ISet<IntPoint>> tilesToWaterSteps = new List<ISet<IntPoint>>();
			ISet<IntPoint> currentTilesToWater = new HashSet<IntPoint>();
			ISet<IntPoint> tilesToWater = new HashSet<IntPoint>();

			void WaterTile(IntPoint tilePosition)
			{
				tilesToWater.Add(tilePosition);
				currentTilesToWater.Add(tilePosition);
			}

			void WaterTiles(IEnumerable<IntPoint> tilePositions)
			{
				foreach (var tilePosition in tilePositions)
					WaterTile(tilePosition);
			}

			void FinishWateringStep()
			{
				if (currentTilesToWater.Count == 0)
					return;
				tilesToWaterSteps.Add(currentTilesToWater.ToHashSet());
				currentTilesToWater.Clear();
			}

			IDictionary<Cluster, IDictionary<IntPoint, int>> sprinklerTileCountToWaterPerCluster = new Dictionary<Cluster, IDictionary<IntPoint, int>>();
			foreach (var (sprinklerPosition, info) in sprinklers)
			{
				int tileCountToWaterLeft = info.Power;
				if (PriorityBehavior is not null)
				{
					foreach (var step in PriorityBehavior.GetSprinklerTilesWithSteps(map, sprinklerPosition, info))
					{
						var actuallyWaterableStepTiles = step.Item1.Where(t => map[t] == SoilType.Waterable).ToHashSet();
						priorityTilesToWaterSteps.Add((actuallyWaterableStepTiles, step.Item2));
						tileCountToWaterLeft -= actuallyWaterableStepTiles.Count;
					}
				}

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
						case ClusterSprinklerBehaviorClusterOrdering.Equally:
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

			var results = priorityTilesToWaterSteps.ToList();
			foreach (var cluster in clusters)
			{
				IList<ISet<IntPoint>> clusterSteps = new List<ISet<IntPoint>>();

				void FinishClusterWateringStep()
				{
					if (currentTilesToWater.Count == 0)
						return;
					clusterSteps.Add(currentTilesToWater.ToHashSet());
					currentTilesToWater.Clear();
				}

				int minX = cluster.Tiles.Min(p => p.X);
				int minY = cluster.Tiles.Min(p => p.Y);
				var grid = GetTileSprinklersGridForCluster(cluster, clusters);
				var totalTileCountToWater = sprinklerTileCountToWaterPerCluster.ContainsKey(cluster) ? sprinklerTileCountToWaterPerCluster[cluster].Values.Sum() : 0;
				if (totalTileCountToWater == 0)
					continue;
				var totalTileCount = cluster.Tiles.Count;
				var totalReachableTileCount = cluster.Tiles.Where(p => (grid[p.X - minX, p.Y - minY]?.Count ?? 0) != 0).Count();

				if (totalTileCountToWater >= totalTileCount && totalReachableTileCount == totalTileCount)
				{
					WaterTiles(cluster.Tiles);
					goto finishCluster;
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
							WaterTile(tilePosition);
						totalTileCountToWater -= stepTiles.Count;
						FinishClusterWateringStep();
					}
					else
					{
						switch (InClusterBalanceMode)
						{
							case ClusterSprinklerBehaviorInClusterBalanceMode.Relaxed:
								foreach (var (tilePosition, _, _) in stepTiles)
									WaterTile(tilePosition);
								totalTileCountToWater -= stepTiles.Count;
								FinishClusterWateringStep();
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
											WaterTile(tilePosition);
											totalTileCountToWater--;
											FinishClusterWateringStep();
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

				finishCluster:;
				FinishClusterWateringStep();
				results = results
					.Union(clusterSteps.Select((step, index) => (step, (priorityTilesToWaterSteps.Count == 0 ? 0f : 1f) + 1f * index / (clusterSteps.Count - 1))))
					.ToList();
			}

			results = results
				.Select(step => priorityTilesToWaterSteps.Count == 0 ? step : (step.Item1, step.Item2 / 2f))
				.OrderBy(step => step.Item2)
				.ToList();
			Cache[map] = (sprinklers, results);
			return results;
		}
	}
}
