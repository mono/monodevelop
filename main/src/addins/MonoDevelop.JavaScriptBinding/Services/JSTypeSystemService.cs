//
// JSTypeSystemService.cs
//
// Author:
//       Harsimran Bath <harsimranbath@gmail.com>
//
// Copyright (c) 2014 Harsimran
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.JavaScript
{
	/*
	static class JSTypeSystemService
	{
		readonly static List<JavaScriptDocumentCache> solutionParsedDocuments;
		static bool isInitialized = false;

		static JSTypeSystemService ()
		{
			MonoDevelop.Ide.IdeApp.Workspace.SolutionLoaded += HandleSolutionLoaded;
			MonoDevelop.Ide.IdeApp.Workspace.SolutionUnloaded += HandleSolutionUnloaded;

			solutionParsedDocuments = new List<JavaScriptDocumentCache> ();
		}

		static void HandleSolutionLoaded (object sender, MonoDevelop.Projects.SolutionEventArgs e)
		{
			solutionParsedDocuments.Clear ();
			if (e.Solution != null) {
				refreshSolutionParsedDocuments (e.Solution);
			}
		}

		static void HandleSolutionUnloaded (object sender, MonoDevelop.Projects.SolutionEventArgs e)
		{
			solutionParsedDocuments.Clear ();
		}

		public static void Initialize ()
		{
			isInitialized = true;
		}

		public static void AddUpdateParsedDocument (string projectId, string fileName, JavaScriptParsedDocument parsedDocument)
		{
			if (!fileName.ToUpper ().EndsWith ("JS"))
				return;

			var currentParsedDocument = solutionParsedDocuments.FirstOrDefault (i => i.FileName == fileName);
			if (currentParsedDocument != null) {
				currentParsedDocument.SimpleAst = parsedDocument.SimpleAst;
			} else {
				solutionParsedDocuments.Add (new JavaScriptDocumentCache {
					FileName = fileName,
					SimpleAst = parsedDocument.SimpleAst,
					ProjectId = projectId
				});
			}
		}

		public static List<SimpleJSAst> GetAllJSAstsForProject (string projectId)
		{
			return (from item in solutionParsedDocuments
				where item.ProjectId == projectId
				select item.SimpleAst).ToList ();
		}

		static void refreshSolutionParsedDocuments (MonoDevelop.Projects.Solution solution)
		{
			if (solution == null)
				return;

			Task parsingTask = new Task (delegate() {
				foreach (MonoDevelop.Projects.Project project in solution.GetAllProjects ()) {
					foreach (MonoDevelop.Projects.ProjectFile file in project.Files) {
						if (!file.Name.ToUpper ().EndsWith ("JS"))
							continue;

						string fileContent = System.IO.File.ReadAllText (file.Name);
						var parsedDocument = new JavaScriptParsedDocument (file.Name, fileContent);
						AddUpdateParsedDocument (project.ItemId, file.Name, parsedDocument);
					}
				}
			});

			parsingTask.Start ();
		}
	}
	*/
}

