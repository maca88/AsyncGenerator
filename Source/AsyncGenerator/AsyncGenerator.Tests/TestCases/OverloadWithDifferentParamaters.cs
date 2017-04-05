using System.Threading;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.TestCases
{
	public class DataReader
	{
		public virtual void Read()
		{
		}

		public Task ReadAsync()
		{
			return ReadAsync(CancellationToken.None);
		}

		public virtual Task ReadAsync(CancellationToken cancellationToken)
		{
			return Task.Delay(1, cancellationToken);
		}
	}

	public class CustomDataReader : DataReader
	{
		public override void Read()
		{
		}

		public override Task ReadAsync(CancellationToken cancellationToken)
		{
			return Task.Delay(2, cancellationToken);
		}
	}

	public class OverloadWithDifferentParamaters
	{
		public void ReadData(DataReader dataReader)
		{
			dataReader.Read();
		}

		public void ReadData(CustomDataReader dataReader)
		{
			dataReader.Read();
		}
	}
}
