using AsyncGenerator.Core.Transformation;

namespace AsyncGenerator.Core.Plugins
{
	public interface IMethodOrAccessorTransformer : IPlugin
	{
		MethodTransformerResult Transform(IMethodOrAccessorTransformationResult methodTransformResult,
			ITypeTransformationMetadata typeMetadata, INamespaceTransformationMetadata namespaceMetadata);

	}
}
