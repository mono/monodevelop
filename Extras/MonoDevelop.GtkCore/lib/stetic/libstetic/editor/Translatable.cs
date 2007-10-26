using System;

namespace Stetic.Editor {

	public abstract class Translatable : Gtk.VBox, IPropertyEditor {

		PropertyDescriptor prop;
		object obj;

		Gtk.Box mainHBox, contextBox, commentBox;
		Gtk.Button button;
		Gdk.Pixbuf globe, globe_not;
		Gtk.Image image;
		Gtk.Menu menu;
		Gtk.CheckMenuItem markItem;
		Gtk.MenuItem addContextItem, remContextItem, addCommentItem, remCommentItem;
		Gtk.Entry contextEntry;
		TextBox commentText;
		bool initializing;

		public virtual void Initialize (PropertyDescriptor prop)
		{
			CheckType (prop);
			
			this.prop = prop;

			mainHBox = new Gtk.HBox (false, 6);
			PackStart (mainHBox, false, false, 0);

			if (!prop.Translatable)
				return;

			button = new Gtk.Button ();
			globe = Gdk.Pixbuf.LoadFromResource ("globe.png");
			globe_not = Gdk.Pixbuf.LoadFromResource ("globe-not.png");
			image = new Gtk.Image (globe);
			button.Add (image);
			button.ButtonPressEvent += ButtonPressed;
			mainHBox.PackEnd (button, false, false, 0);
			mainHBox.ShowAll ();
			
			menu = new Gtk.Menu ();

			markItem = new Gtk.CheckMenuItem ("Mark for Translation");
			markItem.Toggled += ToggleMark;
			markItem.Show ();
			menu.Add (markItem);
			
			addContextItem = new Gtk.MenuItem ("Add Translation Context Hint");
			addContextItem.Activated += AddContext;
			menu.Add (addContextItem);
			remContextItem = new Gtk.MenuItem ("Remove Translation Context Hint");
			remContextItem.Activated += RemoveContext;
			menu.Add (remContextItem);
			
			addCommentItem = new Gtk.MenuItem ("Add Comment for Translators");
			addCommentItem.Activated += AddComment;
			menu.Add (addCommentItem);
			remCommentItem = new Gtk.MenuItem ("Remove Comment for Translators");
			remCommentItem.Activated += RemoveComment;
			menu.Add (remCommentItem);
			
			contextBox = new Gtk.HBox (false, 6);
			Gtk.Label contextLabel = new Gtk.Label ("Translation context");
			contextLabel.Xalign = 0.0f;
			contextBox.PackStart (contextLabel, false, false, 0);
			contextEntry = new Gtk.Entry ();
			contextEntry.WidthChars = 8;
			contextBox.PackStart (contextEntry, true, true, 0);
			contextBox.ShowAll ();
			contextEntry.Changed += ContextChanged;

			commentBox = new Gtk.VBox (false, 3);
			Gtk.Label commentLabel = new Gtk.Label ("Comment for Translators:");
			commentLabel.Xalign = 0.0f;
			commentBox.PackStart (commentLabel, false, false, 0);
			commentText = new TextBox (3);
			commentBox.PackStart (commentText, false, false, 0);
			commentBox.ShowAll ();
			commentText.Changed += CommentChanged;
		}
		
		protected virtual void CheckType (PropertyDescriptor prop)
		{
		}
		
		public virtual void AttachObject (object ob)
		{
			this.obj = ob;
			
			if (!prop.Translatable)
				return;

			initializing = true;

			if (contextBox.Parent != null)
				Remove (contextBox);
			if (commentBox.Parent != null)
				Remove (commentBox);
			
			markItem.Active = prop.IsTranslated (obj);
			image.Pixbuf = markItem.Active ? globe : globe_not;
			
			if (prop.IsTranslated (obj)) {
				if (prop.TranslationContext (obj) != null) {
					remContextItem.Show ();
					PackStart (contextBox, false, false, 0);
					contextEntry.Text = prop.TranslationContext (obj);
				} else
					addContextItem.Show ();
			} else {
				addContextItem.Show ();
				addContextItem.Sensitive = false;
			}

			if (prop.IsTranslated (obj)) {
				if (prop.TranslationComment (obj) != null) {
					remCommentItem.Show ();
					PackEnd (commentBox, false, false, 0);
					commentText.Text = prop.TranslationComment (obj);
				} else
					addCommentItem.Show ();
			} else {
				addCommentItem.Show ();
				addCommentItem.Sensitive = false;
			}
			
			initializing = false;
		}

		
		public abstract object Value { get; set; }
		
		public event EventHandler ValueChanged;
		
		protected virtual void OnValueChanged ()
		{
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}
		
		protected override void OnAdded (Gtk.Widget child)
		{
			mainHBox.PackStart (child, true, true, 0);
		}

		void MenuPosition (Gtk.Menu menu, out int x, out int y, out bool push_in)
		{
			button.GdkWindow.GetOrigin (out x, out y);
			Gdk.Rectangle alloc = button.Allocation;
			x += alloc.X;
			y += alloc.Y + alloc.Height;
			push_in = true;
		}

		[GLib.ConnectBefore]
		void ButtonPressed (object o, Gtk.ButtonPressEventArgs args)
		{
			menu.Popup (null, null, MenuPosition, 1, args.Event.Time);
			args.RetVal = true;
		}

		void ToggleMark (object o, EventArgs args)
		{
			if (initializing) return;
			if (!markItem.Active) {
				// Make sure we're showing the "Add" menu items
				// rather than the "Remove" ones
				if (prop.TranslationContext (obj) != null)
					RemoveContext (remContextItem, EventArgs.Empty);
				if (prop.TranslationComment (obj) != null)
					RemoveComment (remCommentItem, EventArgs.Empty);
			}

			prop.SetTranslated (obj, markItem.Active);
			image.Pixbuf = markItem.Active ? globe : globe_not;
			addContextItem.Sensitive = markItem.Active;
			addCommentItem.Sensitive = markItem.Active;
		}

		void AddContext (object o, EventArgs args)
		{
			prop.SetTranslationContext (obj, contextEntry.Text);
			PackStart (contextBox, false, false, 0);

			addContextItem.Hide ();
			remContextItem.Show ();
		}

		void RemoveContext (object o, EventArgs args)
		{
			prop.SetTranslationContext (obj, null);
			Remove (contextBox);

			remContextItem.Hide ();
			addContextItem.Show ();
		}

		void ContextChanged (object o, EventArgs args)
		{
			if (initializing) return;
			prop.SetTranslationContext (obj, contextEntry.Text);
		}

		void AddComment (object o, EventArgs args)
		{
			prop.SetTranslationComment (obj, commentText.Text);
			PackEnd (commentBox, false, false, 0);

			addCommentItem.Hide ();
			remCommentItem.Show ();
		}

		void RemoveComment (object o, EventArgs args)
		{
			prop.SetTranslationComment (obj, null);
			Remove (commentBox);

			remCommentItem.Hide ();
			addCommentItem.Show ();
		}

		void CommentChanged (object o, EventArgs args)
		{
			if (initializing) return;
			prop.SetTranslationComment (obj, commentText.Text);
		}
	}
}
