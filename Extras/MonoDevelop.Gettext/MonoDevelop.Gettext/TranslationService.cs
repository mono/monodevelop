//
// TranslationService.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Text.RegularExpressions;

using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Gettext
{
	public class TranslationService
	{
		static bool isTranslationEnabled = false;
		
		internal static void Initialize ()
		{
			IdeApp.ProjectOperations.FileChangedInProject += new ProjectFileEventHandler (FileChangedInProject);
			IdeApp.ProjectOperations.CombineOpened += new CombineEventHandler (CombineOpened);
			IdeApp.ProjectOperations.CombineClosed += delegate {
				isTranslationEnabled = false;
			};
		}
		
		static TranslationProject GetTranslationProject ()
		{
			foreach (CombineEntry entry in IdeApp.ProjectOperations.CurrentOpenCombine.Entries) {
				if (entry is TranslationProject) {
					return (TranslationProject)entry;
				}
			}
			return null;
		}
		const string regex = @"translatable=""yes""\s*>(.*)</property>";
		static Regex steticTranslationPattern = new Regex(regex, RegexOptions.Compiled);
		
		static void UpdateSteticTranslations (TranslationProject translationProject, string fileName)
		{
			string text = File.ReadAllText (fileName);
			if (!String.IsNullOrEmpty (text)) {
				foreach (Match match in steticTranslationPattern.Matches (text)) {
					translationProject.AddTranslationString (match.Groups[1].Value);
				}
			}
		}
		
		static void UpdateTranslations (TranslationProject translationProject, string fileName)
		{
			string text = File.ReadAllText (fileName);
		}
		
		static void FileChangedInProject (object sender, ProjectFileEventArgs e)
		{
			if (!isTranslationEnabled)
				return;
			TranslationProject translationProject = GetTranslationProject ();
			if (translationProject == null)
				return;
			UpdateTranslations (translationProject, e.ProjectFile.FilePath);
			ProjectFile steticFile = e.Project.GetProjectFile (Path.Combine (e.Project.BaseDirectory, "gtk-gui/gui.stetic"));
			if (steticFile != null) 
				UpdateSteticTranslations (translationProject, steticFile.FilePath);
		}
		
		static void CombineOpened (object sender, CombineEventArgs e)
		{
			foreach (CombineEntry entry in e.Combine.Entries) {
				if (entry is TranslationProject) {
					isTranslationEnabled = true;
					return;
				}
			}
			isTranslationEnabled = false;
		}
	}
	
	public class TranslationServiceStartupCommand : CommandHandler
	{
		protected override void Run ()
		{
			TranslationService.Initialize ();
		}
	}
	
}