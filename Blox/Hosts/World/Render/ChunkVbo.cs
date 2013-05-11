//#define DEBUG_VBO
using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Hexpoint.Blox.Hosts.World.Render
{
	internal class ChunkVbo
	{
		/// <summary>Construct a new VBO for the supplied block type and texture id in this chunk.</summary>
		internal ChunkVbo(ref Block block, int textureId)
		{
			BlockType = block.Type;
			IsTransparent = block.IsTransparent;
			_textureId = textureId;
		}

		public Block.BlockType BlockType { get; private set; }
		public bool IsTransparent { get; private set; }
		private readonly int _textureId;

		private int _positionId;
		internal readonly List<Vector3> Positions = new List<Vector3>();
		private Vector3[] _positionArray;

		private int _colorId;
		internal readonly List<ColorRgb> Colors = new List<ColorRgb>();
		private ColorRgb[] _colorArray;

		private int _texCoordId;
		internal readonly List<TexCoordsShort> TexCoords = new List<TexCoordsShort>();
		private TexCoordsShort[] _texCoordsArray;

		/// <summary>Count of primitives in this VBO. Used by the GL Draw command when rendering the VBO.</summary>
		public int PrimitiveCount { get; private set; }

		internal void Render()
		{
			//bind to the texture id for this vbo except for water bind to the correct water texture in the animation cycle
			GL.BindTexture(TextureTarget.Texture2D, _textureId == (int)Textures.BlockTextureType.Water ? WorldHost.WaterCycleTextureId : _textureId);

			//position array buffer
			GL.BindBuffer(BufferTarget.ArrayBuffer, _positionId); //bind to the Array Buffer ID
			GL.VertexPointer(3, VertexPointerType.Float, Vector3.SizeInBytes, IntPtr.Zero); //set the pointer to the current bound array describing how the data is stored

			//color array buffer
			GL.BindBuffer(BufferTarget.ArrayBuffer, _colorId);
			GL.ColorPointer(3, ColorPointerType.UnsignedByte, ColorRgb.SIZE, IntPtr.Zero);

			//texCoord array buffer
			GL.BindBuffer(BufferTarget.ArrayBuffer, _texCoordId);
			GL.TexCoordPointer(2, TexCoordPointerType.Short, TexCoordsShort.SIZE, IntPtr.Zero);

			GL.DrawArrays(BeginMode.Quads, 0, PrimitiveCount);
		}

		/// <summary>
		/// Buffer data to a VBO. To enable debugging during VBO creation, use the DEBUG_VBO symbol.
		/// A VBO is created for each texture type in a chunk whenever the chunk is rebuilt.
		/// </summary>
		internal void BufferData()
		{
			DeleteBuffers(); //remove existing vbos from gpu for this chunk before re-buffering

			//positions buffer
			PrimitiveCount = _positionArray.Length;
			GL.GenBuffers(1, out _positionId); //generate array buffer id
			GL.BindBuffer(BufferTarget.ArrayBuffer, _positionId); //bind current context to array buffer id
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(_positionArray.Length * Vector3.SizeInBytes), _positionArray, BufferUsageHint.StaticDraw); //send data to buffer
#if DEBUG_VBO
			int bufferSize;
			GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out bufferSize);
			if (_positionArray.Length * Vector3.SizeInBytes != bufferSize) throw new ApplicationException("Position array not uploaded correctly.");
#endif

			//colors buffer
			GL.GenBuffers(1, out _colorId);
			GL.BindBuffer(BufferTarget.ArrayBuffer, _colorId);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(_colorArray.Length * ColorRgb.SIZE), _colorArray, BufferUsageHint.StaticDraw);
#if DEBUG_VBO
			GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out bufferSize);
			if (_colorArray.Length * Vector3.SizeInBytes != bufferSize) throw new ApplicationException("Color array not uploaded correctly.");
#endif

			//texCoords buffer
			GL.GenBuffers(1, out _texCoordId);
			GL.BindBuffer(BufferTarget.ArrayBuffer, _texCoordId);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(_texCoordsArray.Length * TexCoordsShort.SIZE), _texCoordsArray, BufferUsageHint.StaticDraw);
#if DEBUG_VBO
			GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out bufferSize);
			//if (_texCoordsArray.Length * Vector2.SizeInBytes != bufferSize) throw new ApplicationException("TexCoord array not uploaded correctly."); //floats
			if (_texCoordsArray.Length * TexCoordsShort.SIZE != bufferSize) throw new ApplicationException("TexCoord array not uploaded correctly."); //shorts
#endif

			//ensure the amounts being buffered are equal
			Debug.Assert(_positionArray.Length == _colorArray.Length && _positionArray.Length == _texCoordsArray.Length, "Cannot buffer uneven amounts of values to VBO.", string.Format("Tried to buffer ({0} vertices, {1} colors, {2} texCoords)", _positionArray.Length, _colorArray.Length, _texCoordsArray.Length));

			DeleteData();
		}

		/// <summary>The lists are dumped to arrays during build instead of buffer to reduce the time spent behind the sync lock.</summary>
		internal void WriteListsToArrays()
		{
			_positionArray = Positions.ToArray();
			_colorArray = Colors.ToArray();
			_texCoordsArray = TexCoords.ToArray();

			Positions.Clear();
			Colors.Clear();
			TexCoords.Clear();
		}

		/// <summary>
		/// Reduces memory usage because we keep all the chunkVbos in memory for rendering, but we no longer need the data from these arrays after the vbos are buffered to the gpu.
		/// One side effect of this is that we cant just re-buffer a vbo without first rebuilding the chunk. This is probably a good trade off because even though it takes a little
		/// longer when only rebuffering of a chunk would have been required; we end up saving memory. Could revisit this trade-off again in the future, especially if editing vbos
		/// rather then always recreating is useful.
		/// </summary>
		private void DeleteData()
		{
			_positionArray = null;
			_colorArray = null;
			_texCoordsArray = null;
		}

		/// <summary>Remove the buffers for this vbo from the gpu.</summary>
		internal void DeleteBuffers()
		{
			GL.DeleteBuffers(3, new[] { _positionId, _colorId, _texCoordId });
		}
	}
}
