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
			AddSubview (new PathSelectorView ());
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			var p = (NSPathControl)Subviews [0];
			dirtyRect.Width = 10 + 
				p.PathComponentCells [ConfigurationIdx].CellSize.Width +
				p.PathComponentCells [RuntimeIdx].CellSize.Width;

			if (ResizeRequested != null)
				ResizeRequested (this, new SizeRequestedEventArgs (dirtyRect.Size));

			SetFrameSize (dirtyRect.Size);
			p.SetFrameSize (dirtyRect.Size);
			base.DrawRect (dirtyRect);
		}

		#region PathSelectorView
		[Register]
		public class PathSelectorView : NSPathControl
		{
			static readonly string ConfigurationPlaceholder = GettextCatalog.GetString ("Default");
			static readonly string RuntimePlaceholder = GettextCatalog.GetString ("Default");

			public PathSelectorView ()
			{
				PathComponentCells = new [] {
					new NSPathComponentCell {
						Image = ImageService.GetIcon ("project").ToNSImage (),
						Title = ConfigurationPlaceholder,
						Enabled = false,
					},
					new NSPathComponentCell {
						Image = ImageService.GetIcon ("device").ToNSImage (),
						Title = RuntimePlaceholder,
						Enabled = false,
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
						foreach (var runtime in RuntimeModel) {
							if (idx == -1 && runtime.DisplayString == ActiveRuntime.DisplayString)
								idx = i;

							var _runtime = runtime;
							if (runtime.IsSeparator)
								menu.AddItem (NSMenuItem.SeparatorItem);
							else
								menu.AddItem (new NSMenuItem (runtime.DisplayString, (o2, e2) => {
									string old = ActiveRuntime.FullDisplayString;
									ActiveRuntime = runtimeModel.First (r => r.FullDisplayString == _runtime.FullDisplayString);
									var ea = new HandledEventArgs ();
									if (RuntimeChanged != null)
										RuntimeChanged (o2, ea);

									if (ea.Handled)
										ActiveRuntime = runtimeModel.First (r => r.FullDisplayString == old);
								}) {
									IndentationLevel = runtime.IsIndented ? 2 : 1,
									Enabled = runtime.Enabled,
									Hidden = !runtime.Visible,
								});
							++i;
						}
					} else
						throw new NotSupportedException ();

					if (menu.Count > 1)
						menu.PopUpMenu (menu.ItemAt (idx), new CGPoint (componentRect.Left, componentRect.Top + 5), this);
				};
			}

			public override void ViewDidMoveToWindow ()
			{
				base.ViewDidMoveToWindow ();

				NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidChangeScreenNotification,
					notification => RealignTexts () );
				RealignTexts ();
			}

			void RealignTexts ()
			{
				// fix the icon alignment, move it slightly up
				// 1px on retina and non-retina, resulting in 0.5pt on retina
				var alignFix = new CGRect (0, -1 * Window.BackingScaleFactor, 16, 16);
				// Retina flag should be in Screen.ScaleFactor, but Screen needs to be an active screen
				// Also it needs to refresh on screen change, it seems you currently don't have an even listener for this event.
				// (I might be wrong :) )
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
					UpdatePathText (RuntimeIdx, value.FullDisplayString);
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
