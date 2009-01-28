//
// PropertyEditorCell.cs
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

using System;
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using MonoDevelop.DesignerSupport.PropertyGrid.PropertyEditors;
using Gtk;
using Gdk;

namespace MonoDevelop.DesignerSupport.PropertyGrid
{
	public class PropertyEditorCell
	{
		Pango.Layout layout;
		PropertyDescriptor property; 
		object obj;
		Gtk.Widget container;
		EditorManager editorManager;
		
		public object Instance {
			get { return obj; }
		}
		
		public PropertyDescriptor Property {
			get { return property; }
		}
		
		public Gtk.Widget Container {
			get { return container; }
		}
		
		internal EditorManager EditorManager {
			get { return editorManager; }
		}
		
		internal void Initialize (Widget container, EditorManager editorManager, PropertyDescriptor property, object obj)
		{
			this.container = container;
			this.editorManager = editorManager;
			
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
			return new EditSession (container, obj, property, ed);
		}
		
		protected virtual string GetValueText ()
		{
			if (obj == null) return "";
			object val = property.GetValue (obj);
			if (val == null) return "";
			else return property.Converter.ConvertToString (val);
		}
		
		protected virtual string GetValueMarkup ()
		{
			return null;
		}
		
		string GetNormalizedText (string s)
		{
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
			string s = GetValueMarkup ();
			if (s != null)
				layout.SetMarkup (GetNormalizedText (s));
			else
				layout.SetText (GetNormalizedText (GetValueText ()));
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
			if (DialogueEdit && (!property.IsReadOnly || EditsReadOnlyObject)) {
				return new PropertyDialogueEditor (this);
			}
			else {
				Type editorType = editorManager.GetEditorType (property);
				if (editorType == null)
					return null;
				
				IPropertyEditor editor = Activator.CreateInstance (editorType) as IPropertyEditor;
				if (editor == null)
					throw new Exception ("The property editor '" + editorType + "' must implement the interface IPropertyEditor");
				return editor;
			}
		}
		
		/// <summary>
		/// Whether the editor should show a button.
		/// </summary>
		public virtual bool DialogueEdit {
			get { return false; }
		}

		/// <summary>
		/// If the property is read-only, is is usually not edited. If the editor
		/// can edit sub-properties of a read-only complex object, this must return true.
		/// <remarks>The default value is false.</remarks>
		/// </summary>
		/// <returns>True if the editor can edit read-only properties</returns>
		public virtual bool EditsReadOnlyObject {
			get { return false; }
		}
		
		public virtual void LaunchDialogue ()
		{
			if (DialogueEdit)
				throw new NotImplementedException();
		}
	}
	
	
		class DefaultPropertyEditor: Gtk.Entry, IPropertyEditor
		{
			PropertyDescriptor property;
			
			public void Initialize (EditSession session)
			{
				this.property = session.Property;
			}
			
			public object Value {
				get { 
					return property.Converter.ConvertFromString (Text); 
				}
				set {
					if (value == null)
						Text = string.Empty;
					else
						Text = property.Converter.ConvertToString (value); 
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
	
	public class EditSession : ITypeDescriptorContext
	{
		PropertyDescriptor property; 
		object obj;
		Gtk.Widget container;
		IPropertyEditor currentEditor;
		bool syncing;
		
		public event EventHandler Changed;
		
		public EditSession (Gtk.Widget container, object instance, PropertyDescriptor property, IPropertyEditor currentEditor)
		{
			this.property = property;
			this.obj = instance;
			this.container = container;
			this.currentEditor = currentEditor;
			
			currentEditor.Initialize (this);
			if (instance != null)
				currentEditor.Value = property.GetValue (instance);
			
			currentEditor.ValueChanged += OnValueChanged;
		}
		
		public void Dispose ()
		{
			currentEditor.Dispose ();
		}
		
		public object Instance {
			get { return obj; }
		}
		
		public PropertyDescriptor Property {
			get { return property; }
		}
		
		PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor {
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
				if (!property.IsReadOnly) {
					property.SetValue (obj, currentEditor.Value);
					if (Changed != null)
						Changed (s, a);
				}
				syncing = false;
			}
		}
		
/*		public void AttachObject (object ob)
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
*/		
		public void UpdateEditor ()
		{
			if (!syncing) {
				syncing = true;
				currentEditor.Value = property.GetValue (obj);
				syncing = false;
			}
		}
		
		#region FIXME Unimplemented ITypeDescriptorContext and IServiceProvider members
		
		object IServiceProvider.GetService (Type serviceType)
		{
			return null;
		}
		
		void ITypeDescriptorContext.OnComponentChanged ()
		{
		}
		
		bool ITypeDescriptorContext.OnComponentChanging ()
		{
			return true;
		}
		
		IContainer ITypeDescriptorContext.Container { get { return null; } }
		
		#endregion
	}
	
	class CellRendererWidget: Gtk.DrawingArea
	{
		PropertyEditorCell cell;
		object obj;
		PropertyDescriptor property;
		EditorManager em;
		
		public CellRendererWidget (PropertyEditorCell cell)
		{
			this.cell = cell;
			this.obj = cell.Instance;
			this.property = cell.Property;
			em = cell.EditorManager;
			this.ModifyBg (Gtk.StateType.Normal, this.Style.White);
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			bool res = base.OnExposeEvent (evnt);
			cell.Initialize (this, em, property, obj);
			
			Gdk.Rectangle rect = Allocation;
			rect.Inflate (-3, 0);// Add some margin
			
			cell.Render (this.GdkWindow, rect, StateType.Normal);
			return res;
		}
	}
	
	class PropertyDialogueEditor: HBox, IPropertyEditor
	{
		PropertyEditorCell cell;
		
		public PropertyDialogueEditor (PropertyEditorCell cell)
		{
			this.cell = cell;
			Spacing = 3;
			PackStart (new CellRendererWidget (cell), true, true, 0);
			Label buttonLabel = new Label ();
			buttonLabel.UseMarkup = true;
			buttonLabel.Xpad = 0; buttonLabel.Ypad = 0;
			buttonLabel.Markup = "<span size=\"small\">...</span>";
			Button dialogueButton = new Button (buttonLabel);
			dialogueButton.Clicked += new EventHandler (DialogueButtonClicked);
			PackStart (dialogueButton, false, false, 0);
			this.ModifyBg (Gtk.StateType.Normal, this.Style.White);
			ShowAll ();
		}
		
		void DialogueButtonClicked (object s, EventArgs args)
		{
			cell.LaunchDialogue ();
			if (ValueChanged != null)
				ValueChanged (this, args);
		}
		
		public void Initialize (EditSession session)
		{
		}

		public object Value {
			get { return cell.Value; }
			set {  }
		}

		public event EventHandler ValueChanged;

	}
	
	public interface IPropertyEditor: IDisposable
	{
		// Called once to initialize the editor.
		void Initialize (EditSession session);
		
		// Gets/Sets the value of the editor. If the editor supports
		// several value types, it is the responsibility of the editor 
		// to return values with the expected type.
		object Value { get; set; }

		// To be fired when the edited value changes.
		event EventHandler ValueChanged;
	}
}
