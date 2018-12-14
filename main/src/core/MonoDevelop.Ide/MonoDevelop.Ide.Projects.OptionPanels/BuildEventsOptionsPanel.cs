//
// BuildEventsOptionsPanel.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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

using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;
using Xwt;
using Xwt.Accessibility;
using Xwt.Formats;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	class BuildEventsOptionsPanel : ItemOptionsPanel
	{
		static readonly string RunPostBuildEventPropertyName = "RunPostBuildEvent";
		static readonly string PostBuildEventPropertyName = "PostBuildEvent";
		static readonly string PreBuildEventPropertyName = "PreBuildEvent";

		XwtControl control;
		BuildEventsWidget widget;
		string originalRunPostBuildEvent;

		public override Control CreatePanelWidget ()
		{
			if (control == null) {
				widget = new BuildEventsWidget ();
				control = new XwtControl (widget);

				var project = (DotNetProject)ConfiguredProject;
				widget.SelectedRunPostBuildEvent = project.ProjectProperties.GetValue (RunPostBuildEventPropertyName);
				originalRunPostBuildEvent = widget.SelectedRunPostBuildEvent;

				widget.PreBuildEventText = GetPreBuildEventProperty (project)?.Value;
				widget.PostBuildEventText = GetPostBuildEventProperty (project)?.Value;
			}
			return control;
		}

		public override void ApplyChanges ()
		{
			var project = (DotNetProject)ConfiguredProject;

			if (originalRunPostBuildEvent != widget.SelectedRunPostBuildEvent) {
				project.ProjectProperties.SetValue (RunPostBuildEventPropertyName, widget.SelectedRunPostBuildEvent);
			}

			UpdateBuildEvent (project, PreBuildEventPropertyName, widget.PreBuildEventText);
			UpdateBuildEvent (project, PostBuildEventPropertyName, widget.PostBuildEventText);
		}

		static void UpdateBuildEvent (DotNetProject project, string propertyName, string buildEventText)
		{
			MSBuildProperty buildEventProperty = GetBuildEventProperty (project, propertyName);
			if (buildEventProperty != null) {
				buildEventProperty.SetValue (buildEventText);
			} else if (!string.IsNullOrEmpty (buildEventText)) {
				MSBuildPropertyGroup propertyGroup = project.MSBuildProject.CreatePropertyGroup ();
				propertyGroup.SetValue (propertyName, buildEventText);
				project.MSBuildProject.AddLastChild (propertyGroup);
			}
		}

		static MSBuildProperty GetPostBuildEventProperty (DotNetProject project)
		{
			return GetBuildEventProperty (project, PostBuildEventPropertyName);
		}

		static MSBuildProperty GetPreBuildEventProperty (DotNetProject project)
		{
			return GetBuildEventProperty (project, PreBuildEventPropertyName);
		}

		static MSBuildProperty GetBuildEventProperty (DotNetProject project, string propertyName)
		{
			foreach (MSBuildPropertyGroup propertyGroup in project.MSBuildProject.PropertyGroups) {
				if (string.IsNullOrEmpty (propertyGroup.Condition)) {
					MSBuildProperty buildEventProperty = propertyGroup.GetProperty (propertyName, null);
					if (buildEventProperty != null) {
						return buildEventProperty;
					}
				}
			}

			return null;
		}
	}

	class BuildEventsWidget : Widget
	{
		RichTextView preBuildEventText;
		RichTextView postBuildEventText;
		ComboBox postBuildEventComboBox;

		RunPostBuildEventType [] runPostBuildEventTypes = {
			new RunPostBuildEventType ("Always", GettextCatalog.GetString ("Always")),
			new RunPostBuildEventType ("OnBuildSuccess", GettextCatalog.GetString ("On successful build")),
			new RunPostBuildEventType ("OnOutputUpdated", GettextCatalog.GetString ("When the build updates the project output")),
		};

		public BuildEventsWidget ()
		{
			Build ();
		}

		void Build ()
		{
			var mainVBox = new VBox ();
			mainVBox.Accessible.Role = Role.Filler;

			var preBuildEventLabel = new Label ();
			preBuildEventLabel.Text = GettextCatalog.GetString ("Pre-build event command line");
			mainVBox.PackStart (preBuildEventLabel);

			preBuildEventText = new RichTextView ();
			preBuildEventText.HeightRequest = 200;
			preBuildEventText.ReadOnly = false;
			preBuildEventText.SetCommonAccessibilityAttributes (
				"BuildEvents.PreBuildEventCommandText",
				preBuildEventLabel,
				GettextCatalog.GetString ("Enter the pre-build command"));
			mainVBox.PackStart (preBuildEventText);

			var postBuildEventLabel = new Label ();
			postBuildEventLabel.Text = GettextCatalog.GetString ("Post-build event command line");
			postBuildEventLabel.MarginTop = 25;
			mainVBox.PackStart (postBuildEventLabel);

			postBuildEventText = new RichTextView ();
			postBuildEventText.HeightRequest = 200;
			postBuildEventText.ReadOnly = false;
			postBuildEventText.SetCommonAccessibilityAttributes (
				"BuildEvents.PostBuildEventCommandText",
				postBuildEventLabel,
				GettextCatalog.GetString ("Enter the post-build command"));
			mainVBox.PackStart (postBuildEventText);

			var postBuildEventRunHBox = new HBox ();
			postBuildEventRunHBox.Accessible.Role = Role.Filler;
			var postBuildEventRunLabel = new Label ();
			postBuildEventRunLabel.Text = GettextCatalog.GetString ("Run the post-build event:");
			postBuildEventRunHBox.PackStart (postBuildEventRunLabel);

			postBuildEventComboBox = new ComboBox ();
			postBuildEventRunHBox.PackStart (postBuildEventComboBox);
			postBuildEventComboBox.SetCommonAccessibilityAttributes (
				"BuildEvents.PostBuildEventCommandText",
				postBuildEventRunLabel,
				GettextCatalog.GetString ("Choose when to run the post-build event."));
			mainVBox.PackStart (postBuildEventRunHBox);

			foreach (var item in runPostBuildEventTypes) {
				postBuildEventComboBox.Items.Add (item);
			}
			// Select on successful build by default.
			postBuildEventComboBox.SelectedIndex = 1;

			Content = mainVBox;
		}

		public string SelectedRunPostBuildEvent {
			get {
				var item = (RunPostBuildEventType)postBuildEventComboBox.SelectedItem;
				return item.Value;
			}
			set {
				foreach (var item in runPostBuildEventTypes) {
					if (item.Value == value) {
						postBuildEventComboBox.SelectedItem = item;
						return;
					}
				}
			}
		}

		public string PostBuildEventText {
			get {
				return postBuildEventText.PlainText;
			}
			set {
				postBuildEventText.LoadText (value ?? string.Empty, TextFormat.Plain);
			}
		}

		public string PreBuildEventText {
			get {
				return preBuildEventText.PlainText;
			}
			set {
				preBuildEventText.LoadText (value ?? string.Empty, TextFormat.Plain);
			}
		}

		class RunPostBuildEventType
		{
			public RunPostBuildEventType (string value, string description)
			{
				Value = value;
				Description = description;
			}

			public string Description { get; private set; }
			public string Value { get; private set; }

			public override string ToString ()
			{
				return Description;
			}
		}
	}
}
