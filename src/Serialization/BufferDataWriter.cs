using System;
using System.Collections.Generic;

namespace Pook.Net.Serialization
{
	public class BufferDataWriter : IDataWriter
	{
		public BufferDataWriter()
		{
			buffer = new List<byte>();
		}
		public BufferDataWriter(List<byte> buffer)
		{
			this.buffer = buffer;
		}

		private readonly List<byte> buffer;

		public IDataWriter Write(byte b)
		{
			buffer.Add(b);
			return this;
		}
		public IDataWriter Write(byte[] data)
		{
			buffer.AddRange(data);
			return this;
		}

		public byte[] GetBytes()
		{
			return buffer.ToArray();
		}

		public static implicit operator byte[](BufferDataWriter writer)
		{
			return writer.GetBytes();
		}
	}
}