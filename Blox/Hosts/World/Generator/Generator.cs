using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hexpoint.Blox.Hosts.World.Generator
{
	internal static class Generator
	{
		private const int WATER_LEVEL = Chunk.CHUNK_HEIGHT / 2 - 24;

		/// <summary>Use the raw seed string to come up with an integer based seed.</summary>
		/// <remarks>Maximum seed using 12 characters is '~~~~~~~~~~~~' resulting in 812250, smallest is '!' resulting in -380</remarks>
		internal static int GetNumericSeed()
		{
			long seed = 0;
			for (int position = 1; position <= WorldData.RawSeed.Length; position++)
			{
				var charValue = Convert.ToInt32(WorldData.RawSeed[position - 1]);
				if (charValue < 32 || charValue > 126) continue; //toss out any chars not in this range, 32=space, 126='~'
				seed += ((charValue - 31) * (position + 1) * 95); //add the ascii value (minus 31 so spaces end up as 1) * char position * possible chars
			}
			if (WorldData.RawSeed.Length % 2 != 0) seed *= -1; //if the raw seed had an odd number of chars, make the seed negative
			var finalSeed = unchecked((int)seed); //gm: unchecked is the default, but using it here anyway for clarity
			return finalSeed;
		}

		public static void Generate()
		{
			Debug.WriteLine("Generating new world: " + Settings.WorldFilePath);
			Debug.WriteLine("World type: {0}, Size {1}x{2}", WorldData.WorldType, WorldData.SizeInChunksX, WorldData.SizeInChunksZ);

			Settings.Random = string.IsNullOrEmpty(WorldData.RawSeed) ? new Random() : new Random(GetNumericSeed());
			
			WorldData.Chunks = new Chunks(WorldData.SizeInChunksX, WorldData.SizeInChunksZ);

			const int MIN_SURFACE_HEIGHT = Chunk.CHUNK_HEIGHT / 2 - 40; //max amount below half
			const int MAX_SURFACE_HEIGHT = Chunk.CHUNK_HEIGHT / 2 + 8; //max amount above half
			var heightMap = PerlinNoise.GetIntMap(MIN_SURFACE_HEIGHT, MAX_SURFACE_HEIGHT, 8);
			var mineralMap = PerlinNoise.GetFloatMap(1, MAX_SURFACE_HEIGHT - 5, 2);

			var chunkCount = 1;
			foreach (Chunk chunk in WorldData.Chunks)
			{
				Settings.Launcher.UpdateProgressInvokable(string.Format("Generating Chunks {0} / {1}", chunkCount, WorldData.SizeInChunksX * WorldData.SizeInChunksZ), chunkCount, WorldData.SizeInChunksX * WorldData.SizeInChunksZ);
				
				//bm: we can't run this in parallel or the results will not be deterministic based on our seed.
				GenerateChunk(WorldData.Chunks[chunk.Coords.X, chunk.Coords.Z], heightMap, mineralMap);

				chunkCount++;
			}

			//loop through chunks again for actions that require the neighboring chunks to be built
			Debug.WriteLine("Completing growth in chunks and building heightmaps...");
			Settings.Launcher.UpdateProgressInvokable("Completing growth in chunks...", 0, 0);
			foreach (Chunk chunk in WorldData.Chunks)
			{
				//build heightmap here only so we know where to place trees/clutter (it will get built again on world load anyway)
				chunk.BuildHeightMap();

				var takenPositions = new List<Position>(); //positions taken by tree or clutter, ensures neither spawn on top of or directly touching another

				//generate trees
				if (WorldData.GenerateWithTrees) TreeGenerator.Generate(chunk, takenPositions);

				//generate clutter
				ClutterGenerator.Generate(chunk, takenPositions);
			}

			Settings.Random = new Random(); //reset the random object to ensure the seed is not used for any more random numbers as this could make gameplay predictable
			Debug.WriteLine("World generation complete.");

			//default sun to directly overhead in new worlds
			SkyHost.SunAngleRadians = OpenTK.MathHelper.PiOver2;
			SkyHost.SunLightStrength = SkyHost.BRIGHTEST_SKYLIGHT_STRENGTH;

			Debug.WriteLine("New world saving...");
			Settings.Launcher.UpdateProgressInvokable("New world saving...", 0, 0);
			WorldData.SaveToDisk();
			Debug.WriteLine("New world save complete.");
		}

		private static void GenerateChunk(Chunk chunk, int[][] heightMap, float[][] mineralMap)
		{
			for (var x = chunk.Coords.WorldCoordsX; x < chunk.Coords.WorldCoordsX + Chunk.CHUNK_SIZE; x++)
			{
				for (var z = chunk.Coords.WorldCoordsZ; z < chunk.Coords.WorldCoordsZ + Chunk.CHUNK_SIZE; z++)
				{
					for (var y = 0; y <= Math.Max(heightMap[x][z], WATER_LEVEL); y++)
					{
						Block.BlockType blockType;
						if (y == 0) //world base
						{
							blockType = Block.BlockType.LavaRock;
						}
						else if (y <= 10) //within 10 blocks of world base
						{
							//dont use gravel at this depth (a quick test showed this cuts 15-20% from world file size)
							blockType = Block.BlockType.Rock;
						}
						else if (y == heightMap[x][z]) //ground level
						{
							if (y > WATER_LEVEL)
							{
								switch (WorldData.WorldType)
								{
									case WorldType.Winter: blockType = Block.BlockType.Snow; break;
									case WorldType.Desert: blockType = Block.BlockType.Sand; break;
									default: blockType = Block.BlockType.Grass; break;
								}
							}
							else
							{
								switch (WATER_LEVEL - y)
								{
									case 0:
									case 1:
										blockType = Block.BlockType.Sand; //place sand on shoreline and one block below waterline
										break;
									case 2:
									case 3:
									case 4:
									case 5:
										blockType = Block.BlockType.SandDark; //place dark sand under water within 5 blocks of surface
										break;
									default:
										blockType = Block.BlockType.Gravel; //place gravel lower then 5 blocks under water
										break;
								}
							}
						}
						else if (y > heightMap[x][z] && y <= WATER_LEVEL)
						{
							blockType = (WorldData.WorldType == WorldType.Winter && y == WATER_LEVEL && y - heightMap[x][z] <= 3) ? Block.BlockType.Ice : Block.BlockType.Water;
						}
						else if (y > heightMap[x][z] - 5) //within 5 blocks of the surface
						{
							switch (Settings.Random.Next(0, 37))
							{
								case 0:
									blockType = Block.BlockType.SandDark; //place dark sand below surface
									break;
								case 1:
								case 2:
								case 3:
									blockType = Block.BlockType.Gravel;
									break;
								default:
									blockType = Block.BlockType.Dirt;
									break;
							}
						}
						else
						{
							blockType = Settings.Random.Next(0, 5) == 0 ? Block.BlockType.Gravel : Block.BlockType.Rock;
							//blockType = Block.BlockType.Air; //replace with this to do some quick seismic on what the mineral generator is doing
						}
						chunk.Blocks[x % Chunk.CHUNK_SIZE, y, z % Chunk.CHUNK_SIZE] = new Block(blockType);
					}

					if (mineralMap[x][z] < heightMap[x][z] - 5 && mineralMap[x][z] % 1f > 0.80f)
					{
						Block.BlockType mineralType;
						switch ((int)(mineralMap[x][z] % 0.01 * 1000))
						{
							case 0:
							case 1:
								mineralType = Block.BlockType.Coal;
								break;
							case 2:
							case 3:
								mineralType = Block.BlockType.Iron;
								break;
							case 4:
							case 5:
								mineralType = Block.BlockType.Copper;
								break;
							case 6:
							case 7:
								mineralType = Block.BlockType.Gold;
								break;
							case 8:
							case 9:
								mineralType = Block.BlockType.Oil;
								break;
							default:
								mineralType = Block.BlockType.Iron;
								break;
						}
						var mineralPosition = new Position(x, (int)mineralMap[x][z], z);
						chunk.Blocks[mineralPosition] = new Block(mineralType);
						
						//expand this mineral node
						for (int nextRandom = Settings.Random.Next(); nextRandom % 3600 > 1000; nextRandom = Settings.Random.Next())
						{
							switch (nextRandom % 6)
							{
								case 0:
									mineralPosition.X = Math.Min(mineralPosition.X + 1, WorldData.SizeInBlocksX - 1);
									break;
								case 1:
									mineralPosition.X = Math.Max(mineralPosition.X - 1, 0);
									break;
								case 2:
									mineralPosition.Z = Math.Min(mineralPosition.Z + 1, WorldData.SizeInBlocksZ - 1);
									break;
								case 3:
									mineralPosition.Z = Math.Max(mineralPosition.Z - 1, 0);
									break;
								case 4:
									mineralPosition.Y = Math.Min(mineralPosition.Y + 1, heightMap[x][z] - 5 - 1);
									break;
								case 5:
									mineralPosition.Y = Math.Max(mineralPosition.Y - 1, 0);
									break;
							}
							//need to get the chunk because this block could be expanding into an adjacent chunk
							WorldData.Chunks[mineralPosition].Blocks[mineralPosition] = new Block(mineralType);
						}
					}
				}
			}
		}
	}
}