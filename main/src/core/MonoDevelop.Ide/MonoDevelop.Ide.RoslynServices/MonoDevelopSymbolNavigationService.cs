using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide.RoslynServices
{
	[ExportWorkspaceServiceFactory (typeof (ISymbolNavigationService), ServiceLayer.Host), Shared]
	internal class VisualStudioSymbolNavigationServiceFactory : IWorkspaceServiceFactory
	{
		private readonly ISymbolNavigationService _singleton;

		[ImportingConstructor]
		private VisualStudioSymbolNavigationServiceFactory ()
		{
			_singleton = new MonoDevelopSymbolNavigationService ();
		}

		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{
			return _singleton;
		}
	}

	class MonoDevelopSymbolNavigationService : ISymbolNavigationService
	{
		public bool TryNavigateToSymbol (ISymbol symbol, Project project, OptionSet options = null, CancellationToken cancellationToken = default)
		{
			IdeApp.ProjectOperations.JumpToDeclaration (symbol, ((MonoDevelopWorkspace)project.Solution.Workspace).GetMonoProject (project));
			return true;
		}

		public bool TrySymbolNavigationNotify (ISymbol symbol, Project project, CancellationToken cancellationToken)
		{
			IdeApp.ProjectOperations.JumpToDeclaration (symbol, ((MonoDevelopWorkspace)project.Solution.Workspace).GetMonoProject (project));
			return true;
		}

		public bool WouldNavigateToSymbol (DefinitionItem definitionItem, Solution solution, CancellationToken cancellationToken, out string filePath, out int lineNumber, out int charOffset)
		{
			filePath = null;
			lineNumber = -1;
			charOffset = -1;
			return true;
		}
	}
}
