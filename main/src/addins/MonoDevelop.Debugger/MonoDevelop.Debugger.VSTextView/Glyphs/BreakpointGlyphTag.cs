using System.Windows.Input;
using Microsoft.VisualStudio.Text.Editor;
using Mono.Debugging.Client;
using MonoDevelop.Debugger;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Debugger
{
	class BreakpointGlyphTag : BaseBreakpointGlyphTag
	{
		public BreakpointGlyphTag (BreakEvent breakpoint) : base (breakpoint)
		{
		}
	}
	class BreakpointDisabledGlyphTag : BaseBreakpointGlyphTag
	{
		public BreakpointDisabledGlyphTag (BreakEvent breakpoint) : base (breakpoint)
		{
		}
	}
	class BreakpointInvalidGlyphTag : BaseBreakpointGlyphTag
	{
		public BreakpointInvalidGlyphTag (BreakEvent breakpoint) : base (breakpoint)
		{
		}
	}
	class TracepointGlyphTag : BaseBreakpointGlyphTag
	{
		public TracepointGlyphTag (BreakEvent breakpoint) : base (breakpoint)
		{
		}
	}
	class TracepointDisabledGlyphTag : BaseBreakpointGlyphTag
	{
		public TracepointDisabledGlyphTag (BreakEvent breakpoint) : base (breakpoint)
		{
		}
	}
	class TracepointInvalidGlyphTag : BaseBreakpointGlyphTag
	{
		public TracepointInvalidGlyphTag (BreakEvent breakpoint) : base (breakpoint)
		{
		}
	}

	abstract class BaseBreakpointGlyphTag : IGlyphTag, IInteractiveGlyph
	{
		private readonly BreakEvent breakpoint;

		public BaseBreakpointGlyphTag (BreakEvent breakpoint)
		{
			this.breakpoint = breakpoint;
		}

		public object HoverCursor {
			get {
				return null;
			}
		}

		public IActiveGlyphDropHandler DropHandler {
			get {
				return null;
			}
		}

		public bool IsEnabled {
			get {
				return true;
			}
		}

		public bool ExecuteCommand (GlyphCommandType markerCommand)
		{
			if (markerCommand == GlyphCommandType.SingleClick) {
				DebuggingService.Breakpoints.Remove (breakpoint);
				return true;
			}
			return false;
		}

		public string TooltipText {
			get {
				return null;
			}
		}
	}
}
