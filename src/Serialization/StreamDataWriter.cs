using System;
using System.IO;

namespace Pook.Net.Serialization
{
	public class StreamDataWriter : IDataWriter
	{
		public StreamDataWriter(Stream stream)
		{
			this.stream = stream;
		}

		private readonly Stream stream;

		public IDataWriter Write(byte b)
		{
			stream.WriteByte(b);
			return this;
		}
		public IDataWriter Write(byte[] data)
		{
			stream.Write(data, 0, data.Length);
			return this;
		}
	}
}