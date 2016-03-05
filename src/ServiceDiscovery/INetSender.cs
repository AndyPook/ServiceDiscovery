using System;
using System.Linq;

namespace Pook.Net
{
	public interface INetSender
	{
		void Send(byte[] data);
	}
}