using System;
using System.Xml;
using Hexpoint.Blox.Hosts.World;
using Hexpoint.Blox.Textures;
using Hexpoint.Blox.Utilities;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox.GameObjects.GameItems
{
	internal enum ClutterType
	{
		Bush = 0,
		Grass1 = 1,
		Grass2 = 2,
		Grass3 = 3,
		Grass4 = 4,
		Grass5 = 5,
		Grass6 = 6,
		Grass7 = 7,
		Grass8 = 8
	}

	internal class Clutter : GameItemStatic
	{
		internal Clutter(Coords coords, ClutterType type) : base(ref coords, Face.Bottom)
		{
			Type = type;
			WorldData.Chunks[coords].Clutters.Add(this);
		}

		internal Clutter(XmlNode xmlNode) : base(xmlNode, Face.Bottom)
		{
			if (xmlNode.Attributes == null) throw new Exception("Node attributes is null.");
			Type = (ClutterType)int.Parse(xmlNode.Attributes["T"].Value);
			WorldData.Chunks[Coords].Clutters.Add(this);
		}

		internal override StaticItemType StaticItemType
		{
			get { return StaticItemType.Clutter; }
		}

		private ClutterType _type;
		internal ClutterType Type
		{
			get { return _type; }
			private set
			{
				_type = value;
				switch (_type)
				{
					case ClutterType.Bush: _textureType = ClutterTextureType.Bush; break;
					case ClutterType.Grass1: _textureType = ClutterTextureType.Grass1; break;
					case ClutterType.Grass2: _textureType = ClutterTextureType.Grass2; break;
					case ClutterType.Grass3: _textureType = ClutterTextureType.Grass3; break;
					case ClutterType.Grass4: _textureType = ClutterTextureType.Grass4; break;
					case ClutterType.Grass5: _textureType = ClutterTextureType.Grass5; break;
					case ClutterType.Grass6: _textureType = ClutterTextureType.Grass6; break;
					case ClutterType.Grass7: _textureType = ClutterTextureType.Grass7; break;
					case ClutterType.Grass8: _textureType = ClutterTextureType.Grass8; break;
					default: throw new Exception("Clutter texture not found for clutter type: " + Type);
				}
			}
		}

		private ClutterTextureType _textureType;

		internal override void Render(FrameEventArgs e)
		{
			base.Render(e);
			GL.BindTexture(TextureTarget.Texture2D, TextureLoader.GetClutterTexture(_textureType));
			GL.CallList(DisplayList.ClutterHalfId);
		}

		internal override string XmlElementName
		{
			get { return "C"; }
		}

		internal override XmlNode GetXml(XmlDocument xmlDocument)
		{
			var xmlNode = base.GetXml(xmlDocument);
			if (xmlNode.Attributes == null) throw new Exception("Node attributes is null.");
			xmlNode.Attributes.Append(xmlDocument.CreateAttribute("T")).Value = ((int)Type).ToString();
			return xmlNode;
		}
	}
}