using System;
using System.Collections.Generic;

namespace Hexpoint.Blox.Hosts.World
{
	/// <summary>
	/// Specifies a block position in world coordinates using 3 integers.
	/// Used for buffering vertex position data to a VBO.
	/// </summary>
	internal struct Position
	{
		internal Position(int x, int y, int z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		/// <summary>Construct from a byte array containing the Position values.</summary>
		/// <param name="bytes">Byte array containing position values.</param>
		/// <param name="startIndex">Index position to start at in the byte array. Needed because sometimes other data has been sent first in the same byte array.</param>
		public Position(byte[] bytes, int startIndex)
		{
			X = BitConverter.ToInt32(bytes, startIndex);
			Y = BitConverter.ToInt32(bytes, startIndex + sizeof(int));
			Z = BitConverter.ToInt32(bytes, startIndex + sizeof(int) * 2);
		}

		internal int X;
		internal int Y;
		internal int Z;

		internal const int SIZE = sizeof(int) * 3;

		internal byte[] ToByteArray()
		{
			var bytes = new byte[SIZE];
			BitConverter.GetBytes(X).CopyTo(bytes, 0);
			BitConverter.GetBytes(Y).CopyTo(bytes, sizeof(int));
			BitConverter.GetBytes(Z).CopyTo(bytes, sizeof(int) * 2);
			return bytes;
		}

		/// <summary>
		/// Is this position a valid block location. Includes blocks on the base of the world even though they cannot be removed.
		/// This is because the cursor can still point at them, they can still receive light, etc.
		/// </summary>
		internal bool IsValidBlockLocation
		{
			get { return X >= 0 && X < WorldData.SizeInBlocksX && Y >= 0 && Y < Chunk.CHUNK_HEIGHT && Z >= 0 && Z < WorldData.SizeInBlocksZ; }
		}

		[Obsolete("If this is needed on a Position then you should be using Coords instead.")]
		internal bool IsValidPlayerLocation
		{
			get { throw new NotSupportedException(); }
		}

		/// <summary>Is this position and the compare coords the same block. Fast check as no math is required.</summary>
		/// <remarks>Use this to prevent building on blocks a player is standing on, etc.</remarks>
		internal bool IsOnBlock(ref Coords coords)
		{
			return X == coords.Xblock && Y == coords.Yblock && Z == coords.Zblock;
		}

		public bool IsValidItemLocation
		{
			get { return WorldData.IsValidBlockLocation(X, 0, Z) && Y >= 0; }
		}

		/// <summary>Get the light color corresponding to the block at this position. Uses the light strength table to get the color value.</summary>
		/// <returns>byte color value 0-255</returns>
		internal byte LightColor
		{
			get { return WorldData.GetBlockLightColor(X, Y, Z); }
		}

		/// <summary>Get the light strength corresponding to the block at this position.</summary>
		/// <returns>byte strength value 0-15</returns>
		internal byte LightStrength
		{
			get { return WorldData.GetBlockLightStrength(X, Y, Z); }
		}

		internal bool IsOnChunkBorder
		{
			get { return WorldData.IsOnChunkBorder(X, Z); }
		}

		/// <summary>
		/// Get a block using world coords. Looks up the chunk from the world chunks array and then the block in the chunk blocks array.
		/// Therefore if you have a chunk and chunk relative xyz its faster to get the block straight from the chunk blocks array.
		/// </summary>
		internal Block GetBlock()
		{
			return WorldData.Chunks[this].Blocks[this];
		}

		/// <summary>
		/// Get a List of chunks this block is bordering. Result count must be in the range 0-2 because a block can border at most 2 chunks at a time.
		/// Accounts for world edges and does not add results in those cases.
		/// </summary>
		internal List<Chunk> BorderChunks
		{
			get
			{
				var chunks = new List<Chunk>();
				//check in X direction
				if (X > 0 && X % Chunk.CHUNK_SIZE == 0)
				{
					chunks.Add(WorldData.Chunks[(X - 1) / Chunk.CHUNK_SIZE, Z / Chunk.CHUNK_SIZE]); //add left chunk
				}
				else if (X < WorldData.SizeInBlocksX - 1 && X % Chunk.CHUNK_SIZE == Chunk.CHUNK_SIZE - 1)
				{
					chunks.Add(WorldData.Chunks[(X + 1) / Chunk.CHUNK_SIZE, Z / Chunk.CHUNK_SIZE]); //add right chunk
				}
				//check in Z direction
				if (Z > 0 && Z % Chunk.CHUNK_SIZE == 0)
				{
					chunks.Add(WorldData.Chunks[X / Chunk.CHUNK_SIZE, (Z - 1) / Chunk.CHUNK_SIZE]); //add back chunk
				}
				else if (Z < WorldData.SizeInBlocksZ - 1 && Z % Chunk.CHUNK_SIZE == Chunk.CHUNK_SIZE - 1)
				{
					chunks.Add(WorldData.Chunks[X / Chunk.CHUNK_SIZE, (Z + 1) / Chunk.CHUNK_SIZE]); //add front chunk
				}
				return chunks;
			}
		}

		/// <summary>Get a List of the 6 directly adjacent positions. Exclude positions that are outside the world or on the base of the world.</summary>
		public List<Position> AdjacentPositions
		{
			get
			{
				var positions = new List<Position>();
				var left = new Position(X - 1, Y, Z);
				if (left.IsValidBlockLocation && left.Y >= 1) positions.Add(left);
				var right = new Position(X + 1, Y, Z);
				if (right.IsValidBlockLocation && right.Y >= 1) positions.Add(right);
				var front = new Position(X, Y, Z + 1);
				if (front.IsValidBlockLocation && front.Y >= 1) positions.Add(front);
				var back = new Position(X, Y, Z - 1);
				if (back.IsValidBlockLocation && back.Y >= 1) positions.Add(back);
				var top = new Position(X, Y + 1, Z);
				if (top.IsValidBlockLocation && top.Y >= 1) positions.Add(top);
				var bottom = new Position(X, Y - 1, Z);
				if (bottom.IsValidBlockLocation && bottom.Y >= 1) positions.Add(bottom);
				return positions;
			}
		}

		/// <summary>Get a List of the 6 directly adjacent positions and corresponding faces. Exclude positions that are outside the world or on the base of the world.</summary>
		public List<Tuple<Position, Face>> AdjacentPositionFaces
		{
			get
			{
				var positions = new List<Tuple<Position, Face>>();
				var left = new Position(X - 1, Y, Z);
				if (left.IsValidBlockLocation && left.Y >= 1) positions.Add(new Tuple<Position, Face>(left, Face.Left));
				var right = new Position(X + 1, Y, Z);
				if (right.IsValidBlockLocation && right.Y >= 1) positions.Add(new Tuple<Position, Face>(right, Face.Right));
				var front = new Position(X, Y, Z + 1);
				if (front.IsValidBlockLocation && front.Y >= 1) positions.Add(new Tuple<Position, Face>(front, Face.Front));
				var back = new Position(X, Y, Z - 1);
				if (back.IsValidBlockLocation && back.Y >= 1) positions.Add(new Tuple<Position, Face>(back, Face.Back));
				var top = new Position(X, Y + 1, Z);
				if (top.IsValidBlockLocation && top.Y >= 1) positions.Add(new Tuple<Position, Face>(top, Face.Top));
				var bottom = new Position(X, Y - 1, Z);
				if (bottom.IsValidBlockLocation && bottom.Y >= 1) positions.Add(new Tuple<Position, Face>(bottom, Face.Bottom));
				return positions;
			}
		}

		[Obsolete]internal int[] Array { get { return new[] {X, Y, Z}; } }

		/// <summary>Get the exact distance from the supplied coords.</summary>
		public float GetDistanceExact(ref Position position)
		{
			return (float)Math.Sqrt(Math.Pow(X - position.X, 2) + Math.Pow(Y - position.Y, 2) + Math.Pow(Z - position.Z, 2));
		}

		public static bool operator ==(Position p1, Position p2)
		{
			return p1.X == p2.X && p1.Y == p2.Y && p1.Z == p2.Z;
		}

		public static bool operator !=(Position p1, Position p2)
		{
			return p1.X != p2.X || p1.Y != p2.Y || p1.Z != p2.Z;
		}

		public override bool Equals(object obj)
		{
			return X == ((Position)obj).X && Y == ((Position)obj).Y && Z == ((Position)obj).Z;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// <summary>Returns block position in the format (x={0}, y={1}, z={2})</summary>
		public override string ToString()
		{
			return string.Format("(x={0}, y={1}, z={2})", X, Y, Z);
		}

		/// <summary>Get a Coords struct from this position. Pitch and direction will default to zero.</summary>
		internal Coords ToCoords()
		{
			return new Coords(X, Y, Z);
		}
	}
}
