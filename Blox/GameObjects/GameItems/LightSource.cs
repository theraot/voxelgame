using System;
using System.Xml;
using Hexpoint.Blox.Hosts.World;
using Hexpoint.Blox.Textures;
using Hexpoint.Blox.Utilities;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox.GameObjects.GameItems
{
	internal enum LightSourceType { PlaceholderTorch, Lantern }

	internal class LightSource : GameItemStatic
	{
		#region Constructors
		/// <summary>Use this constructor to create already existing Light Sources, such as would come from the server. Use the constructor that accepts Position to create a brand new Light Source.</summary>
		public LightSource(ref Coords coords, LightSourceType type, Face attachedToFace, int id) : base(ref coords, attachedToFace, id)
		{
			Type = type;
			LightStrength = GetLightSourceStrength();
			WorldData.Chunks[Coords].LightSources.TryAdd(Id, this);
		}

		/// <summary>Use this constructor when creating a brand new light source. Accounts for the type and modifies the coords accordingly.</summary>
		/// <param name="position">Position to create at. This will be a block position and therefore defaults to the back/left corner of the block initially.</param>
		/// <param name="type">Light source type to create.</param>
		/// <param name="attachedToFace">Face being attached to within this block.</param>
		public LightSource(ref Position position, LightSourceType type, Face attachedToFace) : base(ref position, attachedToFace)
		{
			Type = type;
			switch (type)
			{
				case LightSourceType.PlaceholderTorch:
					throw new NotSupportedException("Torch not finished.");
				case LightSourceType.Lantern:
					//adjust coords to account for the shape of a lantern (quarter block)
					const float EIGHTH = Constants.BLOCK_SIZE / 8;
					switch (AttachedToFace)
					{
						case Face.Front:
							Coords.Xf += Constants.HALF_BLOCK_SIZE;
							Coords.Yf += Constants.HALF_BLOCK_SIZE;
							Coords.Zf += EIGHTH * 7;
							break;
						case Face.Right:
							Coords.Xf += EIGHTH * 7;
							Coords.Yf += Constants.HALF_BLOCK_SIZE;
							Coords.Zf += Constants.HALF_BLOCK_SIZE;
							break;
						case Face.Top: //flush with roof
							Coords.Xf += Constants.HALF_BLOCK_SIZE;
							Coords.Yf += Constants.QUARTER_BLOCK_SIZE * 3;
							Coords.Zf += Constants.HALF_BLOCK_SIZE;
							break;
						case Face.Left:
							Coords.Xf += EIGHTH;
							Coords.Yf += Constants.HALF_BLOCK_SIZE;
							Coords.Zf += Constants.HALF_BLOCK_SIZE;
							break;
						case Face.Bottom: //flush with floor
							Coords.Xf += Constants.HALF_BLOCK_SIZE;
							Coords.Zf += Constants.HALF_BLOCK_SIZE;
							break;
						default: //back
							Coords.Xf += Constants.HALF_BLOCK_SIZE;
							Coords.Yf += Constants.HALF_BLOCK_SIZE;
							Coords.Zf += EIGHTH;
							break;
					}
					break;
				default:
					throw new Exception(string.Format("Cannot create unknown light source type: {0}", type));
			}
		}

		// ReSharper disable PossibleNullReferenceException
		public LightSource(XmlNode xmlNode) : base(xmlNode, (Face)int.Parse(xmlNode.Attributes["F"].Value))
		{
			Type = (LightSourceType)int.Parse(xmlNode.Attributes["T"].Value);
			LightStrength = GetLightSourceStrength();
			WorldData.Chunks[Coords].LightSources.TryAdd(Id, this);
		}
		// ReSharper restore PossibleNullReferenceException
		#endregion

		internal override StaticItemType StaticItemType
		{
			get { return StaticItemType.LightSource; }
		}

		private LightSourceType _type;
		internal LightSourceType Type
		{
			get { return _type; }
			private set
			{
				_type = value;
				switch (_type)
				{
					//case LightSourceType.PlaceholderTorch: _textureType = ItemTextureType.Torch; break;
					case LightSourceType.Lantern: _textureType = ItemTextureType.Lantern; break;
					default: throw new Exception("Item texture not found for item type: " + Type);
				}
			}
		}

		private ItemTextureType _textureType;

		internal byte LightStrength; //1-15

		private byte GetLightSourceStrength()
		{
			switch (Type)
			{
				case LightSourceType.PlaceholderTorch:
					return 13;
				case LightSourceType.Lantern:
					return 15;
				default:
					throw new Exception(string.Format("Unknown light source type: {0}", Type));
			}
		}

		internal override void Render(FrameEventArgs e)
		{
			base.Render(e);
			GL.BindTexture(TextureTarget.Texture2D, TextureLoader.GetItemTexture(_textureType));
			GL.CallList(DisplayList.BlockQuarterId);
		}

		internal override string XmlElementName
		{
			get { return "LS"; }
		}

		internal override XmlNode GetXml(XmlDocument xmlDocument)
		{
			var xmlNode = base.GetXml(xmlDocument);
			if (xmlNode.Attributes == null) throw new Exception("Node attributes is null.");
			xmlNode.Attributes.Append(xmlDocument.CreateAttribute("T")).Value = ((int)Type).ToString();
			xmlNode.Attributes.Append(xmlDocument.CreateAttribute("F")).Value = ((int)AttachedToFace).ToString();
			return xmlNode;
		}
	}
}
