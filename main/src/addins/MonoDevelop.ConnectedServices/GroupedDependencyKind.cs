using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.ConnectedServices
{

	/// <summary>
	/// Defines how a grouped dependency should handle each of the component dependencies
	/// </summary>
	public enum GroupedDependencyKind
	{
		/// <summary>
		/// All dependencies should be added.
		/// </summary>
		All,

		/// <summary>
		/// Any of the dependencies are added, as soon as one of the dependecies reports being added then the remaining
		/// dependencies are skipped.
		/// </summary>
		Any
	}
}