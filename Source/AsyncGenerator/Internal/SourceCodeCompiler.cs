using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Extensions.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace AsyncGenerator.Internal
{
	internal class SourceCodeCompiler
	{
		public static Assembly Compile(string sourceCode)
		{
			var assembly = Assembly.GetExecutingAssembly();
			var assemblyPath = assembly.GetPath();
			var assemblyDir = Path.GetDirectoryName(assemblyPath);
			var assemRefs = assembly.GetReferencedAssemblies();
			var references = assemRefs.Select(assemblyName => assemblyName.Name + ".dll").ToList();
			for (var i = 0; i < references.Count; i++)
			{
				var localName = Path.Combine(assemblyDir, references[i]);
				if (File.Exists(localName))
					references[i] = localName;
			}
			references.Add(assemblyPath);

			var result = CSharpScript.Create(sourceCode, ScriptOptions.Default
					.WithReferences(references))
				.GetCompilation();

			using (var mStrem = new MemoryStream())
			{
				var emit = result.Emit(mStrem);
				if (!emit.Success)
					throw new InvalidOperationException($"Source code was not compiled due the following errors:{Environment.NewLine}{string.Join(Environment.NewLine, emit.Diagnostics.Where(o => o.Severity == DiagnosticSeverity.Error))}");
				return Assembly.Load(mStrem.ToArray());
			}
		}
	}
}
