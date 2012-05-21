// 
// MainToolbar.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using Gtk;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Components;
using Cairo;


namespace MonoDevelop.Compontents.MainToolbar
{
	public class MainToolbar: Gtk.EventBox
	{
		HBox contentBox = new HBox (false, 6);

		ComboBox configurationCombo;
		TreeStore configurationStore = new TreeStore (typeof(string), typeof(string));

		ComboBox runtimeCombo;
		TreeStore runtimeStore = new TreeStore (typeof(string), typeof(TargetRuntime));

		StatusArea statusArea;

		SearchEntry matchEntry;

		public Cairo.ImageSurface Background {
			get;
			set;
		}

		public int TitleBarHeight {
			get;
			set;
		}

		public Cairo.Color BottomColor {
			get;
			set;
		}


		/*
		internal class SelectActiveRuntimeHandler : CommandHandler
		{
			protected override void Update (CommandArrayInfo info)
			{
			}
	
			protected override void Run (object dataItem)
			{
			}
		}
		*/
		public MainToolbar ()
		{
			IdeApp.Workspace.ActiveConfigurationChanged += (sender, e) => UpdateConfigurations ();
			IdeApp.Workspace.ActiveRuntimeChanged += (sender, e) => UpdateConfigurations ();
			IdeApp.Workspace.ConfigurationsChanged += (sender, e) => UpdateConfigurations ();

			IdeApp.Workspace.ActiveRuntimeChanged += (sender, e) => UpdateRuntimes ();
			IdeApp.Workspace.RuntimesChanged += (sender, e) => UpdateRuntimes ();

			IdeApp.Workspace.SolutionLoaded += (sender, e) => UpdateCombos ();
			IdeApp.Workspace.SolutionUnloaded += (sender, e) => UpdateCombos ();
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;
			contentBox.BorderWidth = 6;
			var button = new RoundButton ();
			button.Clicked += (sender, e) => IdeApp.CommandService.DispatchCommand (ProjectCommands.Run);
			AddWidget (button);

			configurationCombo = new Gtk.ComboBox ();
			configurationCombo.SetSizeRequest (150, -1);
			configurationCombo.Model = configurationStore;
			var ctx = new Gtk.CellRendererText ();
			configurationCombo.PackStart (ctx, true);
			configurationCombo.AddAttribute (ctx, "text", 0);

			var configurationComboVBox = new VBox ();
			configurationComboVBox.PackStart (configurationCombo, true, false, 0);
			AddWidget (configurationComboVBox);

			runtimeCombo = new Gtk.ComboBox ();
			runtimeCombo.SetSizeRequest (150, -1);
			runtimeCombo.Model = runtimeStore;
			runtimeCombo.PackStart (ctx, true);
			runtimeCombo.AddAttribute (ctx, "text", 0);

			var runtimeComboVBox = new VBox ();
			runtimeComboVBox.PackStart (runtimeCombo, true, false, 0);
			AddWidget (runtimeComboVBox);


			ButtonBar buttonBar = new ButtonBar ();

			buttonBar.Add (new LazyImage ("icoDebug-Pause.png"));
			buttonBar.Add (new LazyImage ("icoDebug-StepOver.png"));
			buttonBar.Add (new LazyImage ("icoDebug-StepIn.png"));
			buttonBar.Add (new LazyImage ("icoDebug-StepOut.png"));

			var buttonBarVBox = new VBox ();
			buttonBarVBox.PackStart (buttonBar, true, false, 0);
			AddWidget (buttonBarVBox);

			statusArea = new StatusArea ();

			var statusAreaVBox = new VBox ();
			statusAreaVBox.PackStart (statusArea, true, false, 0);

			contentBox.PackStart (statusAreaVBox, true, true, 0);

			this.matchEntry = new SearchEntry ();
			this.matchEntry.EmptyMessage = GettextCatalog.GetString ("Press Control + , for search.");
			this.matchEntry.Ready = true;
			this.matchEntry.Visible = true;
			this.matchEntry.IsCheckMenu = true;
			this.matchEntry.SetSizeRequest (240, 26);
			var searchEntryComboVBox = new VBox ();
			searchEntryComboVBox.PackStart (this.matchEntry, true, false, 0);
			AddWidget (searchEntryComboVBox);

			Add (contentBox);
			UpdateCombos ();
		}

		void HandleRuntimeChanged (object sender, EventArgs e)
		{
			TreeIter iter;
			if (!runtimeStore.GetIterFromString (out iter, runtimeCombo.Active.ToString ()))
				return;
			var runtime = (TargetRuntime)runtimeStore.GetValue (iter, 1);
			IdeApp.Workspace.ActiveRuntime = runtime;
		}

		void HandleConfigurationChanged (object sender, EventArgs e)
		{
			TreeIter iter;
			if (!configurationStore.GetIterFromString (out iter, configurationCombo.Active.ToString ()))
				return;
			var config = (string)configurationStore.GetValue (iter, 1);
			IdeApp.Workspace.ActiveConfigurationId = config;
		}

		void UpdateCombos ()
		{
			UpdateConfigurations ();
			UpdateRuntimes ();
		}

		void UpdateConfigurations ()
		{
			configurationCombo.Changed -= HandleConfigurationChanged;
			configurationStore.Clear ();
			if (!IdeApp.Workspace.IsOpen)
				return;
			int selected = -1;
			int i = 0;
			foreach (var conf in IdeApp.Workspace.GetConfigurations ()) {
				configurationStore.AppendValues (conf, conf);
				if (conf == IdeApp.Workspace.ActiveConfigurationId)
					selected = i;
				i++;
			}
			if (selected >= 0)
				configurationCombo.Active = selected;
			configurationCombo.Changed += HandleConfigurationChanged;
		}

		void UpdateRuntimes ()
		{
			runtimeCombo.Changed -= HandleRuntimeChanged;
			runtimeStore.Clear ();
			if (!IdeApp.Workspace.IsOpen || Runtime.SystemAssemblyService.GetTargetRuntimes ().Count () == 0)
				return;
			int selected = -1;
			int i = 0;

			foreach (var tr in Runtime.SystemAssemblyService.GetTargetRuntimes ()) {
				runtimeStore.AppendValues (tr.DisplayName, tr);
				if (tr == IdeApp.Workspace.ActiveRuntime)
					selected = i;
				i++;
			}
			if (selected >= 0)
				runtimeCombo.Active = selected;
			runtimeCombo.Changed += HandleRuntimeChanged;
		}

		public void AddWidget (Gtk.Widget widget)
		{
			contentBox.PackStart (widget, false, false, 0);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				context.Rectangle (
					evnt.Area.X,
					evnt.Area.Y,
					evnt.Area.Width,
					evnt.Area.Height
				);
				context.Clip ();
				context.LineWidth = 1;
				if (Background != null && Background.Width > 0) {
					for (int x=0; x < Allocation.Width; x += Background.Width) {
						Background.Show (context, x, -TitleBarHeight);
					}
				} else {
					context.Rectangle (0, 0, Allocation.Width, Allocation.Height);
					var lg = new LinearGradient (0, 0, 0, Allocation.Height);
					lg.AddColorStop (0, (HslColor)Style.Light (StateType.Normal));
					lg.AddColorStop (1, (HslColor)Style.Mid (StateType.Normal));
					context.Pattern = lg;
					context.Fill ();

				}
				context.MoveTo (0, Allocation.Bottom + 0.5);
				context.LineTo (Allocation.Width + evnt.Area.Width, Allocation.Bottom + 0.5);
				context.Color = BottomColor;
				context.Stroke ();
			}
			return base.OnExposeEvent (evnt);
		}
	}
}

