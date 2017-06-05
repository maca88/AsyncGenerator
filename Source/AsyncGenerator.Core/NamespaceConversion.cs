namespace AsyncGenerator.Core
{
	public enum NamespaceConversion
	{
		/// <summary>
		/// The conversion will be decided by the analyzer
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// The namespace will not get generated
		/// </summary>
		Ignore,
		/// <summary>
		/// The namespace will be generated
		/// </summary>
		Generate
	}
}
