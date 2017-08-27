using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Fields.Input
{
	/// <summary>
	/// The field must be removed when the class is converted to a new type where only the Read method will get converted.
	/// A field must be removed when nobody set it otherwise we get a warning.
	/// </summary>
	public class UnusedField
	{
		private Type _field;
		private Type _field2 = typeof(UnusedField);
#pragma warning disable 649
		private Type _field3;
#pragma warning restore 649
		private static Type _field4 = null;
		private Type[] _field5 = {_field4};

		public void SetFieldValue(Type value)
		{
			_field = value;
		}

		public Type GetFieldValue()
		{
			return _field;
		}

		public Type Read()
		{
			SimpleFile.Read();
			return _field2;
		}

		public Type Property => _field3;

		public Type[] Types => _field5;
	}
}
