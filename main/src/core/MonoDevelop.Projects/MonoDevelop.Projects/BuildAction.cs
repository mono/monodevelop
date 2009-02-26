// 
// BuildAction.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
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
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	
	public static class BuildAction
	{
		public const string None = "None"; //Nothing
		public const string Compile = "Compile";
		public const string EmbeddedResource = "EmbeddedResource"; //EmbedAsResource, "Embed as resource"
		public const string Content = "Content"; //Exclude
		public const string ApplicationDefinition = "ApplicationDefinition";
		public const string Page = "Page";
		public const string Resource = "Resource";
		public const string SplashScreen = "SplashScreen";
		public const string EntityDeploy = "EntityDeploy";
		
		public static string[] StandardActions {
			get {
				return new string[] {
					None,
					Compile,
				};
			}
		}
		
		public static string[] DotNetCommonActions {
			get {
				return new string[] {
					None,
					Compile,
					EmbeddedResource,
				};
			}
		}
		
		public static string[] DotNetActions {
			get {
				return new string[] {
					None,
					Compile,
					Content,
					EmbeddedResource,
					ApplicationDefinition,
					Page,
					Resource,
//					SplashScreen,
//					EntityDeploy
				};
			}
		}
		static Dictionary<string, string> translations = new Dictionary<string, string> ();
		static BuildAction()
		{
			translations[None] = GettextCatalog.GetString ("Nothing");
			translations[Compile] = GettextCatalog.GetString ("Compile");
			translations[EmbeddedResource] = GettextCatalog.GetString ("Embed as resource");
			translations[Content] = GettextCatalog.GetString ("Content");
			translations[ApplicationDefinition] = GettextCatalog.GetString ("Application definition");
			translations[Page] = GettextCatalog.GetString ("Page");
			translations[Resource] = GettextCatalog.GetString ("Resource");
			translations[SplashScreen] = GettextCatalog.GetString ("Splash screen");
			translations[EntityDeploy] = GettextCatalog.GetString ("Entity deploy");
		}
		
		public static string ReTranslate (string translatedAction)
		{
			foreach (KeyValuePair<string, string> translation in translations) {
				if (translation.Value == translatedAction)
					return translation.Key;
			}
			return translatedAction;
		}
		
		public static string Translate (string action)
		{
			string result;
			if (translations.TryGetValue (action, out result))
				return result;
			return action;
		}
		
	}
}
