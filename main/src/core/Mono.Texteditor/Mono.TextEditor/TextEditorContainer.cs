using System;
using Gtk;
using System.Collections.Generic;
namespace Mono.TextEditor
{
	public class TextEditorContainer : Container
	{
		Widget textEditor = new TextEditor ();
		TopLevelChild textEditorChild;
		
		public Widget TextEditor {
			get { return this.textEditor; }
		}
		
		public override ContainerChild this [Widget w] {
			get {
				if (w == textEditor)
					return textEditorChild;
				foreach (TopLevelChild info in topLevels) {
					if (info.Child == w) 
						return info;
				}
				return null;
			}
		}
		
		class TopLevelChild : Container.ContainerChild
		{
			public int X;
			public int Y;
				
			public TopLevelChild (Container parent, Widget child) : base (parent, child)
			{
			}
		}
		
		public TextEditorContainer ()
		{
			WidgetFlags |= WidgetFlags.NoWindow;
			
			textEditor.Parent = this;
			textEditor.Show ();
			textEditorChild = new TopLevelChild (this, textEditor);
		}
		
		public override GLib.GType ChildType ()
		{
			return Gtk.Widget.GType;
		}
		
		List<TopLevelChild> topLevels = new List<TopLevelChild> ();
		
		public void AddTopLevelWidget (Gtk.Widget w, int x, int y)
		{
			w.Parent = this;
			TopLevelChild info = new TopLevelChild (this, w);
			info.X = x;
			info.Y = y;
			topLevels.Add (info);
		}
		
		
		public void MoveTopLevelWidget (Gtk.Widget w, int x, int y)
		{
			foreach (TopLevelChild info in topLevels) {
				if (info.Child == w) {
					info.X = x;
					info.Y = y;
					QueueResize ();
					break;
				}
			}
		}
		
		protected override void OnAdded (Widget widget)
		{
			AddTopLevelWidget (widget, 0, 0);
		}
		
		protected override void OnRemoved (Widget widget)
		{
			foreach (TopLevelChild info in topLevels) {
				if (info.Child == widget) {
					widget.Unparent ();
					topLevels.Remove (info);
					break;
				}
			}
		}
		
		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			
			textEditor.SizeRequest ();
			// Ignore the size of top levels. They are supposed to fit the available space
			foreach (TopLevelChild tchild in topLevels)
				tchild.Child.SizeRequest ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			Gtk.Requisition req = textEditor.SizeRequest ();
			textEditor.SizeAllocate (new Gdk.Rectangle (allocation.X, allocation.Y, allocation.Width, allocation.Height));
			foreach (TopLevelChild child in topLevels) {
				req = child.Child.SizeRequest ();
				Console.WriteLine (new Gdk.Rectangle (allocation.X + child.X + (int)hAdjustement.Value, allocation.Y + child.Y + (int)vAdjustement.Value, req.Width, req.Height));
				child.Child.SizeAllocate (new Gdk.Rectangle (allocation.X + child.X + (int)hAdjustement.Value, allocation.Y + child.Y + (int)vAdjustement.Value, req.Width, req.Height));
			}
		}
		
		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			if (textEditor != null)
				callback (textEditor);
			foreach (TopLevelChild child in topLevels) {
				callback (child.Child);
			}
		}
		
		Adjustment hAdjustement = new Adjustment (0, 0, 0, 0, 0, 0);
		Adjustment vAdjustement = new Adjustment (0, 0, 0, 0, 0, 0);
		protected override void OnSetScrollAdjustments (Adjustment hAdjustement, Adjustment vAdjustement)
		{
			this.hAdjustement = hAdjustement ?? new Adjustment (0, 0, 0, 0, 0, 0);
			this.vAdjustement = vAdjustement ?? new Adjustment (0, 0, 0, 0, 0, 0);
			textEditor.SetScrollAdjustments (hAdjustement, vAdjustement);
			
			this.hAdjustement.Changed += delegate {
				QueueResize ();
			};
			this.vAdjustement.Changed += delegate {
				QueueResize ();
			};
		}
	}
}

