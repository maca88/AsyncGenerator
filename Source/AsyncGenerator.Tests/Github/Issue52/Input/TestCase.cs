using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq.Expressions;
using AsyncGenerator.TestCases;

namespace AsyncGenerator.Tests.Github.Issue52.Input
{
	public class TestCase
	{
		[DefaultValue(nameof(Write))]
		public void Write()
		{
			SimpleFile.Write(nameof(DoSomething));
		}

		private void DoSomething()
		{
			
		}

		[DefaultValue(nameof(DoSomething2))]
		public void Read()
		{
			SimpleFile.Read();
		}

		private void DoSomething2()
		{

		}

		[DefaultValue(nameof(DoSomething3))]
		public bool Property
		{
			get => SimpleFile.Write("");
		}

		private void DoSomething3()
		{

		}

		public bool Accessor
		{
			[DefaultValue(nameof(DoSomething4))]
			get => SimpleFile.Write("");
		}

		private void DoSomething4()
		{

		}
	}
}
