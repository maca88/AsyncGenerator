using System;
using System.IO;
using System.Threading.Tasks;

namespace AsyncGenerator.Tests.Partial.TestCases
{
	public class SimpleAnonymousFunctions
	{
		public delegate void GetMessage(out string message);

		public GetMessage DeclareNamedDelegate()
		{
			var namedDelegateFunction = new GetMessage((out string message) =>
			{
				ReadFile("");
				message = "Success";
			});
			return namedDelegateFunction;
		}

		public Action ReturnDelegate()
		{
			return delegate
			{
				ReadFile("");
			};
		}

		public Func<string, int> DeclareFunction()
		{
			Func<string, int> function = path =>
			{
				return ReadFile(path);
			};
			return function;
		}


		public Action DeclareAction()
		{
			Action delegateFunction = delegate
			{
				ReadFile("");
			};
			return delegateFunction;
		}

		public void ArgumentAction()
		{
			Task.Run(() =>
			{
				ReadFile("");
			});
		}

		public int ReadFile(string path)
		{
			var stream = File.OpenRead(path);
			return stream.Read(new byte[0], 0, 0);
		}
	}
}
