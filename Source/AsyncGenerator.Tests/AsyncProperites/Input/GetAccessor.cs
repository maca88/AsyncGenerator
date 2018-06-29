using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncProperites.Input
{
	public interface IGetAccessor
	{
		bool Write { get; }
#if TEST
		Task<bool> GetWriteAsync();
#endif
	}

	public class GetAccessor : IGetAccessor
	{
		private bool _value;
		private bool _isBusy;

		public bool Write
		{
			get
			{
				if (!IsBusy)
				{
					_value = SimpleFile.Write("");
				}

				return _value;
			}
		}

		public bool IsBusy
		{
			get => _isBusy;
			set => _isBusy = value;
		}
	}
}
