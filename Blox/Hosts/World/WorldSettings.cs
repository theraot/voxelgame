using System;
using System.Diagnostics;
using System.Text;
using System.Xml;
using Hexpoint.Blox.GameObjects.GameItems;
using Hexpoint.Blox.GameObjects.Units;

namespace Hexpoint.Blox.Hosts.World
{
	// ReSharper disable PossibleNullReferenceException
	internal static class WorldSettings
	{
		#region Load
		/// <summary>Load world settings from a byte array in XML format that comes from the zipped world save file or sent from the server in multiplayer.</summary>
		/// <remarks>Remember XML xpaths are case sensitive.</remarks>
		/// <param name="settings">xml byte array</param>
		internal static void LoadSettings(byte[] settings)
		{
			Debug.WriteLine("Loading world settings...");
			try
			{
				var xml = new XmlDocument();
				xml.LoadXml(Encoding.UTF8.GetString(settings));

				XmlNode settingsNode = xml.SelectSingleNode("World/Settings");
				WorldData.RawSeed = settingsNode.Attributes["RawSeed"].Value;
				WorldData.GeneratorVersion = settingsNode.Attributes["GeneratorVersion"].Value;
				WorldData.GameObjectIdSeq = int.Parse(settingsNode.Attributes["GameObjectIdSeq"].Value);
				WorldData.WorldType = (WorldType)Convert.ToInt32(settingsNode.Attributes["WorldType"].Value);
				if ((int)WorldData.WorldType == 0) throw new Exception("Unable to load world type from world settings.");
				WorldData.SizeInChunksX = int.Parse(settingsNode.Attributes["SizeX"].Value);
				//world size Y is only there for future use, no need to load it
				WorldData.SizeInChunksZ = int.Parse(settingsNode.Attributes["SizeZ"].Value);
				if (settingsNode.Attributes["SunAngleRadians"] != null && settingsNode.Attributes["SunLightStrength"] != null)
				{
					SkyHost.SunAngleRadians = float.Parse(settingsNode.Attributes["SunAngleRadians"].Value);
					SkyHost.SunLightStrength = byte.Parse(settingsNode.Attributes["SunLightStrength"].Value);
				}
				else //default to sun directly overhead if the settings are missing (maps prior to 0.3 dont have them)
				{
					SkyHost.SunAngleRadians = OpenTK.MathHelper.PiOver2;
					SkyHost.SunLightStrength = SkyHost.BRIGHTEST_SKYLIGHT_STRENGTH;
				}

				WorldData.Chunks = new Chunks(WorldData.SizeInChunksX, WorldData.SizeInChunksZ); //initialize world chunks (must be done prior to assigning clutter to the chunks)

				XmlNodeList nodeList = xml.SelectNodes("World/Chunks/C");
				foreach (XmlNode chunkNode in nodeList)
				{
					WorldData.Chunks[int.Parse(chunkNode.Attributes["X"].Value), int.Parse(chunkNode.Attributes["Z"].Value)].WaterExpanding = chunkNode.Attributes["WaterExpanding"] != null && bool.Parse(chunkNode.Attributes["WaterExpanding"].Value);
					WorldData.Chunks[int.Parse(chunkNode.Attributes["X"].Value), int.Parse(chunkNode.Attributes["Z"].Value)].GrassGrowing = chunkNode.Attributes["GrassGrowing"] != null && bool.Parse(chunkNode.Attributes["GrassGrowing"].Value);
				}

				nodeList = xml.SelectNodes("World/Clutters/C");
				foreach (XmlNode clutterNode in nodeList)
				{
					new Clutter(clutterNode);
				}

				nodeList = xml.SelectNodes("World/LightSources/LS");
				foreach (XmlNode lightSourceNode in nodeList)
				{
					new LightSource(lightSourceNode);
				}

				nodeList = xml.SelectNodes("World/Mobs/M");
				foreach (XmlNode mobNode in nodeList)
				{
					new Mob(mobNode);
				}

				nodeList = xml.SelectNodes("World/GameItems/GI");
				foreach (XmlNode gameItemNode in nodeList)
				{
					if ((GameItemType)int.Parse(gameItemNode.Attributes["T"].Value) == GameItemType.BlockItem)
					{
						new BlockItem(gameItemNode);
					}
				}
			}
			catch (Exception ex)
			{
				//todo: exceptions here make the client crash hard, may want to write to event log or find a way to handle nicer
				//-can test by just changing any of the select node xpaths to an incorrect one
				throw new Exception("Error loading world settings: " + ex.Message);
			}
		}
		#endregion

		#region Save
		/// <summary>Get world settings in an XML format and return as a byte array to be written in the zipped world save file or sent to clients in multiplayer.</summary>
		/// <remarks>Remember XML xpaths are case sensitive.</remarks>
		/// <returns>xml byte array</returns>
		internal static byte[] GetXmlByteArray()
		{
			try
			{
				var xml = new XmlDocument();
				xml.LoadXml("<?xml version=\"1.0\" ?>\n<World />");

				//settings
				var settingsNode = xml.DocumentElement.AppendChild(xml.CreateNode(XmlNodeType.Element, "Settings", ""));
				settingsNode.Attributes.Append(xml.CreateAttribute("RawSeed")).Value = WorldData.RawSeed;
				settingsNode.Attributes.Append(xml.CreateAttribute("GeneratorVersion")).Value = WorldData.GeneratorVersion;
				settingsNode.Attributes.Append(xml.CreateAttribute("GameObjectIdSeq")).Value = WorldData.GameObjectIdSeq.ToString();
				settingsNode.Attributes.Append(xml.CreateAttribute("WorldType")).Value = ((int)WorldData.WorldType).ToString();
				settingsNode.Attributes.Append(xml.CreateAttribute("SizeX")).Value = WorldData.SizeInChunksX.ToString();
				settingsNode.Attributes.Append(xml.CreateAttribute("SizeY")).Value = Chunk.CHUNK_HEIGHT.ToString(); //for possible future use
				settingsNode.Attributes.Append(xml.CreateAttribute("SizeZ")).Value = WorldData.SizeInChunksZ.ToString();
				settingsNode.Attributes.Append(xml.CreateAttribute("SunAngleRadians")).Value = SkyHost.SunAngleRadians.ToString();
				settingsNode.Attributes.Append(xml.CreateAttribute("SunLightStrength")).Value = SkyHost.SunLightStrength.ToString(); //need the strength because even though it would get calculated on the first update, we need it before that to build the initial frustum of chunks correctly

				//chunks / clutter / light sources
				var chunksNode = xml.DocumentElement.AppendChild(xml.CreateNode(XmlNodeType.Element, "Chunks", ""));
				var cluttersNode = xml.DocumentElement.AppendChild(xml.CreateNode(XmlNodeType.Element, "Clutters", ""));
				var lightSourcesNode = xml.DocumentElement.AppendChild(xml.CreateNode(XmlNodeType.Element, "LightSources", ""));
				foreach (Chunk chunk in WorldData.Chunks)
				{
					chunksNode.AppendChild(chunk.GetXml(xml));
					foreach (var clutter in chunk.Clutters) cluttersNode.AppendChild(clutter.GetXml(xml));
					foreach (var lightSource in chunk.LightSources) lightSourcesNode.AppendChild(lightSource.Value.GetXml(xml));
				}

				//mobs
				var mobsNode = xml.DocumentElement.AppendChild(xml.CreateNode(XmlNodeType.Element, "Mobs", ""));
				foreach (var mob in WorldData.Mobs.Values)
				{
					mobsNode.AppendChild(mob.GetXml(xml));
				}

				//game items
				var gameItemsNode = xml.DocumentElement.AppendChild(xml.CreateNode(XmlNodeType.Element, "GameItems", ""));
				foreach (var gameItem in WorldData.GameItems.Values)
				{
					if (gameItem.Type == GameItemType.BlockItem) gameItemsNode.AppendChild(gameItem.GetXml(xml));
				}

				return Encoding.UTF8.GetBytes(xml.OuterXml);
			}
			catch (Exception ex)
			{
				//gm: exceptions caught here are handled nicely and end up in the windows forms messagebox for the client
				throw new Exception("Error saving world settings: " + ex.Message);
			}
		}
		#endregion
	}
	// ReSharper restore PossibleNullReferenceException
}
