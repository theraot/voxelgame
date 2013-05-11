using System;
using System.Drawing;

namespace Hexpoint.Blox.Hosts.Ui
{
	public enum ChatMessageType : byte { Server, Global, Private, Team, SlashResult, Error }

	internal struct ChatMessage
	{
		public ChatMessage(ChatMessageType type, string message, string userId, DateTime timestamp)
		{
			Type = type;
			Message = message;
			UserId = userId;
			Timestamp = timestamp;
		}

		public ChatMessage(ChatMessageType type, string message)
		{
			Type = type;
			Message = message;
			UserId = string.Empty;
			Timestamp = null;
		}

		public ChatMessageType Type;
		public string Message;
		public string UserId;
		public DateTime? Timestamp;

		public Color Color
		{
			get
			{
				switch (Type)
				{
					case ChatMessageType.Server:
						return Color.Yellow;
					case ChatMessageType.Private:
						return Color.MediumPurple;
					case ChatMessageType.Team:
						return Color.LightGreen;
					case ChatMessageType.SlashResult:
						return Color.Orange;
					case ChatMessageType.Error:
						return Color.IndianRed;
					default:
						return Color.LightCyan;
				}
			}
		}
	}
}
