using System;

namespace Morestachio
{
	/// <summary>
	///		Defines the output that can count on written bytes into a stream
	/// </summary>
	public interface IByteCounterStream : IDisposable
	{
		/// <summary>
		/// Gets or sets the bytes written.
		/// </summary>
		/// <value>
		/// The bytes written.
		/// </value>
		long BytesWritten { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether [reached limit].
		/// </summary>
		/// <value>
		///   <c>true</c> if [reached limit]; otherwise, <c>false</c>.
		/// </value>
		bool ReachedLimit { get; set; }

		/// <summary>
		/// Writes the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="sizeOfContent">Content of the size of.</param>
		void Write(string value, long sizeOfContent);

		/// <summary>
		/// Writes the specified value. Without counting its bytes.
		/// </summary>
		/// <param name="value">The value.</param>
		void Write(string value);

		/// <summary>
		/// Writes the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="sizeOfContent">Content of the size of.</param>
		void Write(char[] value, long sizeOfContent);
	}
}