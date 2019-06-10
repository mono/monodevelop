using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.FindUsages;
using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Ide;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide.TypeSystem;
using Roslyn.Utilities;

namespace MonoDevelop.Refactoring
{
	[Export (typeof (IStreamingFindUsagesPresenter)), Shared]
	class StreamingFindUsagesPresenter : IStreamingFindUsagesPresenter
	{
		public void ClearAll ()
		{
		}

		public FindUsagesContext StartSearch (string title, bool supportsReferences)
		{
			return new MonoDevelopFindUsagesContext ();
		}
	}

	sealed class MonoDevelopFindUsagesContext : FindUsagesContext
	{
		ConcurrentSet<SearchResult> antiDuplicatesSet = new ConcurrentSet<SearchResult> (new SearchResultComparer ());
		SearchProgressMonitor monitor;
		int reportedProgress = 0;
		ITimeTracker timer = null;
		Counters.FindReferencesMetadata metadata;

		public MonoDevelopFindUsagesContext ()
		{
			monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true);
			monitor.BeginTask (GettextCatalog.GetString ("Searching..."), 100);
			CancellationToken = monitor.CancellationToken;
			CancellationToken.Register (Finished);
			metadata = Counters.CreateFindReferencesMetadata ();
			timer = Counters.FindReferences.BeginTiming (metadata);
		}

		public override CancellationToken CancellationToken { get; }

		void Finished ()
		{
			if (!CancellationToken.IsCancellationRequested) {
				monitor?.ReportResults (antiDuplicatesSet);
				metadata.SetUserCancel ();
			}
			monitor?.Dispose ();
			monitor = null;

			timer?.Dispose ();
			timer = null;
		}

		public override Task ReportMessageAsync (string message)
		{
			return base.ReportMessageAsync (message);
		}

		public override Task ReportProgressAsync (int current, int maximum)
		{
			int newProgress = current * 100 / maximum;
			monitor?.Step (newProgress - reportedProgress);
			return Task.CompletedTask;
		}

		public override Task OnDefinitionFoundAsync (DefinitionItem definition)
		{
			var locations = definition.SourceSpans;
			foreach (var loc in locations) {
				var fileName = loc.Document.FilePath;
				var offset = loc.SourceSpan.Start;
				string projectedName;
				int projectedOffset;
				var workspace = loc.Document.Project.Solution.Workspace as MonoDevelopWorkspace;
				if (workspace != null && workspace.TryGetOriginalFileFromProjection (fileName, offset, out projectedName, out projectedOffset)) {
					fileName = projectedName;
					offset = projectedOffset;
				}
				var sr = new MemberReference (loc, fileName, offset, loc.SourceSpan.Length);
				sr.ReferenceUsageType = ReferenceUsageType.Declaration;
				antiDuplicatesSet.Add (sr);
			}
			return Task.CompletedTask;
		}

		public override Task OnReferenceFoundAsync (SourceReferenceItem reference)
		{
			var loc = reference.SourceSpan;
			var fileName = loc.Document.FilePath;
			var offset = loc.SourceSpan.Start;
			string projectedName;
			int projectedOffset;
			var workspace = loc.Document.Project.Solution.Workspace as MonoDevelopWorkspace;
			if (workspace != null && workspace.TryGetOriginalFileFromProjection (fileName, offset, out projectedName, out projectedOffset)) {
				fileName = projectedName;
				offset = projectedOffset;
			}
			var sr = new MemberReference (loc, fileName, offset, loc.SourceSpan.Length);
			if (antiDuplicatesSet.Add (sr)) {
				sr.ReferenceUsageType = reference.IsWrittenTo ? ReferenceUsageType.Write : ReferenceUsageType.Read;
			}
			return Task.CompletedTask;
		}

		public override Task OnCompletedAsync ()
		{
			Finished ();
			return Task.CompletedTask;
		}

		class SearchResultComparer : IEqualityComparer<SearchResult>
		{
			public bool Equals (SearchResult x, SearchResult y)
			{
				return x.FileName == y.FileName &&
							x.Offset == y.Offset &&
							x.Length == y.Length;
			}

			public int GetHashCode (SearchResult obj)
			{
				int hash = 17;
				hash = hash * 23 + obj.Offset.GetHashCode ();
				hash = hash * 23 + obj.Length.GetHashCode ();
				hash = hash * 23 + (obj.FileName ?? "").GetHashCode ();
				return hash;
			}
		}
	}
}
