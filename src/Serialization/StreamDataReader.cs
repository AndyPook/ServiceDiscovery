using System;
using System.IO;

namespace Pook.Net.Serialization
{
	public class StreamDataReader : IDataReader
	{
		public StreamDataReader(Stream stream)
		{
			this.stream = stream;
		}

		private readonly Stream stream;

		public byte ReadByte()
		{
			return (byte)stream.ReadByte();
		}

		public byte[] ReadBytes(int count)
		{
			var buffer = new byte[count];
			stream.Read(buffer, 0, count);
			return buffer;
		}
	}
}