using System;
using Hexpoint.Blox.Hosts.World;
using Hexpoint.Blox.Utilities;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox.GameObjects.Units
{
	internal class Player : Unit
	{
		internal Player(int id, string userName, Coords coords) : base(ref coords)
		{
			Id = id;
			UserName = userName;
		}

		/// <summary>Player Id. Hides the derived GameObject.Id because players use their own Id sequencing.</summary>
		internal new int Id { get; private set; }
		internal string UserName { get; private set; }
		internal readonly int[] Inventory = new int[256]; //for now just the 256 block types

		/// <summary>Coords of the players head. One block higher then regular coords of the player.</summary>
		internal Coords CoordsHead
		{
			get { return new Coords(Coords.Xblock, Coords.Yblock + 1, Coords.Zblock); }
		}

		/// <summary>Coords of the players eyes. PLAYER_EYE_LEVEL higher then regular coords of the player.</summary>
		internal Coords CoordsEyes
		{
			get { return new Coords(Coords.Xf, Coords.Yf + Constants.PLAYER_EYE_LEVEL, Coords.Zf); }
		}

		internal float FallVelocity;

		internal bool EyesUnderWater { get; private set; }
		/// <summary>Check if players eyes are under water. Only checked from the InputHost for the local player so this gets calculated only once per update loop.</summary>
		internal void CheckEyesUnderWater()
		{
			var coordsEyes = CoordsEyes; //make copy so we can pass by ref to GetBlock
			EyesUnderWater = coordsEyes.Yf < Chunk.CHUNK_HEIGHT && WorldData.GetBlock(ref coordsEyes).Type == Block.BlockType.Water;
		}

		internal bool FeetUnderWater { get; private set; }
		/// <summary>Check if players feet are under water. Only checked from the InputHost for the local player so this gets calculated only once per update loop.</summary>
		internal void CheckFeetUnderWater()
		{
			FeetUnderWater = Coords.Yf < Chunk.CHUNK_HEIGHT && WorldData.GetBlock(ref Coords).Type == Block.BlockType.Water;
		}

		internal override void Render(FrameEventArgs e)
		{
			base.Render(e); //sets light color, the color assigned will be the block the lower body is on

			GlHelper.ResetTexture();

			//note that these rotations might seem counterintuitive - use the right-hand-rule -bm
			//render lower body
			GL.PushMatrix();
			GL.Translate(Coords.Xf, Coords.Yf, Coords.Zf);
			GL.Rotate(MathHelper.RadiansToDegrees(Coords.Direction), -Vector3.UnitY);
			GL.CallList(DisplayList.TorsoId);

			//render upper body
			GL.Translate(Vector3.UnitY * Constants.PLAYER_EYE_LEVEL); //moves to eye level and render head
			GL.Rotate(Math.Max(MathHelper.RadiansToDegrees(Coords.Pitch), -40), Vector3.UnitZ); //pitch head up and down, doesnt need to be turned because the body already turned. cap at -40degrees or it looks weird
			GL.CallList(DisplayList.HeadId);
			GL.PopMatrix();
		}

		/// <summary>
		/// Render nameplates last during the transparent rendering stage. Prevents ever seeing missing blocks when looking through transparent portion of player name.
		/// Nameplate is only rendered if the player is within the allowable nameplate viewing distance.
		/// </summary>
		/// <remarks>
		/// By moving this to render independently from the player, the names look much better, and even though we now loop through the players twice, we no longer
		/// need to enable/disable blending and lighting for each player like we did when we rendered the name with the player. So ultimately even though this seems less
		/// efficient, we actually use 4 less GL calls per player as well.
		/// </remarks>
		internal void RenderNameplate()
		{
			const int CHAR_WIDTH = 55;
			var distance = Game.Player.Coords.GetDistanceExact(ref Coords);
			if (distance > Constants.MAXIMUM_DISTANCE_TO_VIEW_NAMEPLATES) return; //if a player is outside the max distance then skip rendering the name
			float scale = Math.Max(0.003f, distance * 0.0002f); //keep the nameplate a minimum size, otherwise scale it relative to the distance to make names more readable at long distances

			//todo: at far distances the nameplate now overlaps onto players head, this is a side effect of making the char display lists draw from top down instead of bottom up
			GL.PushMatrix();
			GL.Translate(Coords.Xf, Coords.Yf + Constants.PLAYER_EYE_LEVEL + 0.8f, Coords.Zf); //nameplate goes above players head
			GL.Scale(scale, scale, scale);
			GL.Rotate(180, Vector3.UnitZ);
			GL.Rotate(MathHelper.RadiansToDegrees(Game.Player.Coords.Direction) - 90, Vector3.UnitY);

			GL.Translate(-(UserName.Length / 2f * CHAR_WIDTH), 0, 0); //centers the name
			foreach (char t in UserName)
			{
				GL.BindTexture(TextureTarget.Texture2D, Textures.TextureLoader.GetLargeCharacterTexture(t));
				GL.CallList(DisplayList.LargeCharId);
				GL.Translate(CHAR_WIDTH, 0, 0); //this is doing one useless translate at the end
			}
			GL.PopMatrix();
		}

		internal override string XmlElementName
		{
			//players arent saved in the xml file yet, in the future we might want to, to store where the player is when they log off etc.
			get { throw new NotImplementedException(); }
		}

	}
}
