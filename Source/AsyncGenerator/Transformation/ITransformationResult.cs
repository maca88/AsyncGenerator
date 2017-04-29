using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncGenerator.Transformation
{
	public interface ITransformationResult
	{
		/// <summary>
		/// The annotation that is applied on the transformed node
		/// </summary>
		string Annotation { get; }
	}
}
