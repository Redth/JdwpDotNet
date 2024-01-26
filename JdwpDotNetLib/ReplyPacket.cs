using System.Buffers.Binary;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection.PortableExecutable;

namespace JdwpDotNet;

public class ReplyPacket : Packet
{
	public void FromMemory(ReadOnlyMemory<byte> header, ReadOnlyMemory<byte> data)
	{
		Id = BinaryPrimitives.ReadInt32BigEndian(header[4..7].Span);
		Flags = header.Span[8];
		ErrorCode = BinaryPrimitives.ReadInt16BigEndian(header[9..10].Span);
		Data = data;
	}

	public ReplyPacket()
	{
		Flags &= 0x80;
	}

	public short ErrorCode { get; set; }

	public override ReadOnlyMemory<byte> ToMemory()
	{
		const uint headerLength = 11;

		var dataLength = Data.Length;

		Memory<byte> headerSpan = new byte[headerLength + dataLength];

		var l = headerLength + ((uint)Math.Max(0, dataLength));

		// Length
		BinaryPrimitives.WriteUInt32BigEndian(headerSpan[..].Span, l);

		// Id
		BinaryPrimitives.WriteInt32BigEndian(headerSpan[4..].Span, Id);

		// Flags
		headerSpan.Span[8] = Flags;

		// Error code
		BinaryPrimitives.WriteInt16BigEndian(headerSpan[9..10].Span, ErrorCode);

		// Data body if there is one
		if (dataLength > 0)
			Data.Span.CopyTo(headerSpan[11..].Span);

		return headerSpan;
	}
}
