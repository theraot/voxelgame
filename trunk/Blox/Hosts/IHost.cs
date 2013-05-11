using System;
using OpenTK;

namespace Hexpoint.Blox.Hosts
{
	internal interface IHost : IDisposable
	{
		void Update(FrameEventArgs e);
		void Render(FrameEventArgs e);
		void Resize(EventArgs e);

		bool Enabled { get; set; }
	}
}
