using System;
using System.Text;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

//using Newtonsoft.Json;

namespace Pook.Net.Serialization
{
	public static class IDataReaderExtensions
	{
		public static long ReadIntegral(this IDataReader reader, int size)
		{
			long result = 0;
			for (int i = 0; i < size; i++)
				result = (result << 8) | reader.ReadByte();

			return result;
		}

		public static Int16 ReadInt16(this IDataReader reader)
		{
			return (short)ReadIntegral(reader, 2);
		}
		public static UInt16 ReadUInt16(this IDataReader reader)
		{
			return (ushort)ReadIntegral(reader, 2);
		}

		public static Int32 ReadInt32(this IDataReader reader)
		{
			return (int)ReadIntegral(reader, 4);
		}
		public static UInt32 ReadUInt32(this IDataReader reader)
		{
			return (uint)ReadIntegral(reader, 4);
		}

		public static Int64 ReadInt64(this IDataReader reader)
		{
			return ReadIntegral(reader, 8);
		}
		public static UInt64 ReadUInt64(this IDataReader reader)
		{
			return (ulong)ReadIntegral(reader, 8);
		}

		public static Guid ReadGuid(this IDataReader reader)
		{
			return new Guid(reader.ReadBytes(16));
		}

		public static IPAddress ReadIPv4Address(this IDataReader reader)
		{
			return new IPAddress(reader.ReadBytes(4));
		}
		public static IPAddress ReadIPv6Address(this IDataReader reader)
		{
			return new IPAddress(reader.ReadBytes(8));
		}

		public static string ReadShortText(this IDataReader reader)
		{
			var slen = reader.ReadByte();
			if (slen == 0)
				return string.Empty;

			string result = Encoding.ASCII.GetString(reader.ReadBytes(slen));
			return result;
		}
		public static string ReadUTF8(this IDataReader reader)
		{
			var slen = reader.ReadInt32();
			if (slen == 0)
				return string.Empty;

			string result = Encoding.UTF8.GetString(reader.ReadBytes(slen));
			return result;
		}

		public static Uri ReadUri(this IDataReader reader)
		{
			string uri = reader.ReadUTF8();
			return new Uri(uri);
		}

		//public static T Read<T>(this IDataReader reader)
		//{
		//	var length = reader.ReadInt32();
		//	var data = reader.ReadBytes(length);
		//	try
		//	{
		//		return new JsonSerializer().Deserialize<T>(new BsonReader(new MemoryStream(data)));
		//	}
		//	catch
		//	{
		//		var x = ReadBinary<T>(data);
		//		//debug.WriteLine("***** " + x.ToString());
		//		return x;
		//	}
		//}

		private static T ReadBinary<T>(byte[] data)
		{
			var formatter = new BinaryFormatter();
			var stream = new MemoryStream();

			stream.Write(data, 0, data.Length);
			stream.Position = 0;
			var result = (T)formatter.Deserialize(stream);
			return result;
		}

		public static T ReadBinary<T>(this IDataReader reader)
		{
			var formatter = new BinaryFormatter();
			var stream = new MemoryStream();

			var length = reader.ReadInt32();
			stream.Write(reader.ReadBytes(length), 0, length);
			stream.Position = 0;
			var result = (T)formatter.Deserialize(stream);
			return result;
		}
	}
}