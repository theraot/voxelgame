using System;
using System.Diagnostics;
using Hexpoint.Blox.Hosts;
using Hexpoint.Blox.Hosts.World;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

namespace Hexpoint.Blox.Sounds
{
	/// <summary>Sound types. The enum value matches the index of the source and buffer arrays. The sounds can be any order in the enum, but only a single enum should be maintained.</summary>
	/// <remarks>When adding a new sound, add it to this enum and add a line in Audio.LoadSounds to load it.</remarks>
	internal enum SoundType : byte
	{
		AddBlock,
		ItemPickup,
		/// <summary>Also used as water place and remove sound.</summary>
		JumpOutOfWater,
		Message,
		PlayerConnect,
		PlayerLanding,
		Splash,
		RemoveBlock,
		WaterBubbles,
		//put music files last
		MusicTimeToDreamMono
	}

	internal static class Audio
	{
		#region Properties
		private static AudioContext _audioContext;
		private static int[] _buffer;
		private static int[] _source;
		#endregion

		#region Load
		/// <summary>Load and buffer all the sounds for the game.</summary>
		internal static void LoadSounds()
		{
			if (!Config.SoundEnabled) return;

			try
			{
				_audioContext = new AudioContext();
				//Debug.WriteLine("Audio device: " + _audioContext.CurrentDevice);
			}
			catch (Exception ex)
			{
				//if we cant create an audio context then disable sounds in the config and return
				Debug.WriteLine("Error creating Audio Context: " + ex.Message);
				Config.SoundEnabled = false;
				Config.Save();
				return;
			}

			int soundsCount = Enum.GetValues(typeof(SoundType)).Length;
			_buffer = new int[soundsCount];
			_source = new int[soundsCount];
			try
			{
				for (int i = 0; i < soundsCount; i++)
				{
					_source[i] = AL.GenSource();
					_buffer[i] = AL.GenBuffer();
				}

				BufferSound(Resources.SoundFiles.AddBlock, SoundType.AddBlock);
				BufferSound(Resources.SoundFiles.ItemPickup, SoundType.ItemPickup);
				BufferSound(Resources.SoundFiles.JumpOutOfWater, SoundType.JumpOutOfWater);
				BufferSound(Resources.SoundFiles.Message, SoundType.Message);
				BufferSound(Resources.SoundFiles.PlayerConnect, SoundType.PlayerConnect);
				BufferSound(Resources.SoundFiles.PlayerLanding, SoundType.PlayerLanding);
				BufferSound(Resources.SoundFiles.Splash, SoundType.Splash);
				BufferSound(Resources.SoundFiles.RemoveBlock, SoundType.RemoveBlock);
				BufferSound(Resources.SoundFiles.WaterBubbles, SoundType.WaterBubbles);
				BufferSound(Resources.SoundFiles.TimeToDreamMono, SoundType.MusicTimeToDreamMono);

				//start music here so it only gets started if sound is enabled and there were no exceptions loading sounds
				if (Config.MusicEnabled) Music.StartMusic();
			}
			catch (Exception ex)
			{
				throw new Exception("Error buffering sounds. If the problem persists try running with sound disabled.\n" + ex.Message);
			}

			//wire sound events
			PerformanceHost.OnSecondElapsed += PerformanceHost_OnSecondElapsed;
			PerformanceHost.OnFiveSecondsElapsed += PerformanceHost_OnFiveSecondsElapsed;
		}

		/// <summary>Buffer the sound to OpenAL.</summary>
		private static void BufferSound(System.IO.Stream soundStream, SoundType type)
		{
			int channels, bitsPerSample, sampleRate;
			byte[] soundData = LoadWave(soundStream, out channels, out bitsPerSample, out sampleRate);
			AL.BufferData(_buffer[(byte)type], GetSoundFormat(channels, bitsPerSample), soundData, soundData.Length, sampleRate);
		}

		/// <summary>Loads a wave/riff audio file from a stream. Resource file can be passed as the stream.</summary>
		private static byte[] LoadWave(System.IO.Stream stream, out int channels, out int bits, out int rate)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			// ReSharper disable UnusedVariable
			using (var reader = new System.IO.BinaryReader(stream))
			{
				//RIFF header
				var signature = new string(reader.ReadChars(4));
				if (signature != "RIFF") throw new NotSupportedException("Specified stream is not a wave file.");

				int riffChunckSize = reader.ReadInt32();

				var format = new string(reader.ReadChars(4));
				if (format != "WAVE") throw new NotSupportedException("Specified stream is not a wave file.");

				//WAVE header
				var formatSignature = new string(reader.ReadChars(4));
				if (formatSignature != "fmt ") throw new NotSupportedException("Specified wave file is not supported.");

				int formatChunkSize = reader.ReadInt32();
				int audioFormat = reader.ReadInt16();
				int numChannels = reader.ReadInt16();
				int sampleRate = reader.ReadInt32();
				int byteRate = reader.ReadInt32();
				int blockAlign = reader.ReadInt16();
				int bitsPerSample = reader.ReadInt16();

				var dataSignature = new string(reader.ReadChars(4));
				//check if the data dignature for this chunk is LIST headers, can happen with .wav files from web, or when converting other formats such as .mp3 to .wav using tools such as audacity
				//if this happens, the extra header info can be cleared using audacity, export to wave and select 'clear' when the extra header info window appears
				//see http://www.lightlink.com/tjweber/StripWav/WAVE.html
				if (dataSignature == "LIST") throw new NotSupportedException("Specified wave file contains LIST headers (author, copyright etc).");
				if (dataSignature != "data") throw new NotSupportedException("Specified wave file is not supported.");

				int dataChunkSize = reader.ReadInt32();

				channels = numChannels;
				bits = bitsPerSample;
				rate = sampleRate;

				return reader.ReadBytes((int)reader.BaseStream.Length);
			}
			// ReSharper restore UnusedVariable
		}

		private static ALFormat GetSoundFormat(int channels, int bits)
		{
			switch (channels)
			{
				case 1: return bits == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
				case 2: return bits == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;
				default: throw new NotSupportedException("The specified sound format is not supported.");
			}
		}

		internal static void Dispose()
		{
			if (_audioContext == null) return;
			StopAllSounds();
			AL.DeleteSources(_source); //delete multiple sources
			AL.DeleteBuffers(_buffer); //delete multiple buffers
			_audioContext.Dispose();
		}
		#endregion

		#region Play
		/// <summary>Plays a sound.</summary>
		/// <param name="sound">sound type to play</param>
		internal static void PlaySound(SoundType sound)
		{
			if (!Config.SoundEnabled) return;
			Play(sound);
		}

		/// <summary>Plays a sound only if the sound is not already playing. Some sounds would sound strange restarting without having played completely.</summary>
		/// <param name="sound">sound type to play</param>
		/// /// <param name="gain">volume, 0 none -> 1 full</param>
		internal static void PlaySoundIfNotAlreadyPlaying(SoundType sound, float gain = 1f)
		{
			if (!Config.SoundEnabled) return;
			if (AL.GetSourceState(_source[(byte)sound]) == ALSourceState.Playing) return;
			Play(sound, gain);
		}

		/// <summary>Plays a sound at specified volume.</summary>
		/// <param name="sound">sound type to play</param>
		/// <param name="gain">volume, 0 none -> 1 full</param>
		internal static void PlaySound(SoundType sound, float gain)
		{
			if (!Config.SoundEnabled) return;
			Play(sound, gain);
		}

		/// <summary>Plays a sound at specified volume that loops infinitely.</summary>
		/// <param name="sound">sound type to play</param>
		/// <param name="gain">volume, 0 none -> 1 full</param>
		internal static void PlaySoundLooping(SoundType sound, float gain)
		{
			if (!Config.SoundEnabled) return;
			Play(sound, gain, true);
		}

		/// <summary>Plays a sound with a volume relative to how far from the listener it is.</summary>
		/// <param name="sound">sound type to play</param>
		/// <param name="sourceCoords">source coords of the sound</param>
		/// <param name="maxDistance">max distance the sound can be heard</param>
		internal static void PlaySound(SoundType sound, ref Coords sourceCoords, byte maxDistance = 25)
		{
			if (!Config.SoundEnabled) return;
			float gain = (maxDistance - Game.Player.Coords.GetDistanceExact(ref sourceCoords)) / maxDistance;
			Play(sound, gain);
		}

		/// <summary>Plays a sound with a volume relative to how far from the listener it is.</summary>
		/// <param name="sound">sound type to play</param>
		/// <param name="sourcePosition">source position of the sound</param>
		/// <param name="maxDistance">max distance the sound can be heard</param>
		internal static void PlaySound(SoundType sound, ref Position sourcePosition, byte maxDistance = 25)
		{
			if (!Config.SoundEnabled) return;
			float gain = (maxDistance - Game.Player.Coords.ToPosition().GetDistanceExact(ref sourcePosition)) / maxDistance;
			Play(sound, gain);
		}

		/// <summary>Plays a sound.</summary>
		/// <param name="sound">sound type to play</param>
		/// <param name="gain">volume, 0 none -> 1 full</param>
		/// <param name="looping">should sound loop infinitely</param>
		private static void Play(SoundType sound, float gain = 1, bool looping = false)
		{
			if (gain <= 0.05) return; //dont bother playing
			if (gain > 1) gain = 1; //play at max volume
			try
			{
				int source = _source[(byte)sound]; //id of the source
				int buffer = _buffer[(byte)sound]; //id of the buffer
				AL.Source(source, ALSourcei.Buffer, buffer);
				AL.Source(source, ALSourcef.Gain, gain); //volume
				if (looping) AL.Source(source, ALSourceb.Looping, true); //only make AL call if we actually need to loop as this is rare
				//AL.Source(source, ALSourcef.Pitch, x); //speed; this slows the sound down
				AL.SourcePlay(source);
				Debug.WriteLineIf(sound != SoundType.PlayerLanding, string.Format("Playing (sound={0} gain={1})", sound, gain));
			}
			catch (Exception ex)
			{
				Debug.WriteLine("PlaySound ({0}) failed: {1}", sound, ex.Message);
			}
		}
		#endregion

		#region Stop
		internal static void StopSound(SoundType sound)
		{
			try
			{
				AL.SourceStop(_source[(byte)sound]);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("StopSound failed: " + ex.Message);
			}
		}

		internal static void StopAllSounds()
		{
			AL.SourceStop(_source.Length, _source); //stop multiple sources
		}
		#endregion

		#region Sound Event Handlers
		private static void PerformanceHost_OnSecondElapsed()
		{
			
		}

		private static void PerformanceHost_OnFiveSecondsElapsed()
		{
			if (Game.Player.EyesUnderWater) PlaySoundIfNotAlreadyPlaying(SoundType.WaterBubbles, Math.Max(0.3f, (float)Settings.Random.NextDouble()));
		}
		#endregion
	}
}