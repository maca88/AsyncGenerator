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
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class AsyncGenerator
	{
		[XmlElement("Solution")]
		public Solution Solution { get; set; }
		[XmlArrayItem(IsNullable = false)]
		public List<MethodRule> MethodRules { get; set; }
		[XmlArrayItem(IsNullable = false)]
		public List<TypeRule> TypeRules { get; set; }
		[XmlElement("CSharpScript", IsNullable = true)]
		public string CSharpScript { get; set; }

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
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class Solution
	{
		[XmlAttribute(AttributeName = "filePath")]
		public string FilePath { get; set; }
		[XmlElement(IsNullable = true)]
		public bool? ConcurrentRun { get; set; }
		[XmlElement(IsNullable = true)]
		public bool? ApplyChanges { get; set; }
		[XmlArrayItem(IsNullable = false)]
		public List<Project> Projects { get; set; }
		[XmlArrayItem("Suppress", IsNullable = false)]
		public List<SuppressDiagnosticFaliure> SuppressDiagnosticFaliures { get; set; }

		public Solution()
		{
			Projects = new List<Project>();
			SuppressDiagnosticFaliures = new List<SuppressDiagnosticFaliure>();
		}
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("Project")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class Project
	{
		[XmlAttribute(AttributeName = "name")]
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
	[XmlRoot("AsyncExtensionMethods")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class AsyncExtensionMethods
	{
		[XmlArrayItem("ProjectFile", IsNullable = false)]
		public List<ProjectFile> ProjectFiles { get; set; }

		public AsyncExtensionMethods()
		{
			ProjectFiles = new List<ProjectFile>();
		}
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("Analyzation")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class Analyzation
	{
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodConversionFilter> MethodConversion { get; set; }
		[XmlElement("AsyncExtensionMethods")]
		public AsyncExtensionMethods AsyncExtensionMethods { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodFilter> PreserveReturnType { get; set; }
		[XmlArrayItem("Type", IsNullable = false)]
		public List<TypeConversionFilter> TypeConversion { get; set; }
		[XmlArrayItem("Document", IsNullable = false)]
		public List<DocumentFilter> IgnoreDocuments { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodFilter> IgnoreSearchForAsyncCounterparts { get; set; }
		[XmlElement(IsNullable = true)]
		public bool? CallForwarding { get; set; }
		[XmlElement(IsNullable = true)]
		public bool? PropertyConversion { get; set; }
		[XmlElement("CancellationTokens")]
		public CancellationTokens CancellationTokens { get; set; }
		[XmlElement(IsNullable = true)]
		public bool? ScanMethodBody { get; set; }
		[XmlArrayItem("Type", IsNullable = false)]
		public List<TypeFilter> ScanForMissingAsyncMembers { get; set; }

		public Analyzation()
		{
			AsyncExtensionMethods = new AsyncExtensionMethods();
			CancellationTokens = new CancellationTokens();
			IgnoreSearchForAsyncCounterparts = new List<MethodFilter>();
			IgnoreDocuments = new List<DocumentFilter>();
			TypeConversion = new List<TypeConversionFilter>();
			PreserveReturnType = new List<MethodFilter>();
			MethodConversion = new List<MethodConversionFilter>();
			ScanForMissingAsyncMembers = new List<TypeFilter>();
		}
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MethodConversionFilter")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class MethodConversionFilter : MethodFilter
	{
		[XmlAttribute(AttributeName = "conversion")]
		public MethodConversion Conversion { get; set; }
	}

	[XmlInclude(typeof(MethodCancellationTokenFilter))]
	[XmlInclude(typeof(MethodConversionFilter))]
	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MethodFilter")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class MethodFilter : MemberFilter
	{
	}

	[XmlInclude(typeof(TypeFilter))]
	[XmlInclude(typeof(TypeScanMissingAsyncMembersFilter))]
	[XmlInclude(typeof(TypeConversionFilter))]
	[XmlInclude(typeof(MethodFilter))]
	[XmlInclude(typeof(MethodCancellationTokenFilter))]
	[XmlInclude(typeof(MethodConversionFilter))]
	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MemberFilter")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class MemberFilter
	{
		[XmlAttribute(AttributeName = "all")]
		public bool All { get; set; }
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
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class Rule
	{
		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("ProjectFile")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class ProjectFile
	{
		[XmlAttribute(AttributeName = "fileName")]
		public string FileName { get; set; }

		[XmlAttribute(AttributeName = "projectName")]
		public string ProjectName { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("TypeRule")]
	[EditorBrowsable(EditorBrowsableState.Never)]
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
	[EditorBrowsable(EditorBrowsableState.Never)]
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
	[EditorBrowsable(EditorBrowsableState.Never)]
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
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class TypeScanMissingAsyncMembersFilter : TypeFilter
	{
		[XmlAttribute(AttributeName = "scan")]
		public bool Scan { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MethodRule")]
	[EditorBrowsable(EditorBrowsableState.Never)]
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
	[EditorBrowsable(EditorBrowsableState.Never)]
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
		[XmlElement("DocumentationComments")]
		public DocumentationComments DocumentationComments { get; set; }

		public Transformation()
		{
			AsyncLock = new TransformationAsyncLock();
			DocumentationComments = new DocumentationComments();
		}
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true, Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("TransformationAsyncLock")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class TransformationAsyncLock
	{
		[XmlAttribute("type")]
		public string Type { get; set; }
		[XmlAttribute("methodName")]
		public string MethodName { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("CancellationTokens")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class CancellationTokens
	{
		[XmlElement("Guards", IsNullable = true)]
		public bool? Guards { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodCancellationTokenFilter> MethodParameter { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodFilter> WithoutCancellationToken { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodFilter> RequiresCancellationToken { get; set; }

		public CancellationTokens()
		{
			MethodParameter = new List<MethodCancellationTokenFilter>();
			WithoutCancellationToken = new List<MethodFilter>();
			RequiresCancellationToken = new List<MethodFilter>();
		}
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("DocumentationComments")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class DocumentationComments
	{
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodContentFilter> AddOrReplaceMethodSummary { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodFilter> RemoveMethodSummary { get; set; }

		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodContentFilter> AddOrReplaceMethodRemarks { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodFilter> RemoveMethodRemarks { get; set; }

		public DocumentationComments()
		{
			AddOrReplaceMethodSummary = new List<MethodContentFilter>();
			RemoveMethodSummary = new List<MethodFilter>();
			AddOrReplaceMethodRemarks = new List<MethodContentFilter>();
			RemoveMethodRemarks = new List<MethodFilter>();
		}
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MethodCancellationTokenFilter")]
	[EditorBrowsable(EditorBrowsableState.Never)]
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
	[XmlRoot("MethodContentFilter")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class MethodContentFilter : MethodFilter
	{
		[XmlElement("Content")]
		public string Content { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("DocumentFilter")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class DocumentFilter
	{
		[XmlAttribute(AttributeName = "filePath")]
		public string FilePath { get; set; }
		[XmlAttribute(AttributeName = "filePathEndsWith")]
		public string FilePathEndsWith { get; set; }
		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true, Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("ProjectPlugin")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class ProjectPlugin
	{
		[XmlAttribute(AttributeName = "type")]
		public string Type { get; set; }
		[XmlAttribute(AttributeName = "assemblyName")]
		public string AssemblyName { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true, Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("SuppressDiagnosticFaliure")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class SuppressDiagnosticFaliure
	{
		[XmlAttribute(AttributeName = "pattern")]
		public string Pattern { get; set; }
	}
}
