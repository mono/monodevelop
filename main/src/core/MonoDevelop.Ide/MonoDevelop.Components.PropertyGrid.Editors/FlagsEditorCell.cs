//
// FlagsEditorCell.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Gtk;
using System;
using System.Collections;
using System.ComponentModel;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Ide;

namespace MonoDevelop.Components.PropertyGrid.PropertyEditors {

	public class FlagsEditorCell: PropertyEditorCell
	{
		internal static int MaxCheckCount = 6;
		internal static int CheckSpacing = 3;
		static int indicatorSize;
		static int indicatorSpacing;
		static Gtk.Style style;

		static FlagsEditorCell ()
		{
			// reinit style
			MonoDevelop.Ide.Gui.Styles.Changed += (sender, e) => style = null;
		}

		// we can't override Initialize () or use the default constructor for this,
		// because a valid Gdk.Window is required for full Gtk.Style initialization
		static void InitializeStyle (Gtk.Widget container)
		{
			if (style == null && container.GdkWindow != null) {
				Gtk.CheckButton cb = new BooleanEditor (); // use the BooleanEditor style for the checks
				cb.GdkWindow = container.GdkWindow;
				cb.Parent = container;
				cb.Realize ();
				style = cb.Style;
				style.Attach (container.GdkWindow);
				indicatorSize = (int)cb.StyleGetProperty ("indicator-size");
				indicatorSpacing = (int)cb.StyleGetProperty ("indicator-spacing");
				style.Detach ();
				cb.Dispose ();
			}
		}

		protected override string GetValueText ()
		{
			if (Value == null)
				return "";

			ulong value = Convert.ToUInt64 (Value);
			Array values = System.Enum.GetValues (base.Property.PropertyType);
			string txt = "";
			foreach (object val in values) {
				ulong uintVal = Convert.ToUInt64 (val);
				if (uintVal == 0 && value == 0)
					return val.ToString (); // zero flag defined and no flags set
				if ((value & uintVal) != 0) {
					if (txt.Length > 0) txt += ", ";
					txt += val.ToString ();
				}
			}
			return txt;
		}

		public override void Render (Gdk.Drawable window, Cairo.Context ctx, Gdk.Rectangle bounds, Gtk.StateType state)
		{
			var values = Enum.GetValues (Property.PropertyType);
			if (values.Length < MaxCheckCount) {
				if (style == null)
					InitializeStyle (Container);

				var container = (Widget)Container;
				using (var layout = new Pango.Layout (container.PangoContext)) {
					layout.Width = -1;
					layout.FontDescription = IdeServices.FontService.SansFont.CopyModified (Ide.Gui.Styles.FontScale11);

					ulong value = Convert.ToUInt64 (Value);
					int dy = 2;
					foreach (var val in values) {
						ulong uintVal = Convert.ToUInt64 (val);
						Gtk.ShadowType sh = (value & uintVal) != 0 ? Gtk.ShadowType.In : Gtk.ShadowType.Out;
						if (value == 0 && uintVal == 0)
							sh = Gtk.ShadowType.In;
						int s = indicatorSize - 1;
						Gtk.Style.PaintCheck (style, window, state, sh, bounds, Container, "checkbutton", bounds.X + indicatorSpacing - 1, bounds.Y + dy, s, s);

						layout.SetText (val.ToString ());
						int tw, th;
						layout.GetPixelSize (out tw, out th);
						ctx.Save ();
						ctx.SetSourceColor (container.Style.Text (state).ToCairoColor ());
						ctx.MoveTo (bounds.X + indicatorSize + indicatorSpacing, dy + bounds.Y + ((indicatorSize - th) / 2));
						Pango.CairoHelper.ShowLayout (ctx, layout);
						ctx.Restore ();

						dy += indicatorSize + CheckSpacing;
					}
				}
			} else {
				base.Render (window, ctx, bounds, state);
				return;
			}
		}
		
		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new FlagsEditor ();
		}

		public override void GetSize (int availableWidth, out int width, out int height)
		{
			base.GetSize (availableWidth, out width, out height);

			var values = Enum.GetValues (Property.PropertyType);
			if (values.Length < MaxCheckCount) {
				if (style == null)
					InitializeStyle (Container);
				height = 4 + (indicatorSize * values.Length) + (CheckSpacing * (values.Length - 1));
			}
		}
	}
	
	public class FlagsEditor : Gtk.HBox, IPropertyEditor
	{
		Hashtable flags;
		Gtk.Entry flagsLabel;
		string property;
		Type propType;
		Array values;

		public FlagsEditor ()
		{
		}
		
		public void Initialize (EditSession session)
		{
			PropertyDescriptor prop = session.Property;
			
			if (!prop.PropertyType.IsEnum)
				throw new ApplicationException ("Flags editor does not support editing values of type " + prop.PropertyType);
			
			Spacing = FlagsEditorCell.CheckSpacing;
			propType = prop.PropertyType;
			
			property = prop.Description;
			if (property == null || property.Length == 0)
				property = prop.Name;

			// For small enums, the editor is a list of checkboxes inside a frame
			// For large enums (>5), use a selector dialog.

			values = System.Enum.GetValues (prop.PropertyType);
			
			if (values.Length < FlagsEditorCell.MaxCheckCount) 
			{
				Gtk.VBox vbox = new Gtk.VBox (true, FlagsEditorCell.CheckSpacing);

				flags = new Hashtable ();

				foreach (object value in values) {
					ulong uintVal = Convert.ToUInt64 (value);
					Gtk.CheckButton check = new BooleanEditor ();
					if (uintVal == 0)
						check.Active = true; // default for None is always enabled
					check.Label = value.ToString ();
					check.TooltipText = value.ToString ();
					flags[check] = uintVal;
					flags[uintVal] = check;
					
					check.Toggled += FlagToggled;
					vbox.PackStart (check, false, false, 3);
				}

				Gtk.Frame frame = new Gtk.Frame ();
				frame.Add (vbox);
				frame.ShowAll ();
				PackStart (frame, true, true, 0);
			} 
			else 
			{
				flagsLabel = new Gtk.Entry ();
				flagsLabel.IsEditable = false;
				flagsLabel.HasFrame = false;
				flagsLabel.ShowAll ();
				PackStart (flagsLabel, true, true, 0);
				
				Gtk.Button but = new Gtk.Button ("...");
				but.Clicked += OnSelectFlags;
				but.ShowAll ();
				PackStart (but, false, false, 0);
			}
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			((IDisposable)this).Dispose ();
		}

		void IDisposable.Dispose ()
		{
		}

		public object Value {
			get {
				return Enum.ToObject (propType, UIntValue);
			}
			set {
				UIntValue = Convert.ToUInt64 (value);
			}
		}

		public event EventHandler ValueChanged;

		ulong uintValue;
		
		ulong UIntValue {
			get {
				return uintValue;
			}
			set {
				if (uintValue != value) {
					uintValue = value;
					UpdateFlags ();
					if (ValueChanged != null)
						ValueChanged (this, EventArgs.Empty);
				}
			}
		}

		void FlagToggled (object o, EventArgs args)
		{
			Gtk.CheckButton check = (Gtk.CheckButton)o;
			ulong val = (ulong)flags[o];

			if (check.Active) {
				if (val == 0)
					UIntValue = 0;
				else
					UIntValue |= val;
			} else
				UIntValue &= ~val;
		}

		void UpdateFlags ()
		{
			if (flagsLabel != null) {
				string txt = "";
				foreach (object val in values) {
					ulong uintVal = Convert.ToUInt64 (val);
					if (UIntValue == 0 && uintVal == 0) {
						txt = val.ToString (); // zero flag defined and no flags set
						break;
					}
					if ((UIntValue & uintVal) != 0) {
						if (txt.Length > 0) txt += ", ";
						txt += val.ToString ();
					}
				}
				flagsLabel.Text = txt;
			} else {
				foreach (object val in values) {
					ulong uintVal = Convert.ToUInt64 (val);
					CheckButton check = (CheckButton)flags [uintVal];
					if (check != null)
						check.Active = (UIntValue == 0 && uintVal == 0) || (UIntValue & uintVal) != 0;
				}
			}
		}
		
		void OnSelectFlags (object o, EventArgs args)
		{
			using (FlagsSelectorDialog dialog = new FlagsSelectorDialog (null, propType, UIntValue, property)) {
				if (dialog.Run () == (int) ResponseType.Ok) {
					Value = Enum.ToObject (propType, dialog.Value);
				}
			}
		}
	}
}
