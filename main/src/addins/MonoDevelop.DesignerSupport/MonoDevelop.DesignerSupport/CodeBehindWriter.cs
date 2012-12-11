// 
// CodeBehindWriter.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using System.CodeDom.Compiler;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects;
using System.IO;
using MonoDevelop.Ide;



namespace MonoDevelop.DesignerSupport
{

	public class CodeBehindWriter
	{
		List<string> openFiles;
		List<KeyValuePair<FilePath,string>> filesToWrite = new List<KeyValuePair<FilePath,string>> ();
		CodeDomProvider provider;
		CodeGeneratorOptions options;
		IProgressMonitor monitor;
		
		public CodeDomProvider Provider { get { return provider; } }
		public CodeGeneratorOptions GeneratorOptions { get { return options; } }
		
		public bool SupportsPartialTypes {
			get {
				return provider.Supports (GeneratorSupport.PartialTypes);
			}
		}

		public CodeBehindWriter ()
		{
		}
		
		CodeBehindWriter (IProgressMonitor monitor, CodeDomProvider provider, CodeGeneratorOptions options)
		{
			this.provider = provider;
			this.options = options;
			this.monitor = monitor;
		}
		
		public static CodeBehindWriter CreateForProject (IProgressMonitor monitor, DotNetProject project)
		{
			var pol = project.Policies.Get<TextStylePolicy> ();
			var options = new CodeGeneratorOptions () {
				IndentString = pol.TabsToSpaces? new string (' ', pol.TabWidth) : "\t",
				BlankLinesBetweenMembers = true,
			};
			var provider = project.LanguageBinding.GetCodeDomProvider ();
			
			return new CodeBehindWriter (monitor, provider, options);
		}
		
		List<string> OpenFiles {
			get {
				if (openFiles == null) {
					openFiles = new List<string> ();
					if (!IdeApp.IsInitialized)
						return openFiles;
					DispatchService.GuiSyncDispatch (delegate {
						foreach (var doc in IdeApp.Workbench.Documents)
						if (doc.GetContent<IEditableTextBuffer> () != null)
							openFiles.Add (doc.FileName);
					});
				}
				return openFiles;
			}
		}
		
		public void WriteFile (FilePath path, Action<TextWriter> write)
		{
			if (OpenFiles.Contains (path)) {
				using (var sw = new StringWriter ()) {
					write (sw);
					filesToWrite.Add (new KeyValuePair<FilePath, string> (path, sw.ToString ()));
				}
				return;
			}
			
			try {
				var tempPath = path.ParentDirectory.Combine (".#" + path.FileName);
				using (var sw = new StreamWriter (tempPath)) {
					write (sw);
				}
				FileService.SystemRename (tempPath, path);
				//mark the file as changed so it gets reparsed
				Gtk.Application.Invoke (delegate {
					FileService.NotifyFileChanged (path);
				});
				WrittenCount++;
			} catch (IOException ex) {
				monitor.ReportError (GettextCatalog.GetString ("Failed to write file '{0}'.", path), ex);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Failed to generate code for file '{0}'.", path), ex);
			}
		}

		public void WriteFile (FilePath path, System.CodeDom.CodeCompileUnit ccu)
		{
			WriteFile (path, (TextWriter tw) => provider.GenerateCodeFromCompileUnit (ccu, tw, options));
		}
		
		public void WriteFile (FilePath path, string contents)
		{
			if (OpenFiles.Contains (path)) {
				using (var sw = new StringWriter ())
					filesToWrite.Add (new KeyValuePair<FilePath, string> (path, contents));
				return;
			}
			
			try {
				var tempPath = path.ParentDirectory.Combine (".#" + path.FileName);
				File.WriteAllText (tempPath, contents);
				FileService.SystemRename (tempPath, path);
				//mark the file as changed so it gets reparsed
				Gtk.Application.Invoke (delegate {
					FileService.NotifyFileChanged (path);
				});
				WrittenCount++;
			} catch (IOException ex) {
				monitor.ReportError (GettextCatalog.GetString ("Failed to write file '{0}'.", path), ex);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Failed to generate code for file '{0}'.", path), ex);
			}
		}
		
		public void WriteOpenFiles ()
		{
			if (filesToWrite == null) {
				return;
			}
			
			if (filesToWrite.Count == 0) {
				filesToWrite = null;
				return;
			}
			
			//these documents are open, so needs to run in GUI thread
			DispatchService.GuiSyncDispatch (delegate {
				foreach (KeyValuePair<FilePath, string> item in filesToWrite) {
					try {
						
						bool updated = false;
						foreach (MonoDevelop.Ide.Gui.Document doc in IdeApp.Workbench.Documents) {
							if (doc.FileName == item.Key) {
								var textFile = doc.GetContent<MonoDevelop.Projects.Text.IEditableTextFile> ();
								if (textFile == null)
									continue;
								
								//change the contents
								//FIXME: Workaround for "Bug 484574 - Setting SourceEditorView.Text doesn't mark the document as dirty"
								// The bug means that the docuemnt doesn't get saved or reparsed.
								doc.Editor.Text = item.Value;
								doc.IsDirty = true;

								doc.Save ();
								updated = true;
								break;
							}
						}
						
						if (!updated) {
							var textFile = MonoDevelop.Projects.Text.TextFile.ReadFile (item.Key);
							textFile.Text = item.Value;
							textFile.Save ();
						}
						
						WrittenCount++;
						
					} catch (IOException ex) {
						monitor.ReportError (
							GettextCatalog.GetString ("Failed to write file '{0}'.", item.Key),
							ex);
					}
				}
			});
			
			filesToWrite = null;
		}
		
		public int WrittenCount { get; private set; }
	}
}
