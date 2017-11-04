using System;
using System.Collections.Generic;
using System.IO;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.PrivateFields.Input
{
	public class UsedByNestedFunction
	{
		private string _content = "";
		private string _content2 = "";
		private string _content3 = "";

		public void Write()
		{
			Runner.Run(s =>
			{
				SimpleFile.Write(_content);
			});

			var arr = new List<Action>
			{
				() =>
				{
					SimpleFile.Write(_content2);
				},
				delegate
				{
					File.Open(_content3, FileMode.Append);
				}
			};
		}
	}
}
