// 
// IPlistEditingHandler.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using Gdk;

namespace MonoDevelop.MacDev.PlistEditor
{
	public interface IPlistEditingHandler
	{
		bool CanHandle (Project project, string projectVirtualPath);
		IEnumerable<PlistEditingSection> GetSections (Project project, PDictionary dictionary);
	}
	
	public class PlistEditingSection
	{
		public PlistEditingSection (string name, Gtk.Widget widget)
		{
			this.Name = name;
			this.Widget = widget;
		}
		
		public string Name { get; private set; }
		public Gtk.Widget Widget { get; private set; }
		public bool IsAdvanced { get; set; }
	}
	
	class IPhonePlistEditingHandler : IPlistEditingHandler
	{
		public bool CanHandle (Project project, string projectVirtualPath)
		{
			return true;
		}
		
		public IEnumerable<PlistEditingSection> GetSections (Project project, PDictionary dictionary)
		{
			var targetWidget = new IOSApplicationTargetWidget (dictionary);
			yield return new PlistEditingSection (GettextCatalog.GetString ("iOS Application Target"), targetWidget);
			
			var iconFileManager = new PlistIconFileManager (project, dictionary);
			targetWidget.Destroyed += delegate {
				iconFileManager.Dispose ();
			};
			
			yield return new PlistEditingSection (
				GettextCatalog.GetString ("iPhone / iPod Deployment Info"),
				new IPhoneDeploymentInfo (project, dictionary, iconFileManager));
			
			yield return new PlistEditingSection (
				GettextCatalog.GetString ("iPad Deployment Info"),
				new IPadDeploymentInfo (project, dictionary, iconFileManager));
			
			yield return new PlistEditingSection (
				GettextCatalog.GetString ("Custom iOS Target Properties"),
				new CustomPropertiesWidget (PListScheme.Scheme) { NSDictionary = dictionary })
					{ IsAdvanced = true };
			
			yield return new PlistEditingSection (
				GettextCatalog.GetString ("Document Types"),
				new DocumentTypeListWidget (project, dictionary)) { IsAdvanced = true };
			
			yield return new PlistEditingSection (
				GettextCatalog.GetString ("Exported UTIs"),
				new ExportedUtiListWidget (project, dictionary)) { IsAdvanced = true };
			
			yield return new PlistEditingSection (
				GettextCatalog.GetString ("Imported UTIs"),
				new ImportedUtiListWidget (project, dictionary)) { IsAdvanced = true };
			
			yield return new PlistEditingSection (
				GettextCatalog.GetString ("URL Types"),
				new UrlTypeListWidget (project, dictionary)) { IsAdvanced = true };
		}
	}
	
	public class PlistIconFileManager : IDisposable
	{
		Project project;
		PDictionary dictionary;
		Dictionary<string, Pixbuf> iconFiles = new Dictionary<string, Pixbuf> ();
		
		public PlistIconFileManager (Project project, PDictionary dictionary)
		{
			this.project = project;
			this.dictionary = dictionary;
			dictionary.Changed += DictionaryChanged;
			Update ();
		}

		void DictionaryChanged (object sender, EventArgs e)
		{
			Update ();
		}
		
		void Update ()
		{
			DisposeIcons ();
			var icons = dictionary.Get<PArray> ("CFBundleIconFiles");
			if (icons != null) {
				foreach (PString icon in icons.Where (v => v is PString)) {
					iconFiles[icon.Value] = new Pixbuf (project.GetAbsoluteChildPath (icon.Value));
				}
			}
		}
		
		void DisposeIcons ()
		{
			foreach (var pixbuf in iconFiles.Values)
				pixbuf.Dispose ();
			iconFiles.Clear ();
		}
		
		public Pixbuf GetIcon (int width, int height)
		{
			foreach (var val in iconFiles) {
				if (val.Value.Width == width && val.Value.Height == height) {
					return val.Value;
				}
			}
			return null;
		}

		public void SetIcon (FilePath selectedPixbuf, int width, int height)
		{
			foreach (var val in iconFiles) {
				if (val.Value.Width == width && val.Value.Height == height) {
					val.Value.Dispose ();
					iconFiles.Remove (val.Key);
					break;
				}
			}
			
			iconFiles[selectedPixbuf] = new Pixbuf (project.GetAbsoluteChildPath (selectedPixbuf));
			
			var icons = dictionary.GetArray ("CFBundleIconFiles");
			icons.Clear ();
			foreach (var key in iconFiles.Keys) {
				icons.Add (new PString (key));
			}
			icons.QueueRebuild ();
		}
		
		public void Dispose ()
		{
			DisposeIcons ();
		}
	}
}