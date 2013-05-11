using System;

// ReSharper disable CheckNamespace
namespace Hexpoint.Blox
// ReSharper restore CheckNamespace
{
	public class ServerConnectException : ApplicationException
	{
		public ServerConnectException(Exception innerException = null) : base(string.Format("Could not connect to server.{0}", innerException == null ? "" : "\n" + innerException.Message), innerException) { }
	}

	public class ServerDisconnectException : ApplicationException
	{
		public ServerDisconnectException(Exception innerException) : base("Lost connection to server: " + innerException.Message, innerException) { }
	}
}