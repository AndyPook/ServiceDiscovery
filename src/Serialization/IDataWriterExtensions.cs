using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Pook.Net.Serialization
{
	public static class IDataWriterExtensions
	{
		public static IDataWriter WriteVersion(this IDataWriter writer, byte version)
		{
			writer.Write(version);
			return writer;
		}

		public static IDataWriter WriteByte(this IDataWriter writer, int number)
		{
			writer.Write((byte)number);
			return writer;
		}
		public static IDataWriter WriteByte(this IDataWriter writer, byte number)
		{
			writer.Write(number);
			return writer;
		}

		public static IDataWriter WriteInt16(this IDataWriter writer, Int16 number)
		{
			writer.Write((byte)(number >> 8), (byte)number);
			return writer;
		}
		public static IDataWriter WriteUInt16(this IDataWriter writer, UInt16 number)
		{
			writer.Write((byte)(number >> 8), (byte)number);
			return writer;
		}

		public static IDataWriter WriteInt(this IDataWriter writer, Int32 number)
		{
			writer.Write((byte)(number >> 24), (byte)(number >> 16), (byte)(number >> 8), (byte)number);
			return writer;
		}
		public static IDataWriter WriteInt(this IDataWriter writer, UInt32 number)
		{
			writer.Write((byte)(number >> 24), (byte)(number >> 16), (byte)(number >> 8), (byte)number);
			return writer;
		}

		public static IDataWriter WriteGuid(this IDataWriter writer, Guid guid)
		{
			writer.Write(guid.ToByteArray());
			return writer;
		}

		public static IDataWriter WriteUri(this IDataWriter writer, Uri uri)
		{
			writer.WriteUTF8(uri.ToString());
			return writer;
		}

		public static IDataWriter WriteShortText(this IDataWriter writer, string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				writer.WriteByte(0);
				return writer;
			}

			if (text.Length > 254)
				text = text.Substring(0, 255);

			writer.WriteByte(text.Length);
			writer.Write(Encoding.ASCII.GetBytes(text));

			return writer;
		}
		public static IDataWriter WriteShortText(this IDataWriter writer, IEnumerable<string> text)
		{
			foreach (var item in text)
				writer.WriteShortText(item);
			return writer;
		}
		public static IDataWriter WriteShortText(this IDataWriter writer, params string[] text)
		{
			foreach (var item in text)
				writer.WriteShortText(item);
			return writer;
		}

		public static IDataWriter WriteUTF8(this IDataWriter writer, string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				writer.WriteByte(0);
				return writer;
			}

			byte[] bytes = Encoding.UTF8.GetBytes(text);
			writer.WriteInt(bytes.Length);
			writer.Write(bytes);
			return writer;
		}
		public static IDataWriter WriteUTF8(this IDataWriter writer, IEnumerable<string> text)
		{
			foreach (var item in text)
				writer.WriteUTF8(item);
			return writer;
		}
		public static IDataWriter WriteUTF8(this IDataWriter writer, params string[] text)
		{
			foreach (var item in text)
				writer.WriteUTF8(item);
			return writer;
		}

		public static IDataWriter WriteIPv4Address(this IDataWriter writer, IPAddress addr)
		{
			if (addr.AddressFamily != AddressFamily.InterNetwork)
				throw new ArgumentException("Expecting IPv4 address");
			byte[] a = addr.GetAddressBytes();
			writer.Write(a);
			return writer;
		}
		public static IDataWriter WriteIPv6Address(this IDataWriter writer, IPAddress addr)
		{
			if (addr.AddressFamily != AddressFamily.InterNetworkV6)
				throw new ArgumentException("Expecting IPv6 address");
			byte[] a = addr.GetAddressBytes();
			writer.Write(a);
			return writer;
		}
	}
}