namespace Hexpoint.Blox
{
	[System.Reflection.Obfuscation(Exclude = true)] //gm: prevent obfuscation because these enum names are implicitly used with .ToString such as in string.Format
	internal enum Face : byte { Front, Right, Top, Left, Bottom, Back }

	/// <summary>North / East / South / West</summary>
	[System.Reflection.Obfuscation(Exclude = true)] //gm: prevent obfuscation because these enum names are implicitly used with .ToString such as in string.Format
	public enum Facing : byte { North, East, South, West }

	internal static class Enums
	{
		#region Enum Extensions
		internal static Face ToOpposite(this Face face)
		{
			switch (face)
			{
				case Face.Front:
					return Face.Back;
				case Face.Right:
					return Face.Left;
				case Face.Top:
					return Face.Bottom;
				case Face.Left:
					return Face.Right;
				case Face.Bottom:
					return Face.Top;
				default: //back
					return Face.Front;
			}
		}

		internal static Facing ToOpposite(this Facing facing)
		{
			switch (facing)
			{
				case Facing.North:
					return Facing.South;
				case Facing.East:
					return Facing.West;
				case Facing.South:
					return Facing.North;
				default: //west
					return Facing.East;
			}
		}

		internal static Face ToFace(this Facing facing)
		{
			switch (facing)
			{
				case Facing.North:
					return Face.Back;
				case Facing.East:
					return Face.Right;
				case Facing.South:
					return Face.Front;
				default: //west
					return Face.Left;
			}
		}
		#endregion
	}
}
