namespace AsyncGenerator.Core.Transformation
{
	public interface ITransformationResult
	{
		/// <summary>
		/// The annotation that is applied on the transformed node
		/// </summary>
		string Annotation { get; }
	}
}
