using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.TextEditor;
using Xwt;

namespace System.Windows.Input
{
	class Mouse
	{
		internal static Point GetPosition (Gtk.Widget widget)
		{
			widget.GetPointer (out int x, out int y);
			return new Point (x, y);
		}
	}
}
