using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Core.Configuration;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Core.Plugins
{
	public class ReaderWriterLockSlimPlugin : IAsyncCounterpartsFinder
	{
		private INamedTypeSymbol _readerWriterLockSlimTypeSymbol;
		private INamedTypeSymbol _asyncReaderWriterLockTypeSymbol;
		private IMethodSymbol _enterReadLockMethodSymbol;
		private IMethodSymbol _enterReadLockAsyncMethodSymbol;
		private IMethodSymbol _enterWriteLockMethodSymbol;
		private IMethodSymbol _enterWriteLockAsyncMethodSymbol;
		private IMethodSymbol _enterUpgradeableReadLockMethodSymbol;
		private IMethodSymbol _enterUpgradeableReadLockAsyncMethodSymbol;
		private readonly string _asyncReaderWriterLockFullName;

		public ReaderWriterLockSlimPlugin(string asyncReaderWriterLockFullName)
		{
			_asyncReaderWriterLockFullName = asyncReaderWriterLockFullName;
		}

		public Task Initialize(Project project, IProjectConfiguration configuration, Compilation compilation)
		{
			_readerWriterLockSlimTypeSymbol =
				compilation.References
					.Select(compilation.GetAssemblyOrModuleSymbol)
					.OfType<IAssemblySymbol>()
					.Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName("System.Threading.ReaderWriterLockSlim"))
					.FirstOrDefault(o => o != null);
			if (_readerWriterLockSlimTypeSymbol == null)
			{
				throw new InvalidOperationException("Unable to find System.Threading.ReaderWriterLockSlim type");
			}

			_asyncReaderWriterLockTypeSymbol = compilation.GetTypeByMetadataName(_asyncReaderWriterLockFullName) ?? compilation.References
				.Select(compilation.GetAssemblyOrModuleSymbol)
				.OfType<IAssemblySymbol>()
				.Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName(_asyncReaderWriterLockFullName))
				.FirstOrDefault(o => o != null);
			if (_asyncReaderWriterLockTypeSymbol == null)
			{
				throw new InvalidOperationException($"Unable to find {_asyncReaderWriterLockFullName} type");
			}

			// Try to get members: EnterReadLock and EnterReadLockAsync
			_enterReadLockMethodSymbol = _readerWriterLockSlimTypeSymbol.GetMembers("EnterReadLock").OfType<IMethodSymbol>()
				.First();
			_enterReadLockAsyncMethodSymbol = _asyncReaderWriterLockTypeSymbol.GetMembers("EnterReadLockAsync").OfType<IMethodSymbol>()
				.FirstOrDefault();

			// Try to get members: EnterWriteLock and EnterWriteLockAsync
			_enterWriteLockMethodSymbol = _readerWriterLockSlimTypeSymbol.GetMembers("EnterWriteLock").OfType<IMethodSymbol>()
				.First();
			_enterWriteLockAsyncMethodSymbol = _asyncReaderWriterLockTypeSymbol.GetMembers("EnterWriteLockAsync").OfType<IMethodSymbol>()
				.FirstOrDefault();

			// Try to get members: EnterUpgradeableReadLock and EnterUpgradeableReadLockAsync
			_enterUpgradeableReadLockMethodSymbol = _readerWriterLockSlimTypeSymbol.GetMembers("EnterUpgradeableReadLock").OfType<IMethodSymbol>()
				.First();
			_enterUpgradeableReadLockAsyncMethodSymbol = _asyncReaderWriterLockTypeSymbol.GetMembers("EnterUpgradeableReadLockAsync").OfType<IMethodSymbol>()
				.FirstOrDefault();

			// Try to get member: void ExitWriteLock()
			var exitWriteLockMethodSymbol = _readerWriterLockSlimTypeSymbol.GetMembers("ExitWriteLock").OfType<IMethodSymbol>()
				.FirstOrDefault();

			//var readerWriterLockSymbol =
			//	compilation.References
			//		.Select(compilation.GetAssemblyOrModuleSymbol)
			//		.OfType<IAssemblySymbol>()
			//		.Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName("System.Threading.ReaderWriterLock"))
			//		.FirstOrDefault(o => o != null);
			//if (readerWriterLockSlimSymbol == null)
			//{
			//	throw new InvalidOperationException("Unable to find System.Threading.ReaderWriterLock type");
			//}

			return Task.CompletedTask;
		}

		public IEnumerable<IMethodSymbol> FindAsyncCounterparts(IMethodSymbol syncMethodSymbol, ITypeSymbol invokedFromType, AsyncCounterpartsSearchOptions options)
		{
			switch (syncMethodSymbol.Name)
			{
				case "EnterReadLock" when _enterReadLockAsyncMethodSymbol != null && syncMethodSymbol.Equals(_enterReadLockMethodSymbol):
					yield return _enterReadLockAsyncMethodSymbol;
					break;
				case "EnterWriteLock" when _enterWriteLockAsyncMethodSymbol != null && syncMethodSymbol.Equals(_enterWriteLockMethodSymbol):
					yield return _enterWriteLockAsyncMethodSymbol;
					break;
				case "EnterUpgradeableReadLock" when _enterUpgradeableReadLockAsyncMethodSymbol != null && syncMethodSymbol.Equals(_enterUpgradeableReadLockMethodSymbol):
					yield return _enterUpgradeableReadLockAsyncMethodSymbol;
					break;
			}
		}

		public void PreAnalyzeType(INamedTypeSymbol typeSymbol)
		{
			var locks = typeSymbol.GetMembers().OfType<IFieldSymbol>().Where(o => _readerWriterLockSlimTypeSymbol.Equals(o.Type))
				.ToList();


		}
	}
}
