using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using System.Threading;

namespace MonoDevelop.Debugger.VSTextView.QuickInfo
{
	public interface IDebugInfoProvider
	{
		Task<DataTipInfo> GetDebugInfoAsync (SnapshotPoint snapshotPoint, CancellationToken cancellationToken);
	}

	public struct DataTipInfo
	{
		public readonly ITrackingSpan Span;
		public readonly string Text;

		public DataTipInfo (ITrackingSpan span, string text)
		{
			this.Span = span;
			this.Text = text;
		}

		public bool IsDefault {
			get { return Span == null && Text == null; }
		}

		public override string ToString () => $"{Span} {Text}";
	}
}
