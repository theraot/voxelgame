using System.Collections;
using System.Diagnostics;
using OpenTK;

namespace Hexpoint.Blox.Hosts.World
{
	internal class Chunks : IEnumerable
	{
		public Chunks(int worldSizeX, int worldSizeZ)
		{
			_chunks = new Chunk[worldSizeX, worldSizeZ];
			for (var x = 0; x < worldSizeX; x++)
			{
				for (var z = 0; z < worldSizeZ; z++)
				{
					this[x, z] = new Chunk(x, z);
				}
			}
		}

		private readonly Chunk[,] _chunks;

		/// <summary>Get a chunk from the array. Based on world coords.</summary>
		public Chunk this[Coords coords]
		{
			get { return _chunks[coords.Xblock / Chunk.CHUNK_SIZE, coords.Zblock / Chunk.CHUNK_SIZE]; }
		}

		/// <summary>Get a chunk from the array. Based on world coords.</summary>
		public Chunk this[Position position]
		{
			get { return _chunks[position.X / Chunk.CHUNK_SIZE, position.Z / Chunk.CHUNK_SIZE]; }
		}

		/// <summary>Get a chunk from the array. Based on the x,z of the chunk in the world. Note these are chunk coords not block coords.</summary>
		public Chunk this[int x, int z]
		{
			get { return _chunks[x, z]; }
			private set { _chunks[x, z] = value; }
		}

		public int LightmapsBuiltCount;
		/// <summary>Clear all the light maps attached to chunks. They are only used during the initialization stage.</summary>
		public void ClearInitialLightMaps()
		{
			foreach (var chunk in _chunks)
			{
				chunk.SkyLightMapInitial = null;
				chunk.ItemLightMapInitial = null;
			}
		}

		/// <summary>
		/// Queue all visible chunks for re-build and re-buffer. Use this for global lighting changes (day/night). Also used for example to highlight all chunk edges.
		/// Chunks are drawn from east to west so sunlight appears in the east first while the sun is coming up and sunlight lasts the longest in the west while the sun is going down.
		/// </summary>
		/// <remarks>All visible chunks are queued for re-build and then re-buffer, they cannot be directly re-buffered without a re-build first because we dont store all the array data needed to re-buffer them.</remarks>
		public int QueueAllWithinViewDistance()
		{
			Debug.Assert(!Config.IsServer, "Servers should not queue chunks.");
			int chunkCount = 0;
			for (int x = WorldData.SizeInChunksX - 1; x >= 0; x--) //loop through chunks from east to west
			{
				for (int z = 0; z < WorldData.SizeInChunksZ; z++)
				{
					//gm: only queue chunks that are already built or building because if they arent they will get built when they come into range anyway
					//-on a 40x40 @ standard view distance, this makes this go from 23secs to 3secs
					switch (_chunks[x, z].ChunkBuildState)
					{
						case Chunk.BuildState.Built:
						case Chunk.BuildState.Building:
							_chunks[x, z].ChunkBuildState = Chunk.BuildState.QueuedDayNight;
							chunkCount++;
							break;
					}
				}
			}
			return chunkCount;
		}

		internal uint UpdateCounter;
		internal const int CHUNK_UPDATE_INTERVAL = Constants.UPDATES_PER_SECOND / 10;
		internal void Update(FrameEventArgs e)
		{
			if (Settings.UpdateCounter % CHUNK_UPDATE_INTERVAL != 0) return;

			if (!Config.IsServer) Game.PerformanceHost.ChunksInMemory = 0;

			foreach (var chunk in _chunks)
			{
				chunk.Update(e);
			}

			unchecked { UpdateCounter++; }
		}

		public IEnumerator GetEnumerator()
		{
			return _chunks.GetEnumerator();
		}
	}
}
