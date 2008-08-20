// TypeSelector.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using MonoDevelop.Core;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using Mono.Addins;

namespace MonoDevelop.AddinAuthoring
{
	
	
	[System.ComponentModel.Category("MonoDevelop.AddinAuthoring")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class TypeSelector : Gtk.Bin
	{
		bool allowCreate = true;
		bool allowCreateInterface = true;
		IClass[] typeList;
		DotNetProject project;
		bool loading;
		
		string newClassLabel = AddinManager.CurrentLocalizer.GetString ("(New Class)");
		string newInterfaceLabel = AddinManager.CurrentLocalizer.GetString ("(New Interface)");
		
		public event EventHandler Changed;
		
		public TypeSelector()
		{
			this.Build();
			FillCombo ();
			combo.Entry.Changed += OnEntryChanged;
		}
		
		public TypeSelector (DotNetProject project, string typeName): this ()
		{
			this.project = project;
			combo.Entry.Text = typeName;
			FillCombo ();
		}
		
		public DotNetProject Project {
			get { return project; }
			set {
				project = value; 
				FillCombo (); 
			}
		}
		
		public bool AllowCreate {
			get {
				return allowCreate; 
			}
			set {
				allowCreate = value;
				typeImage.Visible = value;
				FillCombo ();
			}
		}
		
		public IClass[] TypeList {
			get { 
				if (typeList == null && project != null) {
					IParserContext ctx = IdeApp.Workspace.ParserDatabase.GetProjectParserContext (project);
					typeList = ctx.GetProjectContents ();
				}
				return typeList; 
			}
			set {
				typeList = value;
				FillCombo ();
			}
		}

		public bool AllowCreateInterface {
			get {
				return allowCreateInterface;
			}
			set {
				allowCreateInterface = value;
				FillCombo ();
			}
		}
		
		public new string TypeName {
			get {
				return combo.Entry.Text;
			}
			set {
				combo.Entry.Text = value;
			}
		}
		
		void FillCombo ()
		{
			loading = true;
			
			((Gtk.ListStore)combo.Model).Clear ();
			if (allowCreate) {
				combo.AppendText (newClassLabel);
				if (allowCreateInterface)
					combo.AppendText (newInterfaceLabel);
			}
			
			if (TypeList == null) {
				UpdateIcon ();
				return;
			}
			
			foreach (IClass cls in TypeList) {
				if (cls.ClassType != ClassType.Class && cls.ClassType != ClassType.Interface)
					continue;
				combo.AppendText (cls.FullyQualifiedName);
			}
			UpdateIcon ();
			
			loading = false;
		}

		[GLib.ConnectBeforeAttribute]
		protected virtual void OnComboChanged (object sender, System.EventArgs e)
		{
			string newIcon = null;
			if (combo.Entry.Text == newClassLabel) {
				newIcon = "md-addinauthoring-newclass";
			}
			if (combo.Entry.Text == newInterfaceLabel) {
				newIcon = "md-addinauthoring-newinterface";
			}
			
			if (newIcon != null) {
				if (project.DefaultNamespace.Length > 0)
					combo.Entry.Text = project.DefaultNamespace + ".";
				eventbox.Remove (typeImage);
				typeImage = new Gtk.Image (newIcon, Gtk.IconSize.Menu);
				eventbox.Add (typeImage);
				eventbox.ShowAll ();
			} else {
				eventbox.Hide ();
			}
			if (!loading)
				OnChanged ();
		}
		
		bool IsKnownType ()
		{
			if (TypeList == null)
				return false;
			foreach (IClass cls in TypeList) {
				if (cls.ClassType != ClassType.Class && cls.ClassType != ClassType.Interface)
					continue;
				if (cls.FullyQualifiedName == TypeName)
					return true;
			}
			return false;
		}

		[GLib.ConnectBeforeAttribute]
		protected virtual void OnEntryChanged (object sender, System.EventArgs e)
		{
			UpdateIcon ();
		}
		
		void UpdateIcon ()
		{
			if (TypeName.Length == 0 || IsKnownType ()) {
				eventbox.Hide ();
 			} else {
				eventbox.Show ();
			}
		}
		
		public virtual void OnChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
	}
}
