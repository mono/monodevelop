using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Debugger.VSTextView.QuickInfo;

namespace MonoDevelop.CSharp.Debugger
{
	[Export]
	[Export (typeof (IDebugInfoProvider))]
	public class RoslynDebugInfoProvider : IDebugInfoProvider
	{
		public async Task<DataTipInfo> GetDebugInfoAsync (SnapshotPoint snapshotPoint, CancellationToken cancellationToken)
		{
			var document = snapshotPoint.Snapshot.AsText ().GetOpenDocumentInCurrentContextWithChanges ();
			if (document != null) {
				var debugInfoService = document.Project.LanguageServices.GetService<ILanguageDebugInfoService> ();
				if (debugInfoService != null) {
					var debugInfo = await debugInfoService.GetDataTipInfoAsync (document, snapshotPoint.Position, cancellationToken).ConfigureAwait (false);
					if (!debugInfo.IsDefault) {
						var span = debugInfo.Span;
						return new DataTipInfo (
							snapshotPoint.Snapshot.CreateTrackingSpan (span.Start, span.Length, SpanTrackingMode.EdgeExclusive),
							snapshotPoint.Snapshot.GetText (span.Start, span.Length));
					}
				}
			}

			return default(DataTipInfo);
		}
	}
}
