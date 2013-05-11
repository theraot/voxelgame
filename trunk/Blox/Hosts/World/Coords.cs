using System.Collections.Generic;
using OpenTK;
using System;

namespace Hexpoint.Blox.Hosts.World
{
	/// <summary>
	/// Used for mobs, objects, etc. that need more specific positioning then just block position.
	/// Three floats for 3d position and additional floats for direction and pitch.
	/// </summary>
	internal struct Coords
	{
		public Coords(float x, float y, float z)
		{
			Xf = x;
			Yf = y;
			Zf = z;
			_direction = 0f;
			_pitch = 0f;
		}

		public Coords(float x, float y, float z, float direction, float pitch)
		{
			Xf = x;
			Yf = y;
			Zf = z;
			_direction = direction;
			_pitch = pitch;
		}

		/// <summary>Construct from a byte array containing the Coords values.</summary>
		/// <param name="bytes">Byte array containing coords values.</param>
		/// <param name="startIndex">Index position to start at in the byte array. Needed because sometimes other data has been sent first in the same byte array.</param>
		public Coords(byte[] bytes, int startIndex)
		{
			Xf = BitConverter.ToSingle(bytes, startIndex);
			Yf = BitConverter.ToSingle(bytes, startIndex + sizeof(float));
			Zf = BitConverter.ToSingle(bytes, startIndex + sizeof(float) * 2);
			_direction = BitConverter.ToSingle(bytes, startIndex + sizeof(float) * 3);
			_pitch = BitConverter.ToSingle(bytes, startIndex + sizeof(float) * 4);
		}

		public float Xf;
		public float Yf;
		public float Zf;

		/// <summary>X coord of the corresponding block. Readonly because Xf can always be safely set instead and this prevents accidental truncating.</summary>
		/// <remarks>the block coord is simply the truncated float</remarks>
		public int Xblock
		{
			get { return (int)Xf; } //the straight cast to int is faster then Math.Floor or Math.Truncate
		}

		/// <summary>Y coord of the corresponding block. Readonly because Yf can always be safely set instead and this prevents accidental truncating.</summary>
		/// <remarks>
		/// -the block coord is simply the truncated float
		/// -byte would be enough since you can never build higher then 256 blocks, however int is useful for allowing flying very high so the coords still calculate correctly
		/// </remarks>
		public int Yblock
		{
			get { return (int)Yf; } //the straight cast to int is faster then Math.Floor or Math.Truncate
		}

		/// <summary>Z coord of the corresponding block. Readonly because Zf can always be safely set instead and this prevents accidental truncating.</summary>
		/// <remarks>the block coord is simply the truncated float</remarks>
		public int Zblock
		{
			get { return (int)Zf; } //the straight cast to int is faster then Math.Floor or Math.Truncate
		}

		private float _direction;
		/// <summary>Direction in radians.</summary>
		public float Direction
		{
			get { return _direction; }
			set
			{
				_direction = value;
				if (_direction < 0) _direction += MathHelper.TwoPi; else if (_direction > MathHelper.TwoPi) _direction -= MathHelper.TwoPi;
			}
		}

		public Facing DirectionFacing()
		{
			if (Direction < MathHelper.PiOver4 || Direction > MathHelper.PiOver4 * 7) return Facing.East;
			if (Direction > MathHelper.PiOver4 * 5) return Facing.North;
			return Direction > MathHelper.PiOver4 * 3 ? Facing.West : Facing.South;
		}

		private float _pitch;
		/// <summary>Pitch in radians.</summary>
		public float Pitch
		{
			get { return _pitch; }
			set { _pitch = Math.Max(Math.Min(value, MathHelper.PiOver2 - 0.1f), -MathHelper.PiOver2 + 0.1f); }
		}

		/// <summary>Get the light color corresponding to the block these coords are on. Uses the light strength table to get the color value.</summary>
		/// <returns>byte color value 0-255</returns>
		public byte LightColor
		{
			get { return WorldData.GetBlockLightColor(Xblock, Yblock, Zblock); }
		}

		/// <summary>Get the light strength corresponding to the block these coords are on.</summary>
		/// <returns>byte strength value 0-15</returns>
		[Obsolete("Only usages moved to Position.")]
		public byte LightStrength
		{
			get { return WorldData.GetBlockLightStrength(Xblock, Yblock, Zblock); }
		}

		/// <summary>float * 5</summary>
		public const int SIZE = sizeof(float) * 5;
		
		public byte[] ToByteArray()
		{
			var bytes = new byte[SIZE];
			BitConverter.GetBytes(Xf).CopyTo(bytes, 0);
			BitConverter.GetBytes(Yf).CopyTo(bytes, sizeof(float));
			BitConverter.GetBytes(Zf).CopyTo(bytes, sizeof(float) * 2);
			BitConverter.GetBytes(Direction).CopyTo(bytes, sizeof(float) * 3);
			BitConverter.GetBytes(Pitch).CopyTo(bytes, sizeof(float) * 4);
			return bytes;
		}

		public bool IsValidBlockLocation
		{
			get { return WorldData.IsValidBlockLocation(Xblock, Yblock, Zblock); }
		}

		public bool IsValidPlayerLocation
		{
			get
			{
				return Xf >= 0 && Xf < WorldData.SizeInBlocksX
				       && Yf >= 0 && Yf <= 600 //can't see anything past 600
				       && Zf >= 0 && Zf < WorldData.SizeInBlocksZ
				       && (Yf >= Chunk.CHUNK_HEIGHT || !WorldData.GetBlock(ref this).IsSolid)
				       && (Yf + 1 >= Chunk.CHUNK_HEIGHT || !WorldData.GetBlock(Xblock, Yblock + 1, Zblock).IsSolid)
				       && (Yf % 1 < Constants.PLAYER_HEADROOM || Yf + 2 >= Chunk.CHUNK_HEIGHT || !WorldData.GetBlock(Xblock, Yblock + 2, Zblock).IsSolid); //the player can occupy 3 blocks
			}
		}

		public bool IsValidItemLocation
		{
			get { return WorldData.IsValidBlockLocation(Xblock, 0, Zblock) && Yf >= 0; }
		}

		[Obsolete("Only usages moved to Position.")]
		public bool IsOnChunkBorder
		{
			get { return WorldData.IsOnChunkBorder(Xblock, Zblock); }
		}

		/// <summary>
		/// Get a List of chunks this block is bordering. Result count must be in the range 0-2 because a block can border at most 2 chunks at a time.
		/// Accounts for world edges and does not add results in those cases.
		/// </summary>
		[Obsolete("Only usages moved to Position.")]
		public List<Chunk> BorderChunks
		{
			get { throw new NotSupportedException(); }
		}

		/// <summary>Get a List of the 6 directly adjacent positions. Exclude positions that are outside the world or on the base of the world.</summary>
		public List<Position> AdjacentPositions
		{
			get { return this.ToPosition().AdjacentPositions; }
		}

		/// <summary>Get the exact distance from the supplied coords.</summary>
		public float GetDistanceExact(ref Coords coords)
		{
			return (float)Math.Sqrt(Math.Pow(Xf - coords.Xf, 2) + Math.Pow(Yf - coords.Yf, 2) + Math.Pow(Zf - coords.Zf, 2));
		}

		/// <summary>Is this coord and the compare coord within the same block. Fast check as no math is required.</summary>
		/// <remarks>Use this to prevent building on blocks a player is standing on, etc.</remarks>
		[Obsolete("Only usages moved to Position.")]
		public bool IsOnBlock(ref Coords compare)
		{
			return Xblock == compare.Xblock && Yblock == compare.Yblock && Zblock == compare.Zblock;
		}

		internal Position ToPosition()
		{
			return new Position(Xblock, Yblock, Zblock);
		}

		/// <summary>Returns block coords in the format (x={0}, y={1}, z={2})</summary>
		public override string ToString()
		{
			return string.Format("(x={0}, y={1}, z={2})", Xblock, Yblock, Zblock);
		}
	}
}
