using System;
using System.Diagnostics;
using Hexpoint.Blox.Hosts.World.Render;
using Hexpoint.Blox.Utilities;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox.Hosts.World
{
	internal class SkyHost : IHost
	{
		/// <summary>SkyHost constructor. Note: constructor only runs for clients, not server as it only uses static methods in SkyHost.</summary>
		internal SkyHost()
		{
			SkyTopCurrentColor = SkyTopBrightestColor;
			SkyBottomCurrentColor = SkyBottomBrightestColor;
			var fa = SkyBottomCurrentColor.ToFloatArray();
			GL.ClearColor(fa[0], fa[1], fa[2], 1);
		}

		internal const float DEFAULT_SPEED_MULTIPLIER = 0.006f;
		internal const byte BRIGHTEST_SKYLIGHT_STRENGTH = 15;
		internal const byte DARKEST_SKYLIGHT_STRENGTH = 6; //if this changes, existing saved worlds at this previous value will have slightly off lighting until re-saved
		internal static Vector3 SunPosition;
		/// <summary>
		/// Sun angle in radians. Set in WorldSettings.LoadSettings initially. Updated when receiving ServerSync packets in multiplayer.
		/// 0 is dawn; 90 is noon; 180 is sunset; 270 is midnight
		/// </summary>
		internal static float SunAngleRadians { get; set; }

		private static readonly ColorRgb SkyTopBrightestColor = new ColorRgb(System.Drawing.Color.DodgerBlue);
		private static readonly ColorRgb SkyBottomBrightestColor = new ColorRgb(System.Drawing.Color.LightSkyBlue);
		internal static ColorRgb SkyTopCurrentColor { get; private set; }
		internal static ColorRgb SkyBottomCurrentColor { get; private set; }

		private static byte _sunLightStrength = BRIGHTEST_SKYLIGHT_STRENGTH; //default to full, important to set here because this is static and needs to be set before the game window loads and the host constructors run
		/// <summary>Sunlight current strength. Value can be DARKEST_SKYLIGHT_STRENGTH -> BRIGHTEST_SKYLIGHT_STRENGTH. Queues all visible chunks when setting to a value different from the previous.</summary>
		internal static byte SunLightStrength
		{
			get { return _sunLightStrength; }
			set
			{
				if (_sunLightStrength == value) return;
				Debug.Assert(value >= DARKEST_SKYLIGHT_STRENGTH && value <= BRIGHTEST_SKYLIGHT_STRENGTH, string.Format("Sunlight strength value should be in the range {0}-{1}", DARKEST_SKYLIGHT_STRENGTH, BRIGHTEST_SKYLIGHT_STRENGTH));
				_sunLightStrength = value;

				if (Config.IsServer || WorldData.Chunks == null) return; //World.Chunks will be null when the game is first loading, no need to requeue the chunks
				int chunkCount = WorldData.Chunks.QueueAllWithinViewDistance();
				Debug.WriteLine("Sunlight strength set to {0}; Queueing {1} visible chunks", value, chunkCount);
			}
		}

		internal static bool IsDaytime { get { return SunPosition.Y > 0; } }
		internal static DateTime Time
		{
			get
			{
				float time = (MathHelper.RadiansToDegrees(SunAngleRadians) + 90) % 360 * 24 / 360;
				return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, (int)time, (int)(time % 1 * 60), 0);
			}
		}
		internal static float SpeedMultiplier = DEFAULT_SPEED_MULTIPLIER; //servers get the default speed

		/// <summary>Update and move the sun/moon. Update the SunLightStrength. Used by clients and the server controller.</summary>
		/// <returns>float brightness in the range 0.05 -> 1.6</returns>
		internal static float UpdateSun(FrameEventArgs e)
		{
			//using the frame time in the calc, the sync isnt too bad, the clients seem to run a tiny bit faster then the server
			const float DARKEST = 0.02f; //darkest factor the sky can by multiplied by
			SunAngleRadians = (SunAngleRadians + (float)e.Time * SpeedMultiplier) % MathHelper.TwoPi; //use frame time in the calc or clients get out of sync way too fast
			SunPosition.X = (float)Math.Cos(SunAngleRadians);
			SunPosition.Y = (float)Math.Sin(SunAngleRadians);
			var brightness = Math.Max(DARKEST, SunPosition.Y + 0.6f); //DARKEST -> 1.6
			var rawStrength = Math.Ceiling(brightness * BRIGHTEST_SKYLIGHT_STRENGTH); //needs the ceiling to get back to the full brightness
			SunLightStrength = (byte)Math.Max(Math.Min(rawStrength, BRIGHTEST_SKYLIGHT_STRENGTH), DARKEST_SKYLIGHT_STRENGTH); //DARKEST_SKYLIGHT_STRENGTH -> BRIGHTEST_SKYLIGHT_STRENGTH
			return brightness;
		}

		public void Update(FrameEventArgs e)
		{
			var brightnessPercent = UpdateSun(e);
			if (brightnessPercent > 1) brightnessPercent = 1; //cant multiply by any more than 100%, sky is at 100% or greater for a large portion of the daytime where the sky gradient colors remain the same
			//need to update following values on every update in case the sun position has been altered either by an admin or server sync
			SkyTopCurrentColor = SkyTopBrightestColor * brightnessPercent;
			SkyBottomCurrentColor = SkyBottomBrightestColor * brightnessPercent;
			var fa = SkyBottomCurrentColor.ToFloatArray(); //need a float array to pass to ClearColor and Fog
			GL.ClearColor(fa[0], fa[1], fa[2], 1);
			GL.Fog(FogParameter.FogColor, Game.Player.EyesUnderWater ? (WorldHost.FogColorUnderWater * brightnessPercent).ToFloatArray() : fa);
		}

		/// <summary>
		/// Push the modelview matrix, load the identity and load a new matrix at the origin and looking the same way as the player.
		/// After rendering the skybox, enable blending and render the sun and moon.
		/// </summary>
		/// <remarks>
		/// A future optimization would be to draw all opaque world objects first and then render the skybox followed by transparent world objects.
		/// This prevents the pixel shader from drawing many of the same pixels twice. However is more complicated for a small benefit.
		/// </remarks>
		public void Render(FrameEventArgs e)
		{
			GL.PushMatrix();
			GL.LoadIdentity();
			var sky = Matrix4d.LookAt(0, 0, 0, (float)Math.Cos(Game.Player.Coords.Direction) * (float)Math.Cos(Game.Player.Coords.Pitch), (float)Math.Sin(Game.Player.Coords.Pitch), (float)Math.Sin(Game.Player.Coords.Direction) * (float)Math.Cos(Game.Player.Coords.Pitch), 0, 1, 0);
			GL.LoadMatrix(ref sky);

			GL.PushAttrib(AttribMask.EnableBit);
			GL.Disable(EnableCap.DepthTest);
			GL.Disable(EnableCap.Texture2D);

			const float TOP = 0.3f;
			const float BOTTOM = 0.04f; //make bottom color at the horizon so we can always set the fog color to the same and have nice blending
			const float SIDE = 0.5f;
			const float SIDE_OUT = SIDE + SIDE / 2; //where the sides stick out to create 8 faces

			//draw skybox as a triangle fan with a center top point and 8 bottom points, cuts down on the noticeable gradient corners
			//drawn clockwise because we are "inside" the skybox, not in a display list because the colors change on every render (could make a dynamic vbo)
			GL.Begin(BeginMode.TriangleFan);
			GL.Color3(SkyTopCurrentColor.ToFloatArray());
			GL.Vertex3(0, TOP, 0); //center top point
			GL.Color3(SkyBottomCurrentColor.ToFloatArray());
			GL.Vertex3(SIDE, BOTTOM, SIDE);
			GL.Vertex3(0, BOTTOM, SIDE_OUT);
			GL.Vertex3(-SIDE, BOTTOM, SIDE);
			GL.Vertex3(-SIDE_OUT, BOTTOM, 0);
			GL.Vertex3(-SIDE, BOTTOM, -SIDE);
			GL.Vertex3(0, BOTTOM, -SIDE_OUT);
			GL.Vertex3(SIDE, BOTTOM, -SIDE);
			GL.Vertex3(SIDE_OUT, BOTTOM, 0);
			GL.Vertex3(SIDE, BOTTOM, SIDE);
			GL.End();
			GlHelper.ResetColor();

			//render sun/moon
			GL.Enable(EnableCap.Texture2D);
			GL.Enable(EnableCap.Blend);
			DisplayList.RenderDisplayList(DisplayList.SunId, SunPosition.X, SunPosition.Y, 0, Textures.EnvironmentTextureType.Sun);
			DisplayList.RenderDisplayList(DisplayList.MoonId, -SunPosition.X, -SunPosition.Y, 0, Textures.EnvironmentTextureType.Moon);

			GL.PopAttrib();
			GL.PopMatrix();
		}

		public void Resize(EventArgs e)
		{
		}

		public void Dispose()
		{
		}

		public bool Enabled { get; set; }
	}
}
