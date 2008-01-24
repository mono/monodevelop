
using System;
using System.Xml;
using System.Collections;

namespace Stetic
{
	class ContainerUndoRedoManager: UndoRedoManager
	{
		protected override object GetDiff (ObjectWrapper w)
		{
			// Only track changes in widgets.
			Wrapper.Widget widget = w as Wrapper.Widget;
			if (widget != null) return w.GetUndoDiff ();
			else return null;
		}
	}
}
