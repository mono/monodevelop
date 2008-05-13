using System;
using System.Runtime.InteropServices;
using Mono.Unix;

namespace Stetic.Editor {

	[PropertyEditor ("Accel", "AccelChanged")]
	public class Accelerator : Gtk.HBox, IPropertyEditor {

		uint keyval;
		Gdk.ModifierType mask;
		bool editing;
		
		Gtk.Button clearButton;
		Gtk.Entry entry;

		public const Gdk.ModifierType AcceleratorModifierMask = ~(
			Gdk.ModifierType.Button1Mask |
			Gdk.ModifierType.Button2Mask |
			Gdk.ModifierType.Button3Mask |
			Gdk.ModifierType.Button4Mask |
			Gdk.ModifierType.Button5Mask);

		public Accelerator ()
		{
			entry = new Gtk.Entry ();
			clearButton = new Gtk.Button (new Gtk.Image (Gtk.Stock.Clear, Gtk.IconSize.Menu));
			PackStart (entry, true, true, 0);
			PackStart (clearButton, false, false, 3);
			clearButton.Clicked += delegate (object s, EventArgs args) {
				Value = null;
			};
			entry.IsEditable = false;
			entry.ButtonPressEvent += OnButtonPressEvent;
			entry.KeyPressEvent += OnKeyPressEvent;
			ShowAll ();
		}

		public void Initialize (PropertyDescriptor descriptor)
		{
			if (descriptor.PropertyType != typeof(string))
				throw new ApplicationException ("Accelerator editor does not support editing values of type " + descriptor.PropertyType);
		}
		
		public void AttachObject (object obj)
		{
			Value = null;
		}
		
		[GLib.ConnectBefore]
		void OnButtonPressEvent (object s, Gtk.ButtonPressEventArgs args)
		{
			if (editing)
				Ungrab (args.Event.Time);
			else
				Grab (args.Event.Window, args.Event.Time);
			args.RetVal = true;
		}

		GrabDialog grabWindow;

		void Ungrab (uint time)
		{
			if (!editing)
				return;
			editing = false;

			if (Value != null)
				entry.Text = (string) Value;
			else
				entry.Text = "";
				
			grabWindow.Destroy ();
		}

		void Grab (Gdk.Window window, uint time)
		{
			if (editing)
				return;
				
			grabWindow = new GrabDialog ();
			editing = true;
			entry.Text = Catalog.GetString ("Press a key...");
			grabWindow.Run ();
			this.keyval = grabWindow.Keyval;
			this.mask = grabWindow.Mask;
			Ungrab (time);
			EmitAccelChanged ();
		}

		[GLib.ConnectBefore]
		void OnKeyPressEvent (object s, Gtk.KeyPressEventArgs args)
		{
			Gdk.EventKey evt = args.Event;
			
			if (!editing || !Gtk.Accelerator.Valid (evt.KeyValue, evt.State))
				return;
			
			uint keyval;
			int effectiveGroup, level;
			Gdk.ModifierType consumedMods, mask;
			
			// We know this will succeed, since we're already here...
			Gdk.Keymap.Default.TranslateKeyboardState (evt.HardwareKeycode, evt.State, evt.Group, out keyval, out effectiveGroup, out level, out consumedMods);
			mask = evt.State & AcceleratorModifierMask & ~consumedMods;

			if (evt.Key != Gdk.Key.Escape || mask != 0) {
				this.keyval = keyval;
				this.mask = mask;
			}
			
			clearButton.Sensitive = true;

			Ungrab (evt.Time);
			EmitAccelChanged ();
			args.RetVal = true;
		}

		public object Value {
			get {
				if (keyval != 0)
					return Gtk.Accelerator.Name (keyval, mask);
				else
					return null;
			}
			set {
				string s = value as string;
				if (s == null) {
					keyval = 0;
					mask = 0;
					clearButton.Sensitive = false;
				} else {
					Gtk.Accelerator.Parse (s, out keyval, out mask);
					clearButton.Sensitive = true;
				}
				if (Value != null)
					entry.Text = (string) Value;
				else
					entry.Text = "";
				EmitAccelChanged ();
			}
		}

		public event EventHandler ValueChanged;

		void EmitAccelChanged ()
		{
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}
	}
	
	class GrabDialog: Gtk.Dialog
	{
		public uint Keyval;
		public Gdk.ModifierType Mask;
		
		public GrabDialog ()
		{
			Decorated = false;
			this.SkipPagerHint = true;
			this.SkipTaskbarHint = true;
			this.WindowPosition = Gtk.WindowPosition.CenterOnParent;
			Gtk.Frame f = new Gtk.Frame ();
			f.Shadow = Gtk.ShadowType.Out;
			this.VBox.PackStart (f, true, true, 0);
			Gtk.Label lab = new Gtk.Label (Catalog.GetString ("Press the key combination you want to assign to the accelerator..."));
			lab.Xpad = 12;
			lab.Ypad = 12;
			f.Add (lab);
			ShowAll ();
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey evt)
		{
			uint keyval;
			int effectiveGroup, level;
			Gdk.ModifierType consumedMods, mask;
			
			if (!Gtk.Accelerator.Valid (evt.KeyValue, evt.State))
				return base.OnKeyPressEvent (evt);
			
			// We know this will succeed, since we're already here...
			Gdk.Keymap.Default.TranslateKeyboardState (evt.HardwareKeycode, evt.State, evt.Group, out keyval, out effectiveGroup, out level, out consumedMods);
			mask = evt.State & Accelerator.AcceleratorModifierMask & ~consumedMods;

			if (evt.Key != Gdk.Key.Escape || mask != 0) {
				Keyval = keyval;
				Mask = mask;
				this.Respond (0);
			}
			return false;
		}
	}
}
