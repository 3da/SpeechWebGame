namespace SpeechWebGame.Tools
{
    public class StreamWrapper : Stream
    {
        private readonly Stream _stream;

        private long _position;

        public StreamWrapper(Stream stream)
        {
            _stream = stream;
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _stream.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _stream.Read(buffer, offset, count);
            _position += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
            if (offset > _position)
            {
                var buf = new byte[offset - _position];
                var readedCount = _stream.Read(buf, 0, buf.Length);
            }
            return _position;
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => _stream.CanWrite;
        public override long Length => long.MaxValue;

        public override long Position
        {
            get { return _position; }
            set { }
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return _stream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = await _stream.ReadAsync(buffer, offset, count, cancellationToken);
            _position += read;
            return read;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override int Read(Span<byte> buffer)
        {
            var read = _stream.Read(buffer);
            _position += read;
            return read;
        }

        public override void Close()
        {
            _stream.Close();
        }

        protected override void Dispose(bool disposing)
        {
            _stream.Dispose();
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _stream.Write(buffer);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            var read = await _stream.ReadAsync(buffer, cancellationToken);
            _position += read;
            return read;
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            return _stream.WriteAsync(buffer, cancellationToken);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            throw new NotImplementedException();
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            throw new NotImplementedException();
        }

        public override bool CanTimeout => _stream.CanTimeout;

        public override void CopyTo(Stream destination, int bufferSize)
        {
            _stream.CopyTo(destination, bufferSize);
        }

        public override ValueTask DisposeAsync()
        {
            return _stream.DisposeAsync();
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public override int ReadByte()
        {
            _position++;
            return _stream.ReadByte();
        }

        public override void WriteByte(byte value)
        {
            _stream.WriteByte(value);
        }
    }
}
