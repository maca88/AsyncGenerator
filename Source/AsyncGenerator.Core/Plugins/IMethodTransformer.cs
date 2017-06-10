using AsyncGenerator.Core.Transformation;

namespace AsyncGenerator.Core.Plugins
{
	public interface IMethodTransformer : IPlugin
	{
		MethodTransformerResult Transform(IMethodTransformationResult methodTransformResult,
			ITypeTransformationMetadata typeMetadata, INamespaceTransformationMetadata namespaceMetadata);

	}
}
