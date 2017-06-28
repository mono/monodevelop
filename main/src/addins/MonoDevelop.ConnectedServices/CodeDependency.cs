using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using Xwt.Drawing;

namespace MonoDevelop.ConnectedServices
{
	/// <summary>
	/// A service dependency that represents some code that is added to the project
	/// </summary>
	public abstract class CodeDependency : ConnectedServiceDependency
	{
		/// <summary>
		/// Sets the default timeout for obtaining the compilcation for a project
		/// </summary>
		public static int DefaultCompilationTimeout = 5000;

		/// <summary>
		/// Determines how many times we should attempt to apply the code dependency if we get a version mismatch
		/// </summary>
		const int RetryCount = 3;

		readonly Dictionary<string, INamedTypeSymbol> lookupTypes;

		IList<INamedTypeSymbol> allTypes;
		Compilation compilation;
		IList<INamedTypeSymbol> sourceTypes;

		Image icon;

		protected CodeDependency (IConnectedService service, string displayName, string [] lookupTypes) : base (service, ConnectedServiceDependency.CodeDependencyCategory, displayName)
		{
			this.lookupTypes = new Dictionary<string, INamedTypeSymbol> ();

			foreach (var type in lookupTypes) {
				this.lookupTypes [type] = null;
			}
		}

		public override Image Icon {
			get {
				if (icon == null)
					icon = ImageService.GetIcon ("md-code");
				return icon;
			}
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:MonoDevelop.ConnectedServices.CodeDependency"/>.
		/// </summary>
		public override string ToString ()
		{
			return this.GetType ().ToString ();
		}

		/// <summary>
		/// Gets the types defined in source files for the compilation
		/// </summary>
		protected IList<INamedTypeSymbol> SourceTypes {
			get {
				if (this.sourceTypes == null) {
					this.sourceTypes = this.allTypes.Where (t => t.IsDefinedInSource ()).ToList ();
				}

				return this.sourceTypes;
			}
		}

		/// <summary>
		/// Adds the dependency to the project and returns true if the dependency was added to the project
		/// </summary>
		protected sealed override async Task<bool> OnAddToProject (CancellationToken token)
		{
			int tryCount = 1;
			bool keepTrying = true;
			while (keepTrying) {
				try {
					if (tryCount > 1) {
						LoggingService.LogInfo ("Retrying to add code dependency...");
					} else {
						LoggingService.LogInfo ("Adding code dependency '{0}' to '{1}'...", this, this.Service.Project.Name);
					}

					this.compilation = await TypeSystemService.GetCompilationAsync (this.Service.Project).ConfigureAwait (false);

					if (this.compilation == null) {
						LoggingService.LogInternalError ("Could not get compilation object.", null);
						return false;
					}

					this.InitLookupTypes (token, this.lookupTypes.Keys.ToArray ());

					var result = await Runtime.RunInMainThread<bool> (
						() => this.AddCodeToProject (token)
					);

					LoggingService.LogInfo (result ? "Code dependency added." : "Code dependency was not added.");
					return result;
				} catch (SolutionVersionMismatchException) {
					tryCount++;
				}

				keepTrying = tryCount <= 3;
			}

			return false;
		}

		/// <summary>
		/// Removes the dependency from the project and returns true if the dependency was removed
		/// </summary>
		protected sealed override async Task<bool> OnRemoveFromProject (CancellationToken token)
		{
			int tryCount = 1;
			bool keepTrying = true;
			while (keepTrying) {
				try {
					if (tryCount > 1) {
						LoggingService.LogInfo ("Retrying to remove code dependency...");
					} else {
						LoggingService.LogInfo ("Removing code dependency '{0}' from '{1}'...", this, this.Service.Project.Name);
					}

					this.compilation = await TypeSystemService.GetCompilationAsync (this.Service.Project).ConfigureAwait (false);

					if (this.compilation == null) {
						LoggingService.LogInternalError ("Could not get compilation object.", null);
						return false;
					}

					this.InitLookupTypes (token, this.lookupTypes.Keys.ToArray ());

					var result = await this.RemoveCodeFromProject (token).ConfigureAwait (false);

					LoggingService.LogInfo (result ? "Code dependency removed." : "Code dependency was not removed.");
					return result;
				} catch (SolutionVersionMismatchException) {
					tryCount++;
				}

				keepTrying = tryCount <= 3;
			}

			return false;
		}

		/// <summary>
		/// Performs the task of adding the code to the project
		/// </summary>
		protected abstract Task<bool> AddCodeToProject (CancellationToken token);

		/// <summary>
		/// Performs the task of removing the code from the project. By default we do not assume that code can be removed from
		/// project correctly. Therefore the default implementation of this is to simply return true and to do nothing to 
		/// the project.
		/// </summary>
		protected virtual Task<bool> RemoveCodeFromProject (CancellationToken token)
		{
			return Task.FromResult (true);
		}

		/// <summary>
		/// Updates the given method region with the code required for this dependency.
		/// </summary>
		protected virtual void UpdateMethodWithCodeDependency (Location methodRegion)
		{
			if (methodRegion == null)
				return;

			var sourceTree = methodRegion.SourceTree;
			if (sourceTree == null)
				return;

			var proj = this.Service.Project.GetCodeAnalysisProject ();
			if (proj == null) {
				// this can happen if the TypeSystemService doesn't have the project in it (yet?)
				LoggingService.LogWarning ("Could not get CodeAnalysisProject for the given project, cannot modify code.");
				return;
			}

			var docID = proj.GetDocumentId (sourceTree);
			var root = sourceTree.GetRoot ();

			var methodNode = root.FindNode (methodRegion.SourceSpan) as MethodDeclarationSyntax;
			if (methodNode == null)
				return;

			var newMethodNode = this.RemoveExistingCodeDependencyFromMethod (methodNode);
			var newMethodStatements = newMethodNode.Body.Statements;

			var codeStatements = this.CreateCodeDependencyStatements ();
			for (int i = 0; i < codeStatements.Count; i++) {
				newMethodStatements = newMethodStatements.Insert (i, codeStatements [i]);
			}

			newMethodNode = newMethodNode.WithBody (newMethodNode.Body.WithStatements (newMethodStatements));

			if (newMethodNode != methodNode) {
				var newRoot = root.ReplaceNode<SyntaxNode> (methodNode, newMethodNode);

				newRoot = Formatter.Format (newRoot, proj.Solution.Workspace);

				var newSolution = proj.Solution.WithDocumentSyntaxRoot (docID, newRoot);

				if (!proj.Solution.Workspace.TryApplyChanges (newSolution)) {
					LoggingService.LogWarning ("Failed to add code dependency changes to the workspace.");

					// lets check the version (which is one reason why TryApplyChanges will return false
					if (proj.Solution.Workspace.CurrentSolution.Version != newSolution.Version) {
						LoggingService.LogWarning ("Solution version is different.");
						throw new SolutionVersionMismatchException ();
					}
				}
			}
		}

		/// <summary>
		/// Adds a method created by newMethod to the param name="classRegion", the newMethod should contain the required code dependency
		/// </summary>
		protected void AddMethodWithCodeDependencyToClass (Location classRegion, Func<MethodDeclarationSyntax> newMethod)
		{
			if (classRegion == null)
				return;

			var sourceTree = classRegion.SourceTree;
			if (sourceTree == null)
				return;

			var proj = this.Service.Project.GetCodeAnalysisProject ();
			var docID = proj.GetDocumentId (sourceTree);
			var root = sourceTree.GetRoot ();

			var classNode = root.FindNode (classRegion.SourceSpan) as ClassDeclarationSyntax;
			if (classNode == null)
				return;

			var newClassNode = classNode;

			newClassNode = newClassNode.WithMembers (newClassNode.Members.Add (newMethod ()));

			if (newClassNode != classNode) {
				var newRoot = root.ReplaceNode<SyntaxNode> (classNode, newClassNode);

				newRoot = Formatter.Format (newRoot, proj.Solution.Workspace);

				var newSolution = proj.Solution.WithDocumentSyntaxRoot (docID, newRoot);
				if (!proj.Solution.Workspace.TryApplyChanges (newSolution)) {
					LoggingService.LogWarning ("Failed to add code dependency changes to the workspace.");

					// lets check the version (which is one reason why TryApplyChanges will return false
					if (proj.Solution.Workspace.CurrentSolution.Version != newSolution.Version) {
						LoggingService.LogWarning ("Solution version is different.");
						throw new SolutionVersionMismatchException ();
					}
				}
			}
		}

		/// <summary>
		/// Create the code statements that are required for this code dependency
		/// </summary>
		protected virtual SyntaxList<StatementSyntax> CreateCodeDependencyStatements ()
		{
			return new SyntaxList<StatementSyntax> ();
		}

		/// <summary>
		/// Removes any existing code that was added previously for this dependency
		/// </summary>
		protected virtual MethodDeclarationSyntax RemoveExistingCodeDependencyFromMethod (MethodDeclarationSyntax method)
		{
			var statements = method.Body.Statements;
			var newStatements = statements;
			foreach (var statement in statements) {
				if (this.IsCodeDependencyStatement (statement)) {
					newStatements = newStatements.Remove (statement);
				}
			}

			return method.WithBody (method.Body.WithStatements (newStatements));
		}

		/// <summary>
		/// Returns true if the given statement is (or is part of) the code dependency.
		/// Override this to be able to update added code.
		/// </summary>
		protected virtual bool IsCodeDependencyStatement (StatementSyntax statement)
		{
			return false;
		}

		/// <summary>
		/// Searches the type lookup tabvle for the given type
		/// </summary>
		protected INamedTypeSymbol GetLookupType (string typeName)
		{
			if (this.lookupTypes.ContainsKey (typeName)) {
				return this.lookupTypes [typeName];
			}

			return null;
		}

		/// <summary>
		/// Returns true if the given type is a derived class of 'param name="class"' and has an attribute of type 'param name="attributeType"' applied
		/// </summary>
		protected bool IsAttributedSubclass (INamedTypeSymbol type, string classType, string attributeType)
		{
			return type.IsAttributedSubclass (this.GetLookupType (classType), this.GetLookupType (attributeType));
		}

		/// <summary>
		/// Returns true if the given type has an attribute of type 'param name="attributeType"' applied
		/// </summary>
		protected bool IsAttributed (INamedTypeSymbol type, string attributeType)
		{
			return type.IsAttributed (this.GetLookupType (attributeType));
		}

		/// <summary>
		/// Returns the list of members for a type that are defined in source.
		/// </summary>
		protected IList<ISymbol> GetMembersDefinedInSource (INamedTypeSymbol type)
		{
			return type.GetMembers ().Where (m => m.IsDefinedInSource ()).ToList ();
		}

		/// <summary>
		/// Initializes the type lookup table from the compilation.
		/// </summary>
		void InitLookupTypes (CancellationToken cancel, string [] types)
		{
			this.allTypes = this.compilation.GetAllTypesInMainAssembly (cancel).ToList ();
			this.lookupTypes.Clear ();

			foreach (var type in types) {
				this.lookupTypes [type] = this.compilation.GetTypeByMetadataName (type);
			}
		}

		/// <summary>
		/// Thrown when we should attempt to generate the code dependency again
		/// </summary>
		class SolutionVersionMismatchException : Exception
		{
			public SolutionVersionMismatchException () : base ("The Solution versions were different.")
			{
			}
		}
	}
}