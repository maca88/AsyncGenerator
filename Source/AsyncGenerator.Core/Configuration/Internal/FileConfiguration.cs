using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace AsyncGenerator.Core.Configuration.Internal
{
	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true, Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot(Namespace = "https://github.com/maca88/AsyncGenerator", IsNullable = false)]
	internal class AsyncGenerator
	{
		[XmlElement("Solution")]
		public Solution Solution { get; set; }
		[XmlArrayItem(IsNullable = false)]
		public List<MethodRule> MethodRules { get; set; }
		[XmlArrayItem(IsNullable = false)]
		public List<TypeRule> TypeRules { get; set; }

		public AsyncGenerator()
		{
			TypeRules = new List<TypeRule>();
			MethodRules = new List<MethodRule>();
			Solution = new Solution();
		}
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("Solution")]
	internal class Solution
	{
		[XmlElement("FilePath")]
		public string FilePath { get; set; }
		[XmlElement(IsNullable = true)]
		public bool? ConcurrentRun { get; set; }
		[XmlElement(IsNullable = true)]
		public bool? ApplyChanges { get; set; }
		[XmlArrayItem(IsNullable = false)]
		public List<Project> Projects { get; set; }

		public Solution()
		{
			Projects = new List<Project>();
		}
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("Project")]
	internal class Project
	{
		[XmlElement("Name")]
		public string Name { get; set; }
		[XmlElement("Analyzation")]
		public Analyzation Analyzation { get; set; }
		[XmlElement("Transformation")]
		public Transformation Transformation { get; set; }

		public Project()
		{
			Transformation = new Transformation();
			Analyzation = new Analyzation();
		}
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("Analyzation")]
	internal class Analyzation
	{
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodConversionFilter> MethodConversion { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodConversionFilter> PreserveReturnType { get; set; }
		[XmlArrayItem("Type", IsNullable = false)]
		public List<TypeConversionFilter> TypeConversion { get; set; }
		[XmlArrayItem("Document", IsNullable = false)]
		public List<DocumentFilter> DocumentSelection { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodSearchFilter> SearchForAsyncCounterparts { get; set; }
		[XmlElement(IsNullable = true)]
		public bool? CallForwarding { get; set; }
		[XmlElement("CancellationTokens")]
		public CancellationTokens CancellationTokens { get; set; }
		[XmlElement(IsNullable = true)]
		public bool? ScanMethodBody { get; set; }

		public Analyzation()
		{
			CancellationTokens = new CancellationTokens();
			SearchForAsyncCounterparts = new List<MethodSearchFilter>();
			DocumentSelection = new List<DocumentFilter>();
			TypeConversion = new List<TypeConversionFilter>();
			PreserveReturnType = new List<MethodConversionFilter>();
			MethodConversion = new List<MethodConversionFilter>();
		}
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MethodConversionFilter")]
	internal class MethodConversionFilter : MethodFilter
	{
		[XmlAttribute(AttributeName = "conversion")]
		public MethodConversion Conversion { get; set; }
	}

	[Serializable]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MethodConversion")]
	public enum MethodConversion
	{
		Ignore,
		Unknown,
		Smart,
		ToAsync
	}

	[XmlInclude(typeof(MethodCancellationTokenFilter))]
	[XmlInclude(typeof(MethodRequiresTokenFilter))]
	[XmlInclude(typeof(MethodSearchFilter))]
	[XmlInclude(typeof(MethodConversionFilter))]
	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MethodFilter")]
	internal class MethodFilter : MemberFilter
	{
	}

	[XmlInclude(typeof(TypeFilter))]
	[XmlInclude(typeof(TypeConversionFilter))]
	[XmlInclude(typeof(MethodFilter))]
	[XmlInclude(typeof(MethodCancellationTokenFilter))]
	[XmlInclude(typeof(MethodRequiresTokenFilter))]
	[XmlInclude(typeof(MethodSearchFilter))]
	[XmlInclude(typeof(MethodConversionFilter))]
	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MemberFilter")]
	internal class MemberFilter
	{
		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }
		[XmlAttribute(AttributeName = "hasAttribute")]
		public string HasAttribute { get; set; }
		[XmlAttribute(AttributeName = "hasAttributeName")]
		public string HasAttributeName { get; set; }
		[XmlAttribute(AttributeName = "containingNamespace")]
		public string ContainingNamespace { get; set; }
		[XmlAttribute(AttributeName = "containingType")]
		public string ContainingType { get; set; }
		[XmlAttribute(AttributeName = "containingTypeName")]
		public string ContainingTypeName { get; set; }
		[XmlAttribute(AttributeName = "rule")]
		public string Rule { get; set; }
	}

	[XmlInclude(typeof(TypeRule))]
	[XmlInclude(typeof(MethodRule))]
	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("Rule")]
	internal class Rule
	{
		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("TypeRule")]
	internal class TypeRule : Rule
	{
		[XmlArrayItem("Filter", IsNullable = false)]
		public List<TypeFilter> Filters { get; set; }

		public TypeRule()
		{
			Filters = new List<TypeFilter>();
		}
	}

	[XmlInclude(typeof(TypeConversionFilter))]
	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("TypeFilter")]
	internal class TypeFilter : MemberFilter
	{
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("TypeConversionFilter")]
	internal class TypeConversionFilter : TypeFilter
	{
		[XmlAttribute(AttributeName = "conversion")]
		public TypeConversion Conversion { get; set; }
	}

	[Serializable]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("TypeConversion")]
	public enum TypeConversion
	{
		Ignore,
		Unknown,
		Partial,
		NewType
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MethodRule")]
	internal class MethodRule : Rule
	{
		[XmlArrayItem("Filter", IsNullable = false)]
		public List<MethodFilter> Filters { get; set; }

		public MethodRule()
		{
			Filters = new List<MethodFilter>();
		}
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("Transformation")]
	internal class Transformation
	{
		[XmlElement(IsNullable = true)]
		public bool? Disable { get; set; }
		[XmlElement("AsyncFolder")]
		public string AsyncFolder { get; set; }
		[XmlElement(IsNullable = true)]
		public bool? ConfigureAwaitArgument { get; set; }
		[XmlElement(IsNullable = true)]
		public bool? LocalFunctions { get; set; }
		[XmlElement("AsyncLock")]
		public TransformationAsyncLock AsyncLock { get; set; }

		public Transformation()
		{
			AsyncLock = new TransformationAsyncLock();
		}
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true, Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("TransformationAsyncLock")]
	internal class TransformationAsyncLock
	{
		[XmlElement("FullTypeName")]
		public string FullTypeName { get; set; }
		[XmlElement("MethodName")]
		public string MethodName { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("CancellationTokens")]
	internal class CancellationTokens
	{
		[XmlElement("Guards", IsNullable = true)]
		public bool? Guards { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodCancellationTokenFilter> MethodParameters { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodRequiresTokenFilter> RequiresCancellationToken { get; set; }

		public CancellationTokens()
		{
			MethodParameters = new List<MethodCancellationTokenFilter>();
			RequiresCancellationToken = new List<MethodRequiresTokenFilter>();
		}
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MethodCancellationTokenFilter")]
	internal class MethodCancellationTokenFilter : MethodFilter
	{
		[XmlAttribute(AttributeName = "anyInterfaceRule")]
		public string AnyInterfaceRule { get; set; }
		[XmlAttribute(AttributeName = "parameter")]
		public MethodCancellationToken Parameter { get; set; }
	}

	[Serializable]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MethodCancellationToken")]
	public enum MethodCancellationToken
	{
		Optional,
		Required,
		ForwardNone,
		SealedForwardNone,
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("DocumentFilter")]
	internal class DocumentFilter
	{
		[XmlAttribute(AttributeName = "filePath")]
		public string FilePath { get; set; }
		[XmlAttribute(AttributeName = "filePathEndsWith")]
		public string FilePathEndsWith { get; set; }
		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }
		[XmlAttribute(AttributeName = "select")]
		public bool Select { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MethodRequiresTokenFilter")]
	internal class MethodRequiresTokenFilter : MethodFilter
	{
		[XmlAttribute(AttributeName = "tokenRequired")]
		public bool TokenRequired { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MethodSearchFilter")]
	internal class MethodSearchFilter : MethodFilter
	{
		[XmlAttribute(AttributeName = "search")]
		public bool Search { get; set; }
	}
}
