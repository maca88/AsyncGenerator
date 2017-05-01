using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.FileComments.Input
{
	/// <include file='TestCase.cs.xmldoc' 
	///		path='//members[@type="TestCase"]/member[@name="T:TestCase"]/*'
	/// /> 
	public class TestCase
	{
		/// <include file='..\TestCase.cs.xmldoc' path='//members[@type="TestCase"]/member[@name="M:TestCase.Read"]/*' /> 
		public void Read()
		{
			SimpleFile.Read();
		}
	}
}
