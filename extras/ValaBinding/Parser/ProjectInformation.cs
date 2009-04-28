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
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects.Gui.Completion;

namespace MonoDevelop.ValaBinding.Parser
{
	/// <summary>
	/// Class to obtain parse information for a project
	/// </summary>
	public class ProjectInformation
	{
		private ProcessWrapper p;
		private bool vtgInstalled = false;
		private bool checkedVtgInstalled = false;
		private Dictionary<string,List<Function>> methods;
		private Dictionary<string,List<CodeNode>> cache;
		private HashSet<string> files;
		private HashSet<string> packages;
		private DateTime lastRestarted;
		string lockme = "lockme";

		public Project Project{ get; set; }

		//// <value>
		/// Checks whether <see cref="http://code.google.com/p/vtg/">Vala Toys for GEdit</see> 
		/// is installed.
		/// </value>
		bool DepsInstalled {
			get {
				if (!checkedVtgInstalled) {
					checkedVtgInstalled = true;
					try {
						Runtime.ProcessService.StartProcess ("vsc-shell", "--help", null, null).WaitForOutput ();
					} catch {
						LoggingService.LogWarning ("Cannot update Vala parser database because vsc-shell is not installed: {0}{1}", 
						                           Environment.NewLine, "http://code.google.com/p/vtg/");
						return false;
					}
					vtgInstalled = true;
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

		private void RestartParser ()
		{
			// Don't restart more often than once/five seconds
			lock (lockme) {
				if (0 > DateTime.Now.AddSeconds (-5).CompareTo (lastRestarted)){ return; }
				lastRestarted = DateTime.Now;
			}
            
			if (null != p) {
				try {
					if (!p.HasExited){ p.Kill (); }
					p.Dispose ();
				} catch {
					// We don't care about anything that happens here.
				}
			}
			
			// Don't destroy old cached results
			// cache = new Dictionary<string,List<CodeNode>> ();
			
			if (DepsInstalled) {
				p = Runtime.ProcessService.StartProcess ("vsc-shell", string.Empty, ".", (ProcessEventHandler)null, null, null, true);
				p.StandardError.Close ();
				foreach (string package in packages) {
					AddPackage (package);
				}
				foreach (string file in files) {
					AddFile (file);
				}
			}
			
			lock(lockme){ lastRestarted = DateTime.Now; }
		}// RestartParser
		
		private static Regex endOutputRegex = new Regex (@"^(\s*vsc-shell -\s*$|^\s*>)", RegexOptions.Compiled);
		/// <summary>
		/// Reads process output
		/// </summary>
		/// <returns>
		/// A <see cref="System.String[]"/>: The lines output by the parser process
		/// </returns>
		private string[] ReadOutput ()
		{
			List<string> result = new List<string> ();
			int count = 0;
			
			DataReceivedEventHandler gotdata = delegate(object sender, DataReceivedEventArgs e) {
				// Console.WriteLine(e.Data);
				lock(result){ result.Add(e.Data); }
			};
			
			p.OutputDataReceived += gotdata;

			// for (int i=0; i<100; ++i) {
			for (;;) {
				p.BeginOutputReadLine ();
				Thread.Sleep (50);
				p.CancelOutputRead ();
				lock (result) {
					// if (count < result.Count){ i = 0; }
					if (0 < result.Count && endOutputRegex.Match(result[result.Count-1]).Success) {
						break;
					}
					count = result.Count;
				}
				p.StandardInput.WriteLine(string.Empty);
			}
			p.OutputDataReceived -= gotdata;
			
			return result.ToArray();
		}// ReadOutput

		/// <summary>
		/// Sends a command to the parser
		/// </summary>
		/// <param name="command">
		/// A <see cref="System.String"/>: The command to be sent to the parser
		/// </param>
		/// <param name="args">
		/// A <see cref="System.Object[]"/>: string.Format-style arguments for command
		/// </param>
		/// <returns>
		/// A <see cref="System.String[]"/>: The output from the command
		/// </returns>
		private string[] ParseCommand (string command, params object[] args)
		{
			string[] output = new string[0];
			if (null == p){ return output; }

			try {
			lock (p) {
				// Console.WriteLine (command, args);
				p.StandardInput.WriteLine (string.Format (command, args));
				output =  ReadOutput ();
				// foreach (string line in output){ Console.WriteLine (line); }
			}
			} catch (Exception e) { 
				Console.WriteLine("{0}{1}{2}", e.Message, Environment.NewLine, e.StackTrace); 
			}
			
			if (0 == output.Length) { RestartParser (); }
			
			return output;
		}// ParseCommand

		private ProjectInformation ()
		{
			files = new HashSet<string> ();
			packages = new HashSet<string> ();
			methods = new Dictionary<string,List<Function>> ();
			cache = new Dictionary<string, List<CodeNode>> ();
			lastRestarted = DateTime.Now.AddSeconds(-6);
			RestartParser ();
		}

		public ProjectInformation (Project project): this ()
		{
			this.Project = project;
		}

		~ProjectInformation ()
		{
			lock (p) {
				try {
					p.StandardInput.WriteLine("quit");
					p.WaitForExit (100);
					if (!p.HasExited) {
						p.Kill ();
					}
					p.Dispose ();
				} catch {
				}
			}
		}

		/// <summary>
		/// Gets the children of a given code node
		/// </summary>
		public IList<CodeNode> GetChildren (CodeNode parent)
		{
			IList<CodeNode> children = new List<CodeNode> ();
			
			if (null == parent) {
				string[] output = ParseCommand ("get-namespaces");
				Match match;
				CodeNode child;
	
				foreach (string line in output) {
					if (null != (child = ParseType (string.Empty, line))) {
						children.Add (child);
					}
				}
			} else if ("namespaces" != parent.NodeType) {
				children = CacheCompleteType (parent.FullName, parent.File, parent.FirstLine, 0, null);
			} else {
				children = CacheCompleteType (parent.FullName, string.Empty, 0, 0, null);
			}

			return children;
		}// GetChildren

		private List<CodeNode> CacheCompleteType (string typename, string filename, int linenum, int column, ValaCompletionDataList results) 
		{
			bool cached;
			List<CodeNode> completion;
			
			lock (cache){ cached = cache.TryGetValue (typename, out completion); }
			
			if (cached) {
				AddResults (completion, results);
				ThreadPool.QueueUserWorkItem (delegate{
					List<CodeNode> newcompletion = CompleteType (typename, filename, linenum, column, null);
					if (null != newcompletion && 0 != newcompletion.Count) {
						lock (cache){ cache[typename] = newcompletion; }
					}
				});
			} else {
				completion = CompleteType (typename, filename, linenum, column, results);
					if (null != completion && 0 != completion.Count) {
						lock (cache){ cache[typename] = completion; }
					}
			}
			
			return completion;
		}// CacheCompleteType
		
		/// <summary>
		/// Gets the completion list for a given type name in a given file
		/// </summary>
		internal List<CodeNode> CompleteType (string typename, string filename, int linenum, int column, ValaCompletionDataList results)
		{
			string[] output = ParseCommand ("complete {0} {1}", typename, filename);
			List<CodeNode> children = new List<CodeNode> ();
			CodeNode child;

			foreach (string line in output) {
				if (null != (child = ParseType (typename, line))) {
					children.Add (child);
					if (null != results) {
						CompletionData datum = new CompletionData (child);
						Gtk.Application.Invoke (delegate(object sender, EventArgs args){
							results.Add (datum);
							results.IsChanging = true;
						});
					}
				}
			}
			
			if (null != results) {
				Gtk.Application.Invoke (delegate(object sender, EventArgs args){
					results.IsChanging = false;
				});
			}

			return children;
		}

		/// <summary>
		/// Adds a file to be parsed
		/// </summary>
		public void AddFile (string filename)
		{
			lock (files) {
				// if (files.Contains (filename)){ return; }
				files.Add (filename);
			}
			ThreadPool.QueueUserWorkItem (delegate {
				ParseCommand ("add-source {0}", filename);
			});
		}// AddFile

		/// <summary>
		/// Removes a file from the parse list
		/// </summary>
		public void RemoveFile (string filename)
		{
			lock (files) { files.Remove (filename); }
			ThreadPool.QueueUserWorkItem (delegate {
				ParseCommand ("remove-source {0}", filename);
			});
		}// RemoveFile

		/// <summary>
		/// Adds a package to be parsed
		/// </summary>
		public void AddPackage (string packagename)
		{
			lock (packages) { 
				// if (packages.Contains (packagename)){ return; }
				packages.Add (packagename);
			}
			ThreadPool.QueueUserWorkItem (delegate {
				ParseCommand ("add-package {0}", packagename);
			});
		}// AddPackage

		/// <summary>
		/// Tells the parser to reparse
		/// </summary>
		public void Reparse ()
		{
			//ParseCommand ("reparse");
			RestartParser ();
		}// Reparse

		private static Regex typeNameRegex = new Regex (@"vsc-shell - typename for [^:]+: (?<type>[^\s]+)\s*$", RegexOptions.Compiled);
		/// <summary>
		/// Gets the completion list for a given symbol at a given location
		/// </summary>
		public void Complete (string symbol, string filename, int line, int column, ValaCompletionDataList results)
		{
			if (cache.ContainsKey (symbol)){ CacheCompleteType (symbol, filename, line, column, results); }
			else ThreadPool.QueueUserWorkItem (delegate{
				string expressionType = GetExpressionType (symbol, filename, line, column);
				CacheCompleteType (expressionType, filename, line, column, results);
			});
		}// Complete

		/// <summary>
		/// Get the type of a given expression
		/// </summary>
		public string GetExpressionType (string symbol, string filename, int line, int column)
		{
			AddFile (filename);
			string[] responses = ParseCommand ("type-name {0} {1} {2} {3}", symbol, filename, line, column);
			Match match;
			
			foreach (string response in responses) {
				match = typeNameRegex.Match (response);
				if (match.Success) {
					return match.Groups["type"].Value;
				}
			}

			return symbol;
		}// GetExpressionType
		
		/// <summary>
		/// Get overloads for a method
		/// </summary>
		public List<Function> GetOverloads (string name)
		{
			lock (methods) {
				return (methods.ContainsKey (name))? methods[name]: new List<Function> ();
			}
		}// GetOverloads
		
		/// <summary>
		/// Get constructors for a given type
		/// </summary>
		public List<Function> GetConstructorsForType (string typename, string filename, int line, int column, ValaCompletionDataList results)
		{
			string[] tokens = typename.Split ('.');
			string baseTypeName = tokens[tokens.Length-1];
			List<Function> constructors = new List<Function> ();
			List<CodeNode> cachedNode = null;
			
			if (cache.ContainsKey (typename)) {
				cachedNode = cache[typename];
			} else {
				foreach (string cachedType in cache.Keys) {
					if (cachedType.EndsWith (typename)) { 
						cachedNode = cache[cachedType];
						break;
					}
				}
			}
			
			if (null != cachedNode) {
				constructors = cachedNode.FindAll (delegate (CodeNode node){ 
					return ("method" == node.NodeType && 
					    (baseTypeName == node.Name || node.Name.StartsWith (baseTypeName + ".")));
				}).ConvertAll<Function> (delegate (CodeNode node){ 
					if (null != results) {
						CompletionData datum = new CompletionData (node);
						Gtk.Application.Invoke (delegate (object sender, EventArgs args){
							results.Add (datum);
							results.IsChanging = true;
						});
					}
					return node as Function; 
				});
			} else {
				ThreadPool.QueueUserWorkItem (delegate (object o){ CacheCompleteType (typename, filename, line, column, null); });
			}
			
			if (null != results) {
				Gtk.Application.Invoke (delegate (object sender, EventArgs args){
					results.IsChanging = false;
				});
			}
			
			return constructors;
		}// GetConstructorsForType
		
		/// <summary>
		/// Get constructors for a given expression
		/// </summary>
		public List<Function> GetConstructorsForExpression (string expression, string filename, int line, int column, ValaCompletionDataList results)
		{
			string typename = GetExpressionType (expression, filename, line, column);
			return GetConstructorsForType (typename, filename, line, column, results);
		}// GetConstructorsForExpression
		
		/// <summary>
		/// Get types visible from a given source location
		/// </summary>
		public void GetTypesVisibleFrom (string filename, int line, int column, ValaCompletionDataList results)
		{
			results.IsChanging = true;
			
			ThreadPool.QueueUserWorkItem (delegate{
				string[] output = ParseCommand ("visible-types {0} {1} {2}", filename, line, column);
				CodeNode child;
				
				foreach (string outputline in output) {
					if (null != (child = ParseType (string.Empty, outputline))) {
						CompletionData datum = new CompletionData (child);
						Gtk.Application.Invoke (delegate (object sender, EventArgs args){
							results.Add (datum);
							results.IsChanging = true;
						});
					}
				}
				Gtk.Application.Invoke (delegate (object sender, EventArgs args){
					results.IsChanging = false;
				});
			});
		}// GetTypesVisibleFrom
		
		/// <summary>
		/// Get symbols visible from a given source location
		/// </summary>
		public void GetSymbolsVisibleFrom (string filename, int line, int column, ValaCompletionDataList results) 
		{
			results.IsChanging = true;
			
			ThreadPool.QueueUserWorkItem (delegate{
				string[] output = ParseCommand ("visible-symbols {0} {1} {2}", filename, line, column);
				CodeNode child;
				
				foreach (string outputline in output) {
					if (null != (child = ParseType (string.Empty, outputline))) {
						CompletionData datum = new CompletionData (child);
						Gtk.Application.Invoke (delegate (object sender, EventArgs args){
							results.Add (datum);
							results.IsChanging = true;
						});
					}
				}
				Gtk.Application.Invoke (delegate (object sender, EventArgs args){
					results.IsChanging = false;
				});
			});
		}// GetSymbolsVisibleFrom
		
		private static Regex completionRegex = new Regex (@"^\s*vsc-shell - (?<type>[^:]+):(?<name>[^\s:]+)(:(?<modifier>[^;]*);(?<static>[^:]*))?(:(?<returntype>[^;]*);(?<ownership>[^:]*))?(:(?<args>[^:]*);)?((?<file>[^:]*):(?<first_line>\d+);((?<last_line>\d+);)?)?", RegexOptions.Compiled);
		/// <summary>
		/// Parse out a CodeNode from a vsc-shell description string
		/// </summary>
		private CodeNode ParseType (string typename, string typeDescription)
		{
			Match match = completionRegex.Match (typeDescription);
			
			if (match.Success) {
				string childType = match.Groups["type"].Value;
				string name = match.Groups["name"].Value;
				AccessModifier access = AccessModifier.Public;
				string[] argtokens = typename.Split ('.');
				string baseTypeName = argtokens[argtokens.Length-1];
				string file = match.Groups["file"].Success? match.Groups["file"].Value: string.Empty;
				int first_line = match.Groups["first_line"].Success? int.Parse(match.Groups["first_line"].Value): 0;
				int last_line = match.Groups["last_line"].Success? int.Parse(match.Groups["last_line"].Value): first_line;
				
				switch (match.Groups["modifier"].Value) {
				case "private":
					access = AccessModifier.Private;
					break;
				case "protected":
					access = AccessModifier.Protected;
					break;
				case "internal":
					access = AccessModifier.Internal;
					break;
				default:
					access = AccessModifier.Public;
					break;
				}
				
				switch (childType) {
				case "method":
					List<KeyValuePair<string,string>> paramlist = new List<KeyValuePair<string,string>>();
					string returnType = (match.Groups["returntype"].Success)? match.Groups["returntype"].Value: string.Empty;
					if (name == baseTypeName || name.StartsWith (baseTypeName + ".")) {
						returnType = string.Empty;
					}
					
					if (match.Groups["args"].Success) {
						StringBuilder args = new StringBuilder ();
						foreach (string arg in match.Groups["args"].Value.Split (';')) {
							argtokens = arg.Split (',');
							if (3 == argtokens.Length) {
								paramlist.Add (new KeyValuePair<string,string> (argtokens[0], string.Format("{0} {1}", argtokens[2], argtokens[1])));
							}
						}
					}
					Function function = new Function (childType, name, typename, file, first_line, last_line, access, returnType, paramlist.ToArray ());
					if (!methods.ContainsKey (function.Name)){ methods[function.Name] = new List<Function> (); }
					methods[function.Name].Add (function);
					return function;
					break;
				default:
					return new CodeNode (childType, name, typename, file, first_line, last_line, access);
					break;
				}
			}
			
			return null;
		}// ParseType
		
		/// <summary>
		/// Add results to a ValaCompletionDataList on the GUI thread
		/// </summary>
		private static void AddResults (IList<CodeNode> list, ValaCompletionDataList results) 
		{
			if (null == results){ return; }
			
			Gtk.Application.Invoke (delegate(object sender, EventArgs args){
				results.IsChanging = true;
				foreach (CodeNode node in list) {
					results.Add (new CompletionData (node));
					// results.IsChanging = true;
				}
				results.IsChanging = false;
			});
		}// AddResults
		
		public List<CodeNode> GetClassesForFile (string file)
		{
			List<CodeNode> classes = new List<CodeNode> ();
			AddFile (file);
			CodeNode node = null;
			
			lock(p){ p.StandardInput.WriteLine ("reparse both"); }
			
			foreach (string result in ParseCommand ("get-classes {0}", file)) {
				Console.WriteLine ("get-classes: got {0}", result);
				node = ParseType (string.Empty, result);
				if(null != node){ classes.Add (node); }
			}
			
			return classes;
		}// GetClassesForFile
	}
}
