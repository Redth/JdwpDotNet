﻿using System.Buffers.Binary;

namespace JdwpDotNet;

public class VersionCommandPacket :CommandPacket
{
	public VersionCommandPacket()
	{
		CommandSet = 1;
		Command = 1;
	}
}


public class CommandPacket : Packet
{
	// 0-63 sets of commands sent to target VM
	// 64-127 sets of commands sent to debugger
	// 128-256 vendor defined commands and extensions
	public byte CommandSet { get; set; } = 0x0;

	public byte Command { get; set; } = 0x0;


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

		// Command Set, Command
		headerSpan.Span[9] = CommandSet;
		headerSpan.Span[10] = Command;

		// Data body if there is one
		if (dataLength > 0)
			Data.CopyTo(headerSpan[11..]);

		return headerSpan;
	}

}
