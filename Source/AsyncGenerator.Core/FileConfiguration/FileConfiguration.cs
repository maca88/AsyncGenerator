using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
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
		[XmlArrayItem("Project", IsNullable = false)]
		public List<Project> Projects { get; set; }
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
			Projects = new List<Project>();
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
		private string _filePath;

		[XmlAttribute(AttributeName = "filePath")]
		public string FilePath
		{
			get => _filePath;
			set => _filePath = value?.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
		}
		[XmlElement(IsNullable = true)]
		public bool? ConcurrentRun { get; set; }
		[XmlElement(IsNullable = true)]
		public bool? ApplyChanges { get; set; }
		[XmlElement(IsNullable = true)]
		public string TargetFramework { get; set; }
		[XmlArrayItem(IsNullable = false)]
		public List<Project> Projects { get; set; }
		[XmlArrayItem("Suppress", IsNullable = false)]
		public List<SuppressDiagnosticFailure> SuppressDiagnosticFailures { get; set; }

		public Solution()
		{
			Projects = new List<Project>();
			SuppressDiagnosticFailures = new List<SuppressDiagnosticFailure>();
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
		private string _filePath;

		[XmlAttribute(AttributeName = "filePath")]
		public string FilePath
		{
			get => _filePath;
			set => _filePath = value?.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
		}
		[XmlElement(IsNullable = true)]
		public bool? ConcurrentRun { get; set; }
		[XmlElement(IsNullable = true)]
		public string TargetFramework { get; set; }
		[XmlElement(IsNullable = true)]
		public bool? ApplyChanges { get; set; }
		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }
		[XmlElement("Analyzation")]
		public Analyzation Analyzation { get; set; }
		[XmlElement("Transformation")]
		public Transformation Transformation { get; set; }
		[XmlArrayItem("Plugin", IsNullable = false)]
		public List<ProjectPlugin> RegisterPlugin { get; set; }
		[XmlArrayItem("Suppress", IsNullable = false)]
		public List<SuppressDiagnosticFailure> SuppressDiagnosticFailures { get; set; }

		public Project()
		{
			Transformation = new Transformation();
			Analyzation = new Analyzation();
			RegisterPlugin = new List<ProjectPlugin>();
			SuppressDiagnosticFailures = new List<SuppressDiagnosticFailure>();
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
		[XmlElement("Diagnostics")]
		public Diagnostics Diagnostics { get; set; }
		[XmlElement("ExceptionHandling")]
		public ExceptionHandling ExceptionHandling { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodFilter> PreserveReturnType { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<AsyncReturnTypeFilter> AsyncReturnType { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodFilter> AlwaysAwait { get; set; }
		[XmlArrayItem("Type", IsNullable = false)]
		public List<TypeConversionFilter> TypeConversion { get; set; }
		[XmlArrayItem("Document", IsNullable = false)]
		public List<DocumentFilter> IgnoreDocuments { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodFilter> IgnoreSearchForAsyncCounterparts { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodFilter> IgnoreAsyncCounterparts { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodFilter> IgnoreSearchForMethodReferences { get; set; }
		[XmlElement(IsNullable = true)]
		public bool? CallForwarding { get; set; }
		[XmlElement(IsNullable = true)]
		public bool? PropertyConversion { get; set; }
		[XmlElement("CancellationTokens")]
		public CancellationTokens CancellationTokens { get; set; }
		[XmlElement(IsNullable = true)]
		public bool? ScanMethodBody { get; set; }
		[XmlElement(IsNullable = true)]
		public bool? SearchAsyncCounterpartsInInheritedTypes { get; set; }
		[XmlArrayItem("Type", IsNullable = false)]
		public List<TypeFilter> ScanForMissingAsyncMembers { get; set; }

		public Analyzation()
		{
			AsyncExtensionMethods = new AsyncExtensionMethods();
			Diagnostics = new Diagnostics();
			ExceptionHandling = new ExceptionHandling();
			CancellationTokens = new CancellationTokens();
			IgnoreSearchForAsyncCounterparts = new List<MethodFilter>();
			IgnoreAsyncCounterparts = new List<MethodFilter>();
			IgnoreSearchForMethodReferences = new List<MethodFilter>();
			IgnoreDocuments = new List<DocumentFilter>();
			TypeConversion = new List<TypeConversionFilter>();
			PreserveReturnType = new List<MethodFilter>();
			AlwaysAwait = new List<MethodFilter>();
			MethodConversion = new List<MethodConversionFilter>();
			ScanForMissingAsyncMembers = new List<TypeFilter>();
			AsyncReturnType = new List<AsyncReturnTypeFilter>();
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

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("AsyncReturnTypeFilter")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class AsyncReturnTypeFilter : MethodFilter
	{
		[XmlAttribute(AttributeName = "returnType")]
		public AsyncReturnType ReturnType { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MethodPredicateFilter")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class MethodPredicateFilter : MethodFilter
	{
		[XmlAttribute(AttributeName = "result")]
		public bool Result { get; set; }
	}

	[XmlInclude(typeof(MethodCancellationTokenFilter))]
	[XmlInclude(typeof(MethodConversionFilter))]
	[XmlInclude(typeof(MethodPredicateFilter))]
	[XmlInclude(typeof(AsyncReturnTypeFilter))]
	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("MethodFilter")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class MethodFilter : MemberFilter
	{
		#region ReturnsVoid
		[XmlAttribute(AttributeName = "returnsVoid")]
		public string ReturnsVoidString
		{
			get => ReturnsVoid?.ToString();
			set => ReturnsVoid = bool.TryParse(value, out var boolean)
				? (bool?)boolean
				: null;
		}

		[XmlIgnore]
		public bool? ReturnsVoid { get; private set; }
		#endregion

		[XmlAttribute(AttributeName = "executionPhase")]
		public ExecutionPhase ExecutionPhase { get; set; }
	}

	[XmlInclude(typeof(TypeFilter))]
	[XmlInclude(typeof(TypeScanMissingAsyncMembersFilter))]
	[XmlInclude(typeof(TypeConversionFilter))]
	[XmlInclude(typeof(TypePredicateFilter))]
	[XmlInclude(typeof(MethodFilter))]
	[XmlInclude(typeof(MethodCancellationTokenFilter))]
	[XmlInclude(typeof(MethodConversionFilter))]
	[XmlInclude(typeof(MethodPredicateFilter))]
	[XmlInclude(typeof(AsyncReturnTypeFilter))]
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
		#region HasDocumentationComment
		[XmlAttribute(AttributeName = "hasDocumentationComment")]
		public string HasDocumentationCommentString
		{
			get => HasDocumentationComment?.ToString();
			set => HasDocumentationComment = bool.TryParse(value, out var boolean)
				? (bool?)boolean
				: null;
		}

		[XmlIgnore]
		public bool? HasDocumentationComment { get; private set; }
		#endregion
		#region IsVirtual
		[XmlAttribute(AttributeName = "isVirtual")]
		public string IsVirtualString
		{
			get => IsVirtual?.ToString();
			set => IsVirtual = bool.TryParse(value, out var boolean)
				? (bool?)boolean
				: null;
		}

		[XmlIgnore]
		public bool? IsVirtual { get; private set; }
		#endregion
		#region IsAbstract
		[XmlAttribute(AttributeName = "isAbstract")]
		public string IsAbstractString
		{
			get => IsAbstract?.ToString();
			set => IsAbstract = bool.TryParse(value, out var boolean)
				? (bool?)boolean
				: null;
		}

		[XmlIgnore]
		public bool? IsAbstract { get; private set; }
		#endregion
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
	[XmlInclude(typeof(TypePredicateFilter))]
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
		[XmlAttribute(AttributeName = "executionPhase")]
		public ExecutionPhase ExecutionPhase { get; set; }
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
	[XmlRoot("TypePredicateFilter")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class TypePredicateFilter : TypeFilter
	{
		[XmlAttribute(AttributeName = "result")]
		public bool Result { get; set; }
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
		[XmlElement("PreprocessorDirectives")]
		public PreprocessorDirectives PreprocessorDirectives { get; set; }

		public Transformation()
		{
			AsyncLock = new TransformationAsyncLock();
			DocumentationComments = new DocumentationComments();
			PreprocessorDirectives = new PreprocessorDirectives();
		}
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("Diagnostics")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class Diagnostics
	{
		[XmlElement(IsNullable = true)]
		public bool? Disable { get; set; }
		[XmlArrayItem("Document", IsNullable = false)]
		public List<DocumentPredicateFilter> DiagnoseDocument { get; set; }
		[XmlArrayItem("Type", IsNullable = false)]
		public List<TypePredicateFilter> DiagnoseType { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodPredicateFilter> DiagnoseMethod { get; set; }

		public Diagnostics()
		{
			DiagnoseDocument = new List<DocumentPredicateFilter>();
			DiagnoseType = new List<TypePredicateFilter>();
			DiagnoseMethod = new List<MethodPredicateFilter>();
		}
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("ExceptionHandling")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class ExceptionHandling
	{
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodPredicateFilter> CatchPropertyGetterCalls { get; set; }

		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodPredicateFilter> CatchMethodBody { get; set; }

		public ExceptionHandling()
		{
			CatchPropertyGetterCalls = new List<MethodPredicateFilter>();
			CatchMethodBody = new List<MethodPredicateFilter>();
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
		#region Enabled
		[XmlAttribute(AttributeName = "enabled")]
		public string EnabledString
		{
			get => Enabled?.ToString();
			set => Enabled = bool.TryParse(value, out var boolean)
				? (bool?)boolean
				: null;
		}

		[XmlIgnore]
		public bool? Enabled { get; private set; }
		#endregion


		[XmlElement("Guards", IsNullable = true)]
		public bool? Guards { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodCancellationTokenFilter> MethodParameter { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodFilter> WithoutCancellationToken { get; set; }
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodFilter> RequiresCancellationToken { get; set; }

		public bool IsEnabled =>
			Enabled ?? Guards.HasValue ||
			MethodParameter.Any() ||
			WithoutCancellationToken.Any() ||
			RequiresCancellationToken.Any();

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
		[XmlArrayItem("Type", IsNullable = false)]
		public List<TypeContentFilter> AddOrReplacePartialTypeComments { get; set; }
		[XmlArrayItem("Type", IsNullable = false)]
		public List<TypeFilter> RemovePartialTypeComments { get; set; }

		[XmlArrayItem("Type", IsNullable = false)]
		public List<TypeContentFilter> AddOrReplaceNewTypeComments { get; set; }
		[XmlArrayItem("Type", IsNullable = false)]
		public List<TypeFilter> RemoveNewTypeComments { get; set; }

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
			AddOrReplacePartialTypeComments = new List<TypeContentFilter>();
			RemovePartialTypeComments = new List<TypeFilter>();
			AddOrReplaceNewTypeComments = new List<TypeContentFilter>();
			RemoveNewTypeComments = new List<TypeFilter>();
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
	[XmlRoot("PreprocessorDirectives")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class PreprocessorDirectives
	{
		[XmlArrayItem("Method", IsNullable = false)]
		public List<MethodPreprocessorDirectiveFilter> AddForMethod { get; set; }

		public PreprocessorDirectives()
		{
			AddForMethod = new List<MethodPreprocessorDirectiveFilter>();
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
	[XmlRoot("MethodPreprocessorDirectiveFilter")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class MethodPreprocessorDirectiveFilter : MethodFilter
	{
		[XmlElement("StartDirective")]
		public string StartDirective { get; set; }

		[XmlElement("EndDirective")]
		public string EndDirective { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("TypeContentFilter")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class TypeContentFilter : TypeFilter
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
		private string _filePath;
		private string _filePathEndsWith;

		[XmlAttribute(AttributeName = "filePath")]
		public string FilePath
		{
			get => _filePath;
			set => _filePath = value?.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
		}
		[XmlAttribute(AttributeName = "filePathEndsWith")]
		public string FilePathEndsWith
		{
			get => _filePathEndsWith;
			set => _filePathEndsWith = value?.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
		}
		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }

		[XmlAttribute(AttributeName = "all")]
		public bool All { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("DocumentPredicateFilter")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class DocumentPredicateFilter : DocumentFilter
	{
		[XmlAttribute(AttributeName = "result")]
		public bool Result { get; set; }
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true, Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("Parameter")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class Parameter
	{
		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }

		[XmlAttribute(AttributeName = "value")]
		public string Value { get; set; }
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
		[XmlArrayItem("Parameter", IsNullable = false)]
		public List<Parameter> Parameters { get; set; }

		public ProjectPlugin()
		{
			Parameters = new List<Parameter>();
		}
	}

	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true, Namespace = "https://github.com/maca88/AsyncGenerator")]
	[XmlRoot("SuppressDiagnosticFailure")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class SuppressDiagnosticFailure
	{
		[XmlAttribute(AttributeName = "pattern")]
		public string Pattern { get; set; }
	}
}
