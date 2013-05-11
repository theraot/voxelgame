namespace Hexpoint.Blox.Sounds
{
	internal static class Music
	{
		internal static void StartMusic()
		{
			Audio.PlaySoundLooping(SoundType.MusicTimeToDreamMono, 0.4f);
		}

		internal static void StopMusic()
		{
			Audio.StopSound(SoundType.MusicTimeToDreamMono);
		}
	}
}
