
using System;
using System.Collections;
using Gtk;
using Gdk;

namespace Stetic
{
	public class PropertyEditorCell
	{
		Pango.Layout layout;
		PropertyDescriptor property; 
		object obj;
		Gtk.Widget container;
		
		static Hashtable editors;
		static PropertyEditorCell Default = new PropertyEditorCell ();
		static Hashtable cellCache = new Hashtable ();
		
		static PropertyEditorCell ()
		{
			editors = new Hashtable ();

			editors[typeof (bool)] = typeof (Stetic.Editor.Boolean);
			editors[typeof (byte)] = typeof (Stetic.Editor.IntRange);
			editors[typeof (sbyte)] = typeof (Stetic.Editor.IntRange);
			editors[typeof (short)] = typeof (Stetic.Editor.IntRange);
			editors[typeof (ushort)] = typeof (Stetic.Editor.IntRange);
			editors[typeof (int)] = typeof (Stetic.Editor.IntRange);
			editors[typeof (uint)] = typeof (Stetic.Editor.IntRange);
			editors[typeof (long)] = typeof (Stetic.Editor.IntRange);
			editors[typeof (ulong)] = typeof (Stetic.Editor.IntRange);
			editors[typeof (float)] = typeof (Stetic.Editor.FloatRange);
			editors[typeof (double)] = typeof (Stetic.Editor.FloatRange);
			editors[typeof (char)] = typeof (Stetic.Editor.Char);
			editors[typeof (string)] = typeof (Stetic.Editor.TextEditor);
			editors[typeof (DateTime)] = typeof (Stetic.Editor.DateTimeEditorCell);
			editors[typeof (TimeSpan)] = typeof (Stetic.Editor.TimeSpanEditorCell);
			editors[typeof (string[])] = typeof (Stetic.Editor.StringArray);
			editors[typeof (Gdk.Color)] = typeof (Stetic.Editor.Color);
			editors[typeof (Stetic.ImageInfo)] = typeof (Stetic.Editor.ImageSelector);
		}
		
		public object Instance {
			get { return obj; }
		}
		
		public PropertyDescriptor Property {
			get { return property; }
		}
		
		public Gtk.Widget Container {
			get { return container; }
		}
		
		public void Initialize (Widget container, PropertyDescriptor property, object obj)
		{
			this.container = container;
			layout = new Pango.Layout (container.PangoContext);
			layout.Width = -1;
			
			Pango.FontDescription des = container.Style.FontDescription.Copy();
			layout.FontDescription = des;
			
			this.property = property;
			this.obj = obj;
			Initialize ();
		}

		public EditSession StartEditing (Gdk.Rectangle cell_area, StateType state)
		{
			IPropertyEditor ed = CreateEditor (cell_area, state);
			if (ed == null)
				return null;
			ed.Initialize (property);
			if (obj != null) {
				ed.AttachObject (obj);
				ed.Value = property.GetValue (obj);
			}
			return new EditSession (container, obj, property, ed);
		}
		
		protected virtual string GetValueText ()
		{
			if (obj == null) return "";
			object val = property.GetValue (obj);
			if (val == null) return "";
			else return property.ValueToString (val);
		}
		
		string GetNormalizedText ()
		{
			string s = GetValueText ();
			if (s == null)
				return "";

			int i = s.IndexOf ('\n');
			if (i == -1)
				return s;
			
			s = s.TrimStart ('\n',' ','\t');
			i = s.IndexOf ('\n');
			if (i != -1)
				return s.Substring (0, i) + "...";
			else
				return s;
		}
		
		public object Value {
			get { return obj != null ? property.GetValue (obj) : null; }
		}
		
		protected virtual void Initialize ()
		{
			layout.SetText (GetNormalizedText ());
		}
		
		public virtual void GetSize (int availableWidth, out int width, out int height)
		{
			layout.GetPixelSize (out width, out height);
		}
		
		public virtual void Render (Drawable window, Gdk.Rectangle bounds, StateType state)
		{
			int w, h;
			layout.GetPixelSize (out w, out h);
			int dy = (bounds.Height - h) / 2;
			window.DrawLayout (container.Style.TextGC (state), bounds.X, dy + bounds.Y, layout);
		}
		
		protected virtual IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, StateType state)
		{
			Type editorType = property.EditorType;
			
			if (editorType == null) {
				editorType = GetEditorForType (property.PropertyType);
				if (editorType == null)
					return null;
			}
			
			IPropertyEditor editor = Activator.CreateInstance (editorType) as IPropertyEditor;
			if (editor == null)
				throw new Exception ("The property editor '" + editorType + "' must implement the interface IPropertyEditor");
			return editor;
		}
		
		public static Type GetEditorForType (Type propertyType)
		{
			if (propertyType.IsEnum) {
				if (propertyType.IsDefined (typeof (FlagsAttribute), true))
					return typeof (Stetic.Editor.Flags);
				else
					return typeof (Stetic.Editor.Enumeration);
			} else {
				return editors [propertyType] as Type;
			}
		}
		
		public static PropertyEditorCell GetPropertyCell (PropertyDescriptor property)
		{
			Type editorType = property.EditorType;
			
			if (editorType == null)
				editorType = GetEditorForType (property.PropertyType);
			
			if (editorType == null)
				return Default;

			if (typeof(IPropertyEditor).IsAssignableFrom (editorType)) {
				if (!typeof(Gtk.Widget).IsAssignableFrom (editorType))
					throw new Exception ("The property editor '" + editorType + "' must be a Gtk Widget");
				return Default;
			}

			PropertyEditorCell cell = (PropertyEditorCell) cellCache [editorType];
			if (cell != null)
				return cell;

			if (!typeof(PropertyEditorCell).IsAssignableFrom (editorType))
				throw new Exception ("The property editor '" + editorType + "' must be a subclass of Stetic.PropertyEditorCell or implement Stetic.IPropertyEditor");

			cell = (PropertyEditorCell) Activator.CreateInstance (editorType);
			cellCache [editorType] = cell;
			return cell;
		}
	}
	
	
	class DefaultPropertyEditor: Gtk.Entry, IPropertyEditor
	{
		PropertyDescriptor property;
		
		public void Initialize (PropertyDescriptor property)
		{
			this.property = property;
		}
		
		public void AttachObject (object obj)
		{
		}

		public object Value {
			get { 
				return Convert.ChangeType (Text, property.PropertyType); 
			}
			set {
				if (value == null)
					Text = "";
				else
					Text = Convert.ToString (value); 
			}
		}
		
		protected override void OnChanged ()
		{
			base.OnChanged ();
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}

		public event EventHandler ValueChanged;
	}
	
	public class EditSession
	{
		PropertyDescriptor property; 
		object obj;
		Gtk.Widget container;
		IPropertyEditor currentEditor;
		bool syncing;
		object initialVal;
		
		public EditSession (Gtk.Widget container, object instance, PropertyDescriptor property, IPropertyEditor currentEditor)
		{
			this.property = property;
			this.obj = instance;
			this.container = container;
			this.currentEditor = currentEditor;
			currentEditor.ValueChanged += OnValueChanged;
			initialVal = currentEditor.Value;
		}
		
		public object Instance {
			get { return obj; }
		}
		
		public PropertyDescriptor Property {
			get { return property; }
		}
		
		public Gtk.Widget Container {
			get { return container; }
		}
		
		public IPropertyEditor Editor {
			get { return currentEditor; }
		}
		
		void OnValueChanged (object s, EventArgs a)
		{
			if (!syncing) {
				syncing = true;
				property.SetValue (obj, currentEditor.Value);
				Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (obj) as Stetic.Wrapper.Widget;
				if (wrapper != null)
					wrapper.NotifyChanged ();
				syncing = false;
			}
		}
		
		public void AttachObject (object ob)
		{
			if (ob == null)
				throw new ArgumentNullException ("ob");
			
			syncing = true;
			this.obj = ob;
			currentEditor.AttachObject (obj);
			
			// It is the responsibility of the editor to convert value types
			object initial = property.GetValue (obj);
			currentEditor.Value = initial;
			
			syncing = false;
		}
		
		public void UpdateEditor ()
		{
			if (!syncing) {
				syncing = true;
				currentEditor.Value = property.GetValue (obj);
				syncing = false;
			}
		}
		
		public void Dispose ()
		{
			if (!object.Equals (initialVal, currentEditor.Value))
				OnValueChanged (null, null);
		}
	}
}
