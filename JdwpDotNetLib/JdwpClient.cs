using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace JdwpDotNet;

// Big endian

// Handshake - client sends "JDWP-Handshake" string 
// server responds with teh same string

public class JdwpClient
{
	public JdwpClient(string hostname, int port)
	{
		HostName = hostname;
		Port = port;
	}

	public readonly string HostName;
	public readonly int Port;

	TcpClient? tcpClient;
	NetworkStream? stream;

	const string handshake = "JDWP-Handshake";

	public async Task ConnectAsync(CancellationToken cancellationToken = default)
	{
		tcpClient = new TcpClient();

		await tcpClient.ConnectAsync(HostName, Port);
		stream = tcpClient.GetStream();

		var data = Encoding.ASCII.GetBytes(handshake);

		await stream.WriteAsync(data);

		var buffer = new byte[handshake.Length];

		// Read handshake response
		var read = await stream.ReadAsync(buffer, 0, buffer.Length);

		var str = Encoding.ASCII.GetString(buffer, 0, read);

		Debug.WriteLine($"RX: {str}");

		if (str.Equals(handshake))
		{
			// Send version request command to kick things off
			await Send(new VersionCommandPacket(), cancellationToken);

			// TODO:
			//var replyPacket = await ReadReply(cancellationToken);

			//if (replyPacket is not null)
			//	Debug.WriteLine($"RX Reply: {replyPacket.Id}, Data len: {replyPacket.Data.Length}");
		}
		else
		{
			throw new InvalidDataException($"Debugger response did not match expected value: '{handshake}'");
		}
	}

	async Task<ReplyPacket?> ReadReply(CancellationToken cancellationToken = default)
	{
		while (true)
		{
			Memory<byte> buffer = new();

			if (stream is not null)
			{
				Memory<byte> headerData = new byte[11];

				// Read the header data or bust
				await stream.ReadExactlyAsync(headerData, cancellationToken);

				Debug.WriteLine("RX Reply Header");

				// Get overall packet length from header
				var packetLength = BinaryPrimitives.ReadUInt32BigEndian(buffer[0..3].Span);

				// The remaining packet buffer is total packet length minus header length
				Memory<byte> packetData = new byte[packetLength - headerData.Length];

				// Read the remainder of the packet into the second buffer
				await stream.ReadExactlyAsync(packetData, cancellationToken);

				return new ReplyPacket(headerData, packetData);
			}
			else
			{
				return null;
			}
		}
	}

	public async Task DisconnectAsync()
	{
		if (stream is not null)
		{
			try
			{
				await stream.DisposeAsync();
			}
			catch { }
			finally { stream = null; }
		}

		if (tcpClient is not null)
		{
			try
			{
				tcpClient?.Close();
			} catch { }

			try
			{
				tcpClient?.Dispose();
			}
			catch { }
			finally { tcpClient = null; }
		}
	}

	public async Task Send(CommandPacket packet, CancellationToken cancellationToken = default)
	{
		if (stream is not null)
		{
			await stream.WriteAsync(packet.ToMemory());
		}
	}
}
