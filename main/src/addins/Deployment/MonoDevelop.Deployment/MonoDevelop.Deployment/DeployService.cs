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
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core.Execution;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using System.Reflection;
using Mono.Unix.Native;
using Mono.Unix;
using System.Threading.Tasks;

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
			get {
				if (Platform.IsMac)
					return "MacOSX";
				else if (Platform.IsWindows)
					return "Windows";
				else
					return "Linux";
			}
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
		
		public static PackageBuilder[] GetSupportedPackageBuilders (SolutionFolderItem entry)
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
		
		public static void Install (ProgressMonitor monitor, SolutionFolderItem entry, string prefix, string appName, ConfigurationSelector configuration)
		{
			InstallResolver res = new InstallResolver ();
			res.Install (monitor, entry, appName, prefix, configuration);
		}
		
		public static void CreateArchive (ProgressMonitor mon, string folder, string targetFile)
		{
			string tf = Path.GetFileNameWithoutExtension (targetFile);
			if (tf.EndsWith (".tar")) tf = Path.GetFileNameWithoutExtension (tf);
			
			if (File.Exists (targetFile))
				File.Delete (targetFile);
			
			using (Stream os = File.Create (targetFile)) {
	
				Stream outStream = os;
				// Create the zip file
				switch (GetArchiveExtension (targetFile)) {
				case ".tar.gz":
					outStream = new GZipOutputStream(outStream);
					goto case ".tar";
				case ".tar.bz2":
					outStream = new BZip2OutputStream(outStream, 9);
					goto case ".tar";
				case ".tar":
					TarArchive archive = TarArchive.CreateOutputTarArchive (outStream);
					archive.SetAsciiTranslation (false);
					archive.RootPath = folder;
					archive.ProgressMessageEvent += delegate (TarArchive ac, TarEntry e, string message) {
						if (message != null)
							mon.Log.WriteLine (message);
					};

					foreach (FilePath f in GetFilesRec (new DirectoryInfo (folder))) {
						TarEntry entry = TarEntry.CreateEntryFromFile (f);
						entry.Name = f.ToRelative (folder);
						if (!Platform.IsWindows) {
							UnixFileInfo fi = new UnixFileInfo (f);
							entry.TarHeader.Mode = (int)fi.Protection;
						}
						else {
							entry.Name = entry.Name.Replace ('\\', '/');
							FilePermissions p = FilePermissions.S_IFREG | FilePermissions.S_IROTH | FilePermissions.S_IRGRP | FilePermissions.S_IRUSR;
							if (!new FileInfo (f).IsReadOnly)
								p |= FilePermissions.S_IWUSR;
							entry.TarHeader.Mode = (int) p;
						}
						archive.WriteEntry(entry, false);
					}
					
					// HACK: GNU tar expects to find a double zero record at the end of the archive. TarArchive only emits one.
					// This hack generates the second zero block.
					FieldInfo tarOutField = typeof(TarArchive).GetField ("tarOut", BindingFlags.Instance | BindingFlags.NonPublic);
					if (tarOutField != null) {
						TarOutputStream tarOut = (TarOutputStream) tarOutField.GetValue (archive);
						tarOut.Finish ();
					}
					
					archive.CloseArchive ();
					break;
				case ".zip":
					ZipOutputStream zs = new ZipOutputStream (outStream);
					zs.SetLevel(5);
					
					byte[] buffer = new byte [8092];
					foreach (FilePath f in GetFilesRec (new DirectoryInfo (folder))) {
						string name = f.ToRelative (folder);
						if (Platform.IsWindows)
							name = name.Replace ('\\', '/');
						ZipEntry infoEntry = new ZipEntry (name);
						zs.PutNextEntry (infoEntry);
						using (Stream s = File.OpenRead (f)) {
							int nr;
							while ((nr = s.Read (buffer, 0, buffer.Length)) > 0)
								zs.Write (buffer, 0, nr);
						}
						zs.CloseEntry ();
					}
					zs.Finish ();
					zs.Close ();
					break;
				default:
					mon.Log.WriteLine ("Unsupported file format: " + Path.GetFileName (targetFile));
					return;
				}
			}
		}
		
		static IEnumerable<FilePath> GetFilesRec (DirectoryInfo dir)
		{
			foreach (FileSystemInfo si in dir.GetFileSystemInfos ()) {
				if (si is FileInfo)
					yield return si.FullName;
				else {
					foreach (FilePath f in GetFilesRec ((DirectoryInfo)si))
						yield return f;
				}
			}
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
		
		public static Task<bool> BuildPackage (ProgressMonitor mon, Package package)
		{
			return BuildPackage (mon, package.PackageBuilder);
		}
		
		public static Task<bool> BuildPackage (ProgressMonitor mon, PackageBuilder builder)
		{
			return Task<bool>.Factory.StartNew (delegate {
				DeployServiceExtension extensionChain = GetExtensionChain ();
				return extensionChain.BuildPackage (mon, builder);
			});
		}
		
		public static DeployFileCollection GetDeployFiles (DeployContext ctx, SolutionFolderItem[] entries, ConfigurationSelector configuration)
		{
			DeployFileCollection col = new DeployFileCollection ();
			foreach (SolutionFolderItem e in entries) {
				col.AddRange (GetDeployFiles (ctx, e, configuration));
			}
			return col;
		}
		
		public static DeployFileCollection GetDeployFiles (DeployContext ctx, SolutionFolderItem entry, ConfigurationSelector configuration)
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
					sb.Append (text, lastPos, pos - lastPos);
					sb.Append (dir);
					lastPos = ep + 1;
				}
				pos = text.IndexOf ('@', ep+1);
			}
			sb.Append (text, lastPos, text.Length - lastPos);
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
