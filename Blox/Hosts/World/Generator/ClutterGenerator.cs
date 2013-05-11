using System.Collections.Generic;
using Hexpoint.Blox.GameObjects.GameItems;

namespace Hexpoint.Blox.Hosts.World.Generator
{
	internal static class ClutterGenerator
	{
		private const byte MIN_CLUTTER_PER_CHUNK = 4;
		private const byte MAX_CLUTTER_PER_CHUNK = 7;
		private const byte DISTANCE_TOLERANCE = 2;

		internal static void Generate(Chunk chunk, List<Position> takenPositions)
		{
			int clutterCount = Settings.Random.Next(MIN_CLUTTER_PER_CHUNK, MAX_CLUTTER_PER_CHUNK + 1);
			for (int i = 0; i < clutterCount; i++)
			{
				//returns number avoiding upper chunk boundaries ensuring cross chunk placements dont touch each other
				int xProposedInChunk = Settings.Random.Next(0, Chunk.CHUNK_SIZE - 1);
				int zProposedInChunk = Settings.Random.Next(0, Chunk.CHUNK_SIZE - 1);
				int yProposed = chunk.HeightMap[xProposedInChunk, zProposedInChunk];

				var block = chunk.Blocks[xProposedInChunk, yProposed, zProposedInChunk];
				if (block.Type != Block.BlockType.Grass && block.Type != Block.BlockType.Snow) continue;
				int xProposedInWorld = chunk.Coords.WorldCoordsX + xProposedInChunk;
				int zProposedInWorld = chunk.Coords.WorldCoordsZ + zProposedInChunk;

				//ensure clutter is not placed too close to another taken coord, otherwise skip it
				if (TreeGenerator.IsPositionTaken(takenPositions, xProposedInWorld, zProposedInWorld, DISTANCE_TOLERANCE)) continue;

				//place the clutter
				var clutterType = (ClutterType)Settings.Random.Next(0, 7); //0-6
				takenPositions.Add(new Position(xProposedInWorld, yProposed, zProposedInWorld));
				chunk.Clutters.Add(new Clutter(new Coords(xProposedInWorld, yProposed + 1, zProposedInWorld), clutterType)); //add new clutter to the chunk collection
			}
		}
	}
}
