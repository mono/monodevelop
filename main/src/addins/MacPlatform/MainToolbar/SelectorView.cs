// SelectorView.cs
//
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.Collections.Generic;
using System.Linq;
using AppKit;
using CoreGraphics;
using Foundation;
using MonoDevelop.Components;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	class SizeRequestedEventArgs : EventArgs
	{
		public CGSize Size { get; private set; }
		public SizeRequestedEventArgs (CGSize size)
		{
			Size = size;
		}
	}

	[Register]
	class SelectorView : NSButton
	{
		public event EventHandler<SizeRequestedEventArgs> ResizeRequested;
		internal const int ConfigurationIdx = 0;
		internal const int RuntimeIdx = 1;

		public SelectorView ()
		{
			Title = "";
			BezelStyle = NSBezelStyle.TexturedRounded;
			var pathSelectorView = new PathSelectorView (new CGRect (6, 0, 1, 1));
			pathSelectorView.UnregisterDraggedTypes ();
			AddSubview (pathSelectorView);
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			var p = (NSPathControl)Subviews [0];
			var size = new CGSize (10 +
				p.PathComponentCells [ConfigurationIdx].CellSize.Width +
				p.PathComponentCells [RuntimeIdx].CellSize.Width + p.Frame.Left,
				Frame.Size.Height);
			if (ResizeRequested != null)
				ResizeRequested (this, new SizeRequestedEventArgs (size));

			SetFrameSize (size);
			p.SetFrameSize (size);
			p.SetNeedsDisplay ();
			base.DrawRect (new CGRect (CGPoint.Empty, size));
		}

		#region PathSelectorView
		[Register]
		public class PathSelectorView : NSPathControl
		{
			static readonly string ConfigurationPlaceholder = GettextCatalog.GetString ("Default");
			static readonly string RuntimePlaceholder = GettextCatalog.GetString ("Default");

			NSMenu CreateSubMenuForRuntime (IRuntimeModel runtime)
			{
				if (!runtime.Children.Any ())
					return null;

				var menu = new NSMenu {
					AutoEnablesItems = false,
					ShowsStateColumn = false,
					Font = NSFont.MenuFontOfSize (12),
				};
				foreach (var item in runtime.Children)
					CreateMenuItem (menu, item);
				return menu;
			}

			void CreateMenuItem (NSMenu menu, IRuntimeModel runtime)
			{
				NSMenuItem menuItem;
				string runtimeFullDisplayString;

				using (var mutableModel = runtime.GetMutableModel ()) {
					runtimeFullDisplayString = mutableModel.FullDisplayString;

					menuItem = new NSMenuItem {
						IndentationLevel = runtime.IsIndented ? 2 : 1,
						AttributedTitle = new NSAttributedString (mutableModel.DisplayString, new NSStringAttributes {
							Font = runtime.Notable ? NSFontManager.SharedFontManager.ConvertFont (menu.Font, NSFontTraitMask.Bold) : menu.Font,
						}),
						Enabled = mutableModel.Enabled,
						Hidden = !mutableModel.Visible,
					};
				}

				var subMenu = CreateSubMenuForRuntime (runtime);
				if (subMenu != null) {
					menuItem.Submenu = subMenu;
					menuItem.Enabled = true;
				} else {
					menuItem.Activated += (o2, e2) => {
						string old;
						using (var activeMutableModel = ActiveRuntime.GetMutableModel ())
							old = activeMutableModel.FullDisplayString;

						IRuntimeModel newRuntime = runtimeModel.FirstOrDefault (r => {
							using (var newRuntimeMutableModel = r.GetMutableModel ())
								return newRuntimeMutableModel.FullDisplayString == runtimeFullDisplayString;
						});
						if (newRuntime == null)
							return;

						ActiveRuntime = newRuntime;
						var ea = new HandledEventArgs ();
						if (RuntimeChanged != null)
							RuntimeChanged (o2, ea);

						if (ea.Handled)
							ActiveRuntime = runtimeModel.First (r => {
								using (var newRuntimeMutableModel = r.GetMutableModel ())
									return newRuntimeMutableModel.FullDisplayString == old;
							});
					};
				}
				menu.AddItem (menuItem);
			}


			public PathSelectorView (CGRect frameRect) : base (frameRect)
			{
				PathComponentCells = new [] {
					new NSPathComponentCell {
						Image = ImageService.GetIcon ("project").ToNSImage (),
						Title = ConfigurationPlaceholder,
						Enabled = false,
						TextColor = NSColor.FromRgba (0.34f, 0.34f, 0.34f, 1),
					},
					new NSPathComponentCell {
						Image = ImageService.GetIcon ("device").ToNSImage (),
						Title = RuntimePlaceholder,
						Enabled = false,
						TextColor = NSColor.FromRgba (0.34f, 0.34f, 0.34f, 1),
					}
				};

				BackgroundColor = NSColor.Clear;
				FocusRingType = NSFocusRingType.None;
				Activated += (sender, e) => {
					var item = ClickedPathComponentCell;
					if (item == null)
						return;

					var componentRect = ((NSPathCell)Cell).GetRect (item, Frame, this);
					int idx = -1;
					int i = 0;

					var menu = new NSMenu {
						AutoEnablesItems = false,
						ShowsStateColumn = false,
						Font = NSFont.MenuFontOfSize (12),
					};
					if (object.ReferenceEquals (ClickedPathComponentCell, PathComponentCells [ConfigurationIdx])) {
						foreach (var configuration in ConfigurationModel) {
							if (idx == -1 && configuration.OriginalId == ActiveConfiguration.OriginalId)
								idx = i;

							var _configuration = configuration;
							menu.AddItem (new NSMenuItem (configuration.DisplayString, (o2, e2) => {
								ActiveConfiguration = configurationModel.First (c => c.OriginalId == _configuration.OriginalId);
								if (ConfigurationChanged != null)
									ConfigurationChanged (o2, e2);
								UpdatePathText (ConfigurationIdx, _configuration.DisplayString);
							}) {
								Enabled = true,
								IndentationLevel = 1,
							});
							++i;
						}
					} else if (object.ReferenceEquals (ClickedPathComponentCell, PathComponentCells [RuntimeIdx])) {
						using (var activeMutableModel = ActiveRuntime.GetMutableModel ()) {
							foreach (var runtime in RuntimeModel) {
								using (var mutableModel = runtime.GetMutableModel ()) {
									if (idx == -1 && mutableModel.DisplayString == activeMutableModel.DisplayString)
										idx = i;
								}

								if (runtime.HasParent)
									continue;

								if (runtime.IsSeparator)
									menu.AddItem (NSMenuItem.SeparatorItem);
								else
									CreateMenuItem (menu, runtime);
								++i;
							}
						}
					} else
						throw new NotSupportedException ();

					if (menu.Count > 1) {
						var offs = new CGPoint (componentRect.Left + 3, componentRect.Top + 3);

						if (Window.Screen.BackingScaleFactor == 2)
							offs.Y += 0.5f; // fine tune menu position on retinas

						menu.PopUpMenu (null, offs, this);
					}
				};
			}

			public override void ViewDidMoveToWindow ()
			{
				base.ViewDidMoveToWindow ();

				NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidChangeBackingPropertiesNotification,
					notification => DispatchService.GuiDispatch (RealignTexts));
				RealignTexts ();
			}

			void RealignTexts ()
			{
				if (Window == null)
					return;

				// fix the icon alignment, move it slightly up
				var alignFix = new CGRect (0, Window.BackingScaleFactor == 2 ? -0.5f : -1f, 16, 16);
				PathComponentCells [ConfigurationIdx].Image.AlignmentRect = alignFix;
				PathComponentCells [RuntimeIdx].Image.AlignmentRect = alignFix;
			}

			void UpdatePathText (int idx, string text)
			{
				PathComponentCells [idx].Title = text;
				PathComponentCells [ConfigurationIdx].Image = ImageService.GetIcon ("project").ToNSImage ();
				PathComponentCells [RuntimeIdx].Image = ImageService.GetIcon ("device").ToNSImage ();

				RealignTexts ();
			}

			IConfigurationModel activeConfiguration;
			public IConfigurationModel ActiveConfiguration {
				get { return activeConfiguration; }
				set {
					activeConfiguration = value;
					UpdatePathText (ConfigurationIdx, value.DisplayString);
				}
			}

			IRuntimeModel activeRuntime;
			public IRuntimeModel ActiveRuntime {
				get { return activeRuntime; }
				set {
					activeRuntime = value;
					using (var mutableModel = value.GetMutableModel ())
						UpdatePathText (RuntimeIdx, mutableModel.FullDisplayString);
				}
			}

			IEnumerable<IConfigurationModel> configurationModel;
			public IEnumerable<IConfigurationModel> ConfigurationModel {
				get { return configurationModel; }
				set {
					configurationModel = value;
					int count = value.Count ();
					if (count == 0)
						UpdatePathText (ConfigurationIdx, ConfigurationPlaceholder);
					PathComponentCells [ConfigurationIdx].Enabled = count > 1;
				}
			}

			IEnumerable<IRuntimeModel> runtimeModel;
			public IEnumerable<IRuntimeModel> RuntimeModel {
				get { return runtimeModel; }
				set {
					runtimeModel = value;
					int count = value.Count ();
					if (count == 0)
						UpdatePathText (RuntimeIdx, RuntimePlaceholder);
					PathComponentCells [RuntimeIdx].Enabled = count > 1;
				}
			}

			public event EventHandler ConfigurationChanged;
			public event EventHandler<HandledEventArgs> RuntimeChanged;
		}
		#endregion
	}
}
