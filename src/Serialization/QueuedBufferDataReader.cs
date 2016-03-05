using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Pook.Net.Serialization
{
	public class QueuedBufferDataReader : IDataReader
	{
		private readonly ConcurrentQueue<byte[]> buffers = new ConcurrentQueue<byte[]>();
		private byte[] currentBuffer;
		private int currentBufferPosition;
		private int queuedSize;

		public int QueuedSize { get { return queuedSize; } }

		public void Add(byte[] buffer, int count)
		{
			var b = new byte[count];
			buffers.Enqueue(b);
			Interlocked.Add(ref queuedSize, count);
		}

		private void NextBuffer()
		{
			if (!buffers.TryDequeue(out currentBuffer))
				throw new InvalidOperationException("Read passed end of buffers");

			Interlocked.Add(ref queuedSize, currentBuffer.Length * -1);
			currentBufferPosition = 0;
		}

		public byte ReadByte()
		{
			if (currentBufferPosition >= currentBuffer.Length)
				NextBuffer();

			byte result = currentBuffer[currentBufferPosition];
			currentBufferPosition++;

			return result;
		}

		public byte[] ReadBytes(int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException("count", "Must not be -ve");

			byte[] result = new byte[count];

			if (currentBufferPosition + count > currentBuffer.Length)
			{
				int firstBite = currentBuffer.Length - currentBufferPosition;
				Array.Copy(currentBuffer, currentBufferPosition, result, 0, firstBite);
				NextBuffer();
				Array.Copy(currentBuffer, currentBufferPosition, result, firstBite, count - firstBite);
				currentBufferPosition = count - firstBite;
			}
			else
			{
				Array.Copy(currentBuffer, currentBufferPosition, result, 0, count);
				currentBufferPosition += count;
			}

			return result;
		}
	}
}