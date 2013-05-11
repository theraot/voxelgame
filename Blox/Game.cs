using System;
using System.Diagnostics;
using System.Linq;
using Hexpoint.Blox.GameObjects.Units;
using Hexpoint.Blox.GameActions;
using Hexpoint.Blox.Hosts;
using Hexpoint.Blox.Hosts.Input;
using Hexpoint.Blox.Hosts.Ui;
using Hexpoint.Blox.Hosts.World;
using Hexpoint.Blox.Utilities;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox
{
	internal class Game : GameWindow
	{
		#region Constructors
		//gm: from the OpenTK source code (Graphics\GraphicsMode.cs), here is GraphicsMode.Default, it does seem to select sensible choices -> default display bpp, 16 bit depth buffer, 0 bit stencil buffer, 2 buffers
		public Game() : base(Constants.DEFAULT_GAME_WIDTH, Constants.DEFAULT_GAME_HEIGHT, GraphicsMode.Default, string.Format("Voxel Game {0}: {1}", Settings.VersionDisplay, Config.UserName))
		{
			//note: cant easily thread these loading tasks because they all need to run on the thread that creates the GL context
			Settings.Game = this;
			Diagnostics.LoadDiagnosticProperties();

			var stopwatch = new Stopwatch();
			stopwatch.Start();
			Sounds.Audio.LoadSounds(); //ensure sounds are loaded before they are needed
			Debug.WriteLine("Sounds load time: {0}ms", stopwatch.ElapsedMilliseconds);
			stopwatch.Restart();
			Textures.TextureLoader.Load(); //ensure textures are loaded before they are needed
			Debug.WriteLine("Textures load time: {0}ms", stopwatch.ElapsedMilliseconds);
			stopwatch.Restart();
			DisplayList.LoadDisplayLists(); //ensure display lists are loaded before they are needed
			Debug.WriteLine("Display Lists load time: {0}ms", stopwatch.ElapsedMilliseconds);

			VSync = Config.VSync ? VSyncMode.On : VSyncMode.Off;
		
			if (Config.IsSinglePlayer)
			{
				var playerCoords = new Coords(WorldData.SizeInBlocksX / 2f, 0, WorldData.SizeInBlocksZ / 2f); //start player in center of world
				playerCoords.Yf = WorldData.Chunks[playerCoords].HeightMap[playerCoords.Xblock % Chunk.CHUNK_SIZE, playerCoords.Zblock % Chunk.CHUNK_SIZE] + 1; //start player on block above the surface
				Player = new Player(0, Config.UserName, playerCoords);
				NetworkClient.Players.TryAdd(Player.Id, Player); //note: it is not possible for the add to fail on ConcurrentDictionary, see: http://www.albahari.com/threading/part5.aspx#_Concurrent_Collections
			}
		}
		#endregion

		#region Static Properties
		private static IHost[] _hosts = new IHost[5];
		public static PerformanceHost PerformanceHost;
		public static SkyHost SkyHost;
		public static WorldHost WorldHost;
		public static InputHost InputHost;
		public static BlockCursorHost BlockCursorHost;
		public static UiHost UiHost;

		public static Matrix4d Projection;
		public static Matrix4d ModelView;
		public static Vector4d LeftFrustum;
		public static Vector4d RightFrustum;
		public static Vector4d BottomFrustum;
		public static Vector4d TopFrustum;
		public static Vector4d NearFrustum;
		public static Vector4d FarFrustum;
		public static Player Player;
		#endregion

		/// <summary>Calculate the projection matrix. Needs to be done on initial startup and anytime game width, height or FOV changes.</summary>
		internal void CalculateProjectionMatrix()
		{
			Matrix4d.CreatePerspectiveFieldOfView(Settings.FieldOfView, Width / (float)Height, 0.01f, Settings.ZFar, out Projection);
		}

		protected override void OnLoad(EventArgs e)
		{
			//load hosts (change enabled to false for debugging with any combination of hosts)
			PerformanceHost = new PerformanceHost { Enabled = true };
			SkyHost = new SkyHost { Enabled = true };
			WorldHost = new WorldHost { Enabled = true };
			InputHost = new InputHost { Enabled = true };
			BlockCursorHost = new BlockCursorHost { Enabled = true };
			UiHost = new UiHost { Enabled = true };
			_hosts = new IHost[] {PerformanceHost, SkyHost, WorldHost, InputHost, BlockCursorHost, UiHost};

			NetworkClient.AcceptPackets = true;

			//enable GL states that wont change
			GL.Enable(EnableCap.Texture2D);
			GL.ShadeModel(ShadingModel.Smooth); //allows gradients on polygons when using different colors on the vertices (smooth is the default)
			//GL.Enable(EnableCap.PolygonSmooth); //gm: antialiasing, this is an outdated way of doing it and should be done by multisampling (for us we might be better off just not bothering)
			//GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest); //gm: no noticeable difference, outdated and prob makes no difference on most modern gpus
			//GL.Hint(HintTarget.GenerateMipmapHint, HintMode.Nicest); //gm testing: no noticeable difference
			GL.CullFace(CullFaceMode.Back); //Indicates which polygons should be discarded (culled) before they're converted to screen coordinates. The mode is either GL_FRONT, GL_BACK, or GL_FRONT_AND_BACK to indicate front-facing, back-facing, or all polygons. To take effect, culling must be enabled using glEnable() with GL_CULL_FACE; it can be disabled with glDisable() and the same argument.
			GL.FrontFace(FrontFaceDirection.Ccw); //Controls how front-facing polygons are determined. By default, mode is GL_CCW, which corresponds to a counterclockwise orientation of the ordered vertices of a projected polygon in window coordinates. If mode is GL_CW, faces with a clockwise orientation are considered front-facing
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.PolygonOffset(-1f, -90f); //used by the block cursor, negatives pull polygons closer to the camera

			if (Config.Fog)
			{
				GL.Enable(EnableCap.Fog);
				Misc.SetFogParameters();
			}

			//allow these types of pointers to be used with vbos
			GL.EnableClientState(ArrayCap.VertexArray);
			GL.EnableClientState(ArrayCap.ColorArray);
			GL.EnableClientState(ArrayCap.TextureCoordArray);

			//enable GL states that are on initially and hosts toggle when needed
			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.CullFace);

			CalculateProjectionMatrix();
			UpdateFrustum();
			WorldHost.BuildWorld();
		}

		protected override void OnResize(EventArgs e)
		{
			//prevent resizing below a minimum size
			if (Width < Constants.DEFAULT_GAME_WIDTH) { Width = Constants.DEFAULT_GAME_WIDTH; return; }
			if (Height < Constants.DEFAULT_GAME_HEIGHT) { Height = Constants.DEFAULT_GAME_HEIGHT; return; }

			GL.Viewport(ClientRectangle);
			CalculateProjectionMatrix();
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix(ref Projection);

			foreach (var host in _hosts.Where(host => host.Enabled)) host.Resize(e);
		}

		protected override void OnWindowStateChanged(EventArgs e)
		{
			base.OnWindowStateChanged(e);
			
			//gm: if the window is maximized and you change to full screen, it will first change the window state to normal before changing to full screen
			//and fires this event twice. this is why when you leave full screen it will always send the window back to normal
			switch (WindowState)
			{
				case WindowState.Fullscreen:
					Config.Windowed = false;
					Config.Save();
					break;
				case WindowState.Maximized:
					Config.Windowed = true;
					Config.Maximized = true;
					Config.Save();
					break;
				case WindowState.Normal:
					Config.Windowed = true;
					Config.Maximized = false;
					Config.Save();
					break;
			}
		}

		/// <summary>Update the ModelView and Frustum. Done on every update cycle and before the world is initially loaded so we can preload chunks in the initial frustum.</summary>
		private static void UpdateFrustum()
		{
			ModelView = Matrix4d.LookAt(Player.Coords.Xf, Player.Coords.Yf + Constants.PLAYER_EYE_LEVEL, Player.Coords.Zf, Player.Coords.Xf + (float)Math.Cos(Player.Coords.Direction) * (float)Math.Cos(Player.Coords.Pitch), Player.Coords.Yf + Constants.PLAYER_EYE_LEVEL + (float)Math.Sin(Player.Coords.Pitch), Player.Coords.Zf + (float)Math.Sin(Player.Coords.Direction) * (float)Math.Cos(Player.Coords.Pitch), 0, 1, 0);

			Matrix4d clip;
			Matrix4d.Mult(ref ModelView, ref Projection, out clip);
			LeftFrustum = new Vector4d(clip.M14 + clip.M11, clip.M24 + clip.M21, clip.M34 + clip.M31, clip.M44 + clip.M41);
			LeftFrustum.NormalizeFast();
			RightFrustum = new Vector4d(clip.M14 - clip.M11, clip.M24 - clip.M21, clip.M34 - clip.M31, clip.M44 - clip.M41);
			RightFrustum.NormalizeFast();

			BottomFrustum = new Vector4d(clip.M14 + clip.M12, clip.M24 + clip.M22, clip.M34 + clip.M32, clip.M44 + clip.M42);
			BottomFrustum.NormalizeFast();
			TopFrustum = new Vector4d(clip.M14 - clip.M12, clip.M24 - clip.M22, clip.M34 - clip.M32, clip.M44 - clip.M42);
			TopFrustum.NormalizeFast();
			
			NearFrustum = new Vector4d(clip.M14 + clip.M13, clip.M24 + clip.M23, clip.M34 + clip.M33, clip.M44 + clip.M43);
			NearFrustum.NormalizeFast();
			FarFrustum = new Vector4d(clip.M14 - clip.M13, clip.M24 - clip.M23, clip.M34 - clip.M33, clip.M44 - clip.M43);
			FarFrustum.NormalizeFast();
		}

		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			if (e.Time > Constants.UPDATE_TIME_ACCEPTABLE) return;

			UpdateFrustum();
			
			foreach (var host in _hosts.Where(host => host.Enabled)) host.Update(e);

			unchecked { Settings.UpdateCounter++; }
		}

		protected override void OnRenderFrame(FrameEventArgs e)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix(ref Projection);

			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadMatrix(ref ModelView);

			foreach (var host in _hosts)
			{
				//var err = GL.GetError(); //use this to track down which host is reporting a GL Error
				if (host.Enabled) host.Render(e);
			}

			SwapBuffers();
		}

		protected override void OnUnload(EventArgs e)
		{
			if (!Config.IsSinglePlayer) NetworkClient.Disconnect();
			foreach (var host in _hosts) host.Dispose();
			Sounds.Audio.Dispose();
			DisplayList.DeleteDisplayLists();
		}

	}
}