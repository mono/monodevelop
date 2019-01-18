
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

using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using System;
using MonoDevelop.Components;
using Xamarin.PropertyEditing;
using Xamarin.PropertyEditing.Mac;
using MonoDevelop.Components.Mac;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Theming;
using AppKit;
using CoreGraphics;
using Foundation;

namespace MonoDevelop.DesignerSupport
{
	class MacPropertyGrid : NSStackView, IPropertyGrid
	{
		MacPropertyEditorPanel propertyEditorPanel;

		PropertyPadEditorProvider editorProvider;

		NSScrollView scrollView;

		public event EventHandler Focused;

		public bool IsEditing => false;

		public event EventHandler PropertyGridChanged;

		public MacPropertyGrid () 
		{
			Orientation = NSUserInterfaceLayoutOrientation.Vertical;
			Alignment = NSLayoutAttribute.Leading;
			Spacing = 10;
			Distribution = NSStackViewDistribution.Fill;

			propertyEditorPanel = new MacPropertyEditorPanel ();

			scrollView = new NSScrollView () {
				HasVerticalScroller = true,
				HasHorizontalScroller = false,
			};
			scrollView.WantsLayer = true;
			scrollView.BackgroundColor = Styles.HeaderBackgroundColor;
			scrollView.DocumentView = propertyEditorPanel;

			AddArrangedSubview (scrollView);
		
			propertyEditorPanel.Focused += PropertyEditorPanel_Focused;

			//propertyEditorPanel.PropertiesChanged += PropertyEditorPanel_PropertiesChanged;
		}

		void Widget_Focused (object o, Gtk.FocusedArgs args)
		{
			propertyEditorPanel.BecomeFirstResponder ();
		}

		void PropertyEditorPanel_Focused (object sender, EventArgs e) => Focused?.Invoke (this, EventArgs.Empty);

		public override void SetFrameSize (CGSize newSize)
		{
			scrollView.SetFrameSize (newSize);
			base.SetFrameSize (newSize);
		}

		void PropertyEditorPanel_PropertiesChanged (object sender, EventArgs e) => PropertyGridChanged?.Invoke (this, e);

		public void BlankPad ()
		{
			propertyEditorPanel.SelectedItems.Clear ();
			currentSelectedObject = null;
		}

		internal void OnPadContentShown ()
		{
			if (editorProvider == null) {
				editorProvider = new PropertyPadEditorProvider ();
				propertyEditorPanel.TargetPlatform = new TargetPlatform (editorProvider) {
					AutoExpandGroups = new string [] { "Build", "Misc", "NuGet", "Reference" }
				};
				propertyEditorPanel.ArrangeMode = PropertyArrangeMode.Category;
			}
		}

		Tuple<object, object []> currentSelectedObject;

		public void SetCurrentObject (object lastComponent, object [] propertyProviders)
		{
			if (lastComponent != null) {
				var selection = new Tuple<object, object []> (lastComponent, propertyProviders);
				if (currentSelectedObject != selection) {
					propertyEditorPanel.SelectedItems.Clear ();
					propertyEditorPanel.SelectedItems.Add (new Tuple<object, object []> (lastComponent, propertyProviders));
					currentSelectedObject = selection;
				}
			}
		}

		protected override void Dispose (bool disposing)
		{
			propertyEditorPanel.Focused -= PropertyEditorPanel_Focused;
			base.Dispose (disposing);
		}

		internal void Populate (bool saveEditSession)
		{
			//not implemented
		}

		public void SetToolbarProvider (Components.PropertyGrid.PropertyGrid.IToolbarProvider toolbarProvider)
		{
			//not implemented
		}
	}

	class MacPropertyEditorPanel : PropertyEditorPanel
	{
		public EventHandler Focused;

		public override bool BecomeFirstResponder ()
		{
			Focused?.Invoke (this, EventArgs.Empty);
			return base.BecomeFirstResponder ();
		}
	}
}

#endif