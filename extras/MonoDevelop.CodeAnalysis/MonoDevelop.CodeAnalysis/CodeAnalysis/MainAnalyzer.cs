using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;

using MonoDevelop.Core;
using MonoDevelop.Projects;

using MonoDevelop.CodeAnalysis.Gui;

namespace MonoDevelop.CodeAnalysis {

	/// <summary>
	/// Class that encapsulates independent analyzers (Gendarme, Smokey), loads them
	/// and performs analysis.
	/// </summary>
	static class MainAnalyzer {
		private static bool is_busy = false;
		private static DotNetProject current_project = null;
		private static List<IAnalyzer> analyzers;
		
		static void LoadAnalyzersIfNeccessary ()
		{
			if (analyzers == null)
				LoadAnalyzers ();
		}

		/// <summary>
		/// This method loads available analyzers from the same directory
		/// where current assembly is located (most probably, MD AddIns directory).
		/// Every analyzer plugin must have a file name like
		/// 'MonoDevelop.CodeAnalysis.*.dll' in order to be loaded.
		/// Also each plugin assembly must have a [AssemblyAnalyzer] attribute
		/// applied, pointing to the concrete class implementing IAnalyzer.
		///
		/// example: MonoDevelop.CodeAnalysis.Gendarme.dll has
		///  [assembly:AssemblyAnalyzer (typeof (GendarmeAnalyzer))]
		/// </summary>
		static void LoadAnalyzers ()
		{
			analyzers = new List<IAnalyzer> ();
			string path = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location);
			
			foreach (string dll in Directory.GetFiles (path, "MonoDevelop.CodeAnalysis.*.dll", SearchOption.TopDirectoryOnly)) {
				try {
					Assembly lib = Assembly.LoadFile (dll);
					
					IAnalyzer analyzer = CreateAnalyzer (lib);

					if (analyzer != null)
						analyzers.Add (analyzer);
				} catch { 
					continue;
				}
			}
		}

		/// <summary>
		/// Creates and returns analyzer (if any) for a plugin assembly.
		/// </summary>
		static IAnalyzer CreateAnalyzer (Assembly library)
		{
			object [] attrs = library.GetCustomAttributes (typeof (AssemblyAnalyzerAttribute), false);
			if (attrs.Length == 0)
				return null;
			
			Type Type = ((AssemblyAnalyzerAttribute) attrs [0]).Type;
			return (IAnalyzer) Activator.CreateInstance (Type);		
		}

		/// <value>
		/// Indicates if the analyzer is busy now and cannot perform another analysis.
		/// </value>
		public static bool IsBusy {
			get { return is_busy; }
		}		
		
		/// <value>
		/// Points to the current project being analyzed (if any).
		/// </value>
		public static DotNetProject CurrentProject {
			get { return current_project; }
		}		
		
		/// <summary>
		/// Indicates if analyzer can handle specified type of entry.
		/// </summary>
		public static bool CanAnalyze (object entry)
		{
			return entry is DotNetProject || entry is Solution;
		}
		
		/// <summary>
		/// Begins analysis operation (and starts new thread) for the specified entry.
		/// </summary>
		public static void BeginAnalysis (SolutionItem entry)
		{	
			if (is_busy) // this should never be true (lock GUI, etc)
				throw new InvalidOperationException ();
			
			is_busy = true;
			Thread thread = new Thread (DoAnalyze);
			thread.IsBackground = true;
			thread.Start (entry);
		}
	
		/// <summary>
		/// Starts main analysis and reports the results.
		/// </summary>
		static void DoAnalyze (object param)
		{
			SolutionItem entry = param as SolutionItem;
			
			try {				
				ResultsReporter.AnalysisStarted (entry.Name);
				AnalyzeCombineEntry (entry, 1.0);				
			} catch (CodeAnalysisException ex) {
				ResultsReporter.ReportError (ex);
			} finally {
				is_busy = false;
				ResultsReporter.AnalysisFinished ();
			}
		}
		
		/// <summary>
		/// Runs analysis on a combine entry, if applicable.
		/// </summary>
		static void AnalyzeCombineEntry (object entry, double work)
		{			
			if (entry is Solution)
				AnalyzeCombine ((Solution)entry, work);
			
			else if (entry is DotNetProject)
				AnalyzeProject ((DotNetProject)entry, work);
		}

		/// <summary>
		/// Enumerates each entry in a combine and runs analysis for it.
		/// </summary>
		static void AnalyzeCombine (Solution combine, double work)
		{			
			List<SolutionItem> entriesToAnalyze = new List<SolutionItem> ();
			ReadOnlyCollection<SolutionItem> children = combine.GetAllSolutionItems ();
			
			foreach (object child in children)
				if (child != combine && CanAnalyze (child))
					entriesToAnalyze.Add (child as SolutionItem);
			
			double entryWork = work / entriesToAnalyze.Count;
			
			foreach (SolutionItem childEntry in entriesToAnalyze) {
				AnalyzeCombineEntry (childEntry, entryWork);
			}
		}

		/// <summary>
		/// Performs analysis on specified project and reports violations to GUI.
		/// </summary>
		static void AnalyzeProject (DotNetProject project, double work)
		{	
			current_project = project;
			try {
				IEnumerable<IViolation> violations = RunAnalyzers (current_project, work);				
				ResultsReporter.ReportViolations (violations);
			} catch (CodeAnalysisException) {
				throw;
			} catch (Exception ex) {
				throw new CodeAnalysisException (AddinCatalog.GetString ("Analysis failed because of unexpected error: {0}. Please, contact the plugin developers.", ex), ex);
			} finally {
				current_project = null;
			}
		}
		
		/// <summary>
		/// Determines which analyzers to run, which rule sets to use (TODO)
		/// and invokes the runners.
		/// </summary>
		static IEnumerable<IViolation> RunAnalyzers (DotNetProject project, double work)
		{
			string dll = project.GetOutputFileName (project.DefaultConfiguration.Name);
			if (!File.Exists (dll))
				yield break;
			
			LoadAnalyzersIfNeccessary ();
			
			if (analyzers.Count == 0)
				yield break;
			
			double analyzerWork = work / analyzers.Count;			
			
			foreach (IAnalyzer analyzer in analyzers) {
				IEnumerable<IRule> ruleSet = GetRuleSet (project, analyzer.GetRuleLoader ());
				IRunner runner = analyzer.GetRunner ();
				
				IEnumerable<IViolation> violations = runner.Run (dll, ruleSet);
				foreach (IViolation vio in violations)
						yield return vio;
				
				ResultsReporter.WorkComplete += analyzerWork;
			}
		}
		
		/// <summary>
		/// Gets rule set for specified project (TODO: read project configuration).
		/// </summary>
		static IEnumerable<IRule> GetRuleSet (DotNetProject project, IRuleLoader ruleLoader)
		{
			// TODO: retrieve rule set from project configuration
			return ruleLoader.GetRules ();
		}
	}
}
