namespace Hexpoint.Blox.GameActions
{
	/// <summary>Test action used by client to get kicked off the server.</summary>
	internal class ThrowException : GameAction
	{
		internal ThrowException()
		{
			DataLength = 0;
		}

		internal override ActionType ActionType { get { return ActionType.ThrowException; } }

		public override string ToString()
		{
			return "ThrowException";
		}
	}
}