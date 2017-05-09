using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.CancellationTokens.Input
{
	public interface ITestInteraface
	{
		/// <summary>
		/// Reads
		/// </summary>
		void Read();

		/// <summary>
		/// Reads and writes
		/// </summary>
		/// <param name="content">The content</param>
		/// <returns></returns>
		bool ReadWrite(string content);

		/// <summary>
		/// Multi read
		/// </summary>
		void MultiRead();

		/// <summary>
		/// Writes
		/// </summary>
		/// <param name="content">The content</param>
		/// <returns></returns>
		bool Write(string content);
	}


	public abstract class AbstractTest : ITestInteraface
	{
		public virtual void Read()
		{
		}

		/// <summary>
		/// Reads and writes
		/// </summary>
		/// <param name="content"></param>
		public abstract bool ReadWrite(string content);

		/// <summary>
		/// Writes
		/// </summary>
		/// <param name="content"></param>
		public abstract bool Write(string content);

		public virtual void MultiRead()
		{
		}
	}

	public class TestCase : AbstractTest
	{
		public override void Read()
		{
			SimpleFile.Read();
		}

		public override bool ReadWrite(string content)
		{
			SimpleFile.Read();
			for (var i = 0; i < 10; i++)
			{
				SimpleFile.Write(content);
			}
			return SimpleFile.Write(content);
		}

		public override bool Write(string content)
		{
			if (content == null)
			{
				throw new ArgumentNullException(nameof(content));
			}
			SimpleFile.Read();
			for (var i = 0; i < 10; i++)
			{
				SimpleFile.Write(content);
			}
			return SimpleFile.Write(content);
		}

		public override void MultiRead()
		{
			var num = 5;
			if (num > 10)
			{
				SimpleFile.Read();
			}
		}
	}

	public class DerivedEmptyTestCase : TestCase
	{
		public override void Read()
		{
		}

		public override bool ReadWrite(string content)
		{
			return false;
		}
	}
}
