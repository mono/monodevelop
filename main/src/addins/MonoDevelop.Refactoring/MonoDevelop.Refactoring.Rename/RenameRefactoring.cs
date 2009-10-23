// 
// Rename.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using Mono.TextEditor;
using Mono.TextEditor.PopupWindow;
using System.Text;

namespace MonoDevelop.Refactoring.Rename
{
	public class RenameRefactoring : RefactoringOperation
	{
		public override string AccelKey {
			get {
				return IdeApp.CommandService.GetCommandInfo (RefactoryCommands.Rename, null).AccelKey.Replace ("dead_circumflex", "^");
			}
		}
		
		public RenameRefactoring ()
		{
			Name = "Rename";
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			if (options.SelectedItem is LocalVariable || options.SelectedItem is IParameter)
				return true;

			if (options.SelectedItem is IType)
				return ((IType)options.SelectedItem).SourceProject != null;

			if (options.SelectedItem is IMember) {
				IType cls = ((IMember)options.SelectedItem).DeclaringType;
				return cls != null && cls.SourceProject != null;
			}
			return false;
		}
		
		public override string GetMenuDescription (RefactoringOptions options)
		{
			return IdeApp.CommandService.GetCommandInfo (RefactoryCommands.Rename, null).Text;
		}
		
		internal static Mono.TextEditor.TextEditor GetEditor (Gtk.Widget widget)
		{
			if (widget is Mono.TextEditor.TextEditor)
				return (Mono.TextEditor.TextEditor)widget;
			Gtk.Container container = widget as Gtk.Container;
			if (container != null) {
				foreach (var child in container.Children) {
					Mono.TextEditor.TextEditor editor = GetEditor (child);
					if (editor != null)
						return editor;
				}
			}
			return null;
		}
		
		public override void Run (RefactoringOptions options)
		{
			if (options.SelectedItem is LocalVariable || options.SelectedItem is IParameter) {
				MemberReferenceCollection col = GetReferences (options);
				if (col == null)
					return;
				TextEditorData data = options.GetTextEditorData ();

				Mono.TextEditor.TextEditor editor = GetEditor (options.Document.ActiveView.Control);
				if (editor == null) {
					RenameItemDialog dialog = new RenameItemDialog (options, this);
					dialog.TransientFor = MonoDevelop.Ide.Gui.IdeApp.Workbench.RootWindow;
					dialog.Show ();
					return;
				}

				
				List<TextLink> links = new List<TextLink> ();
				TextLink link = new TextLink ("name");
				int baseOffset = Int32.MaxValue;
				foreach (MemberReference r in col) {
					baseOffset = Math.Min (baseOffset, data.Document.LocationToOffset (r.Line - 1, r.Column - 1));
				}
				foreach (MemberReference r in col) {
					link.AddLink (new Segment (data.Document.LocationToOffset (r.Line - 1, r.Column - 1) - baseOffset, r.Name.Length));
				}

				links.Add (link);
				TextLinkEditMode tle = new TextLinkEditMode (editor, baseOffset, links);
				tle.SetCaretPosition = false;
				if (tle.ShouldStartTextLinkMode) {
					tle.OldMode = data.CurrentMode;
					tle.StartMode ();
					data.CurrentMode = tle;
				}
			} else {
				RenameItemDialog dialog = new RenameItemDialog (options, this);
				dialog.TransientFor = MonoDevelop.Ide.Gui.IdeApp.Workbench.RootWindow;
				dialog.Show ();
			}
		}
		
		public class RenameProperties
		{
			public string NewName {
				get;
				set;
			}
			
			public bool RenameFile {
				get;
				set;
			}
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			RenameProperties properties = (RenameProperties)prop;
			List<Change> result = new List<Change> ();

			MemberReferenceCollection col = GetReferences (options);
			if (col == null)
				return result;
			
			if (properties.RenameFile && options.SelectedItem is IType) {
				IType cls = (IType)options.SelectedItem;
				int currentPart = 1;
				HashSet<string> alreadyRenamed = new HashSet<string> ();
				foreach (IType part in cls.Parts) {
					if (alreadyRenamed.Contains (part.CompilationUnit.FileName))
						continue;
					alreadyRenamed.Add (part.CompilationUnit.FileName);
					
					string oldFileName = System.IO.Path.GetFileNameWithoutExtension (part.CompilationUnit.FileName);
					string newFileName;
					
					int idx = oldFileName.IndexOf (cls.Name);
					if (idx >= 0) {
						newFileName = oldFileName.Substring (0, idx) + properties.NewName + oldFileName.Substring (idx + cls.Name.Length);
					} else {
						newFileName = currentPart != 1 ? properties.NewName + currentPart : properties.NewName;
						currentPart++;
					}
					
					int t = 0;
					while (System.IO.File.Exists (GetFullFileName (newFileName, part.CompilationUnit.FileName, t))) {
						t++;
					}
					result.Add (new RenameFileChange (part.CompilationUnit.FileName, GetFullFileName (newFileName, part.CompilationUnit.FileName, t)));
				}
			}
			
			foreach (MemberReference memberRef in col) {
				TextReplaceChange change = new TextReplaceChange ();
				change.FileName = memberRef.FileName;
				change.Offset = memberRef.Position;
				change.RemovedChars = memberRef.Name.Length;
				change.InsertedText = properties.NewName;
				change.Description = string.Format (GettextCatalog.GetString ("Replace '{0}' with '{1}'"), memberRef.Name, properties.NewName);
				result.Add (change);
			}
			return result;
		}
		
		static string GetFullFileName (string fileName, string oldFullFileName, int tryCount)
		{
			StringBuilder name = new StringBuilder (fileName);
			if (tryCount > 0) {
				name.Append ("_");
				name.Append (tryCount.ToString ());
			}
			if (System.IO.Path.HasExtension (oldFullFileName))
				name.Append (System.IO.Path.GetExtension (oldFullFileName));
			
			return System.IO.Path.Combine (System.IO.Path.GetDirectoryName (oldFullFileName), name.ToString ());
		}
		
		MemberReferenceCollection GetReferences (RefactoringOptions options)
		{
			CodeRefactorer refactorer = IdeApp.Workspace.GetCodeRefactorer (IdeApp.ProjectOperations.CurrentSelectedSolution);
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor (this.Name, null);
			if (options.SelectedItem is IType) {
				IType cls = (IType)options.SelectedItem;
				return refactorer.FindClassReferences (monitor, cls, RefactoryScope.Solution, true);
			} else if (options.SelectedItem is LocalVariable) {
				return refactorer.FindVariableReferences (monitor, (LocalVariable)options.SelectedItem);
			} else if (options.SelectedItem is IParameter) {
				return refactorer.FindParameterReferences (monitor, (IParameter)options.SelectedItem, true);
			} else if (options.SelectedItem is IMember) {
				IMember member = (IMember)options.SelectedItem;
				
				MemberReferenceCollection result = new MemberReferenceCollection ();
				foreach (IMember m in CollectMembers (member.DeclaringType.SourceProjectDom, member)) {
					foreach (MemberReference r in refactorer.FindMemberReferences (monitor, m.DeclaringType, m, true)) {
						result.Add (r);
					}
				}
				return result;
			}
			return null;
		}
		
		internal static IEnumerable<IMember> CollectMembers (ProjectDom dom, IMember member)
		{
			// for members we need to collect the whole 'class' of members (overloads & implementing types)
			HashSet<string> alreadyVisitedTypes = new HashSet<string> ();
			foreach (IType type in dom.GetInheritanceTree (member.DeclaringType)) {
				if (type.ClassType == ClassType.Interface || member.IsOverride || member.IsVirtual || member.IsAbstract) {
					// search in the class for the member
					foreach (IMember interfaceMember in type.SearchMember (member.Name, true)) {
						if (interfaceMember.MemberType == member.MemberType)
							yield return interfaceMember;
					}
					
					// now search in all subclasses of this class for the member
					foreach (IType implementingType in dom.GetSubclasses (type)) {
						string name = implementingType.DecoratedFullName;
						if (alreadyVisitedTypes.Contains (name))
							continue;
						alreadyVisitedTypes.Add (name);
						foreach (IMember typeMember in implementingType.SearchMember (member.Name, true)) {
							if (typeMember.MemberType == member.MemberType)
								yield return typeMember;
						}
					}
				}
			}
		}
	}
}
