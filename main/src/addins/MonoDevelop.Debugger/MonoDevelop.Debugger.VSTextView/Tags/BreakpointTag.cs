using Microsoft.VisualStudio.Text.Tagging;

namespace MonoDevelop.Debugger
{
	class BreakpointTag : TextMarkerTag
	{
		internal const string TagId = "breakpoint";

		public static readonly BreakpointTag Instance = new BreakpointTag ();

		private BreakpointTag ()
			: base (TagId)
		{
		}
	}

	class BreakpointDisabledTag : TextMarkerTag
	{
		internal const string TagId = "breakpoint-disabled";

		public static readonly BreakpointDisabledTag Instance = new BreakpointDisabledTag ();

		private BreakpointDisabledTag ()
			: base (TagId)
		{
		}
	}

	class BreakpointInvalidTag : TextMarkerTag
	{
		internal const string TagId = "breakpoint-invalid";

		public static readonly BreakpointInvalidTag Instance = new BreakpointInvalidTag ();

		private BreakpointInvalidTag ()
			: base (TagId)
		{
		}
	}
}