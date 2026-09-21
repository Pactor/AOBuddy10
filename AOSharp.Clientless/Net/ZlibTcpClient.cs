using Serilog.Core;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using StreamReader = SmokeLounge.AOtomation.Messaging.Serialization.StreamReader;
using Ionic.Zlib;

namespace AOSharp.Clientless.Net
{
    internal class ZlibTcpClient : TcpClient
    {
        private readonly Logger _logger;
        private const ushort HeaderSize = 16;
        private const ushort RecvBufferSize = 8192;
        private List<byte> _buffer;
        private byte[] _recvBuffer;
        private HeaderSerializer _headerSerializer;

        private bool _usingZlib = false;
        private ZlibStream _zlibStream;

        public EventHandler<byte[]> PacketRecv;
        public EventHandler Disconnected;

        public ZlibTcpClient(Logger logger) : base(AddressFamily.InterNetwork)
        {
            _logger = logger;
            _buffer = new List<byte>();
            _headerSerializer = new HeaderSerializer();
            ReceiveTimeout = 180000;
        }

        public void Send(byte[] bytes)
        {
            if(Connected)
                GetStream().BeginWrite(bytes, 0, bytes.Length, SendCallback, null);
        }

        private void SendCallback(IAsyncResult result)
        {
            try
            {
                GetStream().EndWrite(result);
            }
            catch (Exception e) 
            {
                _logger.Error($"Failed to send message: {e}");
            }
        }

        private void ProcessBuffer()
        {
            while (_buffer.Count >= HeaderSize)
            {
                Header header = DeserializeHeader(_buffer.Take(HeaderSize).ToArray());

                if (header.PacketType == PacketType.InitiateCompressionMessage)
                {
                    _usingZlib = true;
                    _zlibStream = new ZlibStream(GetStream(), CompressionMode.Decompress);
                    _zlibStream.FlushMode = FlushType.Sync;
                }

                if (_buffer.Count < header.Size)
                    break;

                PacketRecv?.Invoke(null, _buffer.Take(header.Size).ToArray());

                int padding = header.Size % 4 == 0 ? 0 : 4 - header.Size % 4;
                _buffer.RemoveRange(0, header.Size + (!_usingZlib ? padding : 0));
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            if (!Connected)
                return;

            try
            {
                Stream stream = _usingZlib ? _zlibStream : (Stream)GetStream();
                int bytesRead = stream.EndRead(result);

                if(bytesRead == 0)
                {
                    Disconnected?.Invoke(null, null);
                    return;
                }

                byte[] readBytes = _recvBuffer.Take(bytesRead).ToArray();
                _buffer.AddRange(readBytes);

                ProcessBuffer();
            }
            catch (Exception e)
            {
                _logger.Error("Error on EndRead:\n" + e);
                return;
            }

            BeginReceiving();
        }

        public void BeginReceiving()
        {
            if (!Connected)
                return;

            try
            {
                _recvBuffer = new byte[RecvBufferSize];

                Stream stream = _usingZlib ? _zlibStream : (Stream)GetStream();
                stream.BeginRead(_recvBuffer, 0, RecvBufferSize, new AsyncCallback(ReceiveCallback), null);
            }
            catch (Exception e)
            {
                _logger.Error($"BeginRecv Error: {e}");
            }
        }

        private Header DeserializeHeader(byte[] header)
        {
            using (MemoryStream memStream = new MemoryStream(header))
                using(StreamReader reader = new StreamReader(memStream))
                    return (Header)_headerSerializer.Deserialize(reader, null);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _zlibStream?.Dispose();

            base.Dispose(disposing);
        }
    }
}
