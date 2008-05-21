//
// DeployService.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using System.Text;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using Mono.Addins;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Deployment
{
	public static class DeployService
	{
		static List<FileCopyHandler> copiers;
		static DeployDirectoryInfo[] directoryInfos;
		static DeployPlatformInfo[] platformInfos;
		
		public static string[] SupportedArchiveFormats = new string [] {
			".tar", ".tar.gz", ".tar.bz2", ".zip"
		};
		
		static DeployService ()
		{
			AddinManager.ExtensionChanged += delegate (object s, ExtensionEventArgs args) {
				if (args.PathChanged ("/MonoDevelop/DeployService/DeployDirectories"))
					directoryInfos = null;
			};
		}
		
		public static string CurrentPlatform {
			get { return "Linux"; }
		}
		
		public static DeployProperties GetDeployProperties (ProjectFile file)
		{
			DeployProperties props = (DeployProperties) file.ExtendedProperties [typeof(DeployProperties)];
			if (props != null)
				return props;
			props = new DeployProperties (file);
			file.ExtendedProperties [typeof(DeployProperties)] = props;
			return props;
		}
		
		public static PackageBuilder[] GetSupportedPackageBuilders (SolutionItem entry)
		{
			object[] builders = AddinManager.GetExtensionObjects ("/MonoDevelop/DeployService/PackageBuilders", false);
			ArrayList list = new ArrayList ();
			foreach (PackageBuilder builder in builders) {
				if (builder.CanBuild (entry)) {
					PackageBuilder b = builder.Clone ();
					b.InitializeSettings (entry);
					list.Add (b);
				}
			}

			return (PackageBuilder[]) list.ToArray (typeof(PackageBuilder));
		}
		
		public static PackageBuilder[] GetPackageBuilders ()
		{
			return (PackageBuilder[]) AddinManager.GetExtensionObjects ("/MonoDevelop/DeployService/PackageBuilders", typeof(PackageBuilder), false);
		}
		
		public static void Install (IProgressMonitor monitor, SolutionItem entry, string prefix, string appName, string configuration)
		{
			InstallResolver res = new InstallResolver ();
			res.Install (monitor, entry, appName, prefix, configuration);
		}
		
		public static void CreateArchive (IProgressMonitor mon, string folder, string targetFile)
		{
			string tf = Path.GetFileNameWithoutExtension (targetFile);
			if (tf.EndsWith (".tar")) tf = Path.GetFileNameWithoutExtension (tf);
			
			if (File.Exists (targetFile))
				File.Delete (targetFile);
			                 
			// Create the zip file
			ProcessWrapper pw;
			if (targetFile.EndsWith (".tar"))
				pw = Runtime.ProcessService.StartProcess ("tar", "-cvf \"" + targetFile + "\" .", folder, mon.Log, mon.Log, null);
			else if (targetFile.EndsWith (".tar.gz"))
				pw = Runtime.ProcessService.StartProcess ("tar", "-cvzf \"" + targetFile + "\" .", folder, mon.Log, mon.Log, null);
			else if (targetFile.EndsWith (".tar.bz2"))
				pw = Runtime.ProcessService.StartProcess ("tar", "-cvjf \"" + targetFile + "\" .", folder, mon.Log, mon.Log, null);
			else if (targetFile.EndsWith (".zip"))
				pw = Runtime.ProcessService.StartProcess ("zip", "-r \"" + targetFile + "\" .", folder, mon.Log, mon.Log, null);
			else {
				mon.Log.WriteLine ("Unsupported file format: " + Path.GetFileName (targetFile));
				return;
			}
			pw.WaitForOutput ();
		}
		
		internal static string GetArchiveExtension (string fileName)
		{
			if (fileName.EndsWith (".tar.gz"))
				return ".tar.gz";
			else if (fileName.EndsWith (".tar.bz2"))
				return ".tar.bz2";
			else
				return Path.GetExtension (fileName);
		}
		
		public static FileCopyHandler[] GetFileCopyHandlers ()
		{
			InitCopiers ();
			return copiers.ToArray ();
		}
		
		internal static FileCopyHandler GetFileCopyHandler (string id)
		{
			foreach (FileCopyHandler handler in GetFileCopyHandlers ())
				if (handler.Id == id)
					return handler;

			return null;
		}
		
		public static bool BuildPackage (IProgressMonitor mon, Package package)
		{
			return BuildPackage (mon, package.PackageBuilder);
		}
		
		public static bool BuildPackage (IProgressMonitor mon, PackageBuilder builder)
		{
			DeployServiceExtension extensionChain = GetExtensionChain ();
			return extensionChain.BuildPackage (mon, builder);
		}
		
		public static DeployFileCollection GetDeployFiles (DeployContext ctx, SolutionItem[] entries, string configuration)
		{
			DeployFileCollection col = new DeployFileCollection ();
			foreach (SolutionItem e in entries) {
				col.AddRange (GetDeployFiles (ctx, e, configuration));
			}
			return col;
		}
		
		public static DeployFileCollection GetDeployFiles (DeployContext ctx, SolutionItem entry, string configuration)
		{
			ArrayList todel = new ArrayList ();
			
			DeployFileCollection col = GetExtensionChain ().GetDeployFiles (ctx, entry, configuration);
			foreach (DeployFile df in col) {
				if (!ctx.IncludeFile (df)) {
					todel.Add (df);
					continue;
				}
				df.SetContext (ctx);
				if (df.ContainsPathReferences) {
					string name = df.DisplayName;
					df.SourcePath = ProcessFileTemplate (ctx, df.SourcePath);
					df.DisplayName = name;
				}
			}
			foreach (DeployFile df in todel) {
				col.Remove (df);
			}
			return col;
		}
		
		internal static DeployServiceExtension GetExtensionChain ()
		{
			DeployServiceExtension[] extensions = (DeployServiceExtension[]) AddinManager.GetExtensionObjects ("/MonoDevelop/Deployment/DeployServiceExtensions", typeof(DeployServiceExtension), false);
			for (int n=0; n<extensions.Length - 1; n++)
				extensions [n].Next = extensions [n+1];
			return extensions [0];
		}
		
		internal static string GetDeployDirectory (DeployContext ctx, string folderId)
		{
			return GetExtensionChain ().ResolveDirectory (ctx, folderId);
		}
		
		public static DeployDirectoryInfo[] GetDeployDirectoryInfo ()
		{
			if (directoryInfos != null)
				return directoryInfos;
			
			ArrayList list = new ArrayList ();
			foreach (DeployDirectoryNodeType dir in AddinManager.GetExtensionNodes ("/MonoDevelop/Deployment/DeployDirectories"))
				list.Add (dir.GetDeployDirectoryInfo ());
			
			return directoryInfos = (DeployDirectoryInfo[]) list.ToArray (typeof(DeployDirectoryInfo));
		}
		
		public static DeployPlatformInfo[] GetDeployPlatformInfo ()
		{
			if (platformInfos != null)
				return platformInfos;
			
			ArrayList list = new ArrayList ();
			foreach (DeployPlatformNodeType dir in AddinManager.GetExtensionNodes ("/MonoDevelop/Deployment/DeployPlatforms"))
				list.Add (dir.GetDeployPlatformInfo ());
			
			return platformInfos = (DeployPlatformInfo[]) list.ToArray (typeof(DeployPlatformInfo));
		}
		
		internal static string ProcessFileTemplate (DeployContext ctx, string file)
		{
			TextFile tf = TextFile.ReadFile (file);
			string text = tf.Text;
			StringBuilder sb = new StringBuilder ();
			int lastPos = 0;
			int pos = text.IndexOf ('@');
			while (pos != -1) {
				int ep = text.IndexOf ('@', pos+1);
				if (ep == -1)
					break;
				string tag = text.Substring (pos + 1, ep - pos - 1);
				string dir = ctx.GetDirectory (tag);
				if (dir != null) {
					sb.Append (text.Substring (lastPos, pos - lastPos));
					sb.Append (dir);
					lastPos = ep + 1;
				}
				pos = text.IndexOf ('@', ep+1);
			}
			sb.Append (text.Substring (lastPos, text.Length - lastPos));
			string tmp = ctx.CreateTempFile ();
			TextFile.WriteFile (tmp, sb.ToString (), tf.SourceEncoding);
			return tmp;
		}
		
		static void InitCopiers ()
		{
			if (copiers != null)
				return;
			copiers = new List<FileCopyHandler> ();
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Deployment/DeployFileCopiers", OnCopierExtensionChanged);
		}
		
		static void OnCopierExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				copiers.Add (new FileCopyHandler ((IFileCopyHandler)args.ExtensionObject));
			}
			else {
				IFileCopyHandler h = (IFileCopyHandler)args.ExtensionObject;
				foreach (FileCopyHandler c in copiers) {
					if (c.Id == h.Id) {
						copiers.Remove (c);
						break;
					}
				}
			}
		}
	}
}
