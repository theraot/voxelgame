using Hexpoint.Blox.GameObjects.GameItems;
using Hexpoint.Blox.Hosts.World;

namespace Hexpoint.Blox.Server
{
	internal static class StructureBuilder
	{
		public static void BuildTree(Position position)
		{
			var leafPosition1 = new Position(position.X - 3, position.Y + 4, position.Z - 3);
			var leafPosition2 = new Position(position.X + 3, position.Y + 8, position.Z + 3);
			WorldData.PlaceCuboid(leafPosition1, leafPosition2, WorldData.WorldType == WorldType.Winter ? Block.BlockType.SnowLeaves : Block.BlockType.Leaves, true);
			WorldData.PlaceCuboid(position, new Position(position.X, position.Y + 6, position.Z), Block.BlockType.Tree, true);
		}

		public static void BuildCastle(Position center, int radius, int height, Block.BlockType blockType, Facing frontFace)
		{
			//make sure diagonal positions are valid
			if (!WorldData.IsValidBlockLocation(center.X - radius, center.Y, center.Z - radius)) return;
			if (!WorldData.IsValidBlockLocation(center.X + radius, center.Y, center.Z + radius)) return;

			//clear area with air
			WorldData.PlaceCuboid(new Position(center.X - radius, center.Y, center.Z - radius), new Position(center.X + radius, center.Y + height - 1, center.Z + radius), Block.BlockType.Air, true);
			//front wall
			WorldData.PlaceCuboid(new Position(center.X - radius, center.Y, center.Z + radius), new Position(center.X + radius, center.Y + height - 1, center.Z + radius), blockType, true);
			//back wall
			WorldData.PlaceCuboid(new Position(center.X - radius, center.Y, center.Z - radius), new Position(center.X + radius, center.Y + height - 1, center.Z - radius), blockType, true);
			//left wall
			WorldData.PlaceCuboid(new Position(center.X - radius, center.Y, center.Z - radius), new Position(center.X - radius, center.Y + height - 1, center.Z + radius), blockType, true);
			//right wall
			WorldData.PlaceCuboid(new Position(center.X + radius, center.Y, center.Z - radius), new Position(center.X + radius, center.Y + height - 1, center.Z + radius), blockType, true);

			//front and back tops
			for (var x = center.X - radius; x <= center.X + radius; x += 2)
			{
				WorldData.PlaceBlock(new Position(x, center.Y + height, center.Z + radius), blockType, true);
				WorldData.PlaceBlock(new Position(x, center.Y + height, center.Z - radius), blockType, true);
			}
			//side tops
			for (var z = center.Z - radius; z <= center.Z + radius; z += 2)
			{
				WorldData.PlaceBlock(new Position(center.X + radius, center.Y + height, z), blockType, true);
				WorldData.PlaceBlock(new Position(center.X - radius, center.Y + height, z), blockType, true);
			}

			//top wood floor
			WorldData.PlaceCuboid(new Position(center.X - radius + 1, center.Y + height - 2, center.Z - radius + 1), new Position(center.X + radius - 1, center.Y + height - 2, center.Z + radius - 1), Block.BlockType.WoodTile2, true);

			//door/lights
			Position doorBottom = center;
			Position outsideLight1;
			Position outsideLight2;
			switch (frontFace)
			{
				case Facing.North:
					doorBottom.Z -= radius;
					outsideLight1 = doorBottom;
					outsideLight1.Z--;
					outsideLight2 = outsideLight1;
					outsideLight1.X--;
					outsideLight2.X++;
					break;
				case Facing.East:
					doorBottom.X += radius;
					outsideLight1 = doorBottom;
					outsideLight1.X++;
					outsideLight2 = outsideLight1;
					outsideLight1.Z--;
					outsideLight2.Z++;
					break;
				case Facing.South:
					doorBottom.Z += radius;
					outsideLight1 = doorBottom;
					outsideLight1.Z++;
					outsideLight2 = outsideLight1;
					outsideLight1.X--;
					outsideLight2.X++;
					break;
				default:
					doorBottom.X -= radius;
					outsideLight1 = doorBottom;
					outsideLight1.X--;
					outsideLight2 = outsideLight1;
					outsideLight1.Z--;
					outsideLight2.Z++;
					break;
			}
			outsideLight1.Y += 2;
			outsideLight2.Y += 2;
			WorldData.PlaceCuboid(doorBottom, new Position(doorBottom.X, doorBottom.Y + 1, doorBottom.Z), Block.BlockType.Air, true); //clear space for door

			//add lights
			//todo: downside to this is its causing extra chunk queues and lightbox rebuilds, only for structure builds so not a huge deal for now
			//-clients are independently adding the lightsources, so id's might not match with server, for now theres no reason requiring them to match
			//-another quirk is if the location ends up not being valid, the client will get an error msg and not know why if this came from the server
			//-eventually may want a way for the AddStructure packet to include them, structures might get built containing many static items...
			if (WorldData.IsValidStaticItemPosition(outsideLight1))
			{
				var ls1 = new LightSource(ref outsideLight1, LightSourceType.Lantern, frontFace.ToFace().ToOpposite());
				new LightSource(ref ls1.Coords, ls1.Type, ls1.AttachedToFace, ls1.Id);
			}
			if (WorldData.IsValidStaticItemPosition(outsideLight2))
			{
				var ls2 = new LightSource(ref outsideLight2, LightSourceType.Lantern, frontFace.ToFace().ToOpposite());
				new LightSource(ref ls2.Coords, ls2.Type, ls2.AttachedToFace, ls2.Id);
			}
		}
	}
}