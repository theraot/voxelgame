using System;
using System.Diagnostics;
using Hexpoint.Blox.Hosts.World;

namespace Hexpoint.Blox.GameActions
{
	/// <summary>
	/// Server will send to clients to keep things that are constantly moving in sync over time.
	/// Server will never receive this action and clients will never send it.
	/// This also gives us an additional benefit of flushing out clients that have disconnected without sending the Disconnect action
	/// as the server doesnt figure it out until trying to send them something.
	/// </summary>
	/// <remarks>ex: Sun radians would get slowly out of sync on each client over long periods of time.</remarks>
	internal class ServerSync : GameAction
	{
		internal ServerSync()
		{
			DataLength = sizeof(float);
		}

		/// <summary>Accept network player in constructor because this action is only sent by servers.</summary>
		internal ServerSync(float sunRadians, Server.NetworkPlayer player) : this()
		{
			SunRadians = sunRadians;
			ConnectedPlayer = player;
		}

		internal float SunRadians;

		internal override ActionType ActionType
		{
			get { return ActionType.ServerSync; }
		}

		public override string ToString()
		{
			return "ServerSync";
		}

		protected override void Queue()
		{
			Debug.Assert(Config.IsServer, "Only servers should send ServerSync packets.");
			base.Queue();
			Write(SunRadians);
		}

		internal override void Receive()
		{
			Debug.Assert(!Config.IsSinglePlayer && !Config.IsServer, "Single player or Server should not receive ServerSync packets.");
			lock (TcpClient)
			{
				base.Receive();
				var bytes = ReadStream(DataLength);
				SunRadians = BitConverter.ToSingle(bytes, 0);
			}
			Debug.WriteLine("Received Sync from server.");

			SkyHost.SunAngleRadians = SunRadians;
		}
	}
}
