using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
		/// Saves the file with the given content
		/// </summary>
		static void SaveFile (string file, string content)
		{
			TextFile.WriteFile (file, content, null, null, true);
		}
	}
}
