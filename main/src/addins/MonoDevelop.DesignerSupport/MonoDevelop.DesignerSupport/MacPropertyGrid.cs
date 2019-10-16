
/* 
 * PropertyPad.cs: The pad that holds the MD property grid. Can also 
 * hold custom grid widgets.
 * 
 * Author:
 *   Jose Medrano <josmed@microsoft.com>
 *
 * Copyright (C) 2018 Microsoft, Corp
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#if MAC

using System;
using MonoDevelop.Components;
using Xamarin.PropertyEditing;
using Xamarin.PropertyEditing.Mac;
using AppKit;
using CoreGraphics;

namespace MonoDevelop.DesignerSupport
{
	class MacPropertyGrid : NSView
	{
		readonly MacPropertyEditorPanel propertyEditorPanel;
		ComponentModelEditorProvider editorProvider;
		ComponentModelTarget currentSelectedObject;

		public event EventHandler Focused;

		public bool IsEditing => false;

		//Small hack to cover the missing Proppy feature
		public bool Sensitive { get; set; }
		public override NSView HitTest (CGPoint aPoint)
		{
			if (!Sensitive) return null;
			return base.HitTest (aPoint);
		}

		public event EventHandler PropertyGridChanged;

		public MacPropertyGrid () 
		{
			propertyEditorPanel = new MacPropertyEditorPanel (new MonoDevelopHostResourceProvider ()) {
				ShowHeader = false
			};
			AddSubview (propertyEditorPanel);

			editorProvider = new ComponentModelEditorProvider ();
			editorProvider.PropertyChanged += EditorProvider_PropertyChanged;

			propertyEditorPanel.TargetPlatform = new TargetPlatform (editorProvider) {
				AutoExpandAll = true
			};
			propertyEditorPanel.ArrangeMode = PropertyArrangeMode.Category;
		}

		private void EditorProvider_PropertyChanged (object sender, EventArgs e) =>
			PropertyGridChanged?.Invoke (this, EventArgs.Empty);

		public override void SetFrameSize (CGSize newSize)
		{
			base.SetFrameSize (newSize);
			propertyEditorPanel.SetFrameSize (newSize);
		}

		public void BlankPad ()
		{
			propertyEditorPanel.SelectedItems.Clear ();
			currentSelectedObject = null;
			editorProvider.Clear ();
		}

		public object CurrentObject {
			get => currentSelectedObject.Target;
		}

		//HACK: this 
		bool showToolbar = true;
		public bool ShowToolbar {
			get => showToolbar;
			set {
				//we ensure remove current constraints from proppy


				if (showToolbar) {

				} else {
					//we only want include 
				}
			}
		}

		public void SetCurrentObject (object lastComponent, object [] propertyProviders)
		{
			if (lastComponent != null) {
				var selection = new ComponentModelTarget (lastComponent, propertyProviders);
				if (currentSelectedObject != selection) {
					propertyEditorPanel.SelectedItems.Clear ();
					propertyEditorPanel.SelectedItems.Add (selection);
					currentSelectedObject = selection;
				}
			} else {
				BlankPad ();
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				if (editorProvider != null) {
					editorProvider.PropertyChanged -= EditorProvider_PropertyChanged;
					editorProvider.Dispose ();
					editorProvider = null;
				}
			}
			base.Dispose (disposing);
		}
	}

	class MacPropertyEditorPanel : PropertyEditorPanel
	{
		public EventHandler Focused;

		public MacPropertyEditorPanel (MonoDevelopHostResourceProvider hostResources)
			: base (hostResources)
		{

		}

		public override bool BecomeFirstResponder ()
		{
			Focused?.Invoke (this, EventArgs.Empty);
			return base.BecomeFirstResponder ();
		}
	}
}

#endif