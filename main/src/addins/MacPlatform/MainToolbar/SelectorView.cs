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
using MonoDevelop.Components.Mac;
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
	class SelectorView : NSFocusButton
	{
		public event EventHandler<EventArgs> SizeChanged;
		internal const int RunConfigurationIdx = 0;
		internal const int ConfigurationIdx = 1;
		internal const int RuntimeIdx = 2;

		internal const int SeparatorWidth = 10;

		internal PathSelectorView RealSelectorView { get; private set; }

		public SelectorView ()
		{
			Cell = new ColoredButtonCell ();
			BezelStyle = NSBezelStyle.TexturedRounded;
			Title = "";

			var nsa = (INSAccessibility)this;
			nsa.AccessibilityElement = false;

			RealSelectorView = new PathSelectorView (new CGRect (6, 0, 1, 1));
			RealSelectorView.UnregisterDraggedTypes ();
			AddSubview (RealSelectorView);

			// Disguise this NSButton as a group
			AccessibilityRole = NSAccessibilityRoles.GroupRole;

			// For some reason AddSubview hasn't added RealSelectorView as an accessibility child of SelectorView
			nsa.AccessibilityChildren = new NSObject [] { RealSelectorView };
		}

		public override CGSize SizeThatFits (CGSize size)
		{
			var fitSize = RealSelectorView.SizeThatFits (size);

			return new CGSize (Math.Round (fitSize.Width) + 12.0, size.Height);
		}

		public override void SetFrameSize (CGSize newSize)
		{
			base.SetFrameSize (newSize);
			RealSelectorView.SetFrameSize (newSize);
		}

		public override void ViewDidMoveToWindow ()
		{
			base.ViewDidMoveToWindow ();
			UpdateLayout ();
		}

		void UpdateLayout ()
		{
			// Correct the offset position for the screen
			nfloat yOffset = 1f;
			if (Window?.Screen?.BackingScaleFactor == 2) {
				yOffset = 0.5f;
			}

			RealSelectorView.Frame = new CGRect (RealSelectorView.Frame.X, yOffset, RealSelectorView.Frame.Width, RealSelectorView.Frame.Height);
		}

		public override void DidChangeBackingProperties ()
		{
			base.DidChangeBackingProperties ();
			UpdateLayout ();
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
				RunConfigurationShown = 0x4,
				AllShown = 0x7,
			}

			static readonly string RunConfigurationPlaceholder = GettextCatalog.GetString ("Default");
			static readonly string ConfigurationPlaceholder = GettextCatalog.GetString ("Default");
			static readonly string RuntimePlaceholder = GettextCatalog.GetString ("Default");
			static readonly string RunConfigurationIdentifier = "RunConfiguration";
			static readonly string ConfigurationIdentifier = "Configuration";
			static readonly string RuntimeIdentifier = "Runtime";

			static nfloat iconSize = 28;
			nfloat AddCellSize (int cellId, nfloat totalWidth, nfloat layoutWidth, nfloat allIconsWidth)
			{
				var cellWidth = GetRequiredWidthForPathCell (cellId);

				if (totalWidth + cellWidth + allIconsWidth < layoutWidth) {
					UpdatePathText (cellId, GetTextForCell (cellId));
					return cellWidth;
				}

				UpdatePathText (cellId, string.Empty);
				return iconSize;
			}

			int IndexFromIdentifier (string identifier)
			{
				int i = 0;
				foreach (var cell in Cells) {
					if (cell.Identifier == identifier) {
						return i;
					}
					i++;
				}

				throw new Exception ($"No cell with {identifier} found");
			}

			public override CGSize SizeThatFits (CGSize size)
			{
				int n = 0;
				nfloat totalWidth = SeparatorWidth * VisibleCells.Length - 1;
				nfloat allIconsWidth = iconSize * VisibleCells.Length;

				allIconsWidth -= iconSize;
				totalWidth += AddCellSize (LastSelectedCell, totalWidth, size.Width, allIconsWidth);

				for (;n < VisibleCells.Length; n++) {
					var cellId = VisibleCellIds [n];
					if (cellId == LastSelectedCell)
						continue;
					
					allIconsWidth -= iconSize;
					totalWidth += AddCellSize (cellId, totalWidth, size.Width, allIconsWidth);
				}
				return new CGSize (totalWidth, size.Height);
			}

			string EllipsizeString (string s)
			{
				if (s.Length > 50) {
					var start = s.Substring (0, 20);
					var end = s.Substring (s.Length - 20, 20);

					return start + "…" + end;
				} else {
					return s;
				}
			}

			string GetTextForCell (int cellId)
			{
				switch (cellId) {
					case ConfigurationIdx: return TextForActiveConfiguration;
					case RunConfigurationIdx: return TextForActiveRunConfiguration;
					case RuntimeIdx: return TextForRuntimeConfiguration;
				}
				throw new NotSupportedException ();
			}

			string TextForActiveConfiguration {
				get {
					return EllipsizeString (ActiveConfiguration != null ? ActiveConfiguration.DisplayString : ConfigurationPlaceholder);
				}
			}

			string TextForActiveRunConfiguration {
				get {
					return EllipsizeString (ActiveRunConfiguration != null ? ActiveRunConfiguration.DisplayString : RunConfigurationPlaceholder);
				}
			}

			string TextForRuntimeConfiguration {
				get {
					if (ActiveRuntime != null) {
						using (var mutableModel = ActiveRuntime.GetMutableModel ())
							return EllipsizeString (mutableModel.FullDisplayString);
					} else {
						return EllipsizeString (RuntimePlaceholder);
					}
				}
			}

			nfloat GetRequiredWidthForPathCell (int cellId)
			{
				var cell = Cells [cellId];
				return new NSAttributedString (GetTextForCell (cellId), new NSStringAttributes { Font = cell.Font }).Size.Width + iconSize;
			}

			nfloat GetWidthForPathCell (int cellId)
			{
				var cell = Cells [cellId];
				return new NSAttributedString (cell.Title, new NSStringAttributes { Font = cell.Font }).Size.Width + iconSize;
			}

			NSMenu CreateSubMenuForRuntime (IRuntimeModel runtime)
			{
				if (!runtime.Children.Any ())
					return null;

				var menu = new NSMenu {
					AutoEnablesItems = false,
					ShowsStateColumn = true,
					Font = NSFont.MenuFontOfSize (12),
				};
				foreach (var item in runtime.Children)
					if (item.IsSeparator)
						menu.AddItem (NSMenuItem.SeparatorItem);
					else
						CreateMenuItem (menu, item);
				return menu;
			}

			NSMenuItem CreateMenuItem (NSMenu menu, IRuntimeModel runtime)
			{
				NSMenuItem menuItem;
				string runtimeFullDisplayString;

				using (var mutableModel = runtime.GetMutableModel ()) {
					runtimeFullDisplayString = mutableModel.FullDisplayString;

					menuItem = new NSMenuItem () {
						IndentationLevel = runtime.IsIndented ? 1 : 0,
						AttributedTitle = new NSAttributedString (mutableModel.DisplayString, new NSStringAttributes {
							Font = runtime.Notable ? NSFontManager.SharedFontManager.ConvertFont (menu.Font, NSFontTraitMask.Bold) : menu.Font,
						}),
						Enabled = mutableModel.Enabled,
						Hidden = !mutableModel.Visible,
					};
					if (!string.IsNullOrEmpty (runtime.Image)) {
						menuItem.Image = ImageService.GetIcon (runtime.Image).ToNSImage ();
					}
					if (!string.IsNullOrEmpty (runtime.Tooltip)) {
						menuItem.ToolTip = runtime.Tooltip;
					}
					if (ActiveRuntime == runtime || (ActiveRuntime?.Children.Contains (runtime) ?? false)) {
						menuItem.State = NSCellStateValue.On;
					}
				}

				var subMenu = CreateSubMenuForRuntime (runtime);
				if (subMenu != null) {
					menuItem.Submenu = subMenu;
					menuItem.Enabled = true;
				} else {
					menuItem.Activated += (o2, e2) => {
						ActiveRuntime = runtime;
					};
				}
				menu.AddItem (menuItem);
				return menuItem;
			}

			class NSPathComponentCellFocusable:NSPathComponentCell
			{
				public bool HasFocus { set; get; }
				public override void DrawWithFrame (CGRect cellFrame, NSView inView)
				{
					if (HasFocus) {
						var focusRect = new CGRect (cellFrame.X , cellFrame.Y + 3, cellFrame.Width+2, cellFrame.Height - 6);
						var path = NSBezierPath.FromRoundedRect (focusRect, 3, 3);
						path.LineWidth = 2f;
						NSColor.KeyboardFocusIndicator.SetStroke ();
						path.Stroke ();
					}
					base.DrawWithFrame (cellFrame, inView);
				}
			}

			NSPathComponentCellFocusable [] Cells;
			NSPathComponentCellFocusable [] VisibleCells;
			int [] VisibleCellIds;

			int lastSelectedCell;
			int LastSelectedCell {
				get { return lastSelectedCell; }
				set {
					lastSelectedCell = value;
					SetFrameSize (Frame.Size);
				}
			}

			NSImage projectImage = MultiResImage.CreateMultiResImage ("project", "");
			NSImage projectImageDisabled = MultiResImage.CreateMultiResImage ("project", "disabled");
			NSImage deviceImage = MultiResImage.CreateMultiResImage ("device", "");
			NSImage deviceImageDisabled = MultiResImage.CreateMultiResImage ("device", "disabled");

			string lastDeviceIconId;
			NSImage lastDeviceImage;
			NSImage lastDeviceImageDisabled;
			public PathSelectorView (CGRect frameRect) : base (frameRect)
			{
				Cells = new [] {
					new NSPathComponentCellFocusable {
						Image = projectImageDisabled,
						Title = TextForActiveRunConfiguration,
						Enabled = false,
						Identifier = RunConfigurationIdentifier
					},
					new NSPathComponentCellFocusable {
						Image = projectImageDisabled,
						Title = TextForActiveConfiguration,
						Enabled = false,
						Identifier = ConfigurationIdentifier
					},
					new NSPathComponentCellFocusable {
						Image = deviceImageDisabled,
						Title = TextForRuntimeConfiguration,
						Enabled = false,
						Identifier = RuntimeIdentifier
					}
				};
				SetVisibleCells (RunConfigurationIdx, ConfigurationIdx, RuntimeIdx);

				UpdateStyle ();

				BackgroundColor = NSColor.Clear;
				FocusRingType = NSFocusRingType.None;

				Ide.Gui.Styles.Changed += UpdateStyle;

				var nsa = (INSAccessibility)this;
				nsa.AccessibilityIdentifier = "ConfigurationSelector";
				nsa.AccessibilityLabel = GettextCatalog.GetString ("Configuration Selector");
				nsa.AccessibilityHelp = GettextCatalog.GetString ("Set the project runtime configuration");
			}

			void SetVisibleCells (params int[] ids)
			{
				VisibleCellIds = ids;
				LastSelectedCell = ids [0];
				VisibleCells = new NSPathComponentCellFocusable [ids.Length];
				for (int n = 0; n < ids.Length; n++)
					VisibleCells [n] = Cells [ids [n]];
				PathComponentCells = VisibleCells;
			}

			int IndexOfCellAtX (nfloat x)
			{
				nfloat cx = 0;
				for (int n = 0; n < VisibleCells.Length; n++) {
					var cellWidth = GetWidthForPathCell (VisibleCellIds [n]);
					if (x > cx && x <= cx + cellWidth)
						return VisibleCellIds [n];
					cx += cellWidth;
					if (x >= cx && x < cx + SeparatorWidth)
						// The > in the middle
						return -1;
					cx += SeparatorWidth;
				}
				return -1;
			}

			public override bool AccessibilityPerformShowMenu ()
			{
				if (ClickedPathComponentCell == null) {
					return false;
				}

				PopupMenuForCell (ClickedPathComponentCell);

				return true;
			}

			public override void MouseDown (NSEvent theEvent)
			{
				if (!Enabled)
					return;

				// Can't use ClickedPathComponentCell here because it is only set on MouseUp
				var locationInView = ConvertPointFromView (theEvent.LocationInWindow, null);

				var cellIdx = IndexOfCellAtX (locationInView.X);
				if (cellIdx == -1) {
					return;
				}

				var item = Cells [cellIdx];
				if (item == null || !item.Enabled)
					return;

				PopupMenuForCell (item);
			}

			int focusedCellIndex = 1;
			NSPathComponentCellFocusable focusedItem;

			public override void KeyDown (NSEvent theEvent)
			{
				if(theEvent.Characters == " ")
				{
					var item = Cells [focusedCellIndex];
					PopupMenuForCell (item);
					return;
				}

				if (theEvent.Characters == "\t") {
					focusedCellIndex++;
					if(focusedCellIndex > VisibleCells.Count ()){
						if (NextKeyView != null) {
							Window.MakeFirstResponder (NextKeyView);
							SetSelection ();
							focusedCellIndex = 1;
							focusedItem = null;
							return;
						}
					}
				}

				SetSelection ();
				base.KeyDown (theEvent);
			}

			void SetSelection ()
			{
				if (focusedItem != null) {
					focusedItem.HasFocus = false;
				}
				if (focusedCellIndex < Cells.Count ()) {
					var item = Cells [focusedCellIndex];
					focusedItem = item;
					item.HasFocus = true;
				}
				SetNeedsDisplay ();
			}

			public override bool BecomeFirstResponder ()
			{
				SetSelection ();
				return base.BecomeFirstResponder ();
			}

			void PopupMenuForCell (NSPathComponentCell item)
			{
				var componentRect = ((NSPathCell)Cell).GetRect (item, Frame, this);
				int i = 0;

				NSMenuItem selectedItem = null;
				var menu = new NSMenu {
					AutoEnablesItems = false,
					ShowsStateColumn = true,
					Font = NSFont.MenuFontOfSize (12),
				};

				if (item.Identifier == RunConfigurationIdentifier) {
					if (ActiveRunConfiguration == null)
						return;

					foreach (var configuration in RunConfigurationModel) {

						var _configuration = configuration;
						var menuitem = new NSMenuItem (configuration.DisplayString, (o2, e2) => {
							ActiveRunConfiguration = runConfigurationModel.First (c => c.OriginalId == _configuration.OriginalId);
						}) {
							Enabled = true,
							IndentationLevel = 1,
						};

						menu.AddItem (menuitem);

						if (selectedItem == null && configuration.OriginalId == ActiveRunConfiguration.OriginalId)
							selectedItem = menuitem;
					}
				} else if (item.Identifier == ConfigurationIdentifier) {
					if (ActiveConfiguration == null)
						return;

					foreach (var configuration in ConfigurationModel) {

						var _configuration = configuration;
						var menuitem = new NSMenuItem (configuration.DisplayString, (o2, e2) => {
							ActiveConfiguration = configurationModel.First (c => c.OriginalId == _configuration.OriginalId);
						}) {
							Enabled = true,
							IndentationLevel = 1,
						};

						menu.AddItem (menuitem);

						if (selectedItem == null && configuration.OriginalId == ActiveConfiguration.OriginalId)
							selectedItem = menuitem;
					}
				} else if (item.Identifier == RuntimeIdentifier) {
					if (ActiveRuntime == null)
						return;

					using (var activeMutableModel = ActiveRuntime.GetMutableModel ()) {
						foreach (var runtime in RuntimeModel) {
							NSMenuItem menuitem = null;
							if (runtime.IsSeparator)
								menu.AddItem (NSMenuItem.SeparatorItem);
							else
								menuitem = CreateMenuItem (menu, runtime);

							using (var mutableModel = runtime.GetMutableModel ()) {
								if (selectedItem == null && menuitem != null && mutableModel.DisplayString == activeMutableModel.DisplayString)
									selectedItem = menuitem;
							}

							++i;
						}
					}
				} else
					throw new NotSupportedException ();
				
				LastSelectedCell = IndexFromIdentifier (item.Identifier);
				if (menu.Count > 1) {
					var offs = new CGPoint (componentRect.Left + 3, componentRect.Top + 3);

					if (Window?.Screen?.BackingScaleFactor == 2)
						offs.Y += 0.5f; // fine tune menu position on retinas

					menu.PopUpMenu (selectedItem, offs, this);
				}
			}

			public override void DidChangeBackingProperties ()
			{
				base.DidChangeBackingProperties ();

				// Force a redraw because NSPathControl does not redraw itself when switching to a different resolution
				// and the icons need redrawn
				NeedsDisplay = true;
			}

			void UpdateStyle (object sender = null, EventArgs e = null)
			{
				Cells [RunConfigurationIdx].TextColor = Styles.BaseForegroundColor.ToNSColor ();
				Cells [ConfigurationIdx].TextColor = Styles.BaseForegroundColor.ToNSColor ();
				Cells [RuntimeIdx].TextColor = Styles.BaseForegroundColor.ToNSColor ();

				UpdateImages ();
			}

			protected override void Dispose (bool disposing)
			{
				if (disposing)
					Ide.Gui.Styles.Changed -= UpdateStyle;
				base.Dispose (disposing);
			}

			public override void ViewDidMoveToWindow ()
			{
				base.ViewDidMoveToWindow ();

				NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidChangeBackingPropertiesNotification,
				                                                notification => Runtime.RunInMainThread ((Action) RealignTexts));
				RealignTexts ();
			}

			void RealignTexts ()
			{
				if (Window == null)
					return;

				// fix the icon alignment, move it slightly up
				var alignFix = new CGRect (0, Window.BackingScaleFactor == 2 ? -0.5f : -1f, 16, 16);
				Cells [RunConfigurationIdx].Image.AlignmentRect = alignFix;
				Cells [ConfigurationIdx].Image.AlignmentRect = alignFix;
				Cells [RuntimeIdx].Image.AlignmentRect = alignFix;
			}

			void UpdatePathText (int idx, string text)
			{
				Cells [idx].Title = text;
				UpdateImages ();
			}

			static NSImage FixImageServiceImage (Xwt.Drawing.Image image, double scale, string[] styles)
			{
				NSImage result = image.WithStyles (styles).ToBitmap (scale).ToNSImage ();
				result.Template = true;
				return result;
			}

			NSImage GetDeviceImage (bool enabled)
			{
				if (ActiveRuntime == null || string.IsNullOrEmpty (ActiveRuntime.Image))
					return enabled ? deviceImage : deviceImageDisabled;
				if (ActiveRuntime.Image == lastDeviceIconId)
					return enabled ? lastDeviceImage : lastDeviceImageDisabled;

				lastDeviceIconId = ActiveRuntime.Image;
				var scale = GtkWorkarounds.GetScaleFactor (Ide.IdeApp.Workbench.RootWindow);
				Xwt.Drawing.Image baseIcon = ImageService.GetIcon (ActiveRuntime.Image, Gtk.IconSize.Menu);

				string [] styles, disabledStyles;
				if (IdeApp.Preferences.UserInterfaceTheme == Theme.Dark) {
					styles = new [] { "dark" };
					disabledStyles = new [] { "dark", "disabled" };
				} else {
					styles = null;
					disabledStyles = new [] { "disabled" };
				}

				lastDeviceImage = FixImageServiceImage (baseIcon, scale, styles);
				lastDeviceImageDisabled = FixImageServiceImage (baseIcon, scale, disabledStyles);
				return enabled ? lastDeviceImage : lastDeviceImageDisabled;
			}

			void UpdateImages ()
			{
				NSImage runConfigImage = projectImage;
				NSImage configImage = projectImage;
				NSImage runtimeImage = GetDeviceImage (Cells [RuntimeIdx].Enabled);

				if (!Cells [RunConfigurationIdx].Enabled)
					runConfigImage = projectImageDisabled;

				if (!Cells [ConfigurationIdx].Enabled)
					configImage = projectImageDisabled;

				// HACK
				// For some reason NSPathControl does not like the images that ImageService provides. To use them it requires
				// ToBitmap() to be called first. But a second problem is that ImageService only seems to provide a single resolution
				// for its icons. It may be related to the images being initially loaded through the Gtk backend and then converted to NSImage
				// at a later date.
				// For whatever reason, we custom load the images here through NSImage, providing both 1x and 2x image reps.
				Cells [RunConfigurationIdx].Image = runConfigImage;
				Cells [ConfigurationIdx].Image = configImage;;
				Cells [RuntimeIdx].Image = runtimeImage;
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
					if (activeConfiguration == value)
						return;
					activeConfiguration = value;
					if (ConfigurationChanged != null)
						ConfigurationChanged (this, EventArgs.Empty);
					UpdatePathText (ConfigurationIdx, value.DisplayString);
					OnSizeChanged ();
				}
			}

			IRunConfigurationModel activeRunConfiguration;
			public IRunConfigurationModel ActiveRunConfiguration {
				get { return activeRunConfiguration; }
				set {
					if (activeRunConfiguration == value)
						return;
					activeRunConfiguration = value;
					if (RunConfigurationChanged != null)
						RunConfigurationChanged (this, EventArgs.Empty);
					UpdatePathText (RunConfigurationIdx, value.DisplayString);
					OnSizeChanged ();
				}
			}

			IRuntimeModel activeRuntime;
			public IRuntimeModel ActiveRuntime {
				get { return activeRuntime; }
				set {
					if (activeRuntime == value)
						return;
					var old = ActiveRuntime;

					activeRuntime = value;
					var ea = new HandledEventArgs ();
					if (RuntimeChanged != null)
						RuntimeChanged (this, ea);
					
					if (ea.Handled) {
						activeRuntime = old;

						// Do not update the runtime if we don't change it.
						return;
					}

					using (var mutableModel = value.GetMutableModel ()) {
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
						UpdatePathText (ConfigurationIdx, ConfigurationPlaceholder);
						activeConfiguration = null;
					}
					Cells [ConfigurationIdx].Enabled = count > 1;
					OnSizeChanged ();
				}
			}

			IEnumerable<IRunConfigurationModel> runConfigurationModel;
			public IEnumerable<IRunConfigurationModel> RunConfigurationModel {
				get { return runConfigurationModel; }
				set {
					runConfigurationModel = value;
					int count = value.Count ();
					if (count == 0) {
						UpdatePathText (RunConfigurationIdx, RunConfigurationPlaceholder);
						activeRunConfiguration = null;
					}
					Cells [RunConfigurationIdx].Enabled = count > 1;
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
						UpdatePathText (RuntimeIdx, RuntimePlaceholder);
						activeRuntime = null;
					}
					Cells [RuntimeIdx].Enabled = count > 1;
					OnSizeChanged ();
				}
			}

			public event EventHandler ConfigurationChanged;
			public event EventHandler RunConfigurationChanged;
			public event EventHandler<HandledEventArgs> RuntimeChanged;

			public override bool Enabled {
				get {
					return base.Enabled;
				}
				set {
					base.Enabled = value;

					if (value) {
						Cells [RuntimeIdx].Enabled = runtimeModel.Count () > 1;
						Cells [ConfigurationIdx].Enabled = configurationModel.Count () > 1;
						Cells [RunConfigurationIdx].Enabled = runConfigurationModel.Count () > 1;
					}
				}
			}

			public bool RunConfigurationVisible {
				get {
					return PathComponentCells.Length == 3;
				}
				set {
					if (value)
						SetVisibleCells (RunConfigurationIdx, ConfigurationIdx, RuntimeIdx);
					else
						SetVisibleCells (ConfigurationIdx, RuntimeIdx);
				}
			}

			public bool PlatformSensitivity {
				set {
					Cells [SelectorView.RuntimeIdx].Enabled = value;
				}
			}

		}
		#endregion
	}
}
