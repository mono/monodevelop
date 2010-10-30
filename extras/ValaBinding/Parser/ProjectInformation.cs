//
// ProjectInformation.cs
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// This source code is licenced under The MIT License:
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
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.CodeCompletion;

namespace MonoDevelop.ValaBinding.Parser
{
	/// <summary>
	/// Class to obtain parse information for a project
	/// </summary>
	public class ProjectInformation
	{
		private bool vtgInstalled = false;
		private bool checkedVtgInstalled = false;
		
		private Afrodite.CompletionEngine engine;
		
		static readonly string[] containerTypes = new string[]{ "class", "struct", "interface" };

		public Project Project{ get; set; }
		
		//// <value>
		/// Checks whether <see cref="http://code.google.com/p/vtg/">Vala Toys for GEdit</see> 
		/// is installed.
		/// </value>
		bool DepsInstalled {
			get {
				if (!checkedVtgInstalled) {
					checkedVtgInstalled = true;
					vtgInstalled = false;
					try {
						Afrodite.Utils.GetPackagePaths ("glib-2.0");
						return (vtgInstalled = true);
					} catch (DllNotFoundException) {
						LoggingService.LogWarning ("Cannot update Vala parser database because libafrodite (VTG) is not installed: {0}{1}{2}{3}", 
						                           Environment.NewLine, "http://code.google.com/p/vtg/",
						                           Environment.NewLine, "Note: If you're using Vala 0.10 or higher, you may need to symlink libvala-YOUR_VERSION.so to libvala.so");
					} catch (Exception ex) {
						LoggingService.LogError ("ValaBinding: Error while checking for libafrodite", ex);
					}
				}
				return vtgInstalled;
			}
			set {
				//don't assume that the caller is correct :-)
				if (value)
					checkedVtgInstalled = false; //will re-determine on next getting
				else
					vtgInstalled = false;
			}
		}

		public ProjectInformation (Project project)
		{
			this.Project = project;
			string projectName = (null == project)? "NoExistingProject": project.Name;
			
			if (DepsInstalled) {
				engine = new Afrodite.CompletionEngine (projectName);
			}
		}
		
		/// <summary>
		/// Gets the completion list for a given type name in a given file
		/// </summary>
		internal List<Afrodite.Symbol> CompleteType (string typename, string filename, int linenum, int column, ValaCompletionDataList results)
		{
			List<Afrodite.Symbol> nodes = new List<Afrodite.Symbol> ();
			if (!DepsInstalled){ return nodes; }
			
			using (Afrodite.Ast parseTree = engine.TryAcquireAst ()) {
				if (null != parseTree) {
					Afrodite.Symbol symbol = parseTree.GetSymbolForNameAndPath (typename, filename, linenum, column);
					if (null == symbol){ LoggingService.LogDebug ("CompleteType: Unable to lookup {0} in {1} at {2}:{3}", typename, filename, linenum, column); }
					else{ nodes = symbol.Children; }
				} else {
					LoggingService.LogDebug ("CompleteType: Unable to acquire ast");
				}
			}
			
			return nodes;
		}

		/// <summary>
		/// Adds a file to be parsed
		/// </summary>
		public void AddFile (string filename)
		{
			if (vtgInstalled) {
				LoggingService.LogDebug ("Adding file {0}", filename);
				engine.QueueSourcefile (filename, filename.EndsWith (".vapi", StringComparison.OrdinalIgnoreCase), false);
			}
		}// AddFile

		/// <summary>
		/// Removes a file from the parse list
		/// </summary>
		public void RemoveFile (string filename)
		{
			// Not currently possible with Afrodite completion engine
		}// RemoveFile

		/// <summary>
		/// Adds a package to be parsed
		/// </summary>
		public void AddPackage (string packagename)
		{
			if (!DepsInstalled){ return; }
			
			if ("glib-2.0".Equals (packagename, StringComparison.Ordinal)) {
				LoggingService.LogDebug ("AddPackage: Skipping {0}", packagename);
				return;
			} else {
				LoggingService.LogDebug ("AddPackage: Adding package {0}", packagename);
			}
			
			foreach (string path in Afrodite.Utils.GetPackagePaths (packagename)) {
				LoggingService.LogDebug ("AddPackage: Queueing {0} for package {1}", path, packagename);
				engine.QueueSourcefile (path, true, false);
			}
		}// AddPackage

		/// <summary>
		/// Gets the completion list for a given symbol at a given location
		/// </summary>
		internal List<Afrodite.Symbol> Complete (string symbol, string filename, int line, int column, ValaCompletionDataList results)
		{
			List<Afrodite.Symbol> nodes = new List<Afrodite.Symbol> ();
			if (!DepsInstalled){ return nodes; }
			
			
			using (Afrodite.Ast parseTree = engine.TryAcquireAst ()) {
				if (null != parseTree) {
					LoggingService.LogDebug ("Complete: Looking up symbol at {0}:{1}:{2}", filename, line, column);
					Afrodite.Symbol sym = parseTree.GetSymbolForNameAndPath (symbol, filename, line, column);
					LoggingService.LogDebug ("Complete: Got {0}", (null == sym)? "null": sym.Name);
					if (null != sym) {
						nodes = sym.Children;
						AddResults (nodes, results);
					}
				} else {
					LoggingService.LogDebug ("Complete: Unable to acquire ast");
				}
			}
			
			return nodes;
		}// Complete
		
		internal Afrodite.Symbol GetFunction (string name, string filename, int line, int column)
		{
			if (!DepsInstalled){ return null; }
			
			using (Afrodite.Ast parseTree = engine.TryAcquireAst ()) {
				if (null != parseTree) {
					LoggingService.LogDebug ("GetFunction: Looking up symbol at {0}:{1}:{2}", filename, line, column);
					Afrodite.Symbol symbol = parseTree.GetSymbolForNameAndPath (name, filename, line, column);
					LoggingService.LogDebug ("GetFunction: Got {0}", (null == symbol)? "null": symbol.Name);
					return symbol;
				} else {
					LoggingService.LogDebug ("GetFunction: Unable to acquire ast");
				}
			}

			return null;
		}

		/// <summary>
		/// Get the type of a given expression
		/// </summary>
		public string GetExpressionType (string symbol, string filename, int line, int column)
		{
			if (!DepsInstalled){ return symbol; }
			
			using (Afrodite.Ast parseTree = engine.TryAcquireAst ()) {
				if (null != parseTree) {
					LoggingService.LogDebug ("GetExpressionType: Looking up symbol at {0}:{1}:{2}", filename, line, column);
					Afrodite.Symbol sym = parseTree.LookupSymbolAt (filename, line, column);
					if (null != sym) {
						LoggingService.LogDebug ("Got {0}", sym.DataType.TypeName);
						return sym.DataType.TypeName;
					}
				} else {
					LoggingService.LogDebug ("GetExpressionType: Unable to acquire ast");
				}
			}

			return symbol;
		}// GetExpressionType
		
		/// <summary>
		/// Get overloads for a method
		/// </summary>
		internal List<Afrodite.Symbol> GetOverloads (string name, string filename, int line, int column)
		{
			List<Afrodite.Symbol> overloads = new List<Afrodite.Symbol> ();
			if (!DepsInstalled){ return overloads; }
			
			using (Afrodite.Ast parseTree = engine.TryAcquireAst ()) {
				if (null != parseTree) {
					Afrodite.Symbol symbol = parseTree.GetSymbolForNameAndPath (name, filename, line, column);
					overloads = new List<Afrodite.Symbol> (){ symbol };
				} else {
					LoggingService.LogDebug ("GetOverloads: Unable to acquire ast");
				}
			}
			
			return overloads;
		}// GetOverloads
		
		/// <summary>
		/// Get constructors for a given type
		/// </summary>
		internal List<Afrodite.Symbol> GetConstructorsForType (string typename, string filename, int line, int column, ValaCompletionDataList results)
		{
			List<Afrodite.Symbol> functions = new List<Afrodite.Symbol> ();
			foreach (Afrodite.Symbol node in CompleteType (typename, filename, line, column, null)) {
				if ("constructor".Equals (node.SymbolType, StringComparison.OrdinalIgnoreCase) || 
				      "creationmethod".Equals (node.SymbolType, StringComparison.OrdinalIgnoreCase)) {
					functions.Add (node);
				}
			}
			
			AddResults ((IList<Afrodite.Symbol>)functions, results);
			
			return functions;
		}// GetConstructorsForType
		
		/// <summary>
		/// Get constructors for a given expression
		/// </summary>
		internal List<Afrodite.Symbol> GetConstructorsForExpression (string expression, string filename, int line, int column, ValaCompletionDataList results)
		{
			string typename = GetExpressionType (expression, filename, line, column);
			return GetConstructorsForType (typename, filename, line, column, results);
		}// GetConstructorsForExpression
		
		/// <summary>
		/// Get types visible from a given source location
		/// </summary>
		internal void GetTypesVisibleFrom (string filename, int line, int column, ValaCompletionDataList results)
		{
			if (!DepsInstalled){ return; }
			
			// Add contents of parents
			ICollection<Afrodite.Symbol> containers = GetClassesForFile (filename);
			AddResults (containers, results);
			foreach (Afrodite.Symbol klass in containers) {
				// TODO: check source references once afrodite reliably captures the entire range
				for (Afrodite.Symbol parent = klass.Parent;
				     parent != null;
				     parent = parent.Parent)
				{
					AddResults (parent.Children.FindAll (delegate (Afrodite.Symbol sym){
						return 0 <= Array.IndexOf (containerTypes, sym.SymbolType.ToLower ());
					}), results);
				}
			}
				
			using (Afrodite.Ast parseTree = engine.TryAcquireAst ()) {
				if (null == parseTree){ return; }
				
				AddResults (GetNamespacesForFile (filename), results);
				AddResults (GetClassesForFile (filename), results);
				Afrodite.SourceFile file = parseTree.LookupSourceFile (filename);
				if (null != file) {
					Afrodite.Symbol parent;
					foreach (Afrodite.Symbol directive in file.UsingDirectives) {
						Afrodite.Symbol ns = parseTree.Lookup (directive.FullyQualifiedName, out parent);
						if (null != ns) {
							containers = new List<Afrodite.Symbol> ();
							AddResults (new Afrodite.Symbol[]{ ns }, results);
							foreach (Afrodite.Symbol child in ns.Children) {
								foreach (string containerType in containerTypes) {
									if (containerType.Equals (child.SymbolType, StringComparison.OrdinalIgnoreCase))
										containers.Add (child);
								}
							}
							AddResults (containers, results);
						}
					}
				}
			}
		}// GetTypesVisibleFrom
		
		/// <summary>
		/// Get symbols visible from a given source location
		/// </summary>
		internal void GetSymbolsVisibleFrom (string filename, int line, int column, ValaCompletionDataList results) 
		{
			GetTypesVisibleFrom (filename, line, column, results);
			Complete ("this", filename, line, column, results);
		}// GetSymbolsVisibleFrom
		
		/// <summary>
		/// Add results to a ValaCompletionDataList on the GUI thread
		/// </summary>
		private static void AddResults (IEnumerable<Afrodite.Symbol> list, ValaCompletionDataList results) 
		{
			if (null == list || null == results)
			{
				LoggingService.LogDebug ("AddResults: null list or results!");
				return;
			}
			
			List<CompletionData> data = new List<CompletionData> ();
			foreach (Afrodite.Symbol symbol in list) {
				data.Add (new CompletionData (symbol));
			}
			
			DispatchService.GuiDispatch (delegate {
				results.IsChanging = true;
				results.AddRange (data);
				results.IsChanging = false;
			});
		}// AddResults
		
		/// <summary>
		/// Get a list of classes declared in a given file
		/// </summary>
		internal List<Afrodite.Symbol> GetClassesForFile (string file)
		{
			return GetSymbolsForFile (file, containerTypes);
		}// GetClassesForFile
		
		/// <summary>
		/// Get a list of namespaces declared in a given file
		/// </summary>
		internal List<Afrodite.Symbol> GetNamespacesForFile (string file)
		{
			return GetSymbolsForFile (file, new string[]{ "namespace" });
		}
		
		/// <summary>
		/// Get a list of symbols declared in a given file
		/// </summary>
		/// <param name="file">
		/// A <see cref="System.String"/>: The file to check
		/// </param>
		/// <param name="desiredTypes">
		/// A <see cref="IEnumerable<System.String>"/>: The types of symbols to allow
		/// </param>
		List<Afrodite.Symbol> GetSymbolsForFile (string file, IEnumerable<string> desiredTypes)
		{
			List<Afrodite.Symbol> symbols = null;
			List<Afrodite.Symbol> classes = new List<Afrodite.Symbol> ();
			
			if (!DepsInstalled){ return classes; }
			
			using (Afrodite.Ast parseTree = engine.TryAcquireAst ()) {
				if (null != parseTree){
					Afrodite.SourceFile sourceFile = parseTree.LookupSourceFile (file);
					if (null != sourceFile) {
						symbols = sourceFile.Symbols;
						if (null != symbols) {
							foreach (Afrodite.Symbol symbol in symbols) {
								foreach (string containerType in desiredTypes) {
									if (containerType.Equals (symbol.SymbolType, StringComparison.OrdinalIgnoreCase))
										classes.Add (symbol);
								}
							}
						}
					} else {
						LoggingService.LogDebug ("GetClassesForFile: Unable to lookup source file {0}", file);
					}
				} else {
					LoggingService.LogDebug ("GetClassesForFile: Unable to acquire ast");
				}
				
			}
			
			return classes;
		}
	}
}
