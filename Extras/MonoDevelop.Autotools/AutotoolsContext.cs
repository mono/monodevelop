/*
Copyright (C) 2006  Matthias Braun <matze@braunis.de>
					Scott Ellington <scott.ellington@gmail.com>
 
This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the
Free Software Foundation, Inc., 59 Temple Place - Suite 330,
Boston, MA 02111-1307, USA.
*/

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Deployment;
using MonoDevelop.Core;

namespace MonoDevelop.Autotools
{
	public class AutotoolsContext
	{
		DeployContext deployContext;
		Hashtable deployDirs = new Hashtable ();
		
		string template_dir = Path.GetDirectoryName ( typeof ( AutotoolsContext ).Assembly.Location ) + "/";
		
		Set autoconfConfigFiles = new Set ();
		Set referencedPackages = new Set();
		Set globalFilesReferences = new Set();
		Set compilers = new Set ();
		ArrayList builtFiles = new ArrayList ();
		Dictionary<string, string> configNamesDict = new Dictionary<string, string> (StringComparer.InvariantCultureIgnoreCase);
		
		public DeployContext DeployContext {
			get { return deployContext; }
		}
		
		string base_dir;
		public string BaseDirectory {
			get {
				return base_dir;
			}
		}
		
		string libdir = "lib";
		public string LibraryDirectory {
			get {
				return String.Format ( "{0}/{1}/", base_dir, libdir );
			}
		}
		
		string[] configurations;
		public IEnumerable SupportedConfigurations {
			get {
				return configurations;
			}
		}

		public string EscapeAndUpperConfigName (string configName)
		{
			if (!configNamesDict.ContainsKey (configName))
				configNamesDict [configName] = EscapeStringForAutoconf (configName).ToUpper ();

			return configNamesDict [configName];
		}

		public AutotoolsContext ( DeployContext deployContext, string base_directory, string[] configs )
		{
			this.deployContext = deployContext;
			base_dir = base_directory;
			configurations = configs;
		}

		public bool IsSupportedConfiguration ( string name )
		{
			foreach ( string s in configurations )
			{
				if ( s == name ) return true;
			}
			return false;
		}
		
		public string GetDeployDirectoryVar (string folderId)
		{
			string dir = (string) deployDirs [folderId];
			if (dir != null)
				return dir;
			dir = EscapeStringForAutoconf (folderId.ToUpper ().Replace (".","_").Replace ("/", "_"));
			deployDirs [folderId] = dir;
			return dir;
		}
		
		public void AddRequiredPackage ( string pkg_name )
		{
			referencedPackages.Add (pkg_name);
		}

		public void AddAutoconfFile ( string file_name )
		{
			if ( autoconfConfigFiles.Contains ( file_name ) )
				throw new Exception ( "file '" + file_name + "' has already been registered to be processed by configure. " );
			autoconfConfigFiles.Add( file_name );
		}

		public void AddCommandCheck ( string command_name )
		{
			compilers.Add ( command_name );
		}
		
		public void AddBuiltFile (string filePath)
		{
			string fpath = Runtime.FileService.GetFullPath (filePath);
			string bd = Runtime.FileService.GetFullPath (BaseDirectory);
			if (fpath.StartsWith (bd + Path.DirectorySeparatorChar) || fpath == bd) {
				string rel = Runtime.FileService.AbsoluteToRelativePath (bd, fpath);
				rel = NormalizeRelativePath (rel);
				rel = "$(top_builddir)" + Path.DirectorySeparatorChar + rel;
				builtFiles.Add (rel);
			}
		}

		public IEnumerable GetAutoConfFiles ()
		{
			return autoconfConfigFiles;
		}

		public IEnumerable GetRequiredPackages ()
		{
			return referencedPackages;
		}

		public IEnumerable GetCommandChecks ()
		{
			return compilers;
		}

		public IEnumerable GetGlobalReferencedFiles ()
		{
			ArrayList list = new ArrayList ();
			foreach (string f in globalFilesReferences)
				if (!builtFiles.Contains (f))
					list.Add (f);
			return list;
		}
		
		public IDictionary GetReferencedTargetDirectories ()
		{
			return deployDirs;
		}
		
		public string GetRelativePath (Project project, string path, bool isGenerated)
		{
			string fpath = Path.GetFullPath (path);
			string bd = Path.GetFullPath (project.BaseDirectory);
			if (fpath.StartsWith (bd + Path.DirectorySeparatorChar) || fpath == bd) {
				string rel = Runtime.FileService.AbsoluteToRelativePath (bd, fpath);
				rel = NormalizeRelativePath (rel);
				if (isGenerated)
					return rel;
				else
					return "$(srcdir)" + Path.DirectorySeparatorChar + rel;
			}
			bd = Path.GetFullPath (BaseDirectory);
			if (fpath.StartsWith (bd + Path.DirectorySeparatorChar) || fpath == bd) {
				string rel = Runtime.FileService.AbsoluteToRelativePath (bd, fpath);
				rel = NormalizeRelativePath (rel);
				string file = "$(top_builddir)" + Path.DirectorySeparatorChar + rel;
				if (builtFiles.Contains (file))
					return file;
				else {
					globalFilesReferences.Add (file);
					return "$(top_srcdir)" + Path.DirectorySeparatorChar + rel;
				}
			}
			throw new InvalidOperationException ("The project '" + project.Name + "' references the file '" + Path.GetFileName (path) + "' which is located outside the solution directory.");
		}
		
		public static string NormalizeRelativePath (string path)
		{
			path = path.Trim (Path.DirectorySeparatorChar,' ');
			while (path.StartsWith ("." + Path.DirectorySeparatorChar)) {
				path = path.Substring (2);
				path = path.Trim (Path.DirectorySeparatorChar,' ');
			}
			if (path == ".")
				return string.Empty;
			else
				return path;
		}
			
		// TODO: add an extension point with which addins can implement 
		// autotools functionality.
		public static IMakefileHandler GetMakefileHandler ( CombineEntry entry )
		{
			if ( entry is Combine )
				return new SolutionMakefileHandler ();
			else if ( entry is Project )
				return new SimpleProjectMakefileHandler ();
			else
				return null;
		}
	
		public static string EscapeStringForAutomake (string str) 
		{
			StringBuilder result = new StringBuilder();
			for(int i = 0; i < str.Length; ++i) {
				char c = str[i];
				if(!Char.IsLetterOrDigit(c) && c != '.' && c != '/' && c != '_' && c != '-')
					result.Append('\\');

				result.Append(c);
			}
			return result.ToString();
		}

		public Stream GetTemplateStream ( string id )
		{
			//return GetType().Assembly.GetManifestResourceStream(id); 
			return new FileStream (template_dir + id, FileMode.Open, FileAccess.Read );
		}

		public static string EscapeStringForAutoconf ( string str )
		{
			return EscapeStringForAutoconf(str, false);
		}
		
		public static string EscapeStringForAutoconf ( string str, bool allowPeriods )
		{
			StringBuilder sb = new StringBuilder ();
			foreach (char c in str)
			{
				if ( char.IsLetterOrDigit (c) || (c == '.' && allowPeriods) ) sb.Append ( c );
				else if (c == '-' || c == '_' || c == ' ' || (c == '.' && !allowPeriods) ) sb.Append ( '_' );
			}
			return sb.ToString ();	
		}
		
		public static string GetPkgConfigVariable (string package)
		{
			StringBuilder sb = new StringBuilder ();
			foreach (char c in package)
			{
				if ( char.IsLetterOrDigit (c) ) sb.Append ( char.ToUpper(c) );
				else if (c == '-' || c == '_' ) sb.Append ( '_' );
			}
			return sb.ToString ();	
		}
	}
}
