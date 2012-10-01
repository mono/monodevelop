using System;
using System.Xml;
using System.CodeDom;
using System.Runtime.InteropServices;

namespace Stetic.Wrapper {

	public class ComboBox : Container {

		public static Gtk.ComboBox CreateInstance ()
		{
			Gtk.ComboBox c = Gtk.ComboBox.NewText ();
			// Make sure all children are created, so the mouse events can be
			// bound and the widget can be selected.
			c.EnsureStyle ();
			try {
				FixSensitivity (c);
			} catch {
			}
			return c;
		}
		
		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			if (!initialized)
				textCombo = true;
		}


		string[] items = new string[0];
		bool textCombo;

		public string[] Items {
			get { return items; }
			set {
				Gtk.ComboBox combobox = (Gtk.ComboBox)Wrapped;
				int active = combobox.Active;

				int row = 0, oi = 0, ni = 0;
				while (value != null && oi < items.Length && ni < value.Length) {
					if (items [oi] == value [ni]) {
						oi++;
						ni++;
						row++;
					} else if (ni < value.Length - 1 &&
						   items [oi] == value [ni + 1]) {
						combobox.InsertText (row++, value [ni++]);
						if (active > row)
							active++;
					} else {
						combobox.RemoveText (row);
						if (active > row)
							active--;
						oi++;
					}
				}

				while (oi < items.Length) {
					combobox.RemoveText (row);
					oi++;
				}

				while (value != null && ni < value.Length)
					combobox.InsertText (row++, value [ni++]);

				items = value == null ? new string [0] : value;
				combobox.Active = active;

				EmitNotify ("Items");
			}
		}
		
		public bool IsTextCombo {
			get { return textCombo; }
			set { textCombo = value; EmitNotify ("IsTextCombo"); }
		}
		
		internal protected override CodeExpression GenerateObjectCreation (GeneratorContext ctx)
		{
			if (textCombo) {
				return new CodeMethodInvokeExpression (
					new CodeTypeReferenceExpression (new CodeTypeReference ("Gtk.ComboBox", CodeTypeReferenceOptions.GlobalReference)),
					"NewText"
				);
			} else
				return base.GenerateObjectCreation (ctx);
		}
		
		internal protected override void GenerateBuildCode (GeneratorContext ctx, CodeExpression var)
		{
			if (textCombo && Items != null && Items.Length > 0) {
				foreach (string str in Items) {
					ctx.Statements.Add (new CodeMethodInvokeExpression (
						var,
						"AppendText",
						ctx.GenerateValue (str, typeof(string), true)
					));
				}
			}
			
			base.GenerateBuildCode (ctx, var);
		}
		
		public override void Read (ObjectReader reader, XmlElement element)
		{
			base.Read (reader, element);
			if (reader.Format == FileFormat.Glade && items.Length > 0)
				IsTextCombo = true;
		}
		
		internal static void FixSensitivity (Gtk.ComboBox c)
		{
			// Since gtk+ 2.14, empty combos are disabled by default
			// This method disables this behavior
			gtk_combo_box_set_button_sensitivity (c.Handle, 1);
		}

		[DllImport ("libgtk-win32-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static void gtk_combo_box_set_button_sensitivity (IntPtr combo, int mode);
	}
}
