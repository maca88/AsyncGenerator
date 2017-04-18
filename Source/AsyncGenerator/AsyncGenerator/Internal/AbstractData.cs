namespace AsyncGenerator.Internal
{
	internal abstract class AbstractData
	{
		/// <summary>
		/// References of types that are used inside this data (eg. alias to a type with a using statement, cref reference)
		/// </summary>
		public ConcurrentSet<TypeReferenceData> TypeReferences { get; } = new ConcurrentSet<TypeReferenceData>();
	}
}
