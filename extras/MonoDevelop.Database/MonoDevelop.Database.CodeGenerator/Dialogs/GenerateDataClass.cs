// 
// GenerateDataClass.cs
//  
// Author:
//       Luciano N. Callero <lnc19@hotmail.com>
// 
// Copyright (c) 2010 Lucian0
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
using Gtk;
using System;
using System.IO;
using MonoDevelop.Database.Sql;
using MonoDevelop.Projects;

namespace MonoDevelop.Database.CodeGenerator
{
	public partial class GenerateDataClass : Gtk.Dialog
	{
		
		public GenerateDataClass ()
		{
			this.Build ();
			if (comboProject.SelectedProject != null)
				OnComboProjectChanged (comboProject, new EventArgs ());
		}
		
		protected virtual void OnComboProviderConnectionSelectedDatabaseChanged (object sender, System.EventArgs e)
		{
			
			DatabaseConnectionContext ctx = comboProviderConnection.DatabaseConnection;
			if (ctx != null) {
				if (!(ctx.DbFactory is IDbLinq)){
					labelMessage.Markup = string.Concat ("<span color=\"red\">", 
					                                     AddinCatalog.GetString (string.Concat ("The <b>selected ",
					                                                                            "connection</b> does ",
					                                                                            "not have a valid ",
					                                                                            "provider to complete ",
					                                                                            "this operation.")),
					                                     "</span>");
				} else {
					checkSprocs.Sensitive = ((IDbLinq)ctx.DbFactory).HasProcedures;
					labelMessage.Markup = "";
				}
			}
			Validate ();
		}
		
		public void GenerateClass ()
		{
			string output;
			
			if (comboProject.SelectedDirectory != null)
				output = System.IO.Path.Combine (comboProject.SelectedDirectory, entryFile.Text);
			else {
				FileInfo info = new FileInfo (comboProject.SelectedProject.FileName);
				output = System.IO.Path.Combine (info.Directory.FullName, entryFile.Text);
			}
			bool generated = false;
			DatabaseConnectionContext ctx = comboProviderConnection.DatabaseConnection;
			if (comboOutput.ActiveText.Equals (AddinCatalog.GetString ("Code"), 
			                                   StringComparison.InvariantCultureIgnoreCase) || 
			    comboOutput.ActiveText.Equals (AddinCatalog.GetString ("Code & DBML"), 
			                                   StringComparison.InvariantCultureIgnoreCase) )
				generated = (ctx.DbFactory as IDbLinq).Generate (ctx.ConnectionSettings,
                                     comboOutput.ActiveText,
                                     output,
				                     comboLanguage.ActiveText,
				                     comboStyle.ActiveText,
                                     entryNamespace.Text,
                                     entryEntityBase.Text,
                                     entryEntityAttr.Text,
                                     entryMemberAttr.Text,
                                     entryGenerateType.Text,
                                     entryCulture.Text,
                                     checkSchema.Active,
                                     checkGenerateTimestamps.Active,
                                     checkEqualsAndHash.Active,
                                     checkSprocs.Active,
                                     checkPluralize.Active);
			else
				generated = (ctx.DbFactory as IDbLinq).Generate (ctx.ConnectionSettings,
                                     comboOutput.ActiveText,
                                     output,
                                     entryNamespace.Text,
                                     entryEntityBase.Text,
                                     entryEntityAttr.Text,	
                                     entryMemberAttr.Text,
                                     entryGenerateType.Text,
                                     entryCulture.Text,
                                     checkSchema.Active,
                                     checkGenerateTimestamps.Active,
                                     checkEqualsAndHash.Active,
                                     checkSprocs.Active,
                                     checkPluralize.Active);

			if (generated) {
				comboProject.SelectedProject.AddFile (output);
					// Add references to the project if they do not exist
					string[] references = { 
						"System.Data, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
						"System.Data.Linq, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
					};
				ProjectReference gacRef;
				DotNetProject project = ((DotNetProject)comboProject.SelectedProject);
				foreach(string refName in references) {
					string targetName = project.TargetRuntime.AssemblyContext.GetAssemblyNameForVersion (refName, null, project.TargetFramework);
					if (targetName != null) {
						gacRef = new ProjectReference (ReferenceType.Gac, refName);
						if (!project.References.Contains (gacRef))
							project.References.Add (gacRef);
					}
				}
			}
		}
		
		protected void Validate ()
		{
			if (comboProviderConnection.DatabaseConnection == null)
				buttonOk.Sensitive = false;
			else if (!(comboProviderConnection.DatabaseConnection.DbFactory is IDbLinq)
			    || comboProject.SelectedProject == null)
				buttonOk.Sensitive = false;
			else
				buttonOk.Sensitive = true;
			
		}
		
		protected virtual void OnComboOutputChanged (object sender, System.EventArgs e)
		{
			if (comboOutput.Active == 0 || comboOutput.Active == 2) {
				comboLanguage.Sensitive = true;
				comboStyle.Sensitive = true;
				comboLanguage.Active = 0;
				comboStyle.Active = 0;
			} else {
				entryFile.Text = string.Concat(System.IO.Path.GetFileNameWithoutExtension (entryFile.Text), ".dbml");
				comboLanguage.Sensitive = false;
				comboStyle.Sensitive = false;
				comboLanguage.Active = -1;
				comboStyle.Active = -1;
			}
			Validate ();
		}
		
		protected virtual void OnComboLanguageChanged (object sender, System.EventArgs e)
		{
			if (comboLanguage.ActiveText == "VB")
				entryFile.Text = string.Concat(System.IO.Path.GetFileNameWithoutExtension (entryFile.Text), ".vb");
			else if (comboLanguage.ActiveText != null)
				entryFile.Text = string.Concat(System.IO.Path.GetFileNameWithoutExtension (entryFile.Text), ".cs");
			Validate ();
		}
		
		protected virtual void OnComboProjectChanged (object sender, System.EventArgs e)
		{
			if (comboProject.SelectedProject != null)
				if (comboProject.SelectedDirectory != null)
					entryNamespace.Text = string.Concat (((DotNetProject)comboProject.SelectedProject).DefaultNamespace, 
					                                ".",
					                                GetNamespace(comboProject.SelectedProject.ItemDirectory.FullPath, 
					                                                  comboProject.SelectedDirectory), ".DbLinq");
			else
				entryNamespace.Text = string.Concat (((DotNetProject)comboProject.SelectedProject).DefaultNamespace,
				                                     ".DbLinq");
			Validate ();
		}
		
		private string GetNamespace (string projectPath, string folder)
		{
			return folder.Substring (projectPath.Length+1).Replace ('/', '.');
		}
		
		
	}
}

