using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mustachio.Helper
{
	/// <summary>
	///		Helper class for Steam operations
	/// </summary>
	public static class StreamExtentions
	{
		/// <summary>
		///		Reads all content from the Stream and returns it as a String
		/// </summary>
		/// <param name="source"></param>
		/// <param name="disposeOriginal"></param>
		/// <param name="encoding"></param>
		/// <returns></returns>
		public static string Stringify(this Stream source, bool disposeOriginal, Encoding encoding)
		{
			try
			{
				source.Seek(0, SeekOrigin.Begin);
				var stream = source as MemoryStream;
				if (stream != null)
				{
					return encoding.GetString(stream.ToArray());
				}

				using (var ms = new StreamReader(source, encoding))
				{
					return ms.ReadToEnd();
				}
			}
			finally
			{
				if (disposeOriginal)
				{
					source.Dispose();
				}
			}
		}
	}

}
