using System;
using System.Collections;
using System.CodeDom;

namespace Stetic.Wrapper {

	public class ScrolledWindow : Container {

		Gtk.PolicyType hpolicy = Gtk.PolicyType.Automatic;
		Gtk.PolicyType vpolicy = Gtk.PolicyType.Automatic;
		
		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			if (!initialized) {
				if (scrolled.Child == null && AllowPlaceholders)
					AddPlaceholder ();
				HscrollbarPolicy = VscrollbarPolicy = Gtk.PolicyType.Automatic;
				scrolled.ShadowType = Gtk.ShadowType.In;
			}
			scrolled.SetPolicy (Gtk.PolicyType.Always, Gtk.PolicyType.Always);
		}

		public Gtk.ScrolledWindow scrolled {
			get {
				return (Gtk.ScrolledWindow)Wrapped;
			}
		}
		
		public Gtk.PolicyType HscrollbarPolicy {
			get { return hpolicy; }
			set {
				hpolicy = value;
				EmitNotify ("HscrollbarPolicy");
			}
		}

		public Gtk.PolicyType VscrollbarPolicy {
			get { return vpolicy; }
			set {
				vpolicy = value;
				EmitNotify ("VscrollbarPolicy");
			}
		}

		public override IEnumerable RealChildren {
			get {
				if (scrolled.Child is Gtk.Viewport)
					return ((Gtk.Viewport)scrolled.Child).Children;
				else
					return base.RealChildren;
			}
		}

		internal void AddWithViewport (Gtk.Widget child)
		{
			Gtk.Viewport viewport = new Gtk.Viewport (scrolled.Hadjustment, scrolled.Vadjustment);
			ObjectWrapper.Create (proj, viewport);
			viewport.ShadowType = Gtk.ShadowType.None;
			viewport.Add (child);
			viewport.Show ();
			scrolled.Add (viewport);
		}

		protected override void ReplaceChild (Gtk.Widget oldChild, Gtk.Widget newChild)
		{
			Widget ww = Widget.Lookup (oldChild);
			if (ww != null && ww.ShowScrollbars && ParentWrapper != null) {
				// The viewport is bound to the child widget. Remove it together with the child
				ParentWrapper.ReplaceChild (Wrapped, newChild, false);
				return;
			}
			
			if (scrolled.Child is Gtk.Viewport) {
				Gtk.Viewport vp = (Gtk.Viewport)scrolled.Child;
				vp.Remove (oldChild);
				scrolled.Remove (vp);
				vp.Destroy ();
			}
			else
				scrolled.Remove (scrolled.Child);
			
			if (newChild.SetScrollAdjustments (null, null))
				scrolled.Add (newChild);
			else
				AddWithViewport (newChild);
			
			NotifyChildAdded (scrolled.Child);
		}

		public override Placeholder AddPlaceholder ()
		{
			Placeholder ph = CreatePlaceholder ();
			AddWithViewport (ph);
			return ph;
		}
		
		protected override void GenerateChildBuildCode (GeneratorContext ctx, CodeExpression parentVar, Widget wrapper)
		{
			Gtk.Viewport vp = wrapper.Wrapped as Gtk.Viewport;
			if (vp == null || (vp.Child != null && !(vp.Child is Placeholder)))
				base.GenerateChildBuildCode (ctx, parentVar, wrapper);
		}

		public override void Delete (Stetic.Placeholder ph)
		{
			using (UndoManager.AtomicChange) {
				Delete ();
			}
		}
	}
}
