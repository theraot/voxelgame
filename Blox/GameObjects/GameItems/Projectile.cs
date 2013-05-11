using System.Collections.Generic;
using Hexpoint.Blox.GameActions;
using Hexpoint.Blox.Hosts.World;
using Hexpoint.Blox.Utilities;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox.GameObjects.GameItems
{
	internal class Projectile : GameItemDynamic
	{
		internal Projectile(ref Coords coords, Block.BlockType blockType, bool allowBounce, Vector3? velocity = null, int id = -1) : base(ref coords, GameItemType.Projectile, allowBounce, velocity, id)
		{
			BlockType = blockType;

			//Stop += OnItemStop;
			Decay += OnItemDecay;
		}

		internal Block.BlockType BlockType;

		internal override void Render(FrameEventArgs e)
		{
			base.Render(e);

			GL.PushMatrix();
			GL.Translate(Coords.Xf, Coords.Yf, Coords.Zf);
			GL.Rotate(WorldHost.RotationCounter, -Vector3.UnitY);
			DisplayList.RenderDisplayList(DisplayList.BlockQuarterId, Block.FaceTexture(BlockType, Face.Top));
			GL.PopMatrix();
		}

		internal override int DecaySeconds { get { return 1; } } //projectiles decay as soon as they stop

		/// <summary>Projectile explodes on decay.</summary>
		internal void OnItemDecay(FrameEventArgs e)
		{
			if (Config.IsServer || Config.IsSinglePlayer)
			{
				var positions = Coords.AdjacentPositions;
				var removeBlocks = new List<RemoveBlock>();
				var addBlockItems = new List<AddBlockItem>();
				Settings.ChunkUpdatesDisabled = true;
				foreach (var position in positions)
				{
					var block = position.GetBlock();
					if (block.Type != Block.BlockType.Air && block.Type != Block.BlockType.Water)
					{
						WorldData.PlaceBlock(position, Block.BlockType.Air);
						var tempPosition = position; //copy to pass by ref
						removeBlocks.Add(new RemoveBlock(ref tempPosition));

						if (!block.IsTransparent && position.IsValidItemLocation)
						{
							var tempCoords = position.ToCoords(); //copy to pass by ref
							var newBlockItem = new BlockItem(ref tempCoords, block.Type);
							addBlockItems.Add(new AddBlockItem(ref newBlockItem.Coords, ref newBlockItem.Velocity, newBlockItem.BlockType, newBlockItem.Id));
						}
					}
				}
				Settings.ChunkUpdatesDisabled = false;

				if (Config.IsServer && removeBlocks.Count > 0)
				{
					foreach (var player in Server.Controller.Players.Values)
					{
						var removeBlockMulti = new RemoveBlockMulti {ConnectedPlayer = player};
						removeBlockMulti.Blocks.AddRange(removeBlocks);
						removeBlockMulti.BlockItems.AddRange(addBlockItems);
						removeBlockMulti.Send();
					}
				}
			}
		}
	}
}
