using System;

namespace Pook.Net.Serialization
{
	public interface IDataReader
	{
		byte ReadByte();
		byte[] ReadBytes(int count);
	}
}