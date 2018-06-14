using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Core;
using MonoDevelop.Projects.Text;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// Extension methods for working with code and roslyn
	/// </summary>
	public static class CodeExtensions
	{
		/// <summary>
		/// Returns true if the given type has the given attribute applied to it
		/// </summary>
		public static bool HasAttribute (this INamedTypeSymbol type, INamedTypeSymbol attributeType)
		{
			var attrs = type.GetAttributes ();
			return attrs.Any (a => a.AttributeClass == attributeType);
		}

		/// <summary>
		/// Gets a CodeAnalysis project from a MonoDevelop project
		/// </summary>
		public static Microsoft.CodeAnalysis.Project GetCodeAnalysisProject (this MonoDevelop.Projects.Project monoDevelopProject)
		{
			return TypeSystemService.GetCodeAnalysisProject (monoDevelopProject);
		}

		/// <summary>
		/// Gets the given attribute from the given type, returns null if the type does not have the attribute applied
		/// </summary>
		public static AttributeData GetAttribute (this INamedTypeSymbol type, INamedTypeSymbol attributeType)
		{
			var attrs = type.GetAttributes ();
			return attrs.FirstOrDefault (a => a.AttributeClass == attributeType);
		}

		/// <summary>
		/// Determines if the other is derived from type
		/// </summary>
		public static bool IsDerivedFromClass (this IType type, IType other)
		{
			var derived = type.DirectBaseTypes.Any (baseType => baseType == other);
			if (derived)
				return true;

			foreach (var baseType in type.DirectBaseTypes) {
				derived = IsDerivedFromClass (baseType, other);
				if (derived)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Determines if the given type is defined in source code, ie is part of the project
		/// </summary>
		public static bool IsDefinedInSource (this ITypeDefinition type, ICompilation compilation)
		{
			if (compilation == null)
				return false;

			return type.ParentAssembly.UnresolvedAssembly.Location == compilation.MainAssembly.UnresolvedAssembly.Location;
		}

		/// <summary>
		/// Determines if the given type is defined in source code, ie is part of the project
		/// </summary>
		public static bool IsDefinedInSource (this IMember type, ICompilation compilation)
		{
			return type.ParentAssembly.UnresolvedAssembly.Location == compilation.MainAssembly.UnresolvedAssembly.Location;
		}

		/// <summary>
		/// Formats the syntax tree and saves it
		/// </summary>
		public static void FormatAndSave (this ICSharpCode.NRefactory.CSharp.SyntaxTree file, string fileName)
		{
			var result = FormatFile (file);
			SaveFile (fileName, result);
		}

		/// <summary>
		/// Returns true if the given type is a derived class of 'param name="class"' and has an attribute of type 'param name="attributeType"' applied
		/// </summary>
		public static bool IsAttributedSubclass (this INamedTypeSymbol type, INamedTypeSymbol classType, INamedTypeSymbol attributeType)
		{
			if (classType != null && attributeType != null) {
				return type.IsDerivedFromClass (classType) && type.HasAttribute (attributeType);
			}

			return false;
		}

		/// <summary>
		/// Returns true if the given type has an attribute of type 'param name="attributeType"' applied
		/// </summary>
		public static bool IsAttributed (this INamedTypeSymbol type, INamedTypeSymbol attributeType)
		{
			if (attributeType != null) {
				return type.HasAttribute (attributeType);
			}

			return false;
		}

		/// <summary>
		/// Formats the file
		/// </summary>
		static string FormatFile (ICSharpCode.NRefactory.CSharp.SyntaxTree file)
		{
			var formatting = FormattingOptionsFactory.CreateMono ();
			formatting.AutoPropertyFormatting = PropertyFormatting.ForceOneLine;
			formatting.SimplePropertyFormatting = PropertyFormatting.ForceOneLine;

			var formatter = new CSharpFormatter (formatting) {
				FormattingMode = FormattingMode.Intrusive
			};

			return formatter.Format (file.ToString ());
		}

		/// <summary>
		/// Saves the file with the given content
		/// </summary>
		static void SaveFile (string file, string content)
		{
			TextFile.WriteFile (file, content, null, null, true);
		}
	}
}