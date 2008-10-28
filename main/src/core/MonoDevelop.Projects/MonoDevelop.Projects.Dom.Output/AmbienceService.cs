//
// AmbienceService.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

using Mono.Addins;

namespace MonoDevelop.Projects.Dom.Output
{
	public static class AmbienceService
	{
		static Ambience defaultAmbience                = new NetAmbience ();
		static Dictionary <string, Ambience> ambiences= new Dictionary <string, Ambience> ();
		
		static AmbienceService ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/ProjectModel/Ambiences", delegate(object sender, ExtensionNodeEventArgs args) {
					Ambience ambience = args.ExtensionObject as Ambience;
					if (ambience == null)
						return;
					string[] mimeTypes = ambience.MimeTypes.Split (';');
						
					switch (args.Change) {
					case ExtensionChange.Add:
						foreach (string mimeType in mimeTypes)
							ambiences[mimeType] = ambience;
						break;
					case ExtensionChange.Remove:
						foreach (string mimeType in mimeTypes) {
							if (ambiences.ContainsKey (mimeType))
								ambiences.Remove (mimeType);
						}
						break;
					}
				});
		}
		
		public static Ambience GetAmbience (IMember member)
		{
			if (member.DeclaringType != null && member.DeclaringType.CompilationUnit != null)
				return GetAmbienceForFile (member.DeclaringType.CompilationUnit.FileName);
			return defaultAmbience;
		}
		
		public static Ambience GetAmbienceForFile (string fileName)
		{
			foreach (Ambience ambience in ambiences.Values) {
				if (ambience.IsValidFor (fileName))
					return ambience;
			}
			return defaultAmbience;
		}
		
		public static Ambience GetAmbience (string mimeType)
		{
			Ambience result;
			ambiences.TryGetValue (mimeType, out result);
			return result ?? defaultAmbience;
		}
		
		public static Ambience GetAmbienceForLanguage (string mimeType)
		{
			Ambience result;
			ambiences.TryGetValue (mimeType, out result);
			return result ?? defaultAmbience;
		}
		
	}
}
