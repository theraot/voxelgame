namespace Hexpoint.Blox.Hosts.World.Generator
{
	internal class PerlinNoise
	{
		private static float[][] GenerateWhiteNoise(int width, int height)
		{
			float[][] noise = GetEmptyArray<float>(width, height);

			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < height; j++)
				{
					noise[i][j] = (float)Settings.Random.NextDouble();
				}
			}
			return noise;
		}

		private static float Interpolate(float x0, float x1, float alpha)
		{
			return x0 * (1 - alpha) + alpha * x1;
		}

		private static int Interpolate(int minY, int maxY, float t)
		{
			float u = 1 - t;
			return (int)(minY * u + maxY * t);
		}

		private static int[][] MapInts(int minY, int maxY, float[][] perlinNoise)
		{
			int width = perlinNoise.Length;
			int height = perlinNoise[0].Length;
			int[][] heightMap = GetEmptyArray<int>(width, height);

			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < height; j++)
				{
					heightMap[i][j] = Interpolate(minY, maxY, perlinNoise[i][j]);
				}
			}
			return heightMap;
		}

		private static float[][] MapFloats(float minY, float maxY, float[][] perlinNoise)
		{
			int width = perlinNoise.Length;
			int height = perlinNoise[0].Length;
			float[][] treeMap = GetEmptyArray<float>(width, height);

			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < height; j++)
				{
					treeMap[i][j] = Interpolate(minY, maxY, perlinNoise[i][j]);
				}
			}
			return treeMap;
		}

		private static T[][] GetEmptyArray<T>(int width, int height)
		{
			var image = new T[width][];

			for (int i = 0; i < width; i++)
			{
				image[i] = new T[height];
			}
			return image;
		}

		private static float[][] GenerateSmoothNoise(float[][] baseNoise, int octave)
		{
			int width = baseNoise.Length;
			int height = baseNoise[0].Length;

			float[][] smoothNoise = GetEmptyArray<float>(width, height);
			int samplePeriod = 1 << octave; // calculates 2 ^ k
			float sampleFrequency = 1.0f / samplePeriod;

			for (int i = 0; i < width; i++)
			{
				//calculate the horizontal sampling indices
				int iSample0 = (i / samplePeriod) * samplePeriod;
				int iSample1 = (iSample0 + samplePeriod) % width; //wrap around
				float horizontalBlend = (i - iSample0) * sampleFrequency;

				for (int j = 0; j < height; j++)
				{
					//calculate the vertical sampling indices
					int jSample0 = (j / samplePeriod) * samplePeriod;
					int jSample1 = (jSample0 + samplePeriod) % height; //wrap around
					float verticalBlend = (j - jSample0) * sampleFrequency;

					//blend the top two corners
					float top = Interpolate(baseNoise[iSample0][jSample0], baseNoise[iSample1][jSample0], horizontalBlend);

					//blend the bottom two corners
					float bottom = Interpolate(baseNoise[iSample0][jSample1], baseNoise[iSample1][jSample1], horizontalBlend);

					//final blend
					smoothNoise[i][j] = Interpolate(top, bottom, verticalBlend);
				}
			}
			return smoothNoise;
		}

		private static float[][] GeneratePerlinNoise(float[][] baseNoise, int octaveCount)
		{
			int width = baseNoise.Length;
			int height = baseNoise[0].Length;

			var smoothNoise = new float[octaveCount][][]; //an array of 2D arrays containing

			const float PERSISTANCE = 0.4f;

			//generate smooth noise
			for (int i = 0; i < octaveCount; i++)
			{
				smoothNoise[i] = GenerateSmoothNoise(baseNoise, i);
			}

			float[][] perlinNoise = GetEmptyArray<float>(width, height); //an array of floats initialised to 0

			float amplitude = 1f;
			float totalAmplitude = 0.0f;

			//blend noise together
			for (int octave = octaveCount - 1; octave >= 0; octave--)
			{
				amplitude *= PERSISTANCE;
				totalAmplitude += amplitude;

				for (int i = 0; i < width; i++)
				{
					for (int j = 0; j < height; j++)
					{
						perlinNoise[i][j] += smoothNoise[octave][i][j] * amplitude;
					}
				}
			}

			//normalisation
			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < height; j++)
				{
					perlinNoise[i][j] /= totalAmplitude;
				}
			}
			return perlinNoise;
		}

		public static int[][] GetIntMap(int minY, int maxY, int octaveCount)
		{
			float[][] baseNoise = GenerateWhiteNoise(WorldData.SizeInBlocksX, WorldData.SizeInBlocksZ);
			float[][] perlinNoise = GeneratePerlinNoise(baseNoise, octaveCount);
			return MapInts(minY, maxY, perlinNoise);
		}

		public static float[][] GetFloatMap(float minY, float maxY, int octaveCount)
		{
			float[][] baseNoise = GenerateWhiteNoise(WorldData.SizeInBlocksX, WorldData.SizeInBlocksZ);
			float[][] perlinNoise = GeneratePerlinNoise(baseNoise, octaveCount);
			return MapFloats(minY, maxY, perlinNoise);
		}
	}
}