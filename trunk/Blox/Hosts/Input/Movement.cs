using System;
using Hexpoint.Blox.GameActions;
using Hexpoint.Blox.Hosts.World;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox.Hosts.Input
{
	internal static class Movement
	{
		internal static void MovePlayer(bool moveForward, bool moveBack, bool strafeLeft, bool strafeRight, FrameEventArgs e)
		{
			double distance = (moveBack ? -Settings.MoveSpeed : Settings.MoveSpeed) * e.Time;
			if (Math.Abs(distance) > 1) distance = Math.Sign(distance);
			if (Game.Player.EyesUnderWater)
			{
				distance *= 0.5; //50% move speed when under water
			}
			else if (InputHost.IsFloating && !Config.CreativeMode)
			{
				//if the player isnt in creative mode AND is floating AND is not under water; it probably means they walked out of a waterfall and floating should be cancelled
				InputHost.IsFloating = false;
			}
			double direction = Game.Player.Coords.Direction;
			if (strafeLeft ^ strafeRight) //xor, if strafing both ways then ignore as they would cancel each other out
			{
				if (moveForward)
				{
					direction += strafeLeft ? -MathHelper.PiOver4 : MathHelper.PiOver4; //move forward diagonally while strafing
				}
				else if (moveBack)
				{
					direction += strafeLeft ? MathHelper.PiOver4 : -MathHelper.PiOver4; //move back diagonally while strafing
				}
				else //strafing only
				{
					direction += strafeLeft ? -MathHelper.PiOver2 : MathHelper.PiOver2;
				}
			}

			double collisionTestDistance = distance + Math.Sign(distance) * Constants.MOVE_COLLISION_BUFFER; //account for same distance whether player is going forward or backward
			bool moved = false;

			//move along the X plane
			var destCoords = Game.Player.Coords;
			destCoords.Xf += (float)(Math.Cos(direction) * collisionTestDistance);
			if (destCoords.IsValidPlayerLocation)
			{
				Game.Player.Coords.Xf += (float)(Math.Cos(direction) * distance);
				moved = true;
			}

			//move along the Z plane
			destCoords = Game.Player.Coords;
			destCoords.Zf += (float)(Math.Sin(direction) * collisionTestDistance);
			if (destCoords.IsValidPlayerLocation)
			{
				Game.Player.Coords.Zf += (float)(Math.Sin(direction) * distance);
				moved = true;
			}

			if (moved) NetworkClient.SendPlayerLocation(Game.Player.Coords);
		}

		internal static void RotateDirection(float radians)
		{
			Game.Player.Coords.Direction += radians;
			NetworkClient.SendPlayerLocation(Game.Player.Coords);
		}

		internal static void RotatePitch(float radians)
		{
			Game.Player.Coords.Pitch += radians;
			NetworkClient.SendPlayerLocation(Game.Player.Coords);
		}

		/// <summary>
		/// Checks if either the players feet or eyes are entering or exiting water. Plays sound and changes Fog where applicable.
		/// Contained here so it only gets calculated once per update.
		/// </summary>
		internal static void CheckPlayerEnteringOrExitingWater()
		{
			var eyesUnderWaterPreviously = Game.Player.EyesUnderWater;
			var feetUnderWaterPreviously = Game.Player.FeetUnderWater;
			Game.Player.CheckEyesUnderWater(); //only calc this once per update for local player
			Game.Player.CheckFeetUnderWater(); //only calc this once per update for local player
			if (eyesUnderWaterPreviously != Game.Player.EyesUnderWater) //players eyes entered or exited water on this update
			{
				if (Game.Player.EyesUnderWater) //players eyes entered water
				{
					GL.Fog(FogParameter.FogMode, (int)FogMode.Exp); //density param applies to the Exponential fog mode
					GL.Fog(FogParameter.FogDensity, 0.06f);
					//GL.Fog(FogParameter.FogColor, new[] { 0.2f, 0.5f, 0.8f, 1.0f }); //51, 128, 204
					GL.Fog(FogParameter.FogColor, WorldHost.FogColorUnderWater.ToFloatArray());
				}
				else //players eyes exited water
				{
					Utilities.Misc.SetFogParameters();
				}
			}
			if (feetUnderWaterPreviously != Game.Player.FeetUnderWater) //players feet entered or exited water on this update
			{
				//play splash sound if players feet entered water during a fall, otherwise play jump out of water sound
				if (Game.Player.FeetUnderWater && Game.Player.FallVelocity * 2 > 0.05) Sounds.Audio.PlaySoundIfNotAlreadyPlaying(Sounds.SoundType.Splash, Game.Player.FallVelocity * 2); else Sounds.Audio.PlaySoundIfNotAlreadyPlaying(Sounds.SoundType.JumpOutOfWater);
			}
		}
	}
}
