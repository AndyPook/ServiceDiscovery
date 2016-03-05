using System;

namespace Pook.Net.Serialization
{
	public interface IDataWriter
	{
		IDataWriter Write(byte b);
		IDataWriter Write(params byte[] data);
	}
}