using System;
using System.Collections.Generic;
using System.Linq;

namespace Hexpoint.Blox.Hosts.World.Generator
{
	internal static class TreeGenerator
	{
		private const byte MIN_TREES_PER_CHUNK = 2;
		private const byte MAX_TREES_PER_CHUNK = 5;
		private const byte MIN_TRUNK_HEIGHT = 7;
		private const byte MAX_TRUNK_HEIGHT = 9;
		private const byte DISTANCE_TOLERANCE = 4;

		internal static void Generate(Chunk chunk, List<Position> takenPositions)
		{
			int numberOfTreesToGenerate = Settings.Random.Next(MIN_TREES_PER_CHUNK, MAX_TREES_PER_CHUNK + 1);
			for (int tree = 0; tree < numberOfTreesToGenerate; tree++)
			{
				//returns number avoiding upper chunk boundaries ensuring cross chunk placements dont touch each other
				int xProposedInChunk = Settings.Random.Next(0, Chunk.CHUNK_SIZE - 1);
				int zProposedInChunk = Settings.Random.Next(0, Chunk.CHUNK_SIZE - 1);
				int yProposed = chunk.HeightMap[xProposedInChunk, zProposedInChunk];

				var block = chunk.Blocks[xProposedInChunk, yProposed, zProposedInChunk];
				if (block.Type != Block.BlockType.Grass && block.Type != Block.BlockType.Snow) continue;
				int xProposedInWorld = chunk.Coords.WorldCoordsX + xProposedInChunk;
				int zProposedInWorld = chunk.Coords.WorldCoordsZ + zProposedInChunk;

				//ensure tree is not placed too close to another taken coord, otherwise skip it
				if (IsPositionTaken(takenPositions, xProposedInWorld, zProposedInWorld, DISTANCE_TOLERANCE)) continue;

				//generate a tree
				takenPositions.Add(new Position(xProposedInWorld, yProposed, zProposedInWorld));

				//create the tree blocks
				bool isElmTree = Settings.Random.Next(0, 6) == 0;
				int treeHeight = Settings.Random.Next(MIN_TRUNK_HEIGHT, MAX_TRUNK_HEIGHT + 1); //possible heights 7,8,9
				//int trunkHeight = treeHeight - 2; //top 2 levels get leaves, so actual trunks can be 5-7
				double leafRadius = Settings.Random.NextDouble() + 1.9 + ((treeHeight - MIN_TRUNK_HEIGHT) * 0.2); //will return 1.9-3.3 (influences taller trees to get a larger leaf radius)
				for (int yTrunkLevel = 1; yTrunkLevel <= treeHeight + 1; yTrunkLevel++)
				{
					var trunkPosition = new Position(xProposedInWorld, yProposed + yTrunkLevel, zProposedInWorld);
					if (yTrunkLevel < treeHeight) //place the trunk
					{
						chunk.Blocks[trunkPosition] = new Block(isElmTree ? Block.BlockType.ElmTree : Block.BlockType.Tree);
					}
					else //place leaves on the top 2 blocks of the trunk instead of more trunk pieces
					{
						chunk.Blocks[trunkPosition] = new Block(WorldData.WorldType == WorldType.Winter ? Block.BlockType.SnowLeaves : Block.BlockType.Leaves);
					}

					//place leaves at this trunk level
					if (yTrunkLevel < 3) continue;
					for (int leafX = -3; leafX <= 3; leafX++)
					{
						for (int leafZ = -3; leafZ <= 3; leafZ++)
						{
							if (leafX == 0 && leafZ == 0) continue; //dont replace the trunk
							if (Math.Sqrt(leafX * leafX + leafZ * leafZ + Math.Pow(treeHeight - leafRadius - yTrunkLevel + 1, 2)) > leafRadius) continue;
							var leafPosition = new Position(xProposedInWorld + leafX, yProposed + yTrunkLevel, zProposedInWorld + leafZ);
							if (leafPosition.IsValidBlockLocation && leafPosition.GetBlock().Type == Block.BlockType.Air)
							{
								//need to get the chunk because this block could be expanding into an adjacent chunk
								WorldData.Chunks[leafPosition].Blocks[leafPosition] = new Block(WorldData.WorldType == WorldType.Winter ? Block.BlockType.SnowLeaves : Block.BlockType.Leaves);
							}
						}
					}
				}
			}
		}

		/// <summary>Check if the proposed position has already been taken or would be within the distance tolerance of another taken position.</summary>
		internal static bool IsPositionTaken(List<Position> takenPositions, int xProposed, int zProposed, byte distanceTolerance)
		{
			//dont use the Y in this check
			return takenPositions.Any(takenPosition => Math.Abs(xProposed - takenPosition.X) + Math.Abs(zProposed - takenPosition.Z) <= distanceTolerance);
		}
	}
}
