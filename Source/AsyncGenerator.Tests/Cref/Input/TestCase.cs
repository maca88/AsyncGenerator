using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Cref.Input
{
	public class TestCase
	{
		/// <summary>
		/// Uses the <see cref="Read(bool, IList{bool})"/> method
		/// </summary>
		public void Read()
		{
			SimpleFile.Read();
		}

		public void Read(bool value, IList<bool> generic)
		{
			SimpleFile.Read();
		}

		/// <summary>
		/// It is the same as calling <see cref="ReadFile(bool, bool, bool)"/> with the last two parameters as false
		/// </summary>
		/// <param name="value"></param>
		public void ReadFile(bool value)
		{
			ReadFile(value, false);
		}

		/// <summary>
		/// Uses <see cref="ReadFile(bool, bool, bool)"/> with the last parameter as false
		/// </summary>
		/// <param name="value"></param>
		/// <param name="value2"></param>
		public void ReadFile(bool value, bool value2)
		{
			ReadFile(value, value2, false);
		}

		public void ReadFile(bool value, bool value2, bool value3)
		{
			SimpleFile.Read();
		}

		/// <summary>
		/// Uses <see cref="WriteToFile"/>
		/// </summary>
		/// <returns></returns>
		public bool Write()
		{
			return WriteToFile(false, null, null);
		}

		public bool WriteToFile(bool value, byte[] bytes, IList<bool> bools)
		{
			return SimpleFile.Write("");
		}

	}
}
