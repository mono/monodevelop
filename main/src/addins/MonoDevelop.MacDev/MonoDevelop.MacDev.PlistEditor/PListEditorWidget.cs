// 
// PListEditorWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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
using Gdk;
using Gtk;
using MonoDevelop.Core;
using System.Collections.Generic;
using MonoMac.Foundation;
using MonoDevelop.Components;
using Mono.TextEditor;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using System.Linq;

namespace MonoDevelop.MacDev.PlistEditor
{
	[System.ComponentModel.ToolboxItem(false)]
	public partial class PListEditorWidget : Gtk.Bin
	{
		Project proj;
		
		public Project Project {
			get {
				return proj;
			}
		}
		
		public PDictionary NSDictionary {
			get {
				return customProperties.NSDictionary;
			}
			set {
				customProperties.NSDictionary = value;
				iOSApplicationTargetWidget.Dict = value;
				iPhoneDeploymentInfo.Dict = value;
				iPadDeploymentInfo.Dict = value;
				value.Changed += HandleValueChanged;
				Update ();
			}
		}

		void HandleValueChanged (object sender, EventArgs e)
		{
			Update (true);
		}
		
		CustomPropertiesWidget customProperties = new CustomPropertiesWidget ();
		
		IOSApplicationTargetWidget iOSApplicationTargetWidget;
		IPhoneDeploymentInfo iPhoneDeploymentInfo;
		IPadDeploymentInfo iPadDeploymentInfo;
		MacExpander iosApplicationTargetContainer, iPhoneDeploymentInfoContainer, iPadDeploymentInfoContainer;
		
		ExpanderList documentTypeList = new ExpanderList (GettextCatalog.GetString ("No Document Types"), GettextCatalog.GetString ("Add Document Type"));
		ExpanderList exportedUTIList = new ExpanderList (GettextCatalog.GetString ("No Exported UTIs"), GettextCatalog.GetString ("Add Exported UTI"));
		ExpanderList importedUTIList = new ExpanderList (GettextCatalog.GetString ("No Imported UTIs"), GettextCatalog.GetString ("Add Imported UTI"));
		ExpanderList urlTypeList = new ExpanderList (GettextCatalog.GetString ("No URL Types"), GettextCatalog.GetString ("Add URL Type"));
		
		Dictionary<string, Pixbuf> iconFiles = new Dictionary<string, Pixbuf> ();

		public Dictionary<string, Pixbuf> IconFiles {
			get {
				return this.iconFiles;
			}
		}
		
		void DisposeIcons ()
		{
			foreach (var pixbuf in iconFiles.Values)
				pixbuf.Dispose ();
			iconFiles.Clear ();
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
			
			iconFiles[selectedPixbuf] = new Pixbuf (Project.GetAbsoluteChildPath (selectedPixbuf));
			
			var icons = NSDictionary.GetArray ("CFBundleIconFiles");
			icons.Clear ();
			foreach (var key in iconFiles.Keys) {
				icons.Add (new PString (key));
			}
			icons.QueueRebuild ();
			
			Update ();
			
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			DisposeIcons ();
		}
		
		public PListEditorWidget (Project proj)
		{
			
			this.proj = proj;
			this.Build ();
			
			var summaryScrolledWindow = new CompactScrolledWindow ();
			var summaryLabel = new Label (GettextCatalog.GetString ("Summary"));
			summaryLabel.Show ();
			notebook1.PrependPage (summaryScrolledWindow, summaryLabel);
			var summaryVbox = new VBox (false, 0);
			summaryScrolledWindow.AddWithViewport (summaryVbox);
			
			
			iOSApplicationTargetWidget = new IOSApplicationTargetWidget ();
			iPhoneDeploymentInfo = new IPhoneDeploymentInfo (this);
			iPadDeploymentInfo = new IPadDeploymentInfo (this);
			
			iosApplicationTargetContainer = new MacExpander () {
				ContentLabel = GettextCatalog.GetString ("iOS Application Target"),
				Expandable = true,
			};
			iosApplicationTargetContainer.SetWidget (iOSApplicationTargetWidget);
			
			iPhoneDeploymentInfoContainer = new MacExpander () {
				ContentLabel = GettextCatalog.GetString ("iPhone / iPod Deployment Info"),
				Expandable = true,
			};
			iPhoneDeploymentInfoContainer.SetWidget (iPhoneDeploymentInfo);
			
			iPadDeploymentInfoContainer = new MacExpander () {
				ContentLabel = GettextCatalog.GetString ("iPad Deployment Info"),
				Expandable = true,
			};
			iPadDeploymentInfoContainer.SetWidget (iPadDeploymentInfo);
			
			summaryVbox.PackStart (iosApplicationTargetContainer, false, false, 0);
			summaryVbox.PackStart (iPhoneDeploymentInfoContainer, false, false, 0);
			summaryVbox.PackStart (iPadDeploymentInfoContainer, false, false, 0);
			summaryScrolledWindow.ShowAll ();
			
			customTargetPropertiesContainer.SetWidget (customProperties);
			
			documentTypeList.CreateNew += delegate {
				var dict = NSDictionary.Get<PArray> ("CFBundleDocumentTypes");
				if (dict == null) {
					NSDictionary["CFBundleDocumentTypes"] = dict = new PArray ();
					NSDictionary.QueueRebuild ();
				}
				var newEntry = new PDictionary ();
				dict.Add (newEntry);
				dict.QueueRebuild ();
				
				var dtw = new DocumentTypeWidget (proj, newEntry);
				dtw.Expander = documentTypeList.AddListItem (GettextCatalog.GetString ("Untitled"), dtw, newEntry);
			};
			
			exportedUTIList.CreateNew += delegate {
				var dict = NSDictionary.Get<PArray> ("UTExportedTypeDeclarations");
				if (dict == null) {
					NSDictionary["UTExportedTypeDeclarations"] = dict = new PArray ();
					NSDictionary.QueueRebuild ();
				}
				var newEntry = new PDictionary ();
				dict.Add (newEntry);
				dict.QueueRebuild ();
				
				var dtw = new DocumentTypeWidget (proj, newEntry);
				dtw.Expander = exportedUTIList.AddListItem (GettextCatalog.GetString ("Untitled"), dtw, newEntry);
			};
			
			importedUTIList.CreateNew += delegate {
				var dict = NSDictionary.Get<PArray> ("UTImportedTypeDeclarations");
				if (dict == null) {
					NSDictionary["UTImportedTypeDeclarations"] = dict = new PArray ();
					NSDictionary.QueueRebuild ();
				}
				var newEntry = new PDictionary ();
				dict.Add (newEntry);
				dict.QueueRebuild ();
				
				var dtw = new DocumentTypeWidget (proj, newEntry);
				dtw.Expander = importedUTIList.AddListItem (GettextCatalog.GetString ("Untitled"), dtw, newEntry);
			};
			
			urlTypeList.CreateNew += delegate {
				var dict = NSDictionary.Get<PArray> ("CFBundleURLTypes");
				if (dict == null) {
					NSDictionary["CFBundleURLTypes"] = dict = new PArray ();
					NSDictionary.QueueRebuild ();
				}
				var newEntry = new PDictionary ();
				dict.Add (newEntry);
				dict.QueueRebuild ();
				
				var dtw = new URLTypeWidget (proj, newEntry);
				dtw.Expander = urlTypeList.AddListItem (GettextCatalog.GetString ("Untitled"), dtw, newEntry);
			};
			
			documentTypeExpander.SetWidget (documentTypeList);
			exportedUTIExpander.SetWidget (exportedUTIList);
			importedUTIExpander.SetWidget (importedUTIList);
			urlTypeExpander.SetWidget (urlTypeList);
		}
		
		void Update (bool soft = false)
		{
			DisposeIcons ();
			
			var icons = NSDictionary.Get<PArray> ("CFBundleIconFiles");
			
			if (icons != null) {
				foreach (PString icon in icons.Where (v => v is PString)) {
					iconFiles[icon.Value] = new Pixbuf (Project.GetAbsoluteChildPath (icon.Value));
				}
			}
			
			iOSApplicationTargetWidget.Update ();
			iPhoneDeploymentInfo.Update ();
			iPadDeploymentInfo.Update ();
			
			var iphone = NSDictionary.Get<PArray> ("UISupportedInterfaceOrientations");
			iPhoneDeploymentInfoContainer.Visible = iphone != null;
			
			var ipad   = NSDictionary.Get<PArray> ("UISupportedInterfaceOrientations~ipad");
			iPadDeploymentInfoContainer.Visible = ipad != null;
			
			if (!soft) {
				var documentTypes = NSDictionary.Get<PArray> ("CFBundleDocumentTypes");
				documentTypeList.Clear ();
				if (documentTypes != null) {
					foreach (var pObject in documentTypes) {
						var dict = (PDictionary)pObject;
						if (dict == null)
							continue;
						string name = GettextCatalog.GetString ("Untitled");
						var dtw = new DocumentTypeWidget (proj, dict);
						dtw.Expander = documentTypeList.AddListItem (name, dtw, dict);
						
					}
				}
				
				var exportedUTIs = NSDictionary.Get<PArray> ("UTExportedTypeDeclarations");
				exportedUTIList.Clear ();
				if (exportedUTIs != null) {
					foreach (var pObject in exportedUTIs) {
						var dict = (PDictionary)pObject;
						if (dict == null)
							continue;
						string name = GettextCatalog.GetString ("Untitled");
						var dtw = new UTIWidget (proj, dict);
						dtw.Expander = exportedUTIList.AddListItem (name, dtw, dict);
					}
				}
				
				var importedUTIs = NSDictionary.Get<PArray> ("UTImportedTypeDeclarations");
				importedUTIList.Clear ();
				if (importedUTIs != null) {
					foreach (var pObject in importedUTIs) {
						var dict = (PDictionary)pObject;
						if (dict == null)
							continue;
						string name = GettextCatalog.GetString ("Untitled");
						var dtw = new UTIWidget (proj, dict);
						dtw.Expander = importedUTIList.AddListItem (name, dtw, dict);
					}
				}
				
				var urlTypes = NSDictionary.Get<PArray> ("CFBundleURLTypes");
				urlTypeList.Clear ();
				
				if (urlTypes != null) {
					foreach (var pObject in urlTypes) {
						var dict = (PDictionary)pObject;
						if (dict == null)
							continue;
						string name = GettextCatalog.GetString ("Untitled");
						var dtw = new URLTypeWidget (proj, dict);
						dtw.Expander = urlTypeList.AddListItem (name, dtw, dict);
					}
				}
			}
		}
	}
}

