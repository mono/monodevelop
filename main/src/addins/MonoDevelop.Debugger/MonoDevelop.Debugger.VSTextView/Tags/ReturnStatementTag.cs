using Microsoft.VisualStudio.Text.Tagging;

namespace MonoDevelop.Debugger
{
	internal class ReturnStatementTag : TextMarkerTag
	{
		internal const string TagId = "returnstatement";

		public static readonly ReturnStatementTag Instance = new ReturnStatementTag ();

		private ReturnStatementTag ()
			: base (TagId)
		{
		}
	}
}