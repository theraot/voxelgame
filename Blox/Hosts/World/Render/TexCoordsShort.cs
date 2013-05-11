namespace Hexpoint.Blox.Hosts.World.Render
{
	/// <summary>Used for buffering tex coord data to a VBO.</summary>
	/// <remarks>Using short uses the smallest amount of memory possible for tex coord data.</remarks>
	internal struct TexCoordsShort
	{
		internal TexCoordsShort(short x, short y) { X = x; Y = y; }
		internal short X;
		internal short Y;
		internal const int SIZE = 4; //2 bytes each
		internal short[] Array { get { return new[] { X, Y }; } }
	}
}
