//
// CodeMetricsWidget.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//	 Nikhil Sarda <diff.operator@gmail.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com), Nikhil Sarda
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
using System.IO;

using Gtk;
using Gdk;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Mono.TextEditor;
using System.ComponentModel;

namespace MonoDevelop.CodeMetrics
{
	// TODO Use thread synchronization to coordinate between TreeStore drawing and Metrics calculation 
	public partial class CodeMetricsWidget : Gtk.Bin
	{
		public static CodeMetricsWidget widget;
		
		List<string> files = new List<string> ();
		List<ProjectProperties> projects = new List<ProjectProperties>();
		
		public List<ProjectProperties> Projects {
			get {
				return projects;
			}
		}
		
		MetricsContext ctx = new MetricsContext ();
		
		//TODO Treestore will have LOC, real LOC, commented LOC and cyclometric complexity (later to add code coverage)
		TreeStore metricStore = new TreeStore (typeof (Pixbuf), // Icon
		                                 typeof (string), // type name
		                                 typeof (string), // cyclometric complexity
		                                 typeof (string), // class coupling
		                                 typeof (string), 	  // real loc
		                                 typeof (string),	  // comments loc
		                                 typeof (IProperties)  // reference to objects
		                                 );
		TreeViewColumn iconCol, 
						typenameCol, 
						cyclometricComplexityCol,
						classCouplingCol,
						realLocCol, 
						commentsLocCol;
		
		CellRendererText crt;
		IProperties rowSelectTypeName;
		bool clicked = false;
		
		public CodeMetricsWidget()
		{
			this.Build();
			treeviewMetrics.RulesHint = true;
			treeviewMetrics.Model = metricStore;
			
			projects = new List<ProjectProperties>();
			
			crt = new CellRendererText ();
			crt.Ellipsize = Pango.EllipsizeMode.Start;
			
			iconCol = new TreeViewColumn (GettextCatalog.GetString ("Icon"), new Gtk.CellRendererPixbuf (), "pixbuf", 0);
			iconCol.SortIndicator = true;
			iconCol.SortColumnId = 1;
			iconCol.Expand = false;
			iconCol.Resizable = true;
			treeviewMetrics.AppendColumn (iconCol);
						
			typenameCol = new TreeViewColumn (GettextCatalog.GetString ("Type name"), crt, "text", 1);
			typenameCol.SortIndicator = true;
			typenameCol.SortColumnId = 0;
			typenameCol.Expand = true;
			typenameCol.Resizable = true;
			treeviewMetrics.AppendColumn (typenameCol);
			
			cyclometricComplexityCol = new TreeViewColumn (GettextCatalog.GetString ("Cyclometric Complexity"), new CellRendererText (), "text", 2);
			cyclometricComplexityCol.SortIndicator = true;
			cyclometricComplexityCol.SortColumnId = 0;
			cyclometricComplexityCol.Reorderable = true;
			cyclometricComplexityCol.Resizable = false;
			treeviewMetrics.AppendColumn (cyclometricComplexityCol);
			
			classCouplingCol = new TreeViewColumn (GettextCatalog.GetString ("Class Coupling"), new CellRendererText (), "text", 3);
			classCouplingCol.SortIndicator = true;
			classCouplingCol.SortColumnId = 0;
			classCouplingCol.Reorderable = true;
			classCouplingCol.Resizable = false;
			treeviewMetrics.AppendColumn (classCouplingCol);
			
			realLocCol = new TreeViewColumn (GettextCatalog.GetString ("Real Loc"), new CellRendererText (), "text", 4);
			realLocCol.SortIndicator = true;
			realLocCol.SortColumnId = 0;
			realLocCol.Reorderable = true;
			realLocCol.Resizable = false;
			treeviewMetrics.AppendColumn (realLocCol);
			
			commentsLocCol = new TreeViewColumn (GettextCatalog.GetString ("Comments Loc"), new CellRendererText (), "text", 5);
			commentsLocCol.SortIndicator = true;
			commentsLocCol.SortColumnId = 0;
			commentsLocCol.Reorderable = true;
			commentsLocCol.Resizable = false;
			treeviewMetrics.AppendColumn (commentsLocCol);
			
			// TODO: When user clicks on the respective type then the corresponding filename containing that type should open
			
			this.treeviewMetrics.RowActivated += delegate {
				Gtk.TreeIter selectedIter;
				if (treeviewMetrics.Selection.GetSelected (out selectedIter)) {
					rowSelectTypeName = (IProperties)metricStore.GetValue (selectedIter, 6);
						MonoDevelop.Ide.IdeApp.Workbench.OpenDocument (rowSelectTypeName.FilePath);
						MonoDevelop.Ide.IdeApp.Workbench.ActiveDocument.Editor.SetCaretTo (rowSelectTypeName.StartLine, 0);	
				}
			};
			
			this.treeviewMetrics.CursorChanged += delegate {
				Gtk.TreeIter selectedIter;
				if (treeviewMetrics.Selection.GetSelected (out selectedIter)) {
					rowSelectTypeName = (IProperties)metricStore.GetValue (selectedIter, 6);
					Gtk.Application.Invoke( delegate {
						textviewReport.Buffer.Text = CodeMetricsService.GenerateTypeMetricText(rowSelectTypeName);	
					});
				}
			};
		}
		
		protected override void OnDestroyed ()
		{
			if (metricStore != null) {
				metricStore.Dispose ();
				metricStore = null;
			}
			base.OnDestroyed ();
		}
		
		class MetricsWorkerThread : BackgroundWorker
		{
			//Earlier wasnt using the static thing, maybe not required as well
			CodeMetricsWidget widget=CodeMetricsWidget.widget;
			
			public static object lockCounter = new object();
			
			public MetricsWorkerThread (CodeMetricsWidget widget)
			{
				this.widget = widget;
				base.WorkerSupportsCancellation = true;
			}
			
			protected override void OnDoWork (DoWorkEventArgs e)
			{
				int counter=0;
				int totalProjects = widget.projects.Count;
				try {
					foreach(ProjectProperties projectprop in widget.projects)
						CodeMetricsService.AddTypes(projectprop, widget.ctx);
					
					foreach(ProjectProperties projectprop in widget.projects) {
						ObjectOrientedMetrics.EvaluateOOMetrics(widget.ctx, projectprop);
//						ComplexityMetrics.EvaluateComplexityMetrics(widget.ctx, projectprop);
						CodeMetricsService.ProcessInnerTypes(projectprop);
					
						Gtk.Application.Invoke ( delegate {
							FillTree(projectprop);
						});
						if(base.CancellationPending)
							return;
						lock(lockCounter)
						{
							counter++;
							DispatchService.GuiSyncDispatch (delegate {
								IdeApp.Workbench.StatusBar.SetProgressFraction (counter / (double)totalProjects);
							});
						}
					}
				} catch (Exception ex) {
					Console.WriteLine("Error : " + ex.ToString());
				}
				
				
				Gtk.Application.Invoke (delegate {
					IdeApp.Workbench.StatusBar.ShowMessage("Finished calculating metrics\n");
					IdeApp.Workbench.StatusBar.EndProgress ();
					widget.textviewReport.Buffer.Text = GettextCatalog.GetString ("Finished calculating metrics\n");
					widget.textviewReport.Buffer.Text += CodeMetricsService.GenerateAssemblyMetricText();
				});
				
			}
			
			protected void FillTree (ProjectProperties projprop)
			{
				var rootIter = widget.metricStore.AppendValues ( ImageService.GetPixbuf("md-project", Gtk.IconSize.Menu),
					                                           projprop.Project.Name, 
				    	                                       projprop.CyclometricComplexity.ToString(),
				        	                                   "",
				            	                               projprop.LOCReal.ToString(),
				                	                           projprop.LOCComments.ToString(),
				                       	                       projprop);
				
					FillNamespaces(projprop.Namespaces, rootIter);
					FillClasses(projprop.Classes, rootIter);
					FillEnums(projprop.Enums, rootIter);
					FillStructs(projprop.Structs, rootIter);
					FillDelegates(projprop.Delegates, rootIter);
					FillInterfaces(projprop.Interfaces, rootIter);
			}
			
			private void FillNamespaces (Dictionary<string, NamespaceProperties> namespaces, TreeIter parentIter)
			{
				foreach (var namesp in namespaces) {
					var subiter = widget.metricStore.AppendValues (parentIter,
					                                           ImageService.GetPixbuf("md-name-space", Gtk.IconSize.Menu),
					                                           namesp.Value.FullName, 
					                                           namesp.Value.CyclometricComplexity.ToString(), 
					                                           namesp.Value.ClassCoupling.ToString(),
					                                           namesp.Value.LOCReal.ToString(),
					                                           namesp.Value.LOCComments.ToString(),
					                                           namesp.Value);
					FillClasses(namesp.Value.Classes, subiter);
					FillEnums(namesp.Value.Enums, subiter);
					FillStructs(namesp.Value.Structs, subiter);
					FillDelegates(namesp.Value.Delegates, subiter);
					FillInterfaces(namesp.Value.Interfaces, subiter);
				}
			}
			
			private void FillEnums (Dictionary<string,EnumProperties> enms, TreeIter parentIter)
			{
				foreach (var cls in enms){
					var iter = widget.metricStore.AppendValues (parentIter,
					                                            ImageService.GetPixbuf("md-enum", Gtk.IconSize.Menu),
					                                            cls.Value.FullName, 
					                                            cls.Value.CyclometricComplexity.ToString(),
					                                            cls.Value.ClassCoupling.ToString(),
					                                            cls.Value.LOCReal.ToString(),
					                                            cls.Value.LOCComments.ToString(),
					                                            cls.Value);
				}
			}
			
			private void FillStructs (Dictionary<string, StructProperties> strcts, TreeIter parentIter)
			{
				foreach (var cls in strcts){
					var iter = widget.metricStore.AppendValues (parentIter,
					                                            ImageService.GetPixbuf("md-struct", Gtk.IconSize.Menu),
					                                            cls.Value.FullName, 
					                                            cls.Value.CyclometricComplexity.ToString(),
					                                            cls.Value.ClassCoupling.ToString(),
					                                            cls.Value.LOCReal.ToString(),
					                                            cls.Value.LOCComments.ToString(),
					                                            cls.Value);
				}
			}
			
			private void FillInterfaces (Dictionary<string, InterfaceProperties> interfces, TreeIter parentIter)
			{
				foreach (var cls in interfces){
					var iter = widget.metricStore.AppendValues (parentIter,
					                                            ImageService.GetPixbuf("md-interface", Gtk.IconSize.Menu),
					                                            cls.Value.FullName, 
					                                            cls.Value.CyclometricComplexity.ToString(),
					                                            cls.Value.ClassCoupling.ToString(),
					                                            cls.Value.LOCReal.ToString(),
					                                            cls.Value.LOCComments.ToString(),
					                                            cls.Value);
					// Add recursive field for members of interfaces
				}
			}
			
			private void FillDelegates (Dictionary<string, DelegateProperties> dlgtes, TreeIter parentIter)
			{
				foreach (var cls in dlgtes){
					var iter = widget.metricStore.AppendValues (parentIter,
					                                            ImageService.GetPixbuf("md-method", Gtk.IconSize.Menu),
					                                            cls.Value.FullName, 
					                                            cls.Value.CyclometricComplexity.ToString(),
					                                            cls.Value.ClassCoupling.ToString(),
					                                            cls.Value.LOCReal.ToString(),
					                                            cls.Value.LOCComments.ToString(),
					                                            cls.Value);
				}
			}
			
			private void FillClasses (Dictionary<string, ClassProperties> clss, TreeIter parentIter)
			{
				foreach (var cls in clss){
					var childIter = widget.metricStore.AppendValues (parentIter,
					                                            ImageService.GetPixbuf("md-class", Gtk.IconSize.Menu),
					                                            cls.Value.FullName, 
					                                            cls.Value.CyclometricComplexity.ToString(),
					                                            cls.Value.ClassCoupling.ToString(),
					                                            cls.Value.LOCReal.ToString(),
					                                            cls.Value.LOCComments.ToString(),
					                                            cls.Value);
					FillMethods(cls.Value.Methods, childIter);
					FillDelegates(cls.Value.InnerDelegates, childIter);
					FillStructs(cls.Value.InnerStructs, childIter);
					FillEnums(cls.Value.InnerEnums, childIter);
					FillInterfaces(cls.Value.InnerInterfaces, childIter);
					FillClasses(cls.Value.InnerClasses, childIter);
				}
			}
		
			private void FillMethods (Dictionary<string, MethodProperties> mthd, TreeIter parentIter)
			{
				foreach (var cls in mthd){
					var childIter = widget.metricStore.AppendValues (parentIter,
					                                            ImageService.GetPixbuf("md-method", Gtk.IconSize.Menu),
					                                            cls.Value.FullName, 
					                                            cls.Value.CyclometricComplexity.ToString(),
					                                            cls.Value.ClassCoupling.ToString(),
					                                            cls.Value.LOCReal.ToString(),
					                                            cls.Value.LOCComments.ToString(),
					                                            cls.Value);
				}
			}
		}
		
		double Percent (ulong a, ulong b)
		{
			if (b == 0)
				return 0.0;
			return (a * 100.0) / b;
		}
		
		public void Run ()
		{
			MetricsWorkerThread thread = new MetricsWorkerThread (this);
			IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Calculating Metrics..."));
			textviewReport.Buffer.Text = GettextCatalog.GetString ("Calculating Metrics...");
			thread.RunWorkerAsync ();
		}
		
		#region AddMethods
		public void Add (string fileName)
		{
			files.Add (fileName);
		}
		
		public void Add (ProjectFile projectFile)
		{
			if (projectFile.BuildAction == BuildAction.Compile) 
				Add (projectFile.FilePath);
		}
		
		public void Add (Project project)
		{
			projects.Add(new ProjectProperties(project));
			foreach (ProjectFile projectFile in project.Files) {
				Add (projectFile);	
			}
		}
		
		public void Add (SolutionFolder combine)
		{
			foreach (Project project in combine.GetAllProjects ()) {
				Add (project);
			}
			
		}
		
		public void Add (WorkspaceItem item)
		{
			foreach (Project project in item.GetAllProjects ()) {
				Add (project);
			}
		}
		#endregion
	}
}