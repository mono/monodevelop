using Microsoft.VisualStudio.Text.Tagging;

namespace MonoDevelop.Debugger
{
	internal class CurrentStatementTag : TextMarkerTag
	{
		internal const string TagId = "currentstatement";

		public static readonly CurrentStatementTag Instance = new CurrentStatementTag ();

		private CurrentStatementTag ()
			: base (TagId)
		{
		}
	}
}