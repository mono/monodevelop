#if WITH_SWF
using System.Windows.Forms;
#endif
#if WITH_GTK
using Gtk;
#endif

namespace MonoDevelop.DebuggerVisualizers
{
	public interface IDialogVisualizerService
	{
#if WITH_SWF
		DialogResult ShowDialog (CommonDialog dialog);

		DialogResult ShowDialog (Control control);

		DialogResult ShowDialog (Form form);
#endif
#if WITH_GTK
		int ShowDialog (Dialog dialog);

		int ShowDialog (Widget w);
#endif
	}
}
