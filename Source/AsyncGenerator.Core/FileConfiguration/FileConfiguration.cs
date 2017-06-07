using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace AsyncGenerator.Core.FileConfiguration
{
	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true, Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot(Namespace = "https://github.com/maca88/AsyncGenerator", IsNullable = false)]
	public class AsyncGenerator
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
	public class Solution
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
	public class Project
	{
		[XmlElement("Name")]
		public string Name { get; set; }
		[XmlElement("Analyzation")]
		public Analyzation Analyzation { get; set; }
		[XmlElement("Transformation")]
		public Transformation Transformation { get; set; }
		[XmlArrayItem("Plugin", IsNullable = false)]
		public List<ProjectPlugin> RegisterPlugin { get; set; }

		public Project()
		{
			Transformation = new Transformation();
			Analyzation = new Analyzation();
			RegisterPlugin = new List<ProjectPlugin>();
		}
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("Analyzation")]
	public class Analyzation
	{
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodConversionFilter> MethodConversion { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodPreserveReturnTypeFilter> PreserveReturnType { get; set; }
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
		[XmlArrayItem("Type", IsNullable = false)]
		public List<TypeScanMissingAsyncMembersFilter> ScanForMissingAsyncMembers { get; set; }

		public Analyzation()
		{
			CancellationTokens = new CancellationTokens();
			SearchForAsyncCounterparts = new List<MethodSearchFilter>();
			DocumentSelection = new List<DocumentFilter>();
			TypeConversion = new List<TypeConversionFilter>();
			PreserveReturnType = new List<MethodPreserveReturnTypeFilter>();
			MethodConversion = new List<MethodConversionFilter>();
			ScanForMissingAsyncMembers = new List<TypeScanMissingAsyncMembersFilter>();
		}
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MethodConversionFilter")]
	public class MethodConversionFilter : MethodFilter
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

	[XmlInclude(typeof(MethodPreserveReturnTypeFilter))]
	[XmlInclude(typeof(MethodCancellationTokenFilter))]
	[XmlInclude(typeof(MethodRequiresTokenFilter))]
	[XmlInclude(typeof(MethodSearchFilter))]
	[XmlInclude(typeof(MethodConversionFilter))]
	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MethodFilter")]
	public class MethodFilter : MemberFilter
	{
	}

	[XmlInclude(typeof(MethodPreserveReturnTypeFilter))]
	[XmlInclude(typeof(TypeFilter))]
	[XmlInclude(typeof(TypeScanMissingAsyncMembersFilter))]
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
	public class MemberFilter
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
		[XmlAttribute(AttributeName = "containingAssemblyName")]
		public string ContainingAssemblyName { get; set; }
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
	public class Rule
	{
		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("TypeRule")]
	public class TypeRule : Rule
	{
		[XmlArrayItem("Filter", IsNullable = false)]
		public List<TypeFilter> Filters { get; set; }

		public TypeRule()
		{
			Filters = new List<TypeFilter>();
		}
	}
	[XmlInclude(typeof(TypeScanMissingAsyncMembersFilter))]
	[XmlInclude(typeof(TypeConversionFilter))]
	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("TypeFilter")]
	public class TypeFilter : MemberFilter
	{
		[XmlAttribute(AttributeName = "anyInterfaceRule")]
		public string AnyInterfaceRule { get; set; }
		[XmlAttribute(AttributeName = "anyBaseTypeRule")]
		public string AnyBaseTypeRule { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("TypeConversionFilter")]
	public class TypeConversionFilter : TypeFilter
	{
		[XmlAttribute(AttributeName = "conversion")]
		public TypeConversion Conversion { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("TypeScanMissingAsyncMembersFilter")]
	public class TypeScanMissingAsyncMembersFilter : TypeFilter
	{
		[XmlAttribute(AttributeName = "scan")]
		public bool Scan { get; set; }
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
	public class MethodRule : Rule
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
	public class Transformation
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
	public class TransformationAsyncLock
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
	public class CancellationTokens
	{
		[XmlElement("Guards", IsNullable = true)]
		public bool? Guards { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodCancellationTokenFilter> MethodParameter { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodRequiresTokenFilter> RequiresCancellationToken { get; set; }

		public CancellationTokens()
		{
			MethodParameter = new List<MethodCancellationTokenFilter>();
			RequiresCancellationToken = new List<MethodRequiresTokenFilter>();
		}
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MethodCancellationTokenFilter")]
	public class MethodCancellationTokenFilter : MethodFilter
	{
		[XmlAttribute(AttributeName = "anyInterfaceRule")]
		public string AnyInterfaceRule { get; set; }
		[XmlAttribute(AttributeName = "parameter")]
		public MethodCancellationToken Parameter { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MethodPreserveReturnTypeFilter")]
	public class MethodPreserveReturnTypeFilter : MethodFilter
	{
		[XmlAttribute(AttributeName = "preserve")]
		public bool Preserve { get; set; }
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
	public class DocumentFilter
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
	public class MethodRequiresTokenFilter : MethodFilter
	{
		[XmlAttribute(AttributeName = "tokenRequired")]
		public bool TokenRequired { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MethodSearchFilter")]
	public class MethodSearchFilter : MethodFilter
	{
		[XmlAttribute(AttributeName = "search")]
		public bool Search { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategoryAttribute("code")]
	[XmlTypeAttribute(AnonymousType = true, Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRootAttribute("ProjectPlugin")]
	public class ProjectPlugin
	{
		[XmlAttribute(AttributeName = "fullTypeName")]
		public string FullTypeName { get; set; }
		[XmlAttribute(AttributeName = "assemblyName")]
		public string AssemblyName { get; set; }
	}
}
