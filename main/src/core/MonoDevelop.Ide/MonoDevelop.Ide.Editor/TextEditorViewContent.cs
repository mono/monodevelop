//
// TextEditorViewContent.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Ide.Editor
{
	/// <summary>
	/// The TextEditor object needs to be available through IBaseViewContent.GetContent therefore we need to insert a 
	/// decorator in between.
	/// </summary>
	class TextEditorViewContent : IViewContent, MonoDevelop.Components.Commands.ICommandRouter
    {
		readonly TextEditor textEditor;
		readonly ITextEditorImpl textEditorImpl;

		MonoDevelop.Projects.Policies.PolicyContainer policyContainer;

		public TextEditorViewContent (TextEditor textEditor, ITextEditorImpl textEditorImpl)
		{
			if (textEditor == null)
				throw new ArgumentNullException ("textEditor");
			if (textEditorImpl == null)
				throw new ArgumentNullException ("textEditorImpl");
			this.textEditor = textEditor;
			this.textEditorImpl = textEditorImpl;
			this.textEditor.MimeTypeChanged += UpdateTextEditorOptions;
			DefaultSourceEditorOptions.Instance.Changed += UpdateTextEditorOptions;
        }


		void UpdateTextEditorOptions (object sender, EventArgs e)
		{
			UpdateStyleParent (Project, textEditor.MimeType);
		}

		void RemovePolicyChangeHandler ()
		{
			if (policyContainer != null)
				policyContainer.PolicyChanged -= HandlePolicyChanged;
		}

		void UpdateStyleParent (Project styleParent, string mimeType)
		{
			RemovePolicyChangeHandler ();

			if (string.IsNullOrEmpty (mimeType))
				mimeType = "text/plain";

			var mimeTypes = DesktopService.GetMimeTypeInheritanceChain (mimeType);

			if (styleParent != null)
				policyContainer = styleParent.Policies;
			else
				policyContainer = MonoDevelop.Projects.Policies.PolicyService.DefaultPolicies;
			var currentPolicy = policyContainer.Get<TextStylePolicy> (mimeTypes);

			policyContainer.PolicyChanged += HandlePolicyChanged;
			textEditor.Options = DefaultSourceEditorOptions.Instance.WithTextStyle (currentPolicy);
		}

		void HandlePolicyChanged (object sender, MonoDevelop.Projects.Policies.PolicyChangedEventArgs args)
		{
			var mimeTypes = DesktopService.GetMimeTypeInheritanceChain (textEditor.MimeType);
			var currentPolicy = policyContainer.Get<TextStylePolicy> (mimeTypes);
			textEditor.Options = DefaultSourceEditorOptions.Instance.WithTextStyle (currentPolicy);
		}

		#region IViewContent implementation

		event EventHandler IViewContent.ContentNameChanged {
			add {
				textEditorImpl.ContentNameChanged += value;
			}
			remove {
				textEditorImpl.ContentNameChanged -= value;
			}
		}

		event EventHandler IViewContent.ContentChanged {
			add {
				textEditorImpl.ContentChanged += value;
			}
			remove {
				textEditorImpl.ContentChanged -= value;
			}
		}

		event EventHandler IViewContent.DirtyChanged {
			add {
				textEditorImpl.DirtyChanged += value;
			}
			remove {
				textEditorImpl.DirtyChanged -= value;
			}
		}

		event EventHandler IViewContent.BeforeSave {
			add {
				textEditorImpl.BeforeSave += value;
			}
			remove {
				textEditorImpl.BeforeSave -= value;
			}
		}

		void IViewContent.Load (string fileName)
		{
			textEditorImpl.Load (fileName);
		}

		void IViewContent.LoadNew (System.IO.Stream content, string mimeType)
		{
			textEditorImpl.LoadNew (content, mimeType);
		}

		void IViewContent.Save (string fileName)
		{
			textEditorImpl.Save (fileName);
		}

		void IViewContent.Save ()
		{
			textEditorImpl.Save ();
		}

		void IViewContent.DiscardChanges ()
		{
			textEditorImpl.DiscardChanges ();
		}

		public Project Project {
			get {
				return textEditorImpl.Project;
			}
			set {
				textEditorImpl.Project = value;
				UpdateTextEditorOptions (null, null);
			}
		}

		string IViewContent.PathRelativeToProject {
			get {
				return textEditorImpl.PathRelativeToProject;
			}
		}

		string IViewContent.ContentName {
			get {
				return textEditorImpl.ContentName;
			}
			set {
				textEditorImpl.ContentName = value;
			}
		}

		string IViewContent.UntitledName {
			get {
				return textEditorImpl.UntitledName;
			}
			set {
				textEditorImpl.UntitledName = value;
			}
		}

		string IViewContent.StockIconId {
			get {
				return textEditorImpl.StockIconId;
			}
		}

		bool IViewContent.IsUntitled {
			get {
				return textEditorImpl.IsUntitled;
			}
		}

		bool IViewContent.IsViewOnly {
			get {
				return textEditorImpl.IsViewOnly;
			}
		}

		bool IViewContent.IsFile {
			get {
				return textEditorImpl.IsFile;
			}
		}

		bool IViewContent.IsDirty {
			get {
				return textEditorImpl.IsDirty;
			}
			set {
				textEditorImpl.IsDirty = value;
			}
		}

		bool IViewContent.IsReadOnly {
			get {
				return textEditorImpl.IsReadOnly;
			}
		}

		#endregion

		#region IBaseViewContent implementation

		object IBaseViewContent.GetContent (Type type)
		{
			if (type.IsAssignableFrom (typeof(TextEditor)))
				return textEditor;
			var ext = textEditorImpl.EditorExtension;
			while (ext != null) {
				if (type.IsInstanceOfType (ext))
					return ext;
				ext = ext.Next;
			}
			return textEditorImpl.GetContent (type);
		}

		bool IBaseViewContent.CanReuseView (string fileName)
		{
			return textEditorImpl.CanReuseView (fileName);
		}

		void IBaseViewContent.RedrawContent ()
		{
			textEditorImpl.RedrawContent ();
		}

		IWorkbenchWindow IBaseViewContent.WorkbenchWindow {
			get {
				return textEditorImpl.WorkbenchWindow;
			}
			set {
				textEditorImpl.WorkbenchWindow = value;
			}
		}

		Gtk.Widget IBaseViewContent.Control {
			get {
				return textEditor;
			}
		}

		string IBaseViewContent.TabPageLabel {
			get {
				return textEditorImpl.TabPageLabel;
			}
		}

		#endregion

		#region IDisposable implementation

		void IDisposable.Dispose ()
		{
			DefaultSourceEditorOptions.Instance.Changed -= UpdateTextEditorOptions;
			RemovePolicyChangeHandler ();
			textEditorImpl.Dispose ();
		}

		#endregion

		#region ICommandRouter implementation

		object MonoDevelop.Components.Commands.ICommandRouter.GetNextCommandTarget ()
		{
			return textEditorImpl;
		}

		#endregion
    }
}