using System.IO;
using System.Text;
using JetBrains.Annotations;

namespace Morestachio
{
	/// <summary>
	///		Internal class to ensure that the given limit of bytes to write is never extended to ensure template quotas
	/// </summary>
	/// <seealso cref="System.IDisposable" />
	internal class ByteCounterStream : IByteCounterStream
	{
		public ByteCounterStream([NotNull] Stream stream, [NotNull] Encoding encoding, int bufferSize, bool leaveOpen)
		{
			BaseWriter = new StreamWriter(stream, encoding, bufferSize, leaveOpen);
		}

		public StreamWriter BaseWriter { get; set; }

		public long BytesWritten { get; set; }
		public bool ReachedLimit { get; set; }

		public void Write(string value, long sizeOfContent)
		{
			BytesWritten += sizeOfContent;
			BaseWriter.Write(value);
		}

		public void Write(string value)
		{
			BaseWriter.Write(value);
		}

		public void Write(char[] value, long sizeOfContent)
		{
			BytesWritten += sizeOfContent;
			BaseWriter.Write(value);
		}

		public void Dispose()
		{
			BaseWriter.Flush();
			BaseWriter.Dispose();
		}
	}
}