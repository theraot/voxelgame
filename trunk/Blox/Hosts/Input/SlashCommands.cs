using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Hexpoint.Blox.GameObjects.GameItems;
using Hexpoint.Blox.GameObjects.Units;
using Hexpoint.Blox.GameActions;
using Hexpoint.Blox.Hosts.Ui;
using Hexpoint.Blox.Hosts.World;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox.Hosts.Input
{
	internal static class SlashCommands
	{
		internal static string LastSlashCommand = "";

		/// <summary>Process a user entered slash command.</summary>
		/// <remarks>
		/// when a command is successful use 'return' to leave the function
		/// when a command is not succesful use 'break' for an invalid command msg to get displayed
		/// </remarks>
		public static void ProcessSlashCommand(string text)
		{
			if (text.Length <= 1) { AddSlashResult("Unknown command."); return;}
			LastSlashCommand = text;
			var args = text.TrimStart('/').ToLower().Split(' ');
			switch (args[0])
			{
				case "?":
				case "help":
				case "commands":
					DisplayCommandHelp();
					return;
				case "admin":
					if (ArgCountInvalid(2, args)) return;
					new PlayerOption(PlayerOption.OptionType.Admin, System.Text.Encoding.UTF8.GetBytes(args[1])).Send();
					return;
				case "broadcast":
					if (Config.IsSinglePlayer) { AddSlashResult("Only available in Multiplayer."); return; }
					new ServerCommand(ServerCommandType.Broadcast).Send(); //this would still need a way to send the actual message
					return;
				case "clear":
					Game.UiHost.ClearChatMessages();
					return;
				case "cr":
				case "creative":
					if (args.Length == 1) { AddSlashResult(string.Format("Creative mode {0}.", Config.CreativeMode ? "On" : "Off")); return; }
					if (ArgCountInvalid(2, args)) return;
					switch (args[1])
					{
						case "on":
							new PlayerOption(PlayerOption.OptionType.Creative, BitConverter.GetBytes(1)).Send();
							return;
						case "off":
							new PlayerOption(PlayerOption.OptionType.Creative, BitConverter.GetBytes(0)).Send();
							return;
					}
					break;
				case "ci":
				case "chunk":
					var chunk = WorldData.Chunks[Game.Player.Coords];
					AddSlashResult(string.Format("Chunk {0}: VBOs {1}; Primitives {2}; Deepest transparent level {3}; Highest non air level {4}", chunk.Coords, chunk.VboCount, chunk.PrimitiveCount, chunk.DeepestTransparentLevel, chunk.HighestNonAirLevel));
					return;
				case "cu":
				case "chunkupdates":
					if (ArgCountInvalid(2, args)) return;
					switch (args[1])
					{
						case "on": Settings.ChunkUpdatesDisabled = false; AddSlashResult("Chunk updates enabled."); return;
						case "off": Settings.ChunkUpdatesDisabled = true; AddSlashResult("Chunk updates disabled."); return;
					}
					break;
				case "heightmap":
					if (!Config.CreativeMode) { AddSlashResult("Must be in Creative Mode."); return; }
					AddSlashResult(string.Format("HeightMap value for {0} is {1}", BlockCursorHost.Position, WorldData.Chunks[BlockCursorHost.Position].HeightMap[BlockCursorHost.Position.X % Chunk.CHUNK_SIZE, BlockCursorHost.Position.Z % Chunk.CHUNK_SIZE]));
					return;
				case "id":
					AddSlashResult(string.Format("UserName: {0} (Id {1})", Game.Player.UserName, Game.Player.Id));
					return;
				case "invhack":
					for (int i = 0; i < Game.Player.Inventory.Length; i++) Game.Player.Inventory[i] += 200;
					return;
				case "itemcount":
					if (!Config.CreativeMode) { AddSlashResult("Must be in Creative Mode."); return; }
					AddSlashResult(string.Format("World contains {0} items.", WorldData.GameItems.Count));
					return;
				case "lantern":
					var position = BlockCursorHost.PositionAdd;
					if (WorldData.IsValidStaticItemPosition(position))
					{
						var lantern = new LightSource(ref position, LightSourceType.Lantern, BlockCursorHost.SelectedFace.ToOpposite());
						new AddStaticItem(lantern).Send();
					}
					return;
				case "loc":
				case "location":
					AddSlashResult(string.Format("{0}:", Game.Player.UserName));
					AddSlashResult(string.Format(" Block {0}", Game.Player.Coords));
					AddSlashResult(string.Format(" Coords (x={0}, y={1}, z={2})", Game.Player.Coords.Xf, Game.Player.Coords.Yf, Game.Player.Coords.Zf));
					AddSlashResult(string.Format(" Dir ({0}) Pitch ({1})", MathHelper.RadiansToDegrees(Game.Player.Coords.Direction), MathHelper.RadiansToDegrees(Game.Player.Coords.Pitch)));
					return;
				case "maxtexturesize":
					int mts;
					GL.GetInteger(GetPName.MaxTextureSize, out mts);
					AddSlashResult("Max texture size: " + mts);
					return;
				case "m":
				case "move":
					if (ArgCountInvalid(3, args)) return;
					if (!Config.CreativeMode) { AddSlashResult("Must be in Creative Mode."); return; }
					short moveTo;
					if (!short.TryParse(args[2], out moveTo)) break;
					var newCoords = Game.Player.Coords;
					switch (args[1])
					{
						case "x": newCoords.Xf = moveTo; break;
						case "y": newCoords.Yf = moveTo; break;
						case "z": newCoords.Zf = moveTo; break;
					}
					if (!newCoords.Equals(Game.Player.Coords))
					{
						if (newCoords.IsValidPlayerLocation) { Game.Player.Coords = newCoords; return; }
						AddSlashResult("Invalid location.");
						return;
					}
					break;
				case "movechunk":
				case "movetochunk":
					if (ArgCountInvalid(3, args)) return;
					if (!Config.CreativeMode) { AddSlashResult("Must be in Creative Mode."); return; }
					byte chunkX, chunkZ;
					if (!byte.TryParse(args[1], out chunkX) || !byte.TryParse(args[2], out chunkZ)) break;
					var newChunkMoveCoords = new Coords(chunkX * Chunk.CHUNK_SIZE + Chunk.CHUNK_SIZE / 2, Chunk.CHUNK_HEIGHT, chunkZ * Chunk.CHUNK_SIZE + Chunk.CHUNK_SIZE / 2, Game.Player.Coords.Direction, Game.Player.Coords.Pitch);
					if (newChunkMoveCoords.IsValidPlayerLocation) { Game.Player.Coords = newChunkMoveCoords; return; }
					break;
				case "music":
					if (ArgCountInvalid(2, args)) return;
					if (!Config.SoundEnabled) { AddSlashResult("Sound is disabled."); return; }
					switch (args[1])
					{
						case "on":
							if (!Config.MusicEnabled)
							{
								Config.MusicEnabled = true;
								Config.Save();
								Sounds.Music.StartMusic();
							}
							AddSlashResult("Music enabled.");
							return;
						case "off":
							Config.MusicEnabled = false;
							Config.Save();
							Sounds.Music.StopMusic();
							AddSlashResult("Music disabled.");
							return;
					}
					break;
				case "opengl":
					AddSlashResult(Utilities.Diagnostics.OpenGlInfo());
					return;
				case "outline":
					Settings.OutlineChunks = !Settings.OutlineChunks;
					WorldData.Chunks.QueueAllWithinViewDistance();
					AddSlashResult(string.Format("Chunk outlining {0}.", Settings.OutlineChunks ? "enabled" : "disabled"));
					return;
				case "phack":
				case "playerhack":
					NetworkClient.Players.TryAdd(5000, new Player(5000, "Tester McGee", new Coords(Game.Player.Coords.Xf - 1, Game.Player.Coords.Yf, Game.Player.Coords.Zf, Game.Player.Coords.Direction, Game.Player.Coords.Pitch)));
					NetworkClient.Players.TryAdd(5001, new Player(5001, "Tester McGee2", new Coords(Game.Player.Coords.Xf + 3, Game.Player.Coords.Yf, Game.Player.Coords.Zf, Game.Player.Coords.Direction + MathHelper.Pi, Game.Player.Coords.Pitch)));
					return;
				case "raiseexception":
					//-can be used to test the msgbox error handler in release mode
					//-can be used to test obfuscation was done properly by looking at the stack trace displayed in release mode
					throw new Exception("Manually created exception from slash command.");
				case "server":
					AddSlashResult(Config.IsSinglePlayer ? "Not applicable in single player mode." : string.Format("{0}:{1}", NetworkClient.ServerIp, NetworkClient.ServerPort));
					return;
				case "serverversion":
					if (Config.IsSinglePlayer) { AddSlashResult("Not applicable in single player mode."); return; }
					new ServerCommand(ServerCommandType.ServerVersion).Send();
					return;
				case "sp":
				case "speed":
					if (ArgCountInvalid(2, args)) return;
					switch (args[1])
					{
						case "on":
							new PlayerOption(PlayerOption.OptionType.Speed, BitConverter.GetBytes(5)).Send();
							return;
						case "off":
							new PlayerOption(PlayerOption.OptionType.Speed, BitConverter.GetBytes(1)).Send();
							return;
						default:
							int multiplier;
							if (int.TryParse(args[1], out multiplier))
							{
								new PlayerOption(PlayerOption.OptionType.Speed, BitConverter.GetBytes(multiplier)).Send();
								return;
							}
							break;
					}
					break;
				case "sound":
					if (ArgCountInvalid(2, args)) return;
					switch (args[1])
					{
						case "on":
							Config.SoundEnabled = true;
							Sounds.Audio.LoadSounds();
							AddSlashResult("Sound enabled.");
							return;
						case "off":
							Config.SoundEnabled = false;
							Sounds.Audio.Dispose();
							AddSlashResult("Sound disabled.");
							return;
					}
					break;
				case "stuck":
					Game.Player.Coords = new Coords(WorldData.SizeInBlocksX / 2f, Chunk.CHUNK_HEIGHT, WorldData.SizeInBlocksZ / 2f, Game.Player.Coords.Direction, Game.Player.Coords.Pitch);
					return;
				case "sun":
					if (ArgCountInvalid(2, 3, args)) return;
					if (!Config.CreativeMode) { AddSlashResult("Must be in Creative Mode."); return; }
					switch (args[1])
					{
						case "loc":
						case "info":
						case "position":
						case "speed":
							AddSlashResult(string.Format("Sun: Degrees {0}, Strength {1}, Speed {2}", MathHelper.RadiansToDegrees(SkyHost.SunAngleRadians), SkyHost.SunLightStrength, SkyHost.SpeedMultiplier));
							return;
						case "move":
						case "degrees":
							if (ArgCountInvalid(3, args)) return;
							ushort sunDegrees;
							if (ushort.TryParse(args[2], out sunDegrees) && sunDegrees <= 360)
							{
								new ServerCommand(ServerCommandType.MoveSun, sunDegrees).Send();
								return;
							}
							AddSlashResult("Invalid degrees.");
							return;
					}

					//following commands only work in single player
					if (!Config.IsSinglePlayer) { AddSlashResult("Cannot change sun speed in Multiplayer."); return; }
					switch (args[1])
					{
						case "+":
						case "faster":
							SkyHost.SpeedMultiplier *= 2;
							AddSlashResult("Sun speed increased.");
							return;
						case "-":
						case "slower":
							SkyHost.SpeedMultiplier /= 2;
							AddSlashResult("Sun speed decreased.");
							return;
						case "default":
						case "start":
							SkyHost.SpeedMultiplier = SkyHost.DEFAULT_SPEED_MULTIPLIER;
							AddSlashResult("Sun reset to default speed.");
							return;
						case "stop":
							SkyHost.SpeedMultiplier = 0;
							AddSlashResult("Sun stopped.");
							return;
					}
					break;
				case "tp":
				case "teleport":
					if (ArgCountInvalid(2, args)) return;
					int playerId;
					if (!Config.CreativeMode)
					{
						AddSlashResult("Must be in Creative Mode.");
					}
					else if (!int.TryParse(args[1], out playerId) || !NetworkClient.Players.ContainsKey(playerId))
					{
						AddSlashResult("Invalid player id.");
					}
					else if(playerId == Game.Player.Id)
					{
						AddSlashResult("Cannot teleport to yourself.");	
					}
					else
					{
						Game.Player.Coords = NetworkClient.Players[playerId].Coords;
					}
					return;
				case "throwexception":
					new ThrowException().Send();
					return;
				case "time":
					AddSlashResult(string.Format("Time in game: {0:h:mm tt}", SkyHost.Time));
					return;
				case "ui":
					if (ArgCountInvalid(2, args)) return;
					switch (args[1])
					{
						case "on": Settings.UiDisabled = false; AddSlashResult("UI enabled."); return;
						case "off": Settings.UiDisabled = true; AddSlashResult("UI disabled."); return;
					}
					break;
				case "username":
					AddSlashResult(Game.Player.UserName);
					return;
				case "ver":
				case "version":
					AddSlashResult(string.Format("Version {0}", Settings.VersionDisplay));
					return;
				case "vd":
				case "view":
				case "viewdistance":
					switch (args.Length)
					{
						case 1:
							AddSlashResult(string.Format("{0} ({1} blocks)", Config.ViewDistance, Settings.ZFar));
							return;
						case 2:
							//view distance can be changed by either entering the string view distance or the numeric enum value
							//the check of enum length is needed because the TryParse lets numeric values through that are larger then the number of values strangely
							ViewDistance vd;
							if (Enum.TryParse(args[1], true, out vd) && (int)vd < Enum.GetValues(typeof(ViewDistance)).Length)
							{
								Utilities.Misc.ChangeViewDistance(vd);
								AddSlashResult(string.Format("{0} ({1} blocks)", Config.ViewDistance, Settings.ZFar));
							}
							else
							{
								AddSlashResult("Unknown view distance.");
							}
							return;
					}
					break;
				case "vsync":
					Config.VSync = !Config.VSync;
					Settings.Game.VSync = Config.VSync ? VSyncMode.On : VSyncMode.Off;
					AddSlashResult("VSync: " + Settings.Game.VSync);
					return;
				case "walloftext":
					for (int i = 0; i < 10; i++) AddSlashResult(new string('X', 80));
					return;
				case "who":
					switch (args.Length)
					{
						case 1:
							if (NetworkClient.Players.Count > 1) AddSlashResult(string.Format("{0} players connected:", NetworkClient.Players.Count));
							foreach (var player in NetworkClient.Players.Values) AddSlashResult(player);
							return;
						case 2:
							foreach (var player in NetworkClient.Players.Values.Where(player => player.UserName.Equals(args[1], StringComparison.InvariantCultureIgnoreCase)))
							{
								AddSlashResult(player);
								return;
							}
							AddSlashResult("Player not found.");
							return;
					}
					break;
				case "wireframe":
					if (!Config.CreativeMode) { AddSlashResult("Must be in Creative Mode."); return; }
					int mode;
					GL.GetInteger(GetPName.PolygonMode, out mode);
					GL.PolygonMode(MaterialFace.FrontAndBack, mode == (int)PolygonMode.Fill ? PolygonMode.Line : PolygonMode.Fill);
					return;
				case "worldname":
					AddSlashResult(string.Format("World name: {0}", Settings.WorldName));
					return;
				case "worldsave":
					if (Config.IsSinglePlayer)
					{
						System.Threading.Tasks.Task.Factory.StartNew(WorldData.SaveToDisk).ContinueWith(task => AddSlashResult(string.Format("World saved: {0}", Settings.WorldFilePath)));
					}
					else
					{
						AddSlashResult("Cannot save in Multiplayer.");
					}
					return;
				case "worldsize":
				case "size":
					new ServerCommand(ServerCommandType.WorldSize).Send();
					return;
				case "worldtype":
					AddSlashResult(string.Format("World type: {0}", WorldData.WorldType));
					return;
				case "xmldump":
					if (!Config.CreativeMode) { AddSlashResult("Must be in Creative Mode."); return; }
					var xml = WorldSettings.GetXmlByteArray();
					using (var file = new FileStream(Path.Combine(Config.SaveDirectory.FullName, Settings.WorldName) + ".xml", FileMode.Create))
					{
						file.Write(xml, 0, xml.Length);
						AddSlashResult("Dumped XML to " + file.Name);
						file.Close();
					}
					return;
			}
			AddSlashResult("Invalid command.");
		}

		/// <summary>Check the number of arguments. Returns true and displays a message if the number of arguments does not match the expected number.</summary>
		/// <param name="expectedArgs">expected number of arguments</param>
		/// <param name="args">args array</param>
		private static bool ArgCountInvalid(byte expectedArgs, string[] args)
		{
			if (args.Length < expectedArgs)
			{
				AddSlashResult("Argument missing.");
				return true;
			}
			if (args.Length > expectedArgs)
			{
				AddSlashResult("Too many arguments.");
				return true;
			}
			return false;
		}

		/// <summary>Check the number of arguments. Returns true and displays a message if the number of arguments does not match the expected number.</summary>
		/// <param name="minExpectedArgs">minimum expected number of arguments</param>
		/// <param name="maxExpectedArgs">maximum expected number of arguments</param>
		/// <param name="args">args array</param>
		private static bool ArgCountInvalid(byte minExpectedArgs, byte maxExpectedArgs, string[] args)
		{
			Debug.Assert(minExpectedArgs < maxExpectedArgs, "Min should be lower than max.");
			if (args.Length < minExpectedArgs)
			{
				AddSlashResult("Argument missing.");
				return true;
			}
			if (args.Length > maxExpectedArgs)
			{
				AddSlashResult("Too many arguments.");
				return true;
			}
			return false;
		}

		private static void AddSlashResult(string message)
		{
			Game.UiHost.AddChatMessage(new ChatMessage(ChatMessageType.SlashResult, message));
		}

		private static void AddSlashResult(Player player)
		{
			AddSlashResult(string.Format("{0} ({1}) {2}", player.UserName, player.Id, player.Coords));
		}

		internal static void DisplayCommandHelp()
		{
			AddSlashResult("Available commands:");
			AddSlashResult("ctrl+C (creative mode / unlimited blocks), ctrl+S (toggle speed), alt+Z (toggle ui)");
			AddSlashResult("R (rain lava), T (bouncing bomb), Y (sticky bomb)");
			AddSlashResult("F1 (help), F3 (debug info), F10 (windowed), F11 (full screen), F12 (screenshot)");
			AddSlashResult("/loc (location info), /cursor (cursor info), /stuck (return to spawn)");
			AddSlashResult("/opengl (GL version), /outline (outline chunks), /sound on|off, /music on|off");
			AddSlashResult("/server, /who, /worldname, /worldsize, /worldtype");
		}
	}
}