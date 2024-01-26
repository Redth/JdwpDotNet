using System.Buffers.Binary;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection.PortableExecutable;

namespace JdwpDotNet;

public class ReplyPacket : Packet
{
	public virtual void FromMemory(ReadOnlyMemory<byte> header, ReadOnlyMemory<byte> data)
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

	internal class EventSetCommandPacket : CommandPacket 
	{
		public EventSetCommandPacket(byte eventKind, byte suspendPolicy)
		{
			CommandSet = 15;
			Command = 1;
			EventKind = eventKind;
			SuspendPolicy = suspendPolicy;
			Memory<byte> data  = new byte[2];
			int index = 0;
			data.Span[index++] = EventKind;
			data.Span[index++] = SuspendPolicy;
			Data = data;
		}

		public byte EventKind = 0;
		public byte SuspendPolicy = 0;
	}

	internal class EventSetCommandIgnoreThrowablePacket : EventSetCommandPacket 
	{
		public EventSetCommandIgnoreThrowablePacket(byte eventKind, byte suspendPolicy) : base (eventKind, suspendPolicy)
		{
			CommandSet = 15;
			Command = 1;
			EventKind = eventKind;
			SuspendPolicy = suspendPolicy;
			Memory<byte> data  = new byte[35];
			int index = 0;
			data.Span[index++] = EventKind;
			data.Span[index++] = SuspendPolicy;
			BinaryPrimitives.WriteInt32BigEndian (data.Slice (index).Span, 2);
			index += 4;
			data.Span[index++] = 5;
			
			var bytes = Encoding.UTF8.GetBytes ("java.lang.Throwable");
			BinaryPrimitives.WriteInt32BigEndian (data.Slice (index).Span, bytes.Length);
			index += 4;
			foreach (var b in bytes) {
				data.Span[index++] = b;
			}
			data.Span[index++] = 1;
			BinaryPrimitives.WriteInt32BigEndian (data.Slice (index).Span, 1);
			Data = data;
		}

		public byte EventKind = 8;
		public byte SuspendPolicy = 2;
	}

	internal class EventClearCommandPacket : CommandPacket 
	{
		public EventClearCommandPacket(int eventKind, int requestId)
		{
			CommandSet = 15;
			Command = 2;
			Memory<byte> data  = new byte[8];
			BinaryPrimitives.WriteInt32BigEndian (data.Slice (0).Span, eventKind);
			BinaryPrimitives.WriteInt32BigEndian (data.Slice (4).Span, requestId);
			Data = data;
		}

		public byte EventKind = 8;
		public byte SuspendPolicy = 2;
		// public Modifier[] Modifiers = new Modifier[0];

		// public struct Modifier
		// {
		// 	public byte ModifierKind = 0;
		// }

	}

	internal class ThreadStatusCommandPacket : CommandPacket
	{
		public ThreadStatusCommandPacket(long threadId)
		{
			CommandSet = 11;
			Command = 4;
			ThreadId = threadId;
			Memory<byte> data  = new byte[8];
			BinaryPrimitives.WriteInt64BigEndian (data.Slice(0).Span, threadId);
			Data = data;
		}

		public long ThreadId {get;set;}
	}

	internal class AllThreadsCommandPacket : CommandPacket
	{
		public AllThreadsCommandPacket()
		{
			CommandSet = 1;
			Command = 4;
		}
	}

	internal class AllThreadReply : ReplyPacket
	{
		public long[] ThreadIds {get; set; }
		public AllThreadReply()
		{
		}

		public override void FromMemory (ReadOnlyMemory<byte> header, ReadOnlyMemory<byte> data)
		{
			base.FromMemory (header, data);
			// process the data.
			var count = BinaryPrimitives.ReadInt32BigEndian(data.Slice(0, 4).Span);
			Console.WriteLine ($"DEBUG! got {count} Threads.");
			var buffer = data.Slice (4);
			ThreadIds = new long[count];
			for (int i=0; i < count; i++) {
				ThreadIds[i] =  BinaryPrimitives.ReadInt64BigEndian (buffer.Slice (i*8, 8).Span);
				Console.WriteLine ($"DEBUG! ThreadIds[{i}] = {ThreadIds[i]}.");
			}
		}
	}

	internal class ThreadStatusReply : ReplyPacket
	{
		public ThreadStatusReply()
		{
		}

		public override void FromMemory (ReadOnlyMemory<byte> header, ReadOnlyMemory<byte> data)
		{
			base.FromMemory (header, data);
			// process the data.
			var threadStatus = BinaryPrimitives.ReadInt32BigEndian(data.Slice(0, 4).Span);
			var suspendStatus = BinaryPrimitives.ReadInt32BigEndian(data.Slice(4, 4).Span);
			Console.WriteLine ($"\t Status:{threadStatus} Suspend:{suspendStatus}.");
			
		}
	}

	internal class EventSetReply : ReplyPacket
	{
		public EventSetReply()
		{
		}

		public override void FromMemory (ReadOnlyMemory<byte> header, ReadOnlyMemory<byte> data)
		{
			base.FromMemory (header, data);
			// process the data.
			RequestId = BinaryPrimitives.ReadInt32BigEndian(data.Slice(0, 4).Span);
			Console.WriteLine ($"\t RequestId:{RequestId}");
			
		}

		public int RequestId;
	}
}
