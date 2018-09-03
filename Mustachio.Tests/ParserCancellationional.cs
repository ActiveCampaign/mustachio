using System.Threading;

namespace Mustachio.Tests
{
	public class ParserCancellationional
	{
		private readonly CancellationTokenSource _tokenSource;
		private string _valueCancel;

		public ParserCancellationional(CancellationTokenSource tokenSource)
		{
			_tokenSource = tokenSource;
			ValueA = "ValueA";
			ValueB = "ValueB";
			ValueCancel = "ValueCancel";
		}

		public string ValueA { get; set; }
		public string ValueB { get; set; }

		public string ValueCancel
		{
			get
			{
				_tokenSource.Cancel();
				return _valueCancel;
			}
			set { _valueCancel = value; }
		}
	}
}