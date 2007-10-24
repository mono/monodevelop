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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Gettext
{
	public class TranslationService
	{
		static bool isTranslationEnabled = false;
		
		public static bool IsTranslationEnabled {
			get {
				return isTranslationEnabled;
			}
			set {
				isTranslationEnabled = value;
			}
		}
		static bool isInitialized = false;
		internal static void InitializeTranslationService ()
		{
			Debug.Assert (!isInitialized);
			isInitialized = true;
//			IdeApp.ProjectOperations.FileChangedInProject += new ProjectFileEventHandler (FileChangedInProject);
			IdeApp.ProjectOperations.CombineOpened += new CombineEventHandler (CombineOpened);
			IdeApp.ProjectOperations.CombineClosed += delegate {
				isTranslationEnabled = false;
			};
		}
		
		static TranslationProject GetTranslationProject (Project p)
		{
			foreach (CombineEntry entry in p.ParentCombine.Entries) {
				if (entry is TranslationProject) {
					return (TranslationProject)entry;
				}
			}
			return null;
		}
		
		static int GetLineNumber (string text, int index)
		{
			int result = 0;
			for (int i = 0; i < index; i++)
				if (text[i] == '\n')
					result++;
			return result;
		}
		
		static Regex xmlTranslationPattern = new Regex(@"_[^""]*=\s*""([^""]*)""", RegexOptions.Compiled);
		static void UpdateXmlTranslations (TranslationProject translationProject, string fileName)
		{
			string text = File.ReadAllText (fileName);
			if (!String.IsNullOrEmpty (text)) {
				List<TranslationProject.MatchLocation> matches = new List<TranslationProject.MatchLocation> ();
				foreach (Match match in xmlTranslationPattern.Matches (text)) {
					matches.Add (new TranslationProject.MatchLocation (match.Groups[1].Value, GetLineNumber (text, match.Index)));
				}
				translationProject.AddTranslationStrings (fileName, matches);
			}
		}
		
		static Regex translationPattern = new Regex(@"GetString\s*\(\s*""([^""]*)""\s*\)", RegexOptions.Compiled);
		static Regex pluralTranslationPattern = new Regex(@"GetPluralString\s*\(\s*""([^""]*)""\s*,\s*""([^""]*)""\s*,.*\)", RegexOptions.Compiled);
		
		static void UpdateTranslations (TranslationProject translationProject, string fileName)
		{
			string text = File.ReadAllText (fileName);
			if (!String.IsNullOrEmpty (text)) {
//				List<TranslationProject.MatchLocation> matches = new List<TranslationProject.MatchLocation> ();
//				for (int i = 0; i + 9 < text.Length; i++) {
//					i = text.IndexOf ("Get", i);
//					if (i < 0)
//						break;
//					if (text.Substring (i + 3, 6) == "String") {
//						Match m = translationPattern.Match (text, i);
//						if (m.Success) 
//							matches.Add (new TranslationProject.MatchLocation (m.Groups[1].Value, GetLineNumber (text, m.Index)));
//					} else if (i + 15 < text.Length && text.Substring (i + 3, 12) == "PluralString") {
//						Match m = pluralTranslationPattern.Match (text, i);
//						if (m.Success) 
//							matches.Add (new TranslationProject.MatchLocation (m.Groups[1].Value, m.Groups[2].Value, GetLineNumber (text, m.Index)));
//					}
//				}
				
				List<TranslationProject.MatchLocation> matches = new List<TranslationProject.MatchLocation> ();
				foreach (Match match in translationPattern.Matches (text)) {
					matches.Add (new TranslationProject.MatchLocation (match.Groups[1].Value, GetLineNumber (text, match.Index)));
				}
				foreach (Match match in pluralTranslationPattern.Matches (text)) {
					matches.Add (new TranslationProject.MatchLocation (match.Groups[1].Value, match.Groups[2].Value, GetLineNumber (text, match.Index)));
				}
				translationProject.AddTranslationStrings (fileName, matches);
			}
		}
		
		public static void UpdateTranslation (TranslationProject translationProject, string fileName, IProgressMonitor monitor)
		{
			if (!File.Exists (fileName)) {
				Runtime.LoggingService.Warn ((object)String.Format (GettextCatalog.GetString ("UpdateTranslation: File {0} not found."), fileName));
				return;
			}
			translationProject.BeginUpdate ();
			try {
				switch (Path.GetExtension (fileName)) {
				case ".xml":
					UpdateXmlTranslations (translationProject, fileName);
					break;
				default:
					UpdateTranslations (translationProject, fileName);
					break;
				}
			} finally {
				translationProject.EndUpdate ();
			}
		}
		
// Currently de-activated due to performance reasons.
//		static void FileChangedInProject (object sender, ProjectFileEventArgs e)
//		{
//			TranslationProject translationProject = GetTranslationProject (e.Project);
//			if (translationProject == null)
//				return;
//			translationProject.BeginUpdate ();
//			try {
//				UpdateTranslation (translationProject, e.ProjectFile.FilePath, null);
//				
//				ProjectFile steticFile = e.Project.GetProjectFile (Path.Combine (e.Project.BaseDirectory, "gtk-gui/gui.stetic"));
//				if (steticFile != null) 
//					UpdateSteticTranslations (translationProject, steticFile.FilePath);
//			} finally {
//				translationProject.EndUpdate ();
//			}
//		}
		
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
			TranslationService.InitializeTranslationService ();
		}
	}
}