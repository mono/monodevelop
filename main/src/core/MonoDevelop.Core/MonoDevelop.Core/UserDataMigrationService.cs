// 
// UserDataMigrationService.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2011 Novell, Inc.
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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Mono.Addins;
using MonoDevelop.Core.AddIns;

namespace MonoDevelop.Core
{
	static class UserDataMigrationService
	{
		static UserProfile profile;
		static string version;
		static bool handlerAdded;
		
		//TODO: it would be nice to migrate custom addins that are installed after first run
		//maybe we would maintain a property with IDs of migrated addins, and check that on addin load
		//and if an unknown addin comes along, re-migrate just nodes of that addin
		public static void SetMigrationSource (UserProfile profile, string version)
		{
			if (UserDataMigrationService.profile != null)
				throw new InvalidOperationException ("Already set");
			if (profile == null)
				throw new ArgumentNullException ("profile");
			if (version == null)
				throw new ArgumentNullException ("version");
			
			UserDataMigrationService.profile = profile;
			UserDataMigrationService.version = version;
		}
		
		public static string SourceVersion {
			get { return version; }
		}
		
		public static bool HasSource {
			get { return version != null; }
		}
		
		public static void StartMigration ()
		{
			if (profile != null && !handlerAdded) {
				AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Core/UserDataMigration", HandleUserDataMigration);
				handlerAdded = true;
			}
		}
		
		static int IndexOfVersion (UserDataMigrationNode node, string version)
		{
			for (int i=0; i < UserProfile.ProfileVersions.Length; i++) {
				if (string.Equals (UserProfile.ProfileVersions[i], version)) {
					return i;
				}
			}
			if (node != null) {
				LoggingService.LogWarning ("Migration in addin '{0}' refers to unknown version '{1}'",
					node.Addin.Id, version);
			}
			return -1;
		}
		
		static bool CheckVersion (UserDataMigrationNode node, string version)
		{
			var sourceVersion = node.SourceVersion;
			
			//exact match
			if (string.Equals (sourceVersion, version)) {
				return true;
			}
			
			//open range of form "X+"
			if (sourceVersion[sourceVersion.Length-1] ==  '+') {
				var idx = IndexOfVersion (null, version);
				var lower = IndexOfVersion (node, sourceVersion.Substring (0, sourceVersion.Length-1));
				return lower >= 0 && idx >= lower;
			}
			
			//closed range of form "X-Y"
			if (sourceVersion.IndexOf ('-') > 0) {
				var split = sourceVersion.Split ('-');
				if (split.Length != 2) {
					throw new Exception ("Invalid migration sourceVersion range in " + node.Addin.Id);
				}
				var idx = IndexOfVersion (null, version);
				var lower = IndexOfVersion (node, split[0]);
				var upper = IndexOfVersion (node, split[1]);
				return lower >= 0 && upper >=0 && idx >= lower && idx >= upper;
			}
			
			return false;
		}
		
		static void HandleUserDataMigration (object sender, ExtensionNodeEventArgs args)
		{
			if (args.Change != ExtensionChange.Add)
				return;
			
			var node = (UserDataMigrationNode) args.ExtensionNode;
			if (!CheckVersion (node, version))
				return;
			
			FilePath source = FilePath.Null;
			FilePath target = FilePath.Null;

			try {
				source = profile.GetLocation (node.SourceKind).Combine (node.SourcePath);
				target = UserProfile.Current.GetLocation (node.TargetKind).Combine (node.TargetPath);

				bool sourceIsDirectory = Directory.Exists (source);

				if (sourceIsDirectory) {
					if (Directory.Exists (target))
						return;
				} else {
					if (File.Exists (target) || Directory.Exists (target) || !File.Exists (source))
						return;
				}
			
				LoggingService.LogInfo ("Migrating '{0}' to '{1}'", source, target);
				if (!sourceIsDirectory)
					FileService.EnsureDirectoryExists (target.ParentDirectory);
				
				var handler = node.GetHandler ();
				if (handler != null) {
					handler.Migrate (source, target);
					return;
				}
				
				if (sourceIsDirectory) {
					DirectoryCopy (source, target);
				} else {
					File.Copy (source, target);
				}
			} catch (Exception ex) {
				string message = string.Format ("{0}: Failed to migrate '{1}' to '{2}'",
					node.Addin.Id, source.ToString () ?? "", target.ToString () ?? "");
				LoggingService.LogError (message, ex);
			}
		}
		
		static void DirectoryCopy (FilePath source, FilePath target)
		{
			// HACK: we were using EnumerateFiles but it's broken in some Mono releases
			// https://bugzilla.xamarin.com/show_bug.cgi?id=2975
			foreach (FilePath f in Directory.GetFiles (source, "*", SearchOption.AllDirectories)) {
				var rel = FileService.AbsoluteToRelativePath (source, f);
				var t = target.Combine (rel);
				var dir = t.ParentDirectory;
				if (!Directory.Exists (dir))
					Directory.CreateDirectory (dir);
				File.Copy (f, t);
			}
		}
	}
	
	public interface IUserDataMigrationHandler
	{
		void Migrate (FilePath source, FilePath target);
	}
}
