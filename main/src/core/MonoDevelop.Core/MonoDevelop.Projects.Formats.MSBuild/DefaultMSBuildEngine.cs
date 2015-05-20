//
// DefaultMSBuildEngine.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.Xml;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.BuildEngine;
using MonoDevelop.Core;
using MonoDevelop.Projects.Formats.MSBuild.Conditions;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	class DefaultMSBuildEngine: MSBuildEngine
	{
		Dictionary<FilePath, LoadedProjectInfo> loadedProjects = new Dictionary<FilePath, LoadedProjectInfo> ();

		class LoadedProjectInfo
		{
			public MSBuildProject Project;
			public int ReferenceCount;
		}

		class ProjectInfo
		{
			public MSBuildProject Project;
			public List<MSBuildItemEvaluated> EvaluatedItemsIgnoringCondition = new List<MSBuildItemEvaluated> ();
			public List<MSBuildItemEvaluated> EvaluatedItems = new List<MSBuildItemEvaluated> ();
			public Dictionary<string,PropertyInfo> Properties = new Dictionary<string, PropertyInfo> ();
			public Dictionary<MSBuildImport,string> Imports = new Dictionary<MSBuildImport, string> ();
			public Dictionary<string,string> GlobalProperties = new Dictionary<string, string> ();
			public List<MSBuildTarget> Targets = new List<MSBuildTarget> ();
			public List<MSBuildProject> ReferencedProjects = new List<MSBuildProject> ();
			public Dictionary<MSBuildImport, List<ProjectInfo>> ImportedProjects = new Dictionary<MSBuildImport, List<ProjectInfo>> ();
        }

		class PropertyInfo
		{
			public string Name;
			public string Value;
			public string FinalValue;
			public bool IsImported;
		}

		#region implemented abstract members of MSBuildEngine

		public DefaultMSBuildEngine (MSBuildEngineManager manager): base (manager)
		{
		}

		public override object LoadProject (MSBuildProject project, XmlDocument doc, FilePath fileName)
		{
			return project;
		}

		public override void UnloadProject (object project)
		{
		}

		MSBuildProject LoadProject (FilePath fileName)
		{
			fileName = fileName.CanonicalPath;
			lock (loadedProjects) {
				LoadedProjectInfo pi;
				if (loadedProjects.TryGetValue (fileName, out pi)) {
					pi.ReferenceCount++;
					return pi.Project;
				}
				MSBuildProject p = new MSBuildProject (EngineManager);
				p.Load (fileName);
				loadedProjects [fileName] = new LoadedProjectInfo { Project = p };
				//Console.WriteLine ("Loaded: " + fileName);
				return p;
			}
		}

		void UnloadProject (MSBuildProject project)
		{
			var fileName = project.FileName.CanonicalPath;
			lock (loadedProjects) {
				LoadedProjectInfo pi;
				if (loadedProjects.TryGetValue (fileName, out pi)) {
					pi.ReferenceCount--;
					if (pi.ReferenceCount == 0) {
						loadedProjects.Remove (fileName);
						project.Dispose ();
						//Console.WriteLine ("Unloaded: " + fileName);
					}
				}
			}
		}

		public override object CreateProjectInstance (object project)
		{
			var pi = new ProjectInfo {
				Project = (MSBuildProject) project
			};
			return pi;
		}

		public override void DisposeProjectInstance (object projectInstance)
		{
			var pi = (ProjectInfo) projectInstance;
			foreach (var p in pi.ReferencedProjects)
				UnloadProject (p);
		}

		public override void Evaluate (object projectInstance)
		{
			var pi = (ProjectInfo) projectInstance;

			pi.EvaluatedItemsIgnoringCondition.Clear ();
			pi.EvaluatedItems.Clear ();
			pi.Properties.Clear ();
			pi.Imports.Clear ();
			pi.Targets.Clear ();

			// Unload referenced projects after evaluating to avoid unnecessary unload + load
			var oldRefProjects = pi.ReferencedProjects;
			pi.ReferencedProjects = new List<MSBuildProject> ();

			try {
				var context = new MSBuildEvaluationContext ();
				foreach (var p in pi.GlobalProperties) {
					context.SetPropertyValue (p.Key, p.Value);
					pi.Properties [p.Key] = new PropertyInfo { Name = p.Key, Value = p.Value, FinalValue = p.Value };
				}
				EvaluateProject (pi, context);
			}
			finally {
				foreach (var p in oldRefProjects)
					UnloadProject (p);
				DisposeImportedProjects (pi);
				pi.ImportedProjects.Clear ();
			}
		}

		void EvaluateProject (ProjectInfo pi, MSBuildEvaluationContext context)
		{
			lock (pi.Project) {
				// XmlDocument is not thread safe, so we need to lock while evaluating
				context.InitEvaluation (pi.Project);
				EvaluateObjects (pi, context, pi.Project.GetAllObjects (), false);
				EvaluateObjects (pi, context, pi.Project.GetAllObjects (), true);
			}
		}

		void EvaluateProject (ProjectInfo pi, MSBuildEvaluationContext context, bool evalItems)
		{
			lock (pi.Project) {
				// XmlDocument is not thread safe, so we need to lock while evaluating
				context.InitEvaluation (pi.Project);
				EvaluateObjects (pi, context, pi.Project.GetAllObjects (), evalItems);
			}
		}

		void EvaluateObjects (ProjectInfo pi, MSBuildEvaluationContext context, IEnumerable<MSBuildObject> objects, bool evalItems)
		{
			foreach (var ob in objects) {
				if (evalItems) {
					if (ob is MSBuildItemGroup)
						Evaluate (pi, context, (MSBuildItemGroup)ob);
					else if (ob is MSBuildTarget)
						Evaluate (pi, context, (MSBuildTarget)ob);
				} else {
					if (ob is MSBuildPropertyGroup)
						Evaluate (pi, context, (MSBuildPropertyGroup)ob);
				}
				if (ob is MSBuildImportGroup)
					Evaluate (pi, context, (MSBuildImportGroup)ob, evalItems);
				else if (ob is MSBuildImport)
					Evaluate (pi, context, (MSBuildImport)ob, evalItems);
				else if (ob is MSBuildChoose)
					Evaluate (pi, context, (MSBuildChoose)ob, evalItems);
			}
		}

		void Evaluate (ProjectInfo project, MSBuildEvaluationContext context, MSBuildPropertyGroup group)
		{
			if (!string.IsNullOrEmpty (group.Condition) && !SafeParseAndEvaluate (group.Condition, context))
				return;

			foreach (var prop in group.Element.ChildNodes.OfType<XmlElement> ())
				Evaluate (project, context, prop);
		}

		void Evaluate (ProjectInfo project, MSBuildEvaluationContext context, MSBuildItemGroup items)
		{
			bool conditionIsTrue = true;

			if (!string.IsNullOrEmpty (items.Condition))
				conditionIsTrue = SafeParseAndEvaluate (items.Condition, context);

			foreach (var item in items.Items) {

				XmlElement e;
				context.Evaluate (item.Element, out e);
				var it = CreateEvaluatedItem (project.Project, item, e, e.GetAttribute ("Include"));

				var trueCond = conditionIsTrue && (string.IsNullOrEmpty (it.Condition) || SafeParseAndEvaluate (it.Condition, context));

				var exclude = e.GetAttribute ("Exclude");
				var excludeRegex = !string.IsNullOrEmpty (exclude) ? new Regex (ExcludeToRegex (exclude)) : null;

				if (it.Include.IndexOf (';') == -1)
					AddItem (project, context, item, it, it.Include, excludeRegex, trueCond);
				else {
					foreach (var inc in it.Include.Split (new [] {';'}, StringSplitOptions.RemoveEmptyEntries))
						AddItem (project, context, item, it, inc, excludeRegex, trueCond);
				}
			}
		}

		static void AddItem (ProjectInfo project, MSBuildEvaluationContext context, MSBuildItem item, MSBuildItemEvaluated it, string include, Regex excludeRegex, bool trueCond)
		{
			if (include.IndexOf ('*') != -1) {
				var path = include;
				if (path == "**" || path.EndsWith ("\\**"))
					path = path + "/*";
				var subpath = path.Split ('\\');
				foreach (var eit in ExpandWildcardFilePath (project.Project, context, item, project.Project.BaseDirectory, FilePath.Null, false, subpath, 0)) {
					if (excludeRegex != null && excludeRegex.IsMatch (eit.Include))
						continue;
					project.EvaluatedItemsIgnoringCondition.Add (eit);
					if (trueCond)
						project.EvaluatedItems.Add (eit);
				}
			}
			else if (include != it.Include) {
				if (excludeRegex != null && excludeRegex.IsMatch (include))
					return;
				XmlElement e;
				context.Evaluate (item.Element, out e);
				it = CreateEvaluatedItem (project.Project, item, e, include);
				project.EvaluatedItemsIgnoringCondition.Add (it);
				if (trueCond)
					project.EvaluatedItems.Add (it);
			}
			else {
				if (excludeRegex != null && excludeRegex.IsMatch (include))
					return;
				project.EvaluatedItemsIgnoringCondition.Add (it);
				if (trueCond)
					project.EvaluatedItems.Add (it);
			}
		}

		void Evaluate (ProjectInfo project, MSBuildEvaluationContext context, MSBuildImportGroup imports, bool evalItems)
		{
			if (!string.IsNullOrEmpty (imports.Condition) && !SafeParseAndEvaluate (imports.Condition, context))
				return;

			foreach (var item in imports.Imports)
				Evaluate (project, context, item, evalItems);
		}

		static IEnumerable<MSBuildItemEvaluated> ExpandWildcardFilePath (MSBuildProject project, MSBuildEvaluationContext context, MSBuildItem sourceItem, FilePath basePath, FilePath baseRecursiveDir, bool recursive, string[] filePath, int index)
		{
			var res = Enumerable.Empty<MSBuildItemEvaluated> ();

			if (index >= filePath.Length)
				return res;

			var path = filePath [index];

			if (path == "..")
				return ExpandWildcardFilePath (project, context, sourceItem, basePath.ParentDirectory, baseRecursiveDir, recursive, filePath, index + 1);
			
			if (path == ".")
				return ExpandWildcardFilePath (project, context, sourceItem, basePath, baseRecursiveDir, recursive, filePath, index + 1);

			if (!Directory.Exists (basePath))
				return res;

			if (path == "**") {
				// if this is the last component of the path, there isn't any file specifier, so there is no possible match
				if (index + 1 >= filePath.Length)
					return res;
				
				// If baseRecursiveDir has already been set, don't overwrite it.
				if (baseRecursiveDir.IsNullOrEmpty)
					baseRecursiveDir = basePath;
				
				return ExpandWildcardFilePath (project, context, sourceItem, basePath, baseRecursiveDir, true, filePath, index + 1);
			}

			if (recursive) {
				// Recursive search. Try to match the remaining subpath in all subdirectories.
				foreach (var dir in Directory.GetDirectories (basePath))
					res = res.Concat (ExpandWildcardFilePath (project, context, sourceItem, dir, baseRecursiveDir, true, filePath, index));
			}

			if (index == filePath.Length - 1) {
				// Last path component. It has to be a file specifier.
				string baseDir = basePath.ToRelative (project.BaseDirectory).ToString().Replace ('/','\\');
				if (baseDir == ".")
					baseDir = "";
				else if (!baseDir.EndsWith ("\\", StringComparison.Ordinal))
					baseDir += '\\';
				res = res.Concat (Directory.GetFiles (basePath, path).Select (f => {
					context.SetItemContext (f, basePath.ToRelative (baseRecursiveDir));
					XmlElement e;
					context.Evaluate (sourceItem.Element, out e);
					var ev = baseDir + Path.GetFileName (f);
					return CreateEvaluatedItem (project, sourceItem, e, ev);
				}));
			}
			else {
				// Directory specifier
				// Look for matching directories.
				// The search here is non-recursive, not matter what the 'recursive' parameter says, since we are trying to match a subpath.
				// The recursive search is done below.

				if (path.IndexOfAny (wildcards) != -1) {
					foreach (var dir in Directory.GetDirectories (basePath, path))
						res = res.Concat (ExpandWildcardFilePath (project, context, sourceItem, dir, baseRecursiveDir, false, filePath, index + 1));
				} else
					res = res.Concat (ExpandWildcardFilePath (project, context, sourceItem, basePath.Combine (path), baseRecursiveDir, false, filePath, index + 1));
			}

			return res;
		}

		static string ExcludeToRegex (string exclude)
		{
			var sb = new StringBuilder ();
			foreach (var ep in exclude.Split (new char [] { ';' }, StringSplitOptions.RemoveEmptyEntries)) {
				var ex = ep.Trim ();
                if (sb.Length > 0)
					sb.Append ('|');
				sb.Append ('^');
                for (int n = 0; n < ex.Length; n++) {
					var c = ex [n];
					if (c == '*') {
						if (n < ex.Length - 1 && ex [n + 1] == '*') {
							if (n < ex.Length - 2 && ex [n + 2] == '\\') {
								// zero or more subdirectories
								sb.Append ("(.*\\\\)?");
								n += 2;
							} else {
								sb.Append (".*");
								n++;
							}
						}
						else
							sb.Append ("[^\\\\.]*");
					} else if (regexEscapeChars.Contains (c)) {
						sb.Append ('\\').Append (c);
					} else
						sb.Append (c);
				}
				sb.Append ('$');
			}
            return sb.ToString ();
        }

		static char [] regexEscapeChars = { '\\', '^', '$', '{', '}', '[', ']', '(', ')', '.', '*', '+', '?', '|', '<', '>', '-', '&' };

		static MSBuildItemEvaluated CreateEvaluatedItem (MSBuildProject project, MSBuildItem sourceItem, XmlElement e, string include)
		{
			var it = new MSBuildItemEvaluated (project, e.LocalName, sourceItem.Include, include);
			var md = new Dictionary<string,MSBuildPropertyEvaluated> ();
			foreach (var c in e.ChildNodes.OfType<XmlElement> ()) {
				var cm = sourceItem.Element.ChildNodes.OfType<XmlElement> ().FirstOrDefault (me => me.LocalName == c.LocalName);
				md [c.LocalName] = new MSBuildPropertyEvaluated (project, c.LocalName, cm.InnerXml, c.InnerXml);
			}
			((MSBuildPropertyGroupEvaluated)it.Metadata).SetProperties (md);
			it.SourceItem = sourceItem;
			it.Condition = sourceItem.Condition;
			return it;
		}

		static char[] wildcards = { '*', '%' };

		void Evaluate (ProjectInfo project, MSBuildEvaluationContext context, XmlElement prop)
		{
			XmlElement e;
			context.Evaluate (prop, out e);
			var ep = new MSBuildProperty (project.Project, e);
			if (string.IsNullOrEmpty (ep.Condition) || SafeParseAndEvaluate (ep.Condition, context)) {
				project.Properties [ep.Name] = new PropertyInfo { Name = ep.Name, Value = prop.Value, FinalValue = ep.Value };
				context.SetPropertyValue (prop.Name, ep.Value);
			}
		}

		MSBuildItemEvaluated Evaluate (ProjectInfo project, MSBuildEvaluationContext context, MSBuildItem item)
		{
			XmlElement e;
			context.Evaluate (item.Element, out e);
			return CreateEvaluatedItem (project.Project, item, e, e.GetAttribute ("Include"));
		}

		IEnumerable<ProjectInfo> GetImportedProjects (ProjectInfo project, MSBuildImport import)
		{
			List<ProjectInfo> prefProjects;
			if (project.ImportedProjects.TryGetValue (import, out prefProjects))
				return prefProjects;
			return Enumerable.Empty<ProjectInfo> ();
		}

		void AddImportedProject (ProjectInfo project, MSBuildImport import, ProjectInfo imported)
		{
			List<ProjectInfo> prefProjects;
			if (!project.ImportedProjects.TryGetValue (import, out prefProjects))
				project.ImportedProjects [import] = prefProjects = new List<ProjectInfo> ();
			prefProjects.Add (imported);
        }

		void DisposeImportedProjects (ProjectInfo pi)
		{
			foreach (var imported in pi.ImportedProjects.Values.SelectMany (i => i)) {
				DisposeImportedProjects (imported);
				DisposeProjectInstance (imported);
			}
		}

		void Evaluate (ProjectInfo project, MSBuildEvaluationContext context, MSBuildImport import, bool evalItems)
		{
			if (evalItems) {
				// Properties have already been evaluated
				// Don't evaluate properties, only items and other elements
				foreach (var p in GetImportedProjects (project, import)) {
					
					EvaluateProject (p, new MSBuildEvaluationContext (context), true);

					foreach (var it in p.EvaluatedItems) {
						it.IsImported = true;
						project.EvaluatedItems.Add (it);
					}
					foreach (var it in p.EvaluatedItemsIgnoringCondition) {
						it.IsImported = true;
						project.EvaluatedItemsIgnoringCondition.Add (it);
					}
					foreach (var t in p.Targets) {
						t.IsImported = true;
						project.Targets.Add (t);
					}
				}
				return;
            }

			var pr = context.EvaluateString (import.Project);
			project.Imports [import] = pr;

			if (!string.IsNullOrEmpty (import.Condition) && !SafeParseAndEvaluate (import.Condition, context))
				return;

			var path = MSBuildProjectService.FromMSBuildPath (project.Project.BaseDirectory, pr);
			var fileName = Path.GetFileName (path);
			if (fileName.IndexOfAny (new [] {'*','?'}) == -1)
				ImportFile (project, context, import, path);
			else {
				var files = Directory.GetFiles (Path.GetDirectoryName (path), fileName).ToList ();
				files.Sort ();
				foreach (var file in files)
					ImportFile (project, context, import, file);
			}
		}

		void ImportFile (ProjectInfo project, MSBuildEvaluationContext context, MSBuildImport import, string file)
		{
			if (!File.Exists (file))
				return;
			
			var pref = LoadProject (file);
			project.ReferencedProjects.Add (pref);

			var prefProject = new ProjectInfo { Project = pref };
			AddImportedProject (project, import, prefProject);

			var refCtx = new MSBuildEvaluationContext (context);

			EvaluateProject (prefProject, refCtx, false);

			foreach (var p in prefProject.Properties) {
				p.Value.IsImported = true;
				project.Properties [p.Key] = p.Value;
			}
		}

		void Evaluate (ProjectInfo project, MSBuildEvaluationContext context, MSBuildChoose choose, bool evalItems)
		{
			foreach (var op in choose.GetOptions ()) {
				if (op.IsOtherwise || SafeParseAndEvaluate (op.Condition, context)) {
					EvaluateObjects (project, context, op.GetAllObjects (), evalItems);
					break;
				}
			}
		}

		void Evaluate (ProjectInfo project, MSBuildEvaluationContext context, MSBuildTarget target)
		{
			XmlElement e;
			context.Evaluate (target.Element, out e);
			project.Targets.Add (new MSBuildTarget (e));
		}

		static bool SafeParseAndEvaluate (string cond, MSBuildEvaluationContext context)
		{
			try {
				return ConditionParser.ParseAndEvaluate (cond, context);
			}
			catch {
				// The condition is likely to be invalid
				return false;
			}
		}

		public override bool GetItemHasMetadata (object item, string name)
		{
			var it = item as MSBuildItem;
			if (it != null)
				return it.Metadata.HasProperty (name);
			return ((IMSBuildItemEvaluated) item).Metadata.HasProperty (name);
		}

		public override string GetItemMetadata (object item, string name)
		{
			var it = item as MSBuildItem;
			if (it != null)
				return it.Metadata.GetValue (name);
			return ((IMSBuildItemEvaluated)item).Metadata.GetValue (name);
		}

		public override string GetEvaluatedItemMetadata (object item, string name)
		{
			IMSBuildItemEvaluated it = (IMSBuildItemEvaluated) item;
			return it.Metadata.GetValue (name);
		}

		public override IEnumerable<object> GetImports (object projectInstance)
		{
			return ((ProjectInfo)projectInstance).Project.Imports;
		}

		public override string GetImportEvaluatedProjectPath (object projectInstance, object import)
		{
			return ((ProjectInfo)projectInstance).Imports [(MSBuildImport)import];
		}

		public override IEnumerable<object> GetEvaluatedItems (object projectInstance)
		{
			return ((ProjectInfo)projectInstance).EvaluatedItems;
		}

		public override IEnumerable<object> GetEvaluatedItemsIgnoringCondition (object projectInstance)
		{
			return ((ProjectInfo)projectInstance).EvaluatedItemsIgnoringCondition;
		}

		public override IEnumerable<object> GetEvaluatedProperties (object projectInstance)
		{
			return ((ProjectInfo)projectInstance).Properties.Values;
		}

		public override void GetItemInfo (object item, out string name, out string include, out string finalItemSpec, out bool imported)
		{
			var it = (MSBuildItem) item;
			name = it.Name;
			include = it.Include;
			finalItemSpec = it.Include;
			imported = it.IsImported;
		}

		public override void GetEvaluatedItemInfo (object item, out string name, out string include, out string finalItemSpec, out bool imported)
		{
			var it = (IMSBuildItemEvaluated) item;
			name = it.Name;
			include = it.Include;
			finalItemSpec = it.Include;
			imported = it.IsImported;
		}

		public override void GetPropertyInfo (object property, out string name, out string value, out string finalValue)
		{
			var prop = (PropertyInfo)property;
			name = prop.Name;
			value = prop.Value;
			finalValue = prop.FinalValue;
		}

		public override IEnumerable<MSBuildTarget> GetTargets (object projectInstance)
		{
			return ((ProjectInfo)projectInstance).Targets;
		}

		public override void SetGlobalProperty (object projectInstance, string property, string value)
		{
			var pi = (ProjectInfo)projectInstance;
			pi.GlobalProperties [property] = value;
		}

		public override void RemoveGlobalProperty (object projectInstance, string property)
		{
			var pi = (ProjectInfo)projectInstance;
			pi.GlobalProperties.Remove (property);
		}

		#endregion
	}
}
