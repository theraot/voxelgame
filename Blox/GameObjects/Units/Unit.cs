using System.Xml;
using Hexpoint.Blox.Hosts.World;

namespace Hexpoint.Blox.GameObjects.Units
{
	internal abstract class Unit : GameObject
	{
		protected Unit(ref Coords coords) : base(ref coords)
		{
			
		}

		internal Unit(XmlNode xmlNode) : base(xmlNode)
		{
			
		}
	}
}