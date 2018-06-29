using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.AsyncProperites.Input
{
	public interface ISetAccessor
	{
		bool Write { set; }
#if TEST
		Task SetWriteAsync(bool value);
#endif
	}

	public class SetAccessor : ISetAccessor
	{
		private bool _value;
		private bool _isBusy;

		public bool Write
		{
			set
			{
				if (!IsBusy)
				{
					_value = SimpleFile.Write("");
				}

				_value = value;
			}
		}

		public bool IsBusy
		{
			get => _isBusy;
			set => _isBusy = value;
		}
	}
}
