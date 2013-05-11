using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hexpoint.Blox.Hosts.World
{
	internal static class Lighting
	{
		/// <summary>
		/// Table uses a non linear scale by dropping each level to x% of the previous.
		/// Allows for wider range of 'darks'. Using the array table prevents needing to calculate these same values repeatedly.
		/// </summary>
		internal static readonly byte[] LightTable = new[] { (byte)37, (byte)43, (byte)48, (byte)55, (byte)62, (byte)71, (byte)81, (byte)92, (byte)104, (byte)118, (byte)135, (byte)153, (byte)174, (byte)197, (byte)224, (byte)255 }; //this table drops by *0.88 without the large initial drop (better for day/night cycle)
		internal const byte DARKEST_COLOR = 37;

		/// <summary>
		/// Update a light box for a change originating at the supplied position. Some logic is used to limit the size of the light box where possible.
		/// Update is performed by blanking out all of the existing light in an inner box area, resetting all light emitting sources in the inner box and then
		/// looping through an outer box that is one larger on each side so that light can be pulled back in from the edges where applicable.
		/// </summary>
		/// <remarks>Further logic could be added for more cases where the size of the light box could be decreased, however this is not a big bottleck now (chunk rebuild time is far more worthwhile to optimize).</remarks>
		/// <param name="position">Position the light change is centered at (the block that is changing).</param>
		/// <param name="positionCuboid">Null when only a single block is changing. End of the cuboid when the change is a cuboid.</param>
		/// <param name="isBlockChange">Is this change for a block add or remove. Any other changes, such as item lights, have no need for full vertical updates.</param>
		/// <param name="isRemove">Is this update for a block removal.</param>
		/// <returns>Queue of affected chunks with the chunk where the change originated queued first if this is a single block change.</returns>
		internal static Queue<Chunk> UpdateLightBox(ref Position position, Position? positionCuboid, bool isBlockChange, bool isRemove)
		{
			const int MAX_LIGHT_SPREAD_DISTANCE = SkyHost.BRIGHTEST_SKYLIGHT_STRENGTH;
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			//use a queue to ensure the chunk where the change originates gets queued first for a single block change
			var chunksAffected = new Queue<Chunk>();

			//inner box depends on if this is a single block or cuboid
			int x1Inner;
			int y1Inner;
			int z1Inner;
			int x2Inner;
			int y2Inner;
			int z2Inner;

			if (!positionCuboid.HasValue) //single block change
			{
				var chunkContainingChange = WorldData.Chunks[position];

				//determine if a full vertical update is required;
				//-not required if the block directly under the change is NOT transparent (this is because sunlight couldnt travel down)
				//-not required if this change is below the heightmap because sunlight stops at the heightmap (sunlight is the only light that can travel > than the max spread distance)
				//-not required if this is NOT a block change, item lights etc. cannot affect sunlight
				//(cuts the light rebuild time by approx 60% in these cases)
				bool fullVerticalRequired = isBlockChange && position.Y > 0 && WorldData.GetBlock(position.X, position.Y - 1, position.Z).IsTransparent && position.Y >= chunkContainingChange.HeightMap[position.X % Chunk.CHUNK_SIZE, position.Z % Chunk.CHUNK_SIZE];

				x1Inner = Math.Max(position.X - MAX_LIGHT_SPREAD_DISTANCE, 0);
				y1Inner = fullVerticalRequired ? 0 : Math.Max(position.Y - MAX_LIGHT_SPREAD_DISTANCE, 0);
				z1Inner = Math.Max(position.Z - MAX_LIGHT_SPREAD_DISTANCE, 0);
				x2Inner = Math.Min(position.X + MAX_LIGHT_SPREAD_DISTANCE, WorldData.SizeInBlocksX - 1);
				y2Inner = fullVerticalRequired ? Chunk.CHUNK_HEIGHT - 1 : Math.Min(position.Y + MAX_LIGHT_SPREAD_DISTANCE, Chunk.CHUNK_HEIGHT - 1);
				z2Inner = Math.Min(position.Z + MAX_LIGHT_SPREAD_DISTANCE, WorldData.SizeInBlocksZ - 1);

				//figure out the chunk at each bottom corner of the light box (this will be at most 4, and will usually be the full 4 unless change is near a world edge or very close to the center of a chunk)
				//-this works because the chunk size is 32 therefore we know no more than 4 chunks can be affected when the max light spread distance is 15
				var chunkBackLeft = WorldData.Chunks[x1Inner / Chunk.CHUNK_SIZE, z1Inner / Chunk.CHUNK_SIZE];
				var chunkFrontLeft = WorldData.Chunks[x1Inner / Chunk.CHUNK_SIZE, z2Inner / Chunk.CHUNK_SIZE];
				var chunkFrontRight = WorldData.Chunks[x2Inner / Chunk.CHUNK_SIZE, z2Inner / Chunk.CHUNK_SIZE];
				var chunkBackRight = WorldData.Chunks[x2Inner / Chunk.CHUNK_SIZE, z1Inner / Chunk.CHUNK_SIZE];

				chunksAffected.Enqueue(chunkContainingChange); //queue the chunk containing the changing block first, this makes the change seem more responsive to the user
				if (!chunksAffected.Contains(chunkBackLeft)) chunksAffected.Enqueue(chunkBackLeft);
				if (!chunksAffected.Contains(chunkFrontLeft)) chunksAffected.Enqueue(chunkFrontLeft);
				if (!chunksAffected.Contains(chunkFrontRight)) chunksAffected.Enqueue(chunkFrontRight);
				if (!chunksAffected.Contains(chunkBackRight)) chunksAffected.Enqueue(chunkBackRight);

				//adjacent chunks need to be queued first for block removes on a chunk border, otherwise we would briefly see a blank face on the adjacent block
				if (isRemove && position.IsOnChunkBorder) chunksAffected.Enqueue(chunksAffected.Dequeue()); //dequeue the first chunk and requeue it last
			}
			else //cuboid change
			{
				//determine 2 diagonal corners of the cuboid on the bottom plane
				int x1 = Math.Min(position.X, positionCuboid.Value.X);
				int z1 = Math.Min(position.Z, positionCuboid.Value.Z);
				int x2 = Math.Max(position.X, positionCuboid.Value.X);
				int z2 = Math.Max(position.Z, positionCuboid.Value.Z);

				x1Inner = Math.Max(x1 - MAX_LIGHT_SPREAD_DISTANCE, 0);
				y1Inner = 0;
				z1Inner = Math.Max(z1 - MAX_LIGHT_SPREAD_DISTANCE, 0);
				x2Inner = Math.Min(x2 + MAX_LIGHT_SPREAD_DISTANCE, WorldData.SizeInBlocksX - 1);
				y2Inner = Chunk.CHUNK_HEIGHT - 1;
				z2Inner = Math.Min(z2 + MAX_LIGHT_SPREAD_DISTANCE, WorldData.SizeInBlocksZ - 1);

				var chunk1 = WorldData.Chunks[x1Inner / Chunk.CHUNK_SIZE, z1Inner / Chunk.CHUNK_SIZE];
				var chunk2 = WorldData.Chunks[x2Inner / Chunk.CHUNK_SIZE, z2Inner / Chunk.CHUNK_SIZE];
				//loop through and add every chunk contained in the inner light box to the queue of affected chunks
				for (int x = chunk1.Coords.X; x <= chunk2.Coords.X; x++)
				{
					for (int z = chunk1.Coords.Z; z <= chunk2.Coords.Z; z++)
					{
						chunksAffected.Enqueue(WorldData.Chunks[x, z]);
					}
				}
			}

			//outer box is the same for single block or cuboid
			int x1Outer = Math.Max(x1Inner - 1, 0);
			int y1Outer = Math.Max(y1Inner - 1, 0);
			int z1Outer = Math.Max(z1Inner - 1, 0);
			int x2Outer = Math.Min(x2Inner + 1, WorldData.SizeInBlocksX - 1);
			int y2Outer = Math.Min(y2Inner + 1, Chunk.CHUNK_HEIGHT - 1);
			int z2Outer = Math.Min(z2Inner + 1, WorldData.SizeInBlocksZ - 1);

			ResetLightBoxSources(x1Inner, x2Inner, y1Inner, y2Inner, z1Inner, z2Inner, null, WorldData.SkyLightMap, WorldData.ItemLightMap);

			//loop through the outer light box to re-propagate, this pulls back in surrounding light as needed because the outer box is one larger
			for (int x = x1Outer; x < x2Outer; x++)
			{
				for (int y = y1Outer; y < y2Outer; y++)
				{
					for (int z = z1Outer; z < z2Outer; z++)
					{
						//propagate sky light
						byte skyLightStrength = WorldData.SkyLightMap[x, y, z];
						if (skyLightStrength > 1) PropagateLightDynamic(x, y, z, skyLightStrength, WorldData.SkyLightMap); //if strength > 1, check 6 adjacent neighbors and propagate recursively as needed
						//propagate item light
						byte itemLightStrength = WorldData.ItemLightMap[x, y, z];
						if (itemLightStrength > 1) PropagateLightDynamic(x, y, z, itemLightStrength, WorldData.ItemLightMap); //if strength > 1, check 6 adjacent neighbors and propagate recursively as needed
					}
				}
			}

			Debug.WriteLine("UpdateLightBox took {0}ms for {1} (Y range {2}) Queueing {3} chunks", stopwatch.ElapsedMilliseconds, position, y2Inner - y1Inner + 1, chunksAffected.Count);

			return chunksAffected;
		}

		/// <summary>Loop through light box, blank out any previous light data and reset all light emitting sources. The light box during initial loading is the full chunk size.</summary>
		/// <param name="x1">lower x of light box for dynamic change, 0 for initial full chunk loading</param>
		/// <param name="x2">upper x of light box for dynamic change, chunk size - 1 for initial full chunk loading</param>
		/// <param name="y1">lower y of light box for dynamic change, 0 for initial full chunk loading</param>
		/// <param name="y2">upper y of light box for dynamic change, chunk height - 1 for initial full chunk loading</param>
		/// <param name="z1">lower z of light box for dynamic change, 0 for initial loading of full chunk</param>
		/// <param name="z2">upper z of light box for dynamic change, chunk size - 1 for initial full chunk loading</param>
		/// <param name="initialChunk">chunk being loaded initially or null for dynamic changes because multiple chunks may be affected</param>
		/// <param name="skyLightMap">world sky lightmap for dynamic change and chunk sky lightmap during initial loading</param>
		/// <param name="itemLightMap">world item lightmap for dynamic change and chunk item lightmap during initial loading</param>
		internal static void ResetLightBoxSources(int x1, int x2, int y1, int y2, int z1, int z2, Chunk initialChunk, byte[, ,] skyLightMap, byte[, ,] itemLightMap)
		{
			var affectedChunks = new HashSet<Chunk>();
			if (initialChunk != null) affectedChunks.Add(initialChunk);

			for (int x = x1; x <= x2; x++)
			{
				for (int z = z1; z <= z2; z++)
				{
					//y is the innermost loop so we only retrieve chunks when needed, this is always the supplied initial chunk during initial loading
					Chunk chunk;
					if (initialChunk == null) //calculate the chunk if this isnt an initial loading chunk
					{
						chunk = WorldData.Chunks[x / Chunk.CHUNK_SIZE, z / Chunk.CHUNK_SIZE];
						affectedChunks.Add(chunk);
					}
					else
					{
						chunk = initialChunk;
					}

					for (int y = y1; y <= y2; y++)
					{
						//chunk relative coords are needlessly calculated on initial loading, but it doesnt noticeably affect performance and this way it doesnt affect dynamic changes at all
						int chunkRelativeX = x % Chunk.CHUNK_SIZE;
						int chunkRelativeZ = z % Chunk.CHUNK_SIZE;

						if (y > chunk.HeightMap[chunkRelativeX, chunkRelativeZ])
						{
							//transparent block above the heightmap surface, it gets full skylight, transparent blocks cannot be light emitting sources
							skyLightMap[x, y, z] = SkyHost.BRIGHTEST_SKYLIGHT_STRENGTH;
							itemLightMap[x, y, z] = 0; //could be a light source item here; this gets checked later
							continue;
						}
						var block = chunk.Blocks[chunkRelativeX, y, chunkRelativeZ];
						if (!block.IsTransparent)
						{
							//this is a non transparent block; it will have zero sky light and only have item light if this block emits light
							skyLightMap[x, y, z] = 0;
							itemLightMap[x, y, z] = block.IsLightSource ? block.LightStrength : (byte)0;
						}
						else
						{
							//this is a transparent block below the heightmap
							skyLightMap[x, y, z] = 0;
							itemLightMap[x, y, z] = 0; //could be a light source item here; this gets checked later
						}
					}
				}
			}

			//light source items; more efficient to loop through each affected chunks LightSources here and set the lightbox sources accordingly (better then checking for light source matches on every transparent block)
			//note: this can redundantly set item lightmap values in these chunks that are outside the light box; this doesnt hurt anything and is probably faster than checking to prevent it
			foreach (var lightSource in affectedChunks.SelectMany(affectedChunk => affectedChunk.LightSources))
			{
				int x = lightSource.Value.Coords.Xblock;
				int z = lightSource.Value.Coords.Zblock;
				if (initialChunk != null)
				{
					x %= Chunk.CHUNK_SIZE;
					z %= Chunk.CHUNK_SIZE;
				}
				itemLightMap[x, lightSource.Value.Coords.Yblock, z] = lightSource.Value.LightStrength;
			}
		}

		/// <summary>
		/// Check 6 adjacent neighbor blocks and propagate recursively as needed. This propagate is used for dynamic changes after the world has already loaded.
		/// Updates world level arrays. Light map can be sky or item.</summary>
		/// <remarks>Changes here may need to be duplicated in PropagateLightInitial</remarks>
		private static void PropagateLightDynamic(int x, int y, int z, byte lightStrength, byte[, ,] lightMap)
		{
			lightMap[x, y, z] = lightStrength;

			var lightMinusOne = (byte)Math.Max(lightStrength - 1, 0);
			var lightMinusTwo = (byte)Math.Max(lightStrength - 2, 0);

			//check top (light going up propogates half as much, seems more realistic) (only need to propagate if existing strength is less then what we want to set it to)
			if (y < Chunk.CHUNK_HEIGHT - 1 && WorldData.GetBlock(x, y + 1, z).IsTransparent && lightMap[x, y + 1, z] < lightMinusTwo) PropagateLightDynamic(x, y + 1, z, lightMinusTwo, lightMap);

			//check bottom (only need to propagate if existing strength is less then what we want to set it to)
			if (y > 0 && WorldData.GetBlock(x, y - 1, z).IsTransparent && lightMap[x, y - 1, z] < lightMinusOne) PropagateLightDynamic(x, y - 1, z, lightMinusOne, lightMap);

			//check left (only need to propagate if existing strength is less then what we want to set it to)
			if (x > 0 && WorldData.GetBlock(x - 1, y, z).IsTransparent && lightMap[x - 1, y, z] < lightMinusOne) PropagateLightDynamic(x - 1, y, z, lightMinusOne, lightMap);

			//check right (only need to propagate if existing strength is less then what we want to set it to)
			if (x < WorldData.SizeInBlocksX - 1 && WorldData.GetBlock(x + 1, y, z).IsTransparent && lightMap[x + 1, y, z] < lightMinusOne) PropagateLightDynamic(x + 1, y, z, lightMinusOne, lightMap);

			//check back (only need to propagate if existing strength is less then what we want to set it to)
			if (z > 0 && WorldData.GetBlock(x, y, z - 1).IsTransparent && lightMap[x, y, z - 1] < lightMinusOne) PropagateLightDynamic(x, y, z - 1, lightMinusOne, lightMap);

			//check front (only need to propagate if existing strength is less then what we want to set it to)
			if (z < WorldData.SizeInBlocksZ - 1 && WorldData.GetBlock(x, y, z + 1).IsTransparent && lightMap[x, y, z + 1] < lightMinusOne) PropagateLightDynamic(x, y, z + 1, lightMinusOne, lightMap);
		}

		/// <summary>
		/// Check 6 adjacent neighbor blocks and propagate recursively as needed within this Chunk. This propagate is used for initial world loading only.
		/// Updates the chunk level arrays. Light map can be sky or item.
		/// </summary>
		/// <remarks>Changes here may need to be duplicated in PropagateLightDynamic</remarks>
		internal static void PropagateLightInitial(int x, int y, int z, byte lightStrength, Chunk chunk, byte[, ,] lightMap)
		{
			lightMap[x, y, z] = lightStrength;

			var lightMinusOne = (byte)Math.Max(lightStrength - 1, 0);
			var lightMinusTwo = (byte)Math.Max(lightStrength - 2, 0);

			//check top (light going up propogates half as much, seems more realistic) (only need to propagate if existing strength is less then what we want to set it to)
			if (y < Chunk.CHUNK_HEIGHT - 1 && chunk.Blocks[x, y + 1, z].IsTransparent && lightMap[x, y + 1, z] < lightMinusTwo) PropagateLightInitial(x, y + 1, z, lightMinusTwo, chunk, lightMap);

			//check bottom (only need to propagate if existing strength is less then what we want to set it to)
			if (y > 0 && chunk.Blocks[x, y - 1, z].IsTransparent && lightMap[x, y - 1, z] < lightMinusOne) PropagateLightInitial(x, y - 1, z, lightMinusOne, chunk, lightMap);

			//check left (only need to propagate if existing strength is less then what we want to set it to)
			if (x > 0 && chunk.Blocks[x - 1, y, z].IsTransparent && lightMap[x - 1, y, z] < lightMinusOne) PropagateLightInitial(x - 1, y, z, lightMinusOne, chunk, lightMap);

			//check right (only need to propagate if existing strength is less then what we want to set it to)
			if (x < Chunk.CHUNK_SIZE - 1 && chunk.Blocks[x + 1, y, z].IsTransparent && lightMap[x + 1, y, z] < lightMinusOne) PropagateLightInitial(x + 1, y, z, lightMinusOne, chunk, lightMap);

			//check back (only need to propagate if existing strength is less then what we want to set it to)
			if (z > 0 && chunk.Blocks[x, y, z - 1].IsTransparent && lightMap[x, y, z - 1] < lightMinusOne) PropagateLightInitial(x, y, z - 1, lightMinusOne, chunk, lightMap);

			//check front (only need to propagate if existing strength is less then what we want to set it to)
			if (z < Chunk.CHUNK_SIZE - 1 && chunk.Blocks[x, y, z + 1].IsTransparent && lightMap[x, y, z + 1] < lightMinusOne) PropagateLightInitial(x, y, z + 1, lightMinusOne, chunk, lightMap);
		}

		/// <summary>
		/// Build the light map for this chunk. Needs to be done before the chunk is buffered to a vbo, but needs to come after the height map has been built for all chunks.
		/// Height map is built on initial world creation, load from disk for single player, or receive from server for multiplayer for all chunks. Light map should then be built for all chunks.
		/// </summary>
		/// <remarks>
		/// -Anything air or transparent at or above the heightmap gets full light.
		/// -Anything under the heightmap can only get sunlight from a neighbor.
		/// </remarks>
		internal static void InitializeLightMap(Chunk chunk)
		{
			if (Config.IsServer) return; //servers have no current need to calculate light maps

			ResetLightBoxSources(0, Chunk.CHUNK_SIZE - 1, 0, Chunk.CHUNK_HEIGHT - 1, 0, Chunk.CHUNK_SIZE - 1, chunk, chunk.SkyLightMapInitial, chunk.ItemLightMapInitial);

			//light propagation
			for (var x = 0; x < Chunk.CHUNK_SIZE; x++)
			{
				for (var y = 0; y < Chunk.CHUNK_HEIGHT; y++)
				{
					for (var z = 0; z < Chunk.CHUNK_SIZE; z++)
					{
						//propagate sky light
						var skyLightStrength = chunk.SkyLightMapInitial[x, y, z];
						if (skyLightStrength > 1) PropagateLightInitial(x, y, z, skyLightStrength, chunk, chunk.SkyLightMapInitial); //if strength > 1, check 6 adjacent neighbors and propagate recursively as needed
						//propagate item light
						byte itemLightStrength = chunk.ItemLightMapInitial[x, y, z];
						if (itemLightStrength > 1) PropagateLightInitial(x, y, z, itemLightStrength, chunk, chunk.ItemLightMapInitial); //if strength > 1, check 6 adjacent neighbors and propagate recursively as needed
					}
				}
			}

			var chunkTotal = WorldData.SizeInChunksX * WorldData.SizeInChunksZ;
			var chunkCount = System.Threading.Interlocked.Increment(ref WorldData.Chunks.LightmapsBuiltCount);
			Settings.Launcher.UpdateProgressInvokable(string.Format("Building Light Maps: {0} / {1}", chunkCount, chunkTotal), chunkCount, chunkTotal);
		}

		/// <summary>
		/// Pull lighting from adjacent chunks into this chunk as needed after all chunks have been initially built.
		/// Done by looping through all blocks on chunk border, look at adjacent block to determine if light needs to be "pulled" across.
		/// Propagate any cases where pulling is needed.
		/// </summary>
		internal static void InitializeCrossChunkPulling(Chunk chunk)
		{
			for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
			{
				if (chunk.Coords.Z > 0) //can skip when on this world edge
				{
					var adjacentChunk = WorldData.Chunks[chunk.Coords.X, chunk.Coords.Z - 1];
					for (int y = Chunk.CHUNK_HEIGHT - 1; y > 0; y--) //note: this used to loop from the heightmap down, however that wouldnt work with item light sources and because this is for initial pulling on edges only, it was only a tiny benefit
					{
						if (!chunk.Blocks[x, y, 0].IsTransparent) continue; //no need to pull light for non transparent blocks

						var skyLightStrength = chunk.SkyLightMapInitial[x, y, 0];
						if (skyLightStrength < 14) //no need to pull light for blocks that already have at least 14 light strength
						{
							var adjacentSkyLightStrength = adjacentChunk.SkyLightMapInitial[x, y, Chunk.CHUNK_SIZE - 1]; //pull light from neighbor chunk
							if (adjacentSkyLightStrength > 1 && adjacentSkyLightStrength > skyLightStrength - 1) //can only propagate if adjacent > 1
							{
								PropagateLightInitial(x, y, 0, (byte)(adjacentSkyLightStrength - 1), chunk, chunk.SkyLightMapInitial);
							}
						}

						var itemLightStrength = chunk.ItemLightMapInitial[x, y, 0];
						if (itemLightStrength < 14) //no need to pull light for blocks that already have at least 14 light strength
						{
							var adjacentItemLightStrength = adjacentChunk.ItemLightMapInitial[x, y, Chunk.CHUNK_SIZE - 1]; //pull light from neighbor chunk
							if (adjacentItemLightStrength > 1 && adjacentItemLightStrength > itemLightStrength - 1) //can only propagate if adjacent > 1
							{
								PropagateLightInitial(x, y, 0, (byte)(adjacentItemLightStrength - 1), chunk, chunk.ItemLightMapInitial);
							}
						}
					}
				}

				if (chunk.Coords.Z < WorldData.SizeInChunksZ - 1) //can skip when on this world edge
				{
					var adjacentChunk = WorldData.Chunks[chunk.Coords.X, chunk.Coords.Z + 1];
					for (int y = Chunk.CHUNK_HEIGHT - 1; y > 0; y--) //note: this used to loop from the heightmap down, however that wouldnt work with item light sources and because this is for initial pulling on edges only, it was only a tiny benefit
					{
						if (!chunk.Blocks[x, y, Chunk.CHUNK_SIZE - 1].IsTransparent) continue; //no need to pull light for non transparent blocks

						var skyLightStrength = chunk.SkyLightMapInitial[x, y, Chunk.CHUNK_SIZE - 1];
						if (skyLightStrength < 14) //no need to pull light for blocks that already have at least 14 light strength
						{
							var adjacentSkyLightStrength = adjacentChunk.SkyLightMapInitial[x, y, 0]; //pull light from neighbor chunk
							if (adjacentSkyLightStrength > 1 && adjacentSkyLightStrength > skyLightStrength - 1) //can only propagate if adjacent > 1
							{
								PropagateLightInitial(x, y, Chunk.CHUNK_SIZE - 1, (byte)(adjacentSkyLightStrength - 1), chunk, chunk.SkyLightMapInitial);
							}
						}

						var itemLightStrength = chunk.ItemLightMapInitial[x, y, Chunk.CHUNK_SIZE - 1];
						if (itemLightStrength < 14) //no need to pull light for blocks that already have at least 14 light strength
						{
							var adjacentItemLightStrength = adjacentChunk.ItemLightMapInitial[x, y, 0]; //pull light from neighbor chunk
							if (adjacentItemLightStrength > 1 && adjacentItemLightStrength > itemLightStrength - 1) //can only propagate if adjacent > 1
							{
								PropagateLightInitial(x, y, Chunk.CHUNK_SIZE - 1, (byte)(adjacentItemLightStrength - 1), chunk, chunk.ItemLightMapInitial);
							}
						}
					}
				}
			}

			for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
			{
				if (chunk.Coords.X > 0) //can skip when on this world edge
				{
					var adjacentChunk = WorldData.Chunks[chunk.Coords.X - 1, chunk.Coords.Z];
					for (int y = Chunk.CHUNK_HEIGHT - 1; y > 0; y--) //note: this used to loop from the heightmap down, however that wouldnt work with item light sources and because this is for initial pulling on edges only, it was only a tiny benefit
					{
						if (!chunk.Blocks[0, y, z].IsTransparent) continue; //no need to pull light for non transparent blocks

						var skyLightStrength = chunk.SkyLightMapInitial[0, y, z];
						if (skyLightStrength < 14) //no need to pull light for blocks that already have at least 14 light strength
						{
							var adjacentSkyLightStrength = adjacentChunk.SkyLightMapInitial[Chunk.CHUNK_SIZE - 1, y, z]; //pull light from neighbor chunk
							if (adjacentSkyLightStrength > 1 && adjacentSkyLightStrength > skyLightStrength - 1) //can only propagate if adjacent > 1
							{
								PropagateLightInitial(0, y, z, (byte)(adjacentSkyLightStrength - 1), chunk, chunk.SkyLightMapInitial);
							}
						}

						var itemLightStrength = chunk.ItemLightMapInitial[0, y, z];
						if (itemLightStrength < 14) //no need to pull light for blocks that already have at least 14 light strength
						{
							var adjacentItemLightStrength = adjacentChunk.ItemLightMapInitial[Chunk.CHUNK_SIZE - 1, y, z];
							if (adjacentItemLightStrength > 1 && adjacentItemLightStrength > itemLightStrength - 1) //can only propagate if adjacent > 1
							{
								PropagateLightInitial(0, y, z, (byte)(adjacentItemLightStrength - 1), chunk, chunk.ItemLightMapInitial);
							}
						}
					}
				}

				if (chunk.Coords.X < WorldData.SizeInChunksX - 1) //can skip when on this world edge
				{
					var adjacentChunk = WorldData.Chunks[chunk.Coords.X + 1, chunk.Coords.Z];
					for (int y = Chunk.CHUNK_HEIGHT - 1; y > 0; y--) //note: this used to loop from the heightmap down, however that wouldnt work with item light sources and because this is for initial pulling on edges only, it was only a tiny benefit
					{
						if (!chunk.Blocks[Chunk.CHUNK_SIZE - 1, y, z].IsTransparent) continue; //no need to pull light for non transparent blocks

						var skyLightStrength = chunk.SkyLightMapInitial[Chunk.CHUNK_SIZE - 1, y, z];
						if (skyLightStrength < 14) //no need to pull light for blocks that already have at least 14 light strength
						{
							var adjacentSkyLightStrength = adjacentChunk.SkyLightMapInitial[0, y, z]; //pull light from neighbor chunk
							if (adjacentSkyLightStrength > 1 && adjacentSkyLightStrength > skyLightStrength - 1) //can only propagate if adjacent > 1
							{
								PropagateLightInitial(Chunk.CHUNK_SIZE - 1, y, z, (byte)(adjacentSkyLightStrength - 1), chunk, chunk.SkyLightMapInitial);
							}
						}

						var itemLightStrength = chunk.ItemLightMapInitial[Chunk.CHUNK_SIZE - 1, y, z];
						if (itemLightStrength < 14)  //no need to pull light for blocks that already have at least 14 light strength
						{
							var adjacentItemLightStrength = adjacentChunk.ItemLightMapInitial[0, y, z]; //pull light from neighbor chunk
							if (adjacentItemLightStrength > 1 && adjacentItemLightStrength > itemLightStrength - 1) //can only propagate if adjacent > 1
							{
								PropagateLightInitial(Chunk.CHUNK_SIZE - 1, y, z, (byte)(adjacentItemLightStrength - 1), chunk, chunk.ItemLightMapInitial);
							}
						}
					}
				}
			}

			#region Diagonal Corners
			//need to look over up to 4 diagonal corners to potentially pull from those chunks
			//subtract 2 from light strengths when looking diagonally because light does not spread directly diagonally
			if (chunk.Coords.X > 0)
			{
				if (chunk.Coords.Z > 0)
				{
					//check left/back diagonal
					var chunkDiagonal = WorldData.Chunks[chunk.Coords.X - 1, chunk.Coords.Z - 1];
					for (int y = Chunk.CHUNK_HEIGHT - 1; y > 0; y--)
					{
						var diagonalSkyLightStrength = chunkDiagonal.SkyLightMapInitial[Chunk.CHUNK_SIZE - 1, y, Chunk.CHUNK_SIZE - 1];
						if (diagonalSkyLightStrength > 2 && diagonalSkyLightStrength > chunk.SkyLightMapInitial[0, y, 0] - 2)
						{
							PropagateLightInitial(0, y, 0, (byte)(diagonalSkyLightStrength - 2), chunk, chunk.SkyLightMapInitial);
						}

						var diagonalItemLightStrength = chunkDiagonal.ItemLightMapInitial[Chunk.CHUNK_SIZE - 1, y, Chunk.CHUNK_SIZE - 1];
						if (diagonalItemLightStrength > 2 && diagonalItemLightStrength > chunk.ItemLightMapInitial[0, y, 0] - 2)
						{
							PropagateLightInitial(0, y, 0, (byte)(diagonalItemLightStrength - 2), chunk, chunk.ItemLightMapInitial);
						}
					}
				}
				if (chunk.Coords.Z < WorldData.SizeInChunksZ - 1)
				{
					//check left/front diagonal
					var chunkDiagonal = WorldData.Chunks[chunk.Coords.X - 1, chunk.Coords.Z + 1];
					for (int y = Chunk.CHUNK_HEIGHT - 1; y > 0; y--)
					{
						var diagonalSkyLightStrength = chunkDiagonal.SkyLightMapInitial[Chunk.CHUNK_SIZE - 1, y, 0];
						if (diagonalSkyLightStrength > 2 && diagonalSkyLightStrength > chunk.SkyLightMapInitial[0, y, Chunk.CHUNK_SIZE - 1] - 2)
						{
							PropagateLightInitial(0, y, Chunk.CHUNK_SIZE - 1, (byte)(diagonalSkyLightStrength - 2), chunk, chunk.SkyLightMapInitial);
						}

						var diagonalItemLightStrength = chunkDiagonal.ItemLightMapInitial[Chunk.CHUNK_SIZE - 1, y, 0];
						if (diagonalItemLightStrength > 2 && diagonalItemLightStrength > chunk.ItemLightMapInitial[0, y, Chunk.CHUNK_SIZE - 1] - 2)
						{
							PropagateLightInitial(0, y, Chunk.CHUNK_SIZE - 1, (byte)(diagonalItemLightStrength - 2), chunk, chunk.ItemLightMapInitial);
						}
					}
				}
			}
			if (chunk.Coords.X < WorldData.SizeInChunksX - 1)
			{
				if (chunk.Coords.Z > 0)
				{
					//check right/back diagonal
					var chunkDiagonal = WorldData.Chunks[chunk.Coords.X + 1, chunk.Coords.Z - 1];
					for (int y = Chunk.CHUNK_HEIGHT - 1; y > 0; y--)
					{
						var diagonalSkyLightStrength = chunkDiagonal.SkyLightMapInitial[0, y, Chunk.CHUNK_SIZE - 1];
						if (diagonalSkyLightStrength > 2 && diagonalSkyLightStrength > chunk.SkyLightMapInitial[Chunk.CHUNK_SIZE - 1, y, 0] - 2)
						{
							PropagateLightInitial(Chunk.CHUNK_SIZE - 1, y, 0, (byte)(diagonalSkyLightStrength - 2), chunk, chunk.SkyLightMapInitial);
						}

						var diagonalItemLightStrength = chunkDiagonal.ItemLightMapInitial[0, y, Chunk.CHUNK_SIZE - 1];
						if (diagonalItemLightStrength > 2 && diagonalItemLightStrength > chunk.ItemLightMapInitial[Chunk.CHUNK_SIZE - 1, y, 0] - 2)
						{
							PropagateLightInitial(Chunk.CHUNK_SIZE - 1, y, 0, (byte)(diagonalItemLightStrength - 2), chunk, chunk.ItemLightMapInitial);
						}
					}
				}
				if (chunk.Coords.Z < WorldData.SizeInChunksZ - 1)
				{
					//check right/front diagonal
					var chunkDiagonal = WorldData.Chunks[chunk.Coords.X + 1, chunk.Coords.Z + 1];
					for (int y = Chunk.CHUNK_HEIGHT - 1; y > 0; y--)
					{
						var diagonalSkyLightStrength = chunkDiagonal.SkyLightMapInitial[0, y, 0];
						if (diagonalSkyLightStrength > 2 && diagonalSkyLightStrength > chunk.SkyLightMapInitial[Chunk.CHUNK_SIZE - 1, y, Chunk.CHUNK_SIZE - 1] - 2)
						{
							PropagateLightInitial(Chunk.CHUNK_SIZE - 1, y, Chunk.CHUNK_SIZE - 1, (byte)(diagonalSkyLightStrength - 2), chunk, chunk.SkyLightMapInitial);
						}

						var diagonalItemLightStrength = chunkDiagonal.ItemLightMapInitial[0, y, 0];
						if (diagonalItemLightStrength > 2 && diagonalItemLightStrength > chunk.ItemLightMapInitial[Chunk.CHUNK_SIZE - 1, y, Chunk.CHUNK_SIZE - 1] - 2)
						{
							PropagateLightInitial(Chunk.CHUNK_SIZE - 1, y, Chunk.CHUNK_SIZE - 1, (byte)(diagonalItemLightStrength - 2), chunk, chunk.ItemLightMapInitial);
						}
					}
				}
			}
			#endregion

			//at this point the lighting in this chunk is finished, so add to gigantic world light map
			//-couldnt do it before now because lighting isnt finalized until the cross chunk pulling is done
			//-this is essentially a copy of an array into a larger array
			for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
			{
				int worldX = chunk.Coords.WorldCoordsX + x;
				for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
				{
					int worldZ = chunk.Coords.WorldCoordsZ + z;
					for (int y = 0; y < Chunk.CHUNK_HEIGHT; y++)
					{
						WorldData.SkyLightMap[worldX, y, worldZ] = chunk.SkyLightMapInitial[x, y, z];
						WorldData.ItemLightMap[worldX, y, worldZ] = chunk.ItemLightMapInitial[x, y, z];
					}
				}
			}
		}
	}
}
