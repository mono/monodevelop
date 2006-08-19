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

using MonoDevelop.Projects;

namespace MonoDevelop.Autotools
{
	public class AutotoolsContext
	{
		string template_dir = Path.GetDirectoryName ( typeof ( AutotoolsContext ).Assembly.Location ) + "/";
		
		Set autoconfConfigFiles = new Set ();
		Set referencedPackages = new Set();
		Set globalDllReferences = new Set();
		Set compilers = new Set ();
		
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
		
		public AutotoolsContext ( string base_directory, string[] configs )
		{
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

		public string AddReferencedDll ( string dll_name )
		{
			globalDllReferences.Add ( dll_name );
			return String.Format ( "{0}/{1}/{2}", base_dir, libdir, Path.GetFileName (dll_name) );
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

		public IEnumerable GetReferencedDlls ()
		{
			return globalDllReferences;
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
				throw new Exception ( "No known IMakefileHandler for type.");
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
			StringBuilder sb = new StringBuilder ();
			foreach (char c in str)
			{
				if ( char.IsLetterOrDigit (c) || c == '.'  ) sb.Append ( c );
				else if (c == '-' || c == '_' ) sb.Append ( '_' );
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
