﻿// SelectorView.cs
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

	class OverflowInfoEventArgs : EventArgs
	{
		public nfloat WindowWidth { get; set; }
		public nfloat AllItemsWidth { get; set; }
		public nfloat ItemsInOverflowWidth { get; set; }
	}

	[Register]
	class SelectorView : NSButton
	{
		public event EventHandler<EventArgs> SizeChanged;
		internal const int ConfigurationIdx = 0;
		internal const int RuntimeIdx = 1;

		internal PathSelectorView RealSelectorView { get; private set; }

		public SelectorView ()
		{
			Title = "";
			BezelStyle = NSBezelStyle.TexturedRounded;

			RealSelectorView = new PathSelectorView (new CGRect (6, 0.5, 1, 1));
			RealSelectorView.UnregisterDraggedTypes ();
			AddSubview (RealSelectorView);
		}

		public override CGSize SizeThatFits (CGSize size)
		{
			var p = (PathSelectorView)Subviews [0];
			var fitSize = p.SizeThatFits (size);

			return new CGSize (fitSize.Width + 12.0, size.Height);
		}

		public override void SetFrameSize (CGSize newSize)
		{
			base.SetFrameSize (newSize);
			Subviews [0].SetFrameSize (newSize);
		}

		internal void OnSizeChanged ()
		{
			if (SizeChanged != null) {
				SizeChanged (this, EventArgs.Empty);
			}
		}

		#region PathSelectorView
		[Register]
		public class PathSelectorView : NSPathControl
		{
			[Flags]
			enum CellState
			{
				AllHidden = 0x0,
				RuntimeShown = 0x1,
				ConfigurationShown = 0x2,
				AllShown = 0x3,
			}

			static readonly string ConfigurationPlaceholder = GettextCatalog.GetString ("Default");
			static readonly string RuntimePlaceholder = GettextCatalog.GetString ("Default");
			CellState state = CellState.AllShown;

			public override CGSize SizeThatFits (CGSize size)
			{
				nfloat rtWidth, cWidth;

				WidthsForPathCells (out cWidth, out rtWidth);

				if (10 + cWidth + rtWidth < size.Width) {
					state = CellState.AllShown;
					UpdatePathText (ConfigurationIdx, TextForActiveConfiguration);
					UpdatePathText (RuntimeIdx, TextForRuntimeConfiguration);
					return new CGSize (10 + cWidth + rtWidth, size.Height);
				}

				if (10 + 28 + cWidth < size.Width) {
					state = CellState.ConfigurationShown;
					UpdatePathText (ConfigurationIdx, TextForActiveConfiguration);
					UpdatePathText (RuntimeIdx, string.Empty);
					return new CGSize (10 + 28 + cWidth, size.Height);
				}

				state = CellState.AllHidden;
				UpdatePathText (ConfigurationIdx, string.Empty);
				UpdatePathText (RuntimeIdx, string.Empty);
				return new CGSize (10 + 52.0, size.Height);
			}

			string TextForActiveConfiguration {
				get {
					return ActiveConfiguration != null ? ActiveConfiguration.DisplayString : ConfigurationPlaceholder;
				}
			}

			string TextForRuntimeConfiguration {
				get {
					if (ActiveRuntime != null) {
						using (var mutableModel = ActiveRuntime.GetMutableModel ())
							return mutableModel.FullDisplayString;
					} else {
						return RuntimePlaceholder;
					}
				}
			}
			void WidthsForPathCells (out nfloat configWidth, out nfloat runtimeWidth)
			{
				string text;
				NSPathComponentCell cell;

				text = TextForActiveConfiguration;
				cell = PathComponentCells [ConfigurationIdx];
				configWidth = new NSAttributedString (text, new NSStringAttributes { Font = cell.Font }).Size.Width + 28;

				text = TextForRuntimeConfiguration;
				cell = PathComponentCells [RuntimeIdx];
				runtimeWidth = new NSAttributedString (text, new NSStringAttributes { Font = cell.Font }).Size.Width + 28;
			}

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
						if (ActiveConfiguration == null)
							return;
						
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
						if (ActiveRuntime == null)
							return;
						
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

			void OnSizeChanged ()
			{
				var sview = (SelectorView)Superview;
				sview.OnSizeChanged ();
			}

			IConfigurationModel activeConfiguration;
			public IConfigurationModel ActiveConfiguration {
				get { return activeConfiguration; }
				set {
					activeConfiguration = value;
					state |= CellState.ConfigurationShown;
					UpdatePathText (ConfigurationIdx, value.DisplayString);
					OnSizeChanged ();
				}
			}

			IRuntimeModel activeRuntime;
			public IRuntimeModel ActiveRuntime {
				get { return activeRuntime; }
				set {
					activeRuntime = value;
					using (var mutableModel = value.GetMutableModel ()) {
						state |= CellState.RuntimeShown;
						UpdatePathText (RuntimeIdx, mutableModel.FullDisplayString);
						OnSizeChanged ();
					}
				}
			}

			IEnumerable<IConfigurationModel> configurationModel;
			public IEnumerable<IConfigurationModel> ConfigurationModel {
				get { return configurationModel; }
				set {
					configurationModel = value;
					int count = value.Count ();
					if (count == 0) {
						state |= CellState.ConfigurationShown;
						UpdatePathText (ConfigurationIdx, ConfigurationPlaceholder);
					}
					PathComponentCells [ConfigurationIdx].Enabled = count > 1;
					OnSizeChanged ();
				}
			}

			IEnumerable<IRuntimeModel> runtimeModel;
			public IEnumerable<IRuntimeModel> RuntimeModel {
				get { return runtimeModel; }
				set {
					runtimeModel = value;
					int count = value.Count ();
					if (count == 0) {
						state |= CellState.RuntimeShown;
						UpdatePathText (RuntimeIdx, RuntimePlaceholder);
					}
					PathComponentCells [RuntimeIdx].Enabled = count > 1;
					OnSizeChanged ();
				}
			}

			public event EventHandler ConfigurationChanged;
			public event EventHandler<HandledEventArgs> RuntimeChanged;
		}
		#endregion
	}
}
