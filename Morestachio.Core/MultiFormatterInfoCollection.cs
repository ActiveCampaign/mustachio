using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Morestachio.Formatter;

namespace Morestachio
{
	/// <inheritdoc />
	public class MultiFormatterInfoCollection : IReadOnlyList<MultiFormatterInfo>
	{
		private readonly IReadOnlyList<MultiFormatterInfo> _source;
		
		/// <inheritdoc />
		public MultiFormatterInfoCollection(IEnumerable<MultiFormatterInfo> source)
		{
			_source = source.ToArray();
		}

		/// <inheritdoc />
		public IEnumerator<MultiFormatterInfo> GetEnumerator()
		{
			return _source.GetEnumerator();
		}
		
		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
		{
			return _source.GetEnumerator();
		}
		
		/// <inheritdoc />
		public int Count
		{
			get { return _source.Count; }
		}

		/// <inheritdoc />
		public MultiFormatterInfo this[int index]
		{
			get { return _source[index]; }
		}

		/// <summary>
		///		Sets the name of an Parameter.
		/// </summary>
		/// <returns></returns>
		public MultiFormatterInfoCollection SetName(string parameterName, string templateParameterName)
		{
			var multiFormatterInfo = this.FirstOrDefault(e => e.Name.Equals(parameterName));
			if (multiFormatterInfo == null)
			{
				return this;
			}

			multiFormatterInfo.Name = templateParameterName;
			return this;
		}

		/// <summary>
		///		When called and the last parameter is an object array, it will be used as an params parameter.
		///		This is quite helpful as you cannot annotate Lambdas.
		/// </summary>
		/// <returns></returns>
		public MultiFormatterInfoCollection LastIsParams()
		{
			var multiFormatterInfo = this.LastOrDefault();
			if (multiFormatterInfo == null)
			{
				return this;
			}

			if (multiFormatterInfo.Type == typeof(object[]))
			{
				multiFormatterInfo.IsRestObject = true;
			}

			return this;
		}
	}
}