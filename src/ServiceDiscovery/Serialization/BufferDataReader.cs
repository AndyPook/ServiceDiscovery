using System;

namespace Pook.Net.Serialization
{
	public class BufferDataReader : IDataReader
	{
		public BufferDataReader(byte[] buffer)
		{
			this.buffer = buffer;
			Position = 0;
		}

		private readonly byte[] buffer;

		public int Position { get; protected set; }
		public bool EndOfData { get; protected set; }

		public byte ReadByte()
		{
			byte result = buffer[Position];
			Position++;
			return result;
		}

		public byte[] ReadBytes(int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException("count", "Must not be -ve");

			if (Position + count > buffer.Length)
				count = buffer.Length - Position;

			byte[] result = new byte[count];

			Array.Copy(buffer, Position, result, 0, count);
			Position += count;

			if (Position >= buffer.Length)
				EndOfData = true;

			return result;
		}
	}
}