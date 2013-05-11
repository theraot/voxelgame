using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hexpoint.Blox.Hosts.World.Render;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox.Hosts.World
{
	internal class WorldHost : IHost
	{
		#region Constructors
		internal WorldHost()
		{
			PerformanceHost.OnHalfSecondElapsed += PerformanceHost_OnHalfSecondElapsed;

			for (var i = 0; i < Math.Max(1, Environment.ProcessorCount / 2); i++)
			{
				var buildChunkThread = new Thread(BuildChunksThread) { IsBackground = true, Priority = ThreadPriority.Lowest, Name = "Chunk Builder " + i}; //Lowest priority makes it noticeably less choppy when working through the queue (startup)
				buildChunkThread.Start();
			}

			FogColorUnderWater = new ColorRgb(51, 128, 204);
		}

		private static void PerformanceHost_OnHalfSecondElapsed()
		{
			WaterCycleTextureId++;
			if (WaterCycleTextureId > (int)Textures.BlockTextureType.Water4) WaterCycleTextureId = (int)Textures.BlockTextureType.Water;
		}
		#endregion

		#region Properties
		/// <summary>Chunks that load/unload in the distance are placed on this queue. Queue so that chunks appear/disappear in the distance in the order they were received.</summary>
		internal static readonly ConcurrentQueue<Chunk> FarChunkQueue = new ConcurrentQueue<Chunk>();

		/// <summary>Chunks that have changed are placed on this queue to be first rebuilt and then rebuffered.</summary>
		internal static readonly ConcurrentQueue<Chunk> ChangedChunkQueue = new ConcurrentQueue<Chunk>();

		internal static ColorRgb FogColorUnderWater { get; private set; }
		internal static int RotationCounter;
		/// <summary>Current water texture id for the water animation cycle. Incremented in the performance host.</summary>
		internal static int WaterCycleTextureId = (int)Textures.BlockTextureType.Water;
		#endregion

		#region Build World
		internal void BuildWorld()
		{
			const int INITIAL_CHUNK_RENDER_DISTANCE = 5;
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var chunksByDist = new List<Chunk>();

			//immediately render up to INITIAL_CHUNK_RENDER_DISTANCE chunks away within the initial view frustum
			var immediateChunkTasks = new List<Task>();
			foreach (Chunk chunk in WorldData.Chunks)
			{
				var dist = chunk.DistanceFromPlayer();
				if (dist <= INITIAL_CHUNK_RENDER_DISTANCE * Chunk.CHUNK_SIZE && chunk.IsInFrustum)
				{
					//chunk is close enough to the player and in the initial view frustum so it needs to be renderable before the game starts
					int taskX = chunk.Coords.X, taskZ = chunk.Coords.Z;
					immediateChunkTasks.Add(Task.Factory.StartNew(() => { WorldData.Chunks[taskX, taskZ].ChunkBuildState = Chunk.BuildState.QueuedInitialFrustum; WorldData.Chunks[taskX, taskZ].BuildData(); }));
				}
				else
				{
					//chunk is outside distance to render initially; now check if it needs to be rendered after initial load
					if (dist < Settings.ZFarForChunkLoad) chunksByDist.Add(chunk);
				}
			}
			Task.WaitAll(immediateChunkTasks.ToArray()); //wait for initial chunks to finish building before we continue
			stopwatch.Stop();
			Debug.WriteLine("Initial chunk frustum load time: {0}ms ({1} chunks)", stopwatch.ElapsedMilliseconds, immediateChunkTasks.Count);
			
			//now sort by distance and queue so nearby chunks are built first
			chunksByDist.Sort((c1, c2) => Math.Sqrt(Math.Pow(Game.Player.Coords.Xblock - c1.Coords.WorldCoordsX, 2) + Math.Pow(Game.Player.Coords.Zblock - c1.Coords.WorldCoordsZ, 2)).CompareTo(Math.Sqrt(Math.Pow(Game.Player.Coords.Xblock - c2.Coords.WorldCoordsX, 2) + Math.Pow(Game.Player.Coords.Zblock - c2.Coords.WorldCoordsZ, 2))));
			chunksByDist.ForEach(c => { c.ChunkBuildState = Chunk.BuildState.QueuedInitialFar; });

			//reset the LastUpdate on all items so they don't go flying
			foreach (var gameItem in WorldData.GameItems.Values)
			{
				gameItem.LastUpdate = DateTime.Now;
			}
		}
		#endregion

		#region Build Chunks Thread
		internal static readonly AutoResetEvent BuildChunkHandle = new AutoResetEvent(false);
		/// <summary>Runs in a thread. Priority is given to chunks that have changed as this is most noticeable to the player.</summary>
		internal static void BuildChunksThread()
		{
			while (true)
			{
				BuildChunkHandle.WaitOne();
				if (Settings.ChunkUpdatesDisabled) continue;

				while (!ChangedChunkQueue.IsEmpty || !FarChunkQueue.IsEmpty)
				{
					Chunk chunk;
					if (!ChangedChunkQueue.IsEmpty && ChangedChunkQueue.TryDequeue(out chunk) && chunk.ChunkBuildState == Chunk.BuildState.Queued)
					{
						chunk.BuildData();
					}
					else if (!FarChunkQueue.IsEmpty && FarChunkQueue.TryDequeue(out chunk) && (chunk.ChunkBuildState == Chunk.BuildState.QueuedDayNight || chunk.ChunkBuildState == Chunk.BuildState.QueuedFar || chunk.ChunkBuildState == Chunk.BuildState.QueuedInitialFar))
					{
						chunk.BuildData();
					}
				}
			}
		// ReSharper disable FunctionNeverReturns
		}
		// ReSharper restore FunctionNeverReturns
		#endregion

		#region Render
		public void Render(FrameEventArgs e)
		{
			GL.PushAttrib(AttribMask.EnableBit);
			RenderPlayers(e);
			RenderWorld(e);
			RenderPlayerNameplates();
			GL.PopAttrib();
		}

		private static void RenderPlayers(FrameEventArgs e)
		{
			if (GameActions.NetworkClient.Players.Count <= 1) return; //skip if theres no other players
			foreach (var player in GameActions.NetworkClient.Players.Values.Where(player => player.Id != Game.Player.Id)) player.Render(e);
			GameObjects.GameObject.ResetColor();
		}

		/// <summary>Render player nameplates. Done after world rendering so there are never any blending issues for nameplates. As a result, nameplates will 'show through' other transparent blocks.</summary>
		private static void RenderPlayerNameplates()
		{
			if (GameActions.NetworkClient.Players.Count <= 1) return; //skip if theres no other players
			foreach (var player in GameActions.NetworkClient.Players.Values.Where(player => player.Id != Game.Player.Id)) player.RenderNameplate();
		}

		private static void RenderWorld(FrameEventArgs e)
		{
			Game.PerformanceHost.ChunksRendered = 0;
			Facing dir = Game.Player.Coords.DirectionFacing();
			switch (dir)
			{
				case Facing.East:
				case Facing.West:
					int startX = (dir == Facing.East ? WorldData.SizeInChunksX - 1 : 0);
					int incrementXBy = (dir == Facing.East ? -1 : 1);
					int endX = (dir == Facing.East ? -1 : WorldData.SizeInChunksX);

					//***OPAQUE STAGE***
					//render opaque blocks
					for (int i = startX; i != endX; i += incrementXBy)
					{
						for (int j = 0; j < WorldData.SizeInChunksZ; j++)
						{
							var chunk = WorldData.Chunks[i, j];
							chunk.RenderOpaqueFaces(e);
						}
					}

					//***TRANSPARENT STAGE***
					//render transparent blocks
					GL.Enable(EnableCap.Blend);
					GL.Disable(EnableCap.CullFace);

					for (int i = startX; i != endX; i += incrementXBy)
					{
						for (int j = 0; j < WorldData.SizeInChunksZ; j++) //todo: work outside-in toward player if there are still blending issues
						{
							WorldData.Chunks[i, j].RenderTransparentFaces();
						}
					}
					break;
				case Facing.South:
				case Facing.North:
					int startZ = (dir == Facing.South ? WorldData.SizeInChunksZ - 1 : 0);
					int incrementZBy = (dir == Facing.South ? -1 : 1);
					int endZ = (dir == Facing.South ? -1 : WorldData.SizeInChunksZ);

					//***OPAQUE STAGE***
					//render opaque blocks
					for (int j = startZ; j != endZ; j += incrementZBy)
					{
						for (int i = 0; i < WorldData.SizeInChunksX; i++)
						{
							var chunk = WorldData.Chunks[i, j];
							chunk.RenderOpaqueFaces(e);
						}
					}

					//***TRANSPARENT STAGE***
					//render transparent blocks
					GL.Enable(EnableCap.Blend);
					GL.Disable(EnableCap.CullFace);

					for (int j = startZ; j != endZ; j += incrementZBy)
					{
						for (int i = 0; i < WorldData.SizeInChunksX; i++) //todo: work outside-in toward player if there are still blending issues
						{
							WorldData.Chunks[i, j].RenderTransparentFaces();
						}
					}
					break;
			}
		}
		#endregion

		public void Resize(EventArgs e)
		{

		}

		public void Update(FrameEventArgs e)
		{
			RotationCounter = (RotationCounter + 1) % 360;

			WorldData.Chunks.Update(e);
			GameObjects.GameItems.GameItemDynamic.UpdateAll(e);
		}

		public void Dispose()
		{
		}

		public bool Enabled { get; set; }
	}
}