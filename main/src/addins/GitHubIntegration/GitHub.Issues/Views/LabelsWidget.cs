using System;
using System.Collections.Generic;

namespace GitHub.Issues
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class LabelsWidget : Gtk.Bin
	{
		#region Private Members

		private IssuesManager manager;
		private CommonControlsFactories commonControlsFactory;

		private Gtk.TreeView labelsTreeView;
		private Gtk.ListStore labelsStore;
		private Gtk.ColorSelection colorSelector;
		private Gtk.TextView labelNameTextBox;
		private Gtk.Button createNewLabelButton;
		private Gtk.Button saveLabelButton;
		private Gtk.Button deleteLabelButton;

		private Octokit.Label currentSelectedLabel;
		private Gtk.TreeIter currentSelectedLabelIterator;

		private EditMode mode = EditMode.Creation;

		#endregion

		#region Public Events

		#endregion

		#region Constructor

		public LabelsWidget ()
		{
			this.Build ();

			this.manager = new IssuesManager ();
			this.commonControlsFactory = new CommonControlsFactories ();

			IReadOnlyList<Octokit.Label> labels = this.manager.GetAllLabels ();

			Gtk.VBox screenContainer = new Gtk.VBox (false, 10);

			// Main horizontal split into 2 panels
			Gtk.HBox mainSplit = new Gtk.HBox ();
			Gtk.HBox headerContainer = new Gtk.HBox ();

			// ****************** Header panel *********************
			this.createNewLabelButton = this.CreateCreateNewLabelButton ();
			this.saveLabelButton = this.CreateSaveLabelButton ();
			this.deleteLabelButton = this.CreateDeleteLabelButton ();

			headerContainer.Add (this.createNewLabelButton);
			headerContainer.Add (this.saveLabelButton);
			headerContainer.Add (this.deleteLabelButton);

			// *************** Left labels panel *******************
			Gtk.VBox labelsPanel = new Gtk.VBox ();
			labelsPanel.Add (this.labelsTreeView = this.CreateLabelsTreeView (labels));

			// ************** Right details panel ******************
			Gtk.VBox detailsPanel = new Gtk.VBox ();
			Gtk.Table detailsTable = new Gtk.Table (2, 2, false);

			Gtk.AttachOptions xOptions = Gtk.AttachOptions.Fill;
			Gtk.AttachOptions yOptions = Gtk.AttachOptions.Expand;

			uint left = 1;
			uint right = 2;
			// First control using this can't use it as ++top like the others since we want to start at 0, -1 is not possible because its a uint
			uint top = 0;
			uint bottom = 0;

			// Label Name
			detailsTable.Attach (LayoutUtilities.LeftAlign (this.CreateLabelNameLabel ()), --left, --right, top, ++bottom, xOptions, Gtk.AttachOptions.Fill, 5, 5);
			detailsTable.Attach (LayoutUtilities.LeftAlign (this.labelNameTextBox = this.CreateLabelNameTextBox (this.currentSelectedLabel)), ++left, ++right, top, bottom, xOptions, yOptions, 5, 5);

			// Label Color
			detailsTable.Attach (LayoutUtilities.LeftAlign (this.CreateLabelColorLabel ()), --left, --right, ++top, ++bottom, xOptions, Gtk.AttachOptions.Fill, 5, 5);
			detailsTable.Attach (LayoutUtilities.LeftAlign (this.colorSelector = this.CreateColorPicker (this.currentSelectedLabel)), ++left, ++right, top, bottom, xOptions, yOptions, 5, 5);

			detailsPanel.PackStart (detailsTable, false, false, 0);

			// Compose screen
			mainSplit.PackStart (labelsPanel, false, false, 5);
			mainSplit.PackStart (detailsPanel, true, true, 5);

			screenContainer.PackStart (LayoutUtilities.LeftAlign (headerContainer), false, false, 0);
			screenContainer.PackStart (mainSplit, true, true, 0);

			this.Add (screenContainer);

			this.ShowAll ();
		}

		#endregion

		#region UI Components Creation

		/// <summary>
		/// Creates the labels tree view.
		/// </summary>
		/// <returns>The labels tree view.</returns>
		/// <param name="issue">Issue.</param>
		private Gtk.TreeView CreateLabelsTreeView (IReadOnlyList<Octokit.Label> labels)
		{
			Gtk.TreeView treeView = new Gtk.TreeView ();

			// Label column
			Gtk.CellRendererText labelRenderer = new Gtk.CellRendererText ();
			Gtk.TreeViewColumn labelColumn = new Gtk.TreeViewColumn ("Label", labelRenderer, "text", 1);

			// Color column
			Gtk.CellRendererText colorRenderer = new Gtk.CellRendererText ();
			Gtk.TreeViewColumn colorColumn = new Gtk.TreeViewColumn ("Color", colorRenderer, "text", 2);

			treeView.AppendColumn (labelColumn);
			treeView.AppendColumn (colorColumn);

			labelColumn.SetCellDataFunc (labelRenderer, new Gtk.TreeCellDataFunc (this.ColorLabelRow));
			colorColumn.SetCellDataFunc (colorRenderer, new Gtk.TreeCellDataFunc (this.ColorLabelRow));

			treeView.ModifyBase (Gtk.StateType.Normal, new Gdk.Color (245, 245, 245));

			treeView.Model = this.labelsStore = this.CreateLabelsTreeListStore (labels);

			// When the selection changes we need to locate the correct Octokit.Label instance
			treeView.CursorChanged += (object sender, EventArgs e) => {
				treeView.Selection.SelectedForeach (new Gtk.TreeSelectionForeachFunc ((Gtk.TreeModel model, Gtk.TreePath path, Gtk.TreeIter iter) => {
					Octokit.Label selectedLabel = (Octokit.Label)model.GetValue (iter, 0);

					this.currentSelectedLabelIterator = iter;
					this.SetSelectedLabel (selectedLabel);
				}));
			};

			return treeView;
		}

		/// <summary>
		/// Creates the label name label.
		/// </summary>
		/// <returns>The label name label.</returns>
		private Gtk.Label CreateLabelNameLabel ()
		{
			return this.commonControlsFactory.CreateLabel (StringResources.LabelName);
		}

		/// <summary>
		/// Creates the label name text box.
		/// </summary>
		/// <returns>The label name text box.</returns>
		/// <param name="label">Label.</param>
		private Gtk.TextView CreateLabelNameTextBox (Octokit.Label label)
		{
			return this.commonControlsFactory.CreateTextBox (label != null ? label.Name : string.Empty, 1, 450);
		}

		/// <summary>
		/// Creates the label color label.
		/// </summary>
		/// <returns>The label color label.</returns>
		private Gtk.Label CreateLabelColorLabel ()
		{
			return this.commonControlsFactory.CreateLabel (StringResources.LabelColor);
		}

		/// <summary>
		/// Creates the color picker.
		/// </summary>
		/// <returns>The color picker.</returns>
		/// <param name="label">Label.</param>
		private Gtk.ColorSelection CreateColorPicker (Octokit.Label label)
		{
			Gtk.ColorSelection colorPicker = new Gtk.ColorSelection ();

			// Fall back on Red if label is null
			string color = label != null ? label.Color : "ff0000";

			System.Drawing.Color colorRGB = System.Drawing.ColorTranslator.FromHtml ('#' + color);
			colorPicker.CurrentColor = new Gdk.Color (colorRGB.R, colorRGB.G, colorRGB.B);
			colorPicker.PreviousColor = new Gdk.Color (colorRGB.R, colorRGB.G, colorRGB.B);

			return colorPicker;
		}

		/// <summary>
		/// Creates the create new label button.
		/// </summary>
		/// <returns>The create new label button.</returns>
		private Gtk.Button CreateCreateNewLabelButton ()
		{
			return this.commonControlsFactory.CreateButton (StringResources.New, this.CreateLabelButtonHandler);
		}

		/// <summary>
		/// Creates the save label button.
		/// </summary>
		/// <returns>The save label button.</returns>
		private Gtk.Button CreateSaveLabelButton ()
		{
			return this.commonControlsFactory.CreateButton (StringResources.Save, this.SaveLabelButtonHandler);
		}

		/// <summary>
		/// Creates the delete label button.
		/// </summary>
		/// <returns>The delete label button.</returns>
		private Gtk.Button CreateDeleteLabelButton ()
		{
			return this.commonControlsFactory.CreateButton (StringResources.Delete, this.DeleteLabelButtonHandler);
		}

		#endregion

		#region UI Helpers

		/// <summary>
		/// Colors the label row.
		/// </summary>
		/// <param name="column">Column.</param>
		/// <param name="cell">Cell.</param>
		/// <param name="model">Model.</param>
		/// <param name="iterator">Iterator.</param>
		private void ColorLabelRow (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iterator)
		{
			String color = (String)model.GetValue (iterator, 2);

			if (!string.IsNullOrEmpty (color)) {
				try {
					System.Drawing.Color colorRGB = System.Drawing.ColorTranslator.FromHtml ('#' + color);
					cell.CellBackgroundGdk = new Gdk.Color (colorRGB.R, colorRGB.G, colorRGB.B);
				} catch (Exception) {
				}
			}
		}

		/// <summary>
		/// Creates the labels tree list store.
		/// </summary>
		/// <returns>The labels tree list store.</returns>
		/// <param name="labels">Labels.</param>
		private Gtk.ListStore CreateLabelsTreeListStore (IReadOnlyList<Octokit.Label> labels)
		{
			Gtk.ListStore store = new Gtk.ListStore (typeof(Octokit.Label), typeof(String), typeof(String));

			foreach (Octokit.Label label in labels) {
				this.AddToLabelsListStore (store, label);
			}

			return store;
		}

		/// <summary>
		/// Adds to labels list store.
		/// </summary>
		/// <returns>Iterator to the newly added label</returns>
		/// <param name="store">Store.</param>
		/// <param name="label">Label.</param>
		private Gtk.TreeIter AddToLabelsListStore (Gtk.ListStore store, Octokit.Label label)
		{
			return store.AppendValues (label, label.Name, label.Color);
		}

		/// <summary>
		/// Sets the selected label.
		/// </summary>
		/// <param name="labelToSelect">Label to select.</param>
		private void SetSelectedLabel (Octokit.Label labelToSelect)
		{
			this.currentSelectedLabel = labelToSelect;

			// Fall back to nothing if label is null
			this.labelNameTextBox.Buffer.Text = labelToSelect != null ? labelToSelect.Name : string.Empty;

			// Fall back on Red if label is null
			string color = labelToSelect != null ? labelToSelect.Color : "ff0000";

			System.Drawing.Color colorRGB = System.Drawing.ColorTranslator.FromHtml ('#' + color);
			colorSelector.CurrentColor = new Gdk.Color (colorRGB.R, colorRGB.G, colorRGB.B);
			colorSelector.PreviousColor = new Gdk.Color (colorRGB.R, colorRGB.G, colorRGB.B);

			this.mode = EditMode.Edit;
		}

		/// <summary>
		/// RGB to Hex;
		/// </summary>
		/// <returns>Hex.</returns>
		/// <param name="color">Color.</param>
		private string RGBToHex (Gdk.Color color)
		{
			string hex = color.Red.ToString ("X2").Substring (0, 2) + color.Green.ToString ("X2").Substring (0, 2) + color.Blue.ToString ("X2").Substring (0, 2);

			return hex.ToLower ();
		}

		#endregion

		#region Event Handlers

		/// <summary>
		/// Click handler for the create label button
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void CreateLabelButtonHandler (object sender, EventArgs e)
		{
			this.labelNameTextBox.Buffer.Text = StringResources.NewLabelTitle;
			this.mode = EditMode.Creation;
		}

		/// <summary>
		/// Click handler for the save button
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void SaveLabelButtonHandler (object sender, EventArgs e)
		{
			if (this.mode == EditMode.Creation) {
				this.CreateLabel ();
				this.mode = EditMode.Edit;
			} else if (this.mode == EditMode.Edit) {
				this.UpdateLabel ();
			}
		}

		/// <summary>
		/// Click handler for the delete button
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void DeleteLabelButtonHandler (object sender, EventArgs e)
		{
			this.DeleteLabel ();
		}

		#endregion

		#region Save, Create and Delete

		/// <summary>
		/// Creates the label.
		/// </summary>
		private void CreateLabel ()
		{
			Octokit.Label label = this.manager.CreateLabel (this.labelNameTextBox.Buffer.Text, this.RGBToHex (this.colorSelector.CurrentColor));

			this.currentSelectedLabelIterator = this.AddToLabelsListStore (this.labelsStore, label);

			this.SetSelectedLabel (label);
		}

		/// <summary>
		/// Updates the label.
		/// </summary>
		private void UpdateLabel ()
		{
			Octokit.Label label = this.manager.UpdateLabel (this.currentSelectedLabel, this.labelNameTextBox.Buffer.Text, this.RGBToHex (this.colorSelector.CurrentColor));

			// Update the values in the list store
			this.labelsStore.SetValue (this.currentSelectedLabelIterator, 0, label);
			this.labelsStore.SetValue (this.currentSelectedLabelIterator, 1, label.Name);
			this.labelsStore.SetValue (this.currentSelectedLabelIterator, 2, label.Color);
		}

		/// <summary>
		/// Deletes the currently selected label.
		/// </summary>
		private void DeleteLabel ()
		{
			this.manager.DeleteLabel (this.currentSelectedLabel);
		}

		#endregion
	}
}