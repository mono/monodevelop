using System;
using System.Collections.Generic;
using System.ComponentModel;
using Gdk;
using Gtk;
using Microsoft.VisualStudio.Text.Editor;
using MonoDevelop.DesignerSupport.Toolbox;

namespace MonoDevelop.TextEditor
{
	partial class TextViewContent<TView, TImports> :
		IToolboxConsumer
	{
		TargetEntry [] IToolboxConsumer.DragTargets { get; } = new TargetEntry [0];

		ToolboxItemFilterAttribute [] IToolboxConsumer.ToolboxFilterAttributes { get; } = new ToolboxItemFilterAttribute [0];

		string IToolboxConsumer.DefaultItemDomain => "Text";

		void IToolboxConsumer.ConsumeItem (ItemToolboxNode item)
		{
			if (item is ITextToolboxNode tn) {
				tn.InsertAtCaret (this.Document);
#if !WINDOWS
				((ITextView3)TextView).Focus ();
#endif
			}
		}

		bool IToolboxConsumer.CustomFilterSupports (ItemToolboxNode item)
		{
			return false;
		}

		void IToolboxConsumer.DragItem (ItemToolboxNode item, Widget source, DragContext ctx)
		{
		}
	}
}
