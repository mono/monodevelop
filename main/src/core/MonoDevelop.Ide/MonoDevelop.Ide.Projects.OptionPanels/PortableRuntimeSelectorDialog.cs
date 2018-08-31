//
// PortableRuntimeSelectorDialog.cs
//
// Copyright (c) 2012 Xamarin Inc.
// Copyright (c) Microsoft Inc.
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
using System.Text;
using System.Linq;
using System.Collections.Generic;

using MonoDevelop.Components;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;

using Gtk;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	class PortableRuntimeSelectorDialog : Gtk.Dialog
	{
		public TargetFramework TargetFramework {
			get; private set;
		}

		readonly TargetFramework missingFramework;
		readonly List<TargetFramework> targetFrameworks;
		readonly SortedDictionary<string, List<SupportedFramework>> supportedFrameworks;
		readonly List<OptionCombo> options;

		HBox warningHBox;
		Label warning;
		ImageView warningImage;
		ImageView infoImage;
		ComboBox selectorCombo;
		bool disableEvents;

		class OptionComboItem
		{
			public readonly string Name;
			public readonly SupportedFramework Framework;

			public OptionComboItem (string name, SupportedFramework sfx)
			{
				this.Name = name;
				this.Framework = sfx;
			}
		}

		class OptionCombo
		{
			public readonly string Name;
			public IList<OptionComboItem> Items;
			public ComboBox Combo;
			public CheckButton Check;

			public OptionComboItem Current {
				get {
					if (Combo != null)
						return Items[Combo.Active];
					else
						return Items[0];
				}
			}

			public OptionCombo (string name)
			{
				Name = name;
			}
		}

		public PortableRuntimeSelectorDialog (TargetFramework initialTarget)
		{
			this.Title = GettextCatalog.GetString ("Change Targets");

			this.AddActionWidget (new Button (Stock.Cancel), ResponseType.Cancel);
			this.AddActionWidget (new Button (Stock.Ok), ResponseType.Ok);
			this.ActionArea.ShowAll ();

			this.TargetFramework = initialTarget;

			// Aggregate all SupportedFrameworks from .NETPortable TargetFrameworks
			targetFrameworks = GetPortableTargetFrameworks ().ToList ();
			targetFrameworks.Sort (CompareFrameworks);
			supportedFrameworks = new SortedDictionary<string, List<SupportedFramework>> ();

			if (!targetFrameworks.Contains (TargetFramework)) {
				missingFramework = TargetFramework;
				targetFrameworks.Insert (0, TargetFramework);
			}

			foreach (var fx in targetFrameworks) {
				foreach (var sfx in fx.SupportedFrameworks) {
					List<SupportedFramework> list;

					if (!supportedFrameworks.TryGetValue (sfx.DisplayName, out list)) {
						list = new List<SupportedFramework> ();
						supportedFrameworks.Add (sfx.DisplayName, list);
					}

					list.Add (sfx);
				}
			}

			// Now create a list of config options from our supported frameworks
			options = new List<OptionCombo> ();
			foreach (var fx in supportedFrameworks) {
				var combo = new OptionCombo (fx.Key);

				var dict = new SortedDictionary<string, OptionComboItem> ();
				foreach (var sfx in fx.Value) {
					var label = GetDisplayName (sfx);

					OptionComboItem item;
					if (!dict.TryGetValue (label, out item)) {
						item = new OptionComboItem (label, sfx);
						dict.Add (label, item);
					}
				}

				combo.Items = dict.Values.ToList ();

				options.Add (combo);
			}

			CreateUI ();

			CurrentProfileChanged (TargetFramework);
		}

		static int CompareFrameworks (TargetFramework x, TargetFramework y)
		{
			var p = CompareProfiles (x.Id.Profile, y.Id.Profile);
			if (p != 0)
				return p;
			return string.Compare (x.Id.Version, y.Id.Version, StringComparison.Ordinal);
		}

		static int CompareProfiles (string x, string y)
		{
			int xn, yn;
			if (TryParseProfileID (x, out xn)) {
				if (TryParseProfileID (y, out yn))
					return xn.CompareTo (yn);
				return 1;
			}
			if (TryParseProfileID (y, out yn))
				return -1;
			return string.Compare (x, y, StringComparison.Ordinal);
		}

		static bool TryParseProfileID (string profile, out int id)
		{
			if (profile != null && profile.StartsWith ("Profile", StringComparison.Ordinal))
				return int.TryParse (profile.Substring ("Profile".Length), out id);
			id = -1;
			return false;
		}

		static string GetDisplayName (SupportedFramework sfx)
		{
			if (!string.IsNullOrEmpty (sfx.MinimumVersionDisplayName))
				return sfx.DisplayName + " " + sfx.MinimumVersionDisplayName;
			else if (!string.IsNullOrEmpty (sfx.MonoSpecificVersionDisplayName))
				return sfx.DisplayName + " " + sfx.MonoSpecificVersionDisplayName;
			else
				return sfx.DisplayName;
		}

		static string GetShortName (SupportedFramework sfx)
		{
			switch (sfx.DisplayName) {
				case ".NET Framework":
					return "NET" + sfx.MinimumVersionDisplayName.Replace (".", "");
				case "Silverlight":
					return "SL" + sfx.MinimumVersionDisplayName;
				case "Xamarin.Android":
					return "Android";
				case ".NET for Windows Store apps":
					return "WinStore";
				case "Windows Phone":
					return "WP" + sfx.MinimumVersionDisplayName.Replace (".", "");
				case "Xbox 360":
					return "XBox";
				case "Xamarin.iOS":
					if (string.IsNullOrEmpty (sfx.MonoSpecificVersionDisplayName))
						return "iOS";
					else
						return "iOS/" + sfx.MonoSpecificVersion;
				default:
					return GetDisplayName (sfx);
			}
		}

		internal static string GetPclShortDisplayName (TargetFramework fx, bool markNotInstalled)
		{
			string shortName = string.IsNullOrEmpty (fx.Id.Profile)
				? fx.Id.Version
				: fx.Id.Version + " - " + fx.Id.Profile;

			if (markNotInstalled)
				return GettextCatalog.GetString ("PCL {0} - not installed", shortName);
			else
				return GettextCatalog.GetString ("PCL {0}", shortName);
		}

		IEnumerable<TargetFramework> GetPortableTargetFrameworks ()
		{
			return Runtime.SystemAssemblyService.GetTargetFrameworks ().Where (fx =>
				fx.Id.Identifier == ".NETPortable" &&
				fx.SupportedFrameworks.Count > 0
			);
		}

		void CreateUI ()
		{
			AddLabel (GettextCatalog.GetString ("Current Profile:"), 0);

			AddTopSelectorCombo ();

			AddLabel (GettextCatalog.GetString ("Target Frameworks:"), 18);

			// Add multi-option combo boxes first
			foreach (var opt in options) {
				if (opt.Items.Count > 1)
					AddMultiOptionCombo (opt);
			}

			// Now add the single-option check boxes
			foreach (var opt in options) {
				if (opt.Items.Count == 1)
					AddSingleOptionCheckbox (opt);
			}

			AddWarningLabel ();
		}

		void AddLabel (string text, uint top)
		{
			var label = new Label (text);
			label.SetAlignment (0.0f, 0.5f);
			label.Show ();

			var alignment = new Alignment (0.0f, 0.5f, 1.0f, 1.0f) {
				TopPadding = top,
				BottomPadding = 4
			};
			alignment.Add (label);
			alignment.Show ();

			VBox.PackStart (alignment, false, true, 0);
		}

		void AddTopSelectorCombo ()
		{
			var model = new ListStore (new Type[] { typeof (string) });
			var renderer = new CellRendererText ();
			var combo = selectorCombo = new ComboBox (model);

			for (int i = 0; i < targetFrameworks.Count; i++) {
				var fx = targetFrameworks[i];

				model.AppendValues (GetPclShortDisplayName (fx, fx == missingFramework));
				if (fx.Id.Equals (TargetFramework.Id))
					combo.Active = i;
			}

			combo.PackStart (renderer, true);
			combo.SetCellDataFunc (renderer, (l, c, m, i) => {
				((CellRendererText)c).Text = (string)model.GetValue (i, 0);
			});

			combo.Show ();

			combo.Changed += (sender, e) => {
				if (combo.Active >= 0)
					CurrentProfileChanged (targetFrameworks[combo.Active]);
			};

			var alignment = new Alignment (0.0f, 0.5f, 1.0f, 1.0f) {
				LeftPadding = 18,
				RightPadding = 18
			};
			alignment.Add (combo);

			alignment.Show ();

			VBox.PackStart (alignment, false, true, 0);
		}

		void AddMultiOptionCombo (OptionCombo option)
		{
			if (option.Items.Count < 2)
				throw new InvalidOperationException ();

			var model = new ListStore (new Type[] { typeof (string), typeof (object) });
			var renderer = new CellRendererText ();

			foreach (var item in option.Items) {
				var label = item.Name;
				var sfx = item.Framework;

				bool hasOtherVersions = false;
				foreach (var other in option.Items) {
					if (sfx == other.Framework)
						continue;
					if (!string.IsNullOrEmpty (other.Framework.MonoSpecificVersionDisplayName))
						continue;
					hasOtherVersions = true;
					break;
				}

				if (hasOtherVersions && string.IsNullOrEmpty (sfx.MonoSpecificVersionDisplayName))
					label += " or later";

				model.AppendValues (label);
			}

			option.Combo = new ComboBox (model);
			option.Check = new CheckButton ();

			option.Combo.PackStart (renderer, true);
			option.Combo.AddAttribute (renderer, "text", 0);

			option.Combo.Active = 0;

			option.Check.Show ();
			option.Combo.Show ();

			option.Combo.Changed += (sender, e) => {
				if (option.Check.Active)
					TargetFrameworkChanged (option);
			};
			option.Check.Toggled += (sender, e) => {
				TargetFrameworkChanged (option);
			};

			var hbox = new HBox ();
			hbox.PackStart (option.Check, false, false, 0);
			hbox.PackStart (option.Combo, true, true, 0);
			hbox.Show ();

			var alignment = new Alignment (0.0f, 0.5f, 1.0f, 1.0f) {
				LeftPadding = 18,
				RightPadding = 18
			};
			alignment.Add (hbox);
			alignment.Show ();

			VBox.PackStart (alignment, false, true, 0);
		}

		void AddSingleOptionCheckbox (OptionCombo option)
		{
			if (option.Items.Count != 1)
				throw new InvalidOperationException ();

			option.Check = new CheckButton (option.Items[0].Name);

			option.Check.Toggled += (sender, e) => {
				TargetFrameworkChanged (option);
			};

			option.Check.Show ();

			var alignment = new Alignment (0.0f, 0.5f, 1.0f, 1.0f) {
				LeftPadding = 18,
				RightPadding = 18
			};
			alignment.Add (option.Check);
			alignment.Show ();

			VBox.PackStart (alignment, false, true, 0);
		}

		void AddWarningLabel ()
		{
			var alignment = new Alignment (0.0f, 0.5f, 1.0f, 1.0f) {
				TopPadding = 8,
				LeftPadding = 18,
				RightPadding = 18
			};

			warning = new Label (GettextCatalog.GetString ("Test Error"));
			warning.SetAlignment (0.0f, 0.5f);
			warning.Show ();

			infoImage = new ImageView (Xwt.Drawing.Image.FromResource (GetType ().Assembly, "warning-16.png"));
			warningImage = new ImageView (Xwt.Drawing.Image.FromResource (GetType ().Assembly, "error-16.png"));

			warningHBox = new HBox (false, 6);
			warningHBox.PackStart (infoImage, false, false, 0);
			warningHBox.PackStart (warningImage, false, false, 0);
			warningHBox.PackStart (warning, false, true, 0);

			alignment.Child = warningHBox;
			alignment.Show ();

			VBox.PackStart (alignment, false, true, 0);
		}

		void ClearWarnings ()
		{
			warning.LabelProp = string.Empty;
			warningHBox.Hide ();

			infoImage.Hide ();
			warningImage.Hide ();
		}

		void SetWarning (string message)
		{
			warning.LabelProp = message;
			infoImage.Hide ();
			warningImage.Show ();
			warningHBox.Show ();
		}

		void SetWarning (string message, params object[] args)
		{
			SetWarning (string.Format (message, args));
		}

		void AddWarning (string message, params object[] args)
		{
			AddWarning (string.Format (message, args));
		}

		void AddWarning (string message)
		{
			if (!string.IsNullOrEmpty (warning.LabelProp))
				warning.LabelProp += Environment.NewLine;
			warning.LabelProp += message;
			infoImage.Hide ();
			warningImage.Show ();
			warningHBox.Show ();
		}

		void AddInfo (string message, params object[] args)
		{
			AddInfo (string.Format (message, args));
		}

		void AddInfo (string message)
		{
			if (string.IsNullOrEmpty (warning.LabelProp)) {
				warningImage.Hide ();
				infoImage.Show ();
			} else {
				warning.LabelProp += Environment.NewLine;
			}
			warning.LabelProp += message;
			warningHBox.Show ();
		}

		void TargetFrameworkChanged (OptionCombo option)
		{
			if (disableEvents)
				return;

			try {
				disableEvents = true;
				TargetFrameworkChanged_internal (option);
			} finally {
				disableEvents = false;
			}
		}

		void TargetFrameworkChanged_internal (OptionCombo option)
		{
			ClearWarnings ();
			selectorCombo.Active = -1;

			// The currently selected combo boxes.
			var selectedOptions = options.Where (o => o.Check.Active).ToList ();

			if (selectedOptions.Count < 2) {
				SetWarning (GettextCatalog.GetString ("Need to select at least two frameworks."));
				return;
			}

			// SupportedFramework from each of the currently selected combo boxes.
			var selectedFrameworks = selectedOptions.Select (s => s.Current.Framework).ToList ();
			SelectFrameworks (selectedFrameworks);
		}

		void SelectFrameworks (List<SupportedFramework> selectedFrameworks)
		{
			// Which TargetFramework's match these?
			var applicable = targetFrameworks.Where (
				f => IsApplicable (f, true, selectedFrameworks)).ToList ();

			if (applicable.Count == 0) {
				AddWarning (GettextCatalog.GetString ("No applicable frameworks for this selection!"));
				return;
			}

			//
			// 'applicable' contains all TargetFrameworks that match _at least_
			// the list of 'selectedFrameworks'.
			//
			// 'exactMatches' is where they do not contain any additional
			// (non-selected) 'SupportedFramework's.
			//

			var exactMatches = applicable.Where (
				a => IsApplicable (a, false, selectedFrameworks)).ToList ();
			if (exactMatches.Count == 1) {
				// Found an exact match.
				SelectFramework (exactMatches[0]);
				return;
			} else if (exactMatches.Count > 1) {
				// This should never happen.
				AddWarning (GettextCatalog.GetString ("Multiple frameworks match the current selection:"));
				exactMatches.ForEach (e => AddWarning ("     " + e.Id));
				AddWarning (GettextCatalog.GetString ("You must manually pick a profile in the drop-down selector."));
				// This is very bad UX, we should really disable "Ok" / add an "Apply"
				// button, but it's better than nothing.
				TargetFramework = exactMatches[0];
				return;
			}

			// Union of all the SupportedFrameworks from our applicable TargetFrameworks.
			var all = applicable.SelectMany (
				a => a.SupportedFrameworks).Distinct (SupportedFramework.EqualityComparer);

			// Minus the ones that we already selected.
			var extra = all.Where (a => !selectedFrameworks.Contains (a)).ToList ();

			// Are there any SupportedFrameworks that all our applicable TargetFrameworks
			// have in common?
			var common = extra.Where (
				e => applicable.All (a => a.SupportedFrameworks.Contains (e))).ToList ();

			if (common.Count == 0) {
				// Ok, the user must pick something.
				AddWarning (GettextCatalog.GetString ("Found multiple applicable frameworks, you need to select additional check boxes."));
				// Same here: randomly pick a profile to make "Ok" happy.
				TargetFramework = applicable[0];
				return;
			}

			AddInfo (GettextCatalog.GetString ("The following frameworks have been implicitly selected:"));
			AddInfo ("   " + string.Join (", ", common.Select (GetDisplayName)));

			// Implicitly select them.
			var implicitlySelected = new List<SupportedFramework> ();
			implicitlySelected.AddRange (selectedFrameworks);
			implicitlySelected.AddRange (common);

			// And let's try again ...
			SelectFrameworks (implicitlySelected);
		}

		void SelectOption (SupportedFramework sfx)
		{
			foreach (var option in options) {
				for (int i = 0; i < option.Items.Count; i++) {
					var item = option.Items[i];
					if (!item.Framework.Equals (sfx))
						continue;

					option.Check.Active = true;
					if (option.Combo != null)
						option.Combo.Active = i;
					return;
				}
			}

			throw new InvalidOperationException ();
		}

		void SelectFramework (TargetFramework framework)
		{
			var frameworks = targetFrameworks.Select ((t, i) => new { Framework = t, Index = i });
			var index = frameworks.First (t => t.Framework == framework).Index;
			selectorCombo.Active = index;
			TargetFramework = framework;
		}

		bool IsApplicable (TargetFramework fx, bool allowExtra, IEnumerable<SupportedFramework> selected)
		{
			return IsApplicable (fx, allowExtra, selected.ToArray ());
		}

		bool IsApplicable (TargetFramework fx, bool allowExtra, params SupportedFramework[] required)
		{
			if (fx == missingFramework)
				return false;

			var present = new List<SupportedFramework> (fx.SupportedFrameworks);
			var matches = required.All (present.Remove);
			return matches && (allowExtra || present.Count == 0);
		}

		void CurrentProfileChanged (TargetFramework framework)
		{
			if (disableEvents)
				return;

			try {
				disableEvents = true;
				CurrentProfileChanged_internal (framework);
				TargetFramework = framework;
			} finally {
				disableEvents = false;
			}
		}

		void CurrentProfileChanged_internal (TargetFramework framework)
		{
			ClearWarnings ();

			foreach (var option in options) {
				var sfx = framework.SupportedFrameworks.FirstOrDefault (
					s => s.DisplayName.Equals (option.Name));
				if (sfx == null) {
					option.Check.Active = false;
					continue;
				}

				option.Check.Active = true;

				if (option.Combo == null)
					continue;

				var label = GetDisplayName (sfx);
				for (int i = 0; i < option.Items.Count; i++) {
					if (!option.Items[i].Name.Equals (label))
						continue;
					option.Combo.Active = i;
					break;
				}
			}
		}
	}
}
