
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
	public class MacPropertyPad : PadContent, ICommandDelegator, IPropertyPad
	{
		Gtk.Widget widget;

		public bool IsPropertyGridEditing => false;

		public event EventHandler PropertyGridChanged;

		PropertyEditorPanel propertyEditorPanel;

		MockEditorProvider editorProvider;
		MockResourceProvider resourceProvider;
		MockBindingProvider bindingProvider;

		protected override void Initialize (IPadWindow window)
		{
			base.Initialize (window);

			var container = NativeViewHelper.CreateVerticalStackView ();
			container.WantsLayer = true;
			container.Layer.BackgroundColor = NSColor.Yellow.CGColor;
			container.TranslatesAutoresizingMaskIntoConstraints = true;
			//propertyEditorPanel.PropertiesChanged += PropertyEditorPanel_PropertiesChanged;

			propertyEditorPanel = new PropertyEditorPanel ();

			var scrollView = new NSScrollView () {
				HasVerticalScroller = true,
				HasHorizontalScroller = false,
			};
			scrollView.BackgroundColor = NSColor.Red;
			scrollView.DocumentView = propertyEditorPanel;

			container.AddArrangedSubview (scrollView);

			//scrollView.WidthAnchor.ConstraintEqualToAnchor (container.WidthAnchor, 0).Active = true;


			widget = GtkMacInterop.NSViewToGtkWidget (container);

			window.PadContentShown += Window_PadContentShown;

			DesignerSupport.Service.SetPad (this);

			NSNotificationCenter.DefaultCenter.AddObserver (NSView.FrameChangedNotification, (s => {
				if (s.Object == container) {
					scrollView.Frame = container.Frame;
					var frame = (CGRect)propertyEditorPanel.Frame;
					//propertyEditorPanel.SetContentSize (new CGSize (container.Frame.Width, frame.Size.Height)); 
				}
			}));

			widget.ShowAll ();
		}

		void PropertyEditorPanel_PropertiesChanged (object sender, EventArgs e) => PropertyGridChanged?.Invoke (this, e);

		void Window_PadContentShown (object sender, EventArgs e)
		{
			if (editorProvider == null) {
				editorProvider = new MockEditorProvider ();
				resourceProvider = new MockResourceProvider ();
				bindingProvider = new MockBindingProvider ();
				propertyEditorPanel.TargetPlatform = new TargetPlatform (editorProvider, resourceProvider, bindingProvider) {
					SupportsCustomExpressions = true,
					SupportsMaterialDesign = true,
				};
			}
		}

		public void BlankPad ()
		{
			propertyEditorPanel.Select (new object [0]);
			CommandRouteOrigin = null;
		}

		#region ICommandDelegatorRouter implementation

		internal object CommandRouteOrigin { get; set; }

		object ICommandDelegator.GetDelegatedCommandTarget ()
		{
			// Route the save command to the object for which we are inspecting the properties,
			// so pressing the Save shortcut when doing changes in the property pad will save
			// the document we are changing
			if (IdeApp.CommandService.CurrentCommand == IdeApp.CommandService.GetCommand (FileCommands.Save))
				return CommandRouteOrigin;
			else
				return null;
		}

		#endregion

		public void PopulateGrid (bool saveEditSession)
		{

		}

		public class TestApp
		{
			public string MyProperty { get; set; }
		}

		public void SetCurrentObject (object lastComponent, object [] propertyProviders)
		{
			if (lastComponent != null) {
				propertyEditorPanel.Select (new object [] { new TestApp () });
			}
		}

		#region AbstractPadContent implementations

		public override Control Control {
			get { return widget; }
		}

		#endregion

		public override void Dispose ()
		{
			//propertyEditorPanel.PropertiesChanged -= PropertyEditorPanel_PropertiesChanged;
			Window.PadContentShown -= Window_PadContentShown;
			DesignerSupport.Service.SetPad (null);
			base.Dispose ();
		}

	}
}

#endif