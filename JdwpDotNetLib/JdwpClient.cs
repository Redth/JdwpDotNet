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
			var replyPackets = await ReadReply<ReplyPacket>(cancellationToken);

			foreach (var replyPacket in replyPackets)
				Debug.WriteLine($"RX Reply: {replyPacket.Id}, Data len: {replyPacket.Data.Length}");

			// Keep the Connection for 1300 milliseconds, otherwise the Android OS ignores the connection!
			//https://github.com/aosp-mirror/platform_frameworks_base/blob/main/core/java/android/os/Debug.java#L101C50-L101C54
			await Task.Delay (1300);
		}
		else
		{
			throw new InvalidDataException($"Debugger response did not match expected value: '{handshake}'");
		}
	}

	async Task<IEnumerable<T>> ReadReply<T>(CancellationToken cancellationToken = default) where T : ReplyPacket, new()
	{
		List<T> packets = new List<T> ();
		do
		{
			if (stream != null)
			{
				byte[] headerData = new byte[11];

				// Read the header data or bust
				var read = await stream.ReadAsync (headerData, 0, headerData.Length, cancellationToken);
				if (read != headerData.Length) {
					break;
				}
				
				// Get overall packet length from header
				ReadOnlyMemory<byte> h = headerData;
				var packetLength = BinaryPrimitives.ReadUInt32BigEndian(h.Slice (0, 4).Span);
				// The remaining packet buffer is total packet length minus header length
				byte[] packetData = new byte[packetLength - headerData.Length];

				if (packetData.Length > 0) {
					// Read the remainder of the packet into the second buffer
					int datalen = packetData.Length;
					while (datalen > 0) {
						read = await stream.ReadAsync (packetData, 0, datalen, cancellationToken);
						datalen -= read;
						if (read == 0)
							break;
					}
					if (datalen > 0) {
						break;
					}
				}
				var packet = new T ();
				packet.FromMemory (headerData, packetData);
				packets.Add (packet);
			}
			else
			{
				break;
			}
		} while (stream.DataAvailable);
		return packets;
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
