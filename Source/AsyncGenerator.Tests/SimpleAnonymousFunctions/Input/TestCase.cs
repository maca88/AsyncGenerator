using System;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.SimpleAnonymousFunctions.Input
{
	public class TestCase
	{
		public delegate void GetMessage(out string message);

		public GetMessage DeclareNamedDelegate()
		{
			var namedDelegateFunction = new GetMessage((out string message) =>
			{
				ReadFile();
				message = "Success";
			});
			return namedDelegateFunction;
		}

		public Action ReturnDelegate()
		{
			return delegate
			{
				ReadFile();
			};
		}

		public Func<string, bool> DeclareFunction()
		{
			Func<string, bool> function = path =>
			{
				ReadFile();
				return true;
			};
			return function;
		}


		public Action DeclareAction()
		{
			Action delegateFunction = delegate
			{
				ReadFile();
			};
			return delegateFunction;
		}

		public void ArgumentAction()
		{
			Task.Run(() =>
			{
				ReadFile();
			}).Wait();
		}

		public void ReadFile()
		{
			SimpleFile.Read();
		}
	}
}
