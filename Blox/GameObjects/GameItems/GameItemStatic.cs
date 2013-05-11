using System.Xml;
using Hexpoint.Blox.Hosts.World;

namespace Hexpoint.Blox.GameObjects.GameItems
{
	internal enum StaticItemType { Clutter, LightSource }

	/// <summary>
	/// Game items that cannot move and are static in the world unless destroyed.
	/// Stored at the chunk level only.
	/// </summary>
	internal abstract class GameItemStatic : GameObject
	{
		#region Constructors
		/// <summary>Use this constructor to create brand new items that will have the Coords shifted accordingly during creation.</summary>
		protected GameItemStatic(ref Position position, Face attachedToFace)
		{
			Coords = position.ToCoords();
			AttachedToFace = attachedToFace;
		}

		/// <summary>Use this constructor to create existing items that do not need to track an Id. ie: Clutter</summary>
		protected GameItemStatic(ref Coords coords, Face attachedToFace) : base(ref coords)
		{
			AttachedToFace = attachedToFace;
		}

		/// <summary>Use this constructor to create existing items that for example will come from the server and also need to track the Id. ie: LightSource</summary>
		protected GameItemStatic(ref Coords coords, Face attachedToFace, int id) : base(ref coords, id)
		{
			AttachedToFace = attachedToFace;
		}

		/// <summary>Use this constructor to create existing items that come from the world XML save file.</summary>
		protected GameItemStatic(XmlNode xmlNode, Face attachedToFace) : base(xmlNode)
		{
			AttachedToFace = attachedToFace;
		}
		#endregion

		internal abstract StaticItemType StaticItemType { get; }

		/// <summary>
		/// The face this item is attached to 'inside' this block. Needed to determine if the item is destroyed because the attached block gets destroyed.
		/// Can also be used to render extra features in the correct direction.
		/// </summary>
		internal Face AttachedToFace;
	}
}
