//  JavaLanguageBinding.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Reflection;
using System.Resources;
using System.Xml;
using System.CodeDom.Compiler;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core;
using MonoDevelop.Projects.CodeGeneration;

namespace JavaBinding
{
	/// <summary>
	/// This class describes the main functionalaty of a language binding
	/// </summary>
	public class JavaLanguageBinding : IDotNetLanguageBinding
	{
		internal const string LanguageName = "Java";
		static GlobalProperties props = new GlobalProperties ();
		
		public static GlobalProperties Properties {
			get { return props; }
		}
		
		public string Language {
			get {
				return LanguageName;
			}
		}
		
		public bool IsSourceCodeFile (string fileName)
		{
			return Path.GetExtension (fileName) == ".java";
		}
		
		public BuildResult Compile (ProjectItemCollection projectItems, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			return IKVMCompilerManager.Compile (projectItems, configuration, monitor);
		}
		
		public ICloneable CreateCompilationParameters (XmlElement projectOptions)
		{
			JavaCompilerParameters parameters = new JavaCompilerParameters ();
			if (Properties.Classpath.Length > 0)
				parameters.ClassPath = Properties.Classpath;
				
			parameters.Compiler = Properties.CompilerType;
			parameters.CompilerPath = Properties.CompilerCommand;
			
			if (projectOptions != null) {
				if (projectOptions.Attributes["MainClass"] != null) {
					parameters.MainClass = projectOptions.GetAttribute ("MainClass");
				}
				if (projectOptions.Attributes["ClassPath"] != null) {
					parameters.ClassPath += ":" + projectOptions.GetAttribute ("ClassPath");
				}
			}
			return parameters;
		}
		
		// http://www.nbirn.net/Resources/Developers/Conventions/Commenting/Java_Comments.htm#CommentBlock
		public string CommentTag
		{
			get { return "//"; }
		}
		
		public CodeDomProvider GetCodeDomProvider ()
		{
			return null;
		}
		
		public string GetFileName (string baseName)
		{
			return baseName + ".java";
		}
		
		public IParser Parser {
			get { return null; }
		}
		
		public IRefactorer Refactorer {
			get { return null; }
		}
		
		public ClrVersion[] GetSupportedClrVersions ()
		{
			return new ClrVersion[] { ClrVersion.Net_1_1 };
		}
	}
	
	public class GlobalProperties
	{
		Properties props = (Properties) PropertyService.Get ("JavaBinding.GlobalProps", new Properties ());
		
		public string IkvmPath {
			get { return props.Get ("IkvmPath", ""); }
			set { props.Set ("IkvmPath", value != null ? value : ""); }
		}
		
		public string CompilerCommand {
			get { return props.Get ("CompilerCommand", ""); }
			set { props.Set ("CompilerCommand", value != null ? value : "javac"); }
		}
		
		public JavaCompiler CompilerType {
			get { return (JavaCompiler) props.Get ("CompilerType", 0); }
			set { props.Set ("CompilerType", (int)value); }
		}
		
		public string Classpath {
			get { return props.Get ("Classpath", ""); }
			set { props.Set ("Classpath", value != null ? value : ""); }
		}
	}
}
