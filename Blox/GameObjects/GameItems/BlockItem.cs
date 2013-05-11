using System;
using System.Xml;
using Hexpoint.Blox.Hosts.World;
using Hexpoint.Blox.Utilities;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox.GameObjects.GameItems
{
	internal class BlockItem : GameItemDynamic
	{
		internal BlockItem(ref Coords coords, Block.BlockType blockType, Vector3? velocity = null, int id = -1) : base(ref coords, GameItemType.BlockItem, true, velocity, id)
		{
			Coords.Xf = (float)Math.Floor(Coords.Xf) + Constants.HALF_BLOCK_SIZE; //spawn in the middle of the block
			Coords.Yf += Constants.HALF_BLOCK_SIZE;
			Coords.Zf = (float)Math.Floor(Coords.Zf) + Constants.HALF_BLOCK_SIZE;
			if (!Coords.IsValidItemLocation) throw new Exception(string.Format("Invalid BlockItem location: {0}", Coords));

			switch (blockType)
			{
				case Block.BlockType.Grass:
				case Block.BlockType.Snow:
					BlockType = Block.BlockType.Dirt;
					break;
				default:
					BlockType = blockType;
					break;
			}
		}

		internal BlockItem(XmlNode xmlNode) : base(xmlNode)
		{
			if (xmlNode.Attributes == null) throw new Exception("Node attributes is null.");
			BlockType = (Block.BlockType)int.Parse(xmlNode.Attributes["BT"].Value);
		}

		internal Block.BlockType BlockType;
		internal override int DecaySeconds { get { return 600; } } //10min

		/// <summary>
		/// Give each item a random rotation counter upon creation. This allows all items to seemingly rotate independently.
		/// It doesnt matter that this number isnt consistent each time the world is loaded or between players on servers
		/// because its only used for item rotation and bobbing.
		/// </summary>
		private readonly int _rotationDegreesRandom = Settings.Random.Next(360);

		internal override void Render(FrameEventArgs e)
		{
			base.Render(e);

			GL.PushMatrix();
			var degrees = (_rotationDegreesRandom + WorldHost.RotationCounter) % 360;
			GL.Translate(Coords.Xf, Coords.Yf + Math.Sin(MathHelper.DegreesToRadians(degrees)) / 24f, Coords.Zf);
			GL.Rotate(degrees, -Vector3.UnitY);
			switch (BlockType)
			{
				case Block.BlockType.Tree:
				case Block.BlockType.ElmTree:
					DisplayList.RenderDisplayList(DisplayList.BlockQuarterId, Block.FaceTexture(BlockType, Face.Front));
					break;
				default:
					DisplayList.RenderDisplayList(DisplayList.BlockQuarterId, Block.FaceTexture(BlockType, Face.Top));
					break;
			}
			GL.PopMatrix();
		}

		internal override XmlNode GetXml(XmlDocument xmlDocument)
		{
			var xmlNode = base.GetXml(xmlDocument);
			if (xmlNode.Attributes == null) throw new Exception("Node attributes is null.");
			xmlNode.Attributes.Append(xmlDocument.CreateAttribute("BT")).Value = ((int)BlockType).ToString();
			return xmlNode;
		}
	}
}
