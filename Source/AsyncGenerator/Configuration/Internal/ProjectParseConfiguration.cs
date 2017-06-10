using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Configuration;
using Microsoft.CodeAnalysis.CSharp;

namespace AsyncGenerator.Configuration.Internal
{
	internal class ProjectParseConfiguration : IFluentProjectParseConfiguration
	{
		public List<string> AddPreprocessorSymbolNames { get; } = new List<string>();

		public List<string> RemovePreprocessorSymbolNames { get; } = new List<string>();

		public LanguageVersion? LanguageVersion { get; private set; }

		#region IFluentProjectParseConfiguration

		IFluentProjectParseConfiguration IFluentProjectParseConfiguration.AddPreprocessorSymbolName(string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}
			AddPreprocessorSymbolNames.Add(value);
			return this;
		}

		IFluentProjectParseConfiguration IFluentProjectParseConfiguration.RemovePreprocessorSymbolName(string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}
			RemovePreprocessorSymbolNames.Add(value);
			return this;
		}

		IFluentProjectParseConfiguration IFluentProjectParseConfiguration.LanguageVersion(LanguageVersion value)
		{
			LanguageVersion = value;
			return this;
		}

		#endregion
	}
}
