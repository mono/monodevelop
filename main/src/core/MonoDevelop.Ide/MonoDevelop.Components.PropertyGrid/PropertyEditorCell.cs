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
using System.ComponentModel;
using Gdk;
using Gtk;
using Mono.TextEditor;

namespace MonoDevelop.Components.PropertyGrid
{
	public class PropertyEditorCell
	{
		Pango.Layout layout;
		ITypeDescriptorContext context;
		Widget container;
		EditorManager editorManager;
		
		public object Instance {
			get { return context.Instance; }
		}
		
		public PropertyDescriptor Property {
			get { return context.PropertyDescriptor; }
		}

		protected ITypeDescriptorContext Context {
			get { return context; }
		}
		
		public Widget Container {
			get { return container; }
		}
		
		internal EditorManager EditorManager {
			get { return editorManager; }
		}
		
		internal void Initialize (Widget container, EditorManager editorManager, ITypeDescriptorContext context)
		{
			this.container = container;
			this.editorManager = editorManager;
			
			layout = new Pango.Layout (container.PangoContext);
			layout.Width = -1;
			
			Pango.FontDescription des = container.Style.FontDescription.Copy();
			layout.FontDescription = des;
			
			this.context = context;
			Initialize ();
		}

		public EditSession StartEditing (Rectangle cellArea, StateType state)
		{
			IPropertyEditor ed = CreateEditor (cellArea, state);
			if (ed == null)
				return null;
			return new EditSession (container, context, ed);
		}
		
		protected virtual string GetValueText ()
		{
			var val = Value;
			if (val != null)
				return Property.Converter.ConvertToString (context, val);
			return "";
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
			
			s = s.TrimStart ('\n', ' ', '\t');
			i = s.IndexOf ('\n');
			if (i != -1)
				return s.Substring (0, i) + "...";
			return s;
		}
		
		public object Value {
			get { return Instance != null ? Property.GetValue (Instance) : null; }
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

		public virtual void Render (Drawable window, Cairo.Context ctx, Rectangle bounds, StateType state)
		{
			int w, h;
			layout.GetPixelSize (out w, out h);
			int dy = (bounds.Height - h) / 2;

			ctx.Save ();
			ctx.SetSourceColor (container.Style.Text (state).ToCairoColor ());
			ctx.MoveTo (bounds.X, dy + bounds.Y);
			Pango.CairoHelper.ShowLayout (ctx, layout);
			ctx.Restore ();
		}
		
		protected virtual IPropertyEditor CreateEditor (Rectangle cellArea, StateType state)
		{
			if (DialogueEdit && (!Property.IsReadOnly || EditsReadOnlyObject)) {
				return new PropertyDialogueEditor (this, context);
			}
			else {
				Type editorType = editorManager.GetEditorType (context);
				if (editorType == null)
					return null;
				
				var editor = Activator.CreateInstance (editorType) as IPropertyEditor;
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
				throw new NotImplementedException ();
		}
	}
	
	
	class DefaultPropertyEditor: Entry, IPropertyEditor
	{
		PropertyDescriptor property;
		
		public void Initialize (EditSession session)
		{
			property = session.Property;
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
	
	public class EditSession : ITypeDescriptorContext
	{
		Widget container;
		IPropertyEditor currentEditor;
		bool syncing;
		readonly ITypeDescriptorContext context;
		
		public event EventHandler Changed;
		
		internal EditSession (Widget container, ITypeDescriptorContext context, IPropertyEditor currentEditor)
		{
			this.context = context;
			this.container = container;
			this.currentEditor = currentEditor;
			
			currentEditor.Initialize (this);
			if (Instance != null)
				currentEditor.Value = context.PropertyDescriptor.GetValue (Instance);
			
			currentEditor.ValueChanged += OnValueChanged;
		}
		
		public void Dispose ()
		{
			currentEditor.Dispose ();
		}
		
		public object Instance {
			get { return context.Instance; }
		}
		
		public PropertyDescriptor Property {
			get { return context.PropertyDescriptor; }
		}
		
		PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor {
			get { return context.PropertyDescriptor; }
		}
		
		public Widget Container {
			get { return container; }
		}
		
		public IPropertyEditor Editor {
			get { return currentEditor; }
		}
		
		void OnValueChanged (object s, EventArgs a)
		{
			if (!syncing) {
				syncing = true;
				if (!context.PropertyDescriptor.IsReadOnly) {
					context.PropertyDescriptor.SetValue (context.Instance, currentEditor.Value);
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
				currentEditor.Value = context.PropertyDescriptor.GetValue (context.Instance);
				syncing = false;
			}
		}

		object IServiceProvider.GetService (Type serviceType)
		{
			return context.GetService (serviceType);
		}
		
		void ITypeDescriptorContext.OnComponentChanged ()
		{
			context.OnComponentChanged ();
		}
		
		bool ITypeDescriptorContext.OnComponentChanging ()
		{
			return context.OnComponentChanging ();
		}
		
		IContainer ITypeDescriptorContext.Container { get { return context.Container; } }
	}
	
	class CellRendererWidget: DrawingArea
	{
		readonly PropertyEditorCell cell;
		readonly ITypeDescriptorContext context;
		readonly EditorManager em;
		
		public CellRendererWidget (PropertyEditorCell cell, ITypeDescriptorContext context)
		{
			this.cell = cell;
			this.context = context;
			em = cell.EditorManager;
			this.ModifyBg (StateType.Normal, this.Style.White);
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			bool res = base.OnExposeEvent (evnt);
			cell.Initialize (this, em, context);
			
			Rectangle rect = Allocation;
			rect.Inflate (-3, 0);// Add some margin

			using (Cairo.Context ctx = CairoHelper.Create (GdkWindow)) {
				cell.Render (GdkWindow, ctx, rect, StateType.Normal);
			}
			return res;
		}
	}
	
	class PropertyDialogueEditor: HBox, IPropertyEditor
	{
		PropertyEditorCell cell;
		
		public PropertyDialogueEditor (PropertyEditorCell cell, ITypeDescriptorContext context)
		{
			this.cell = cell;
			Spacing = 3;
			PackStart (new CellRendererWidget (cell, context), true, true, 0);
			Label buttonLabel = new Label ();
			buttonLabel.UseMarkup = true;
			buttonLabel.Xpad = 0; buttonLabel.Ypad = 0;
			buttonLabel.Markup = "<span size=\"small\">...</span>";
			Button dialogueButton = new Button (buttonLabel);
			dialogueButton.Clicked += DialogueButtonClicked;
			PackStart (dialogueButton, false, false, 0);
			this.ModifyBg (StateType.Normal, this.Style.White);
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
