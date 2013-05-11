using Hexpoint.Blox.Textures;

namespace Hexpoint.Blox.Hosts.World
{
	internal struct Block
	{
		public Block(BlockType type)
		{
			BlockData = (ushort)type;
		}

		public Block(ushort blockData)
		{
			BlockData = blockData;
		}

		public ushort BlockData;

		public BlockType Type
		{
			get { return (BlockType)(BlockData & 0xFF); }
			set { BlockData = (ushort)(BlockData & 0xFF00 | (byte)value); }
		}

		/// <summary>Block types starting with 'Placeholder' are not included in the action picker buttons grid and will appear white because they dont have associated textures.</summary>
		public enum BlockType : byte
		{
			//Naturally occurring
			Air = 0,
			Water = 1,
			Dirt = 2,
			Grass = 3,
			Snow = 4,
			Sand = 5,
			SandDark = 6,
			Ice = 7,
			Gravel = 8,
			Rock = 9,
			Coal = 10,
			Copper = 11,
			Iron = 12,
			Gold = 13,
			Oil = 14,
			Tree = 15,
			ElmTree = 16,
			Leaves = 17,
			SnowLeaves = 18,
			Lava = 19,
			LavaRock = 20,
			
			//Crafted Material
			WoodTile1 = 50,
			WoodTile2,
			Bricks,
			Cobble = 54,

			//Crafted Item
			PlaceholderWorkBench = 100,
			PlaceholderStove,
			PlaceholderFurnace,
			PlaceholderPipeline,
			Crate,
			Placeholder2, //removed, leaving placeholder to not break worlds
			Shelf1,
			SteelDoorTop,
			SteelDoorBottom,
			Placeholder3, //removed, leaving placeholder to not break worlds
			Speaker,
			PrisonBars,
			
			//Other
			Placeholder4 = 220, //removed, leaving placeholder to not break worlds
			Placeholder5, //removed, leaving placeholder to not break worlds
			Placeholder1, //removed, leaving placeholder to not break worlds
			Placeholder6, //removed, leaving placeholder to not break worlds
			Placeholder7, //removed, leaving placeholder to not break worlds
			Placeholder8, //removed, leaving placeholder to not break worlds
			FancyBlack,
			FancyGreen,
			FancyRed,
			FancyWhite,
			Placeholder9, //removed, leaving placeholder to not break worlds
			SteelPlate,
			SteelPlate2,
		}

		public Facing Orientation
		{
			get { return (Facing)(BlockData >> 8 & 0x3); }
			set { BlockData = (ushort)((BlockData & 0xFCFF) | (byte)value << 8); }
		}

		/// <summary>Is this block solid. Solid blocks cause collision.</summary>
		/// <remarks>Note that some transparent blocks can be considered solid if they also cause collision.</remarks>
		public bool IsSolid
		{
			get { return IsBlockTypeSolid(Type); }
		}

		public static bool IsBlockTypeSolid(BlockType type)
		{
			switch (type)
			{
				case BlockType.Air:
				case BlockType.Water:
					return false;
				default:
					return true;
			}
		}

		/// <summary>Is this block transparent.</summary>
		/// <remarks>Note that some blocks are transparent but still considered solid.</remarks>
		public bool IsTransparent
		{
			get { return IsBlockTypeTransparent(Type); }
		}

		public static bool IsBlockTypeTransparent(BlockType type)
		{
			switch (type)
			{
				case BlockType.Air:
				case BlockType.Leaves:
				case BlockType.SnowLeaves:
				case BlockType.Water:
				case BlockType.PrisonBars:
				case BlockType.SteelDoorTop:
					return true;
				default:
					return false;
			}
		}

		/// <summary>Is this block a light emitting source.</summary>
		public bool IsLightSource
		{
			get { return LightStrength > 0; }
		}

		/// <summary>Light strength this block emits.</summary>
		public byte LightStrength
		{
			get
			{
				switch (Type)
				{
					case BlockType.Lava:
						return 11;
					case BlockType.LavaRock:
						return 10;
					default:
						return 0;
				}
			}
		}

		public static BlockTextureType FaceTexture(BlockType type, Face face)
		{
			switch (type)
			{
				case BlockType.Water:
					return BlockTextureType.Water;
				case BlockType.Grass:
					switch (face)
					{
						case Face.Top:
							return BlockTextureType.Grass;
						case Face.Bottom:
							return BlockTextureType.Dirt;
						default:
							return BlockTextureType.GrassSide;
					}
				case BlockType.Bricks:
					return BlockTextureType.Bricks;
				case BlockType.Coal:
					return BlockTextureType.Coal;
				case BlockType.Cobble:
					return BlockTextureType.Cobble;
				case BlockType.Copper:
					return BlockTextureType.Copper;
				case BlockType.Crate:
					switch (face)
					{
						case Face.Top:
						case Face.Bottom:
							return BlockTextureType.CrateSide;
						default:
							return BlockTextureType.Crate;
					}
				case BlockType.Dirt:
					return BlockTextureType.Dirt;
				case BlockType.FancyBlack:
					return BlockTextureType.FancyBlack;
				case BlockType.FancyGreen:
					return BlockTextureType.FancyGreen;
				case BlockType.FancyRed:
					return BlockTextureType.FancyRed;
				case BlockType.FancyWhite:
					return BlockTextureType.FancyWhite;
				case BlockType.Gold:
					return BlockTextureType.Gold;
				case BlockType.Gravel:
					return BlockTextureType.Gravel;
				case BlockType.WoodTile1:
					return BlockTextureType.WoodTile1;
				case BlockType.Ice:
					return BlockTextureType.Ice;
				case BlockType.Iron:
					return BlockTextureType.Iron;
				case BlockType.Lava:
					return BlockTextureType.Lava;
				case BlockType.LavaRock:
					return BlockTextureType.LavaRock;
				case BlockType.Leaves:
					return BlockTextureType.Leaves;
				case BlockType.WoodTile2:
					return BlockTextureType.WoodTile2;
				case BlockType.Oil:
					return BlockTextureType.Oil;
				case BlockType.PrisonBars:
					return BlockTextureType.PrisonBars;
				case BlockType.Sand:
					//this will prevent beaches in winter worlds; side effect is that any sand placed will instantly get snow on top
					//might be nice to have a SandSnowSide texture to make it look better, however that would require another block type, so this is ok for now
					if (WorldData.WorldType == WorldType.Winter && face == Face.Top) return BlockTextureType.Snow;
					return BlockTextureType.Sand;
				case BlockType.SandDark:
					return BlockTextureType.SandDark;
				case BlockType.Shelf1:
					return BlockTextureType.Shelf1;
				case BlockType.SteelDoorTop:
					return BlockTextureType.SteelDoorTop;
				case BlockType.SteelDoorBottom:
					return BlockTextureType.SteelDoorBottom;
				case BlockType.Snow:
					switch (face)
					{
						case Face.Top:
							return BlockTextureType.Snow;
						case Face.Bottom:
							return BlockTextureType.Dirt;
						default:
							return BlockTextureType.SnowSide;
					}
				case BlockType.SnowLeaves:
					return BlockTextureType.SnowLeaves;
				case BlockType.Speaker:
					return BlockTextureType.Speaker;
				case BlockType.SteelPlate:
					return BlockTextureType.SteelPlate;
				case BlockType.SteelPlate2:
					return BlockTextureType.SteelPlate2;
				case BlockType.Rock:
					return BlockTextureType.Rock;
				case BlockType.Tree:
					switch (face)
					{
						case Face.Top:
						case Face.Bottom:
							return BlockTextureType.TreeTrunk;
						default:
							return BlockTextureType.Tree;
					}
				case BlockType.ElmTree:
					switch (face)
					{
						case Face.Top:
						case Face.Bottom:
							return BlockTextureType.TreeTrunk;
						default:
							return BlockTextureType.ElmTree;
					}
				default:
					return BlockTextureType.Air;
			}
		}

		internal bool IsDirty
		{
			get { return (BlockData & 0x8000) != 0; }
		}
	}
}
