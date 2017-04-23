using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Plugins
{
	public abstract class AbstractPlugin : IPlugin
	{
		public virtual Task Initialize(Project project)
		{
			return Task.CompletedTask;
		}
	}
}
