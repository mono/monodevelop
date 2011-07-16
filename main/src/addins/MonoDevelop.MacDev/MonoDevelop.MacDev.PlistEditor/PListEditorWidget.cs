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

namespace MonoDevelop.MacDev.PlistEditor
{
	[System.ComponentModel.ToolboxItem(false)]
	public partial class PListEditorWidget : Gtk.Bin
	{
		Project proj;
		
		public PDictionary NSDictionary {
			get {
				return customProperties.NSDictionary;
			}
			set {
				customProperties.NSDictionary = value;
				iOSApplicationTargetWidget.Dict = value;
				iPhoneDeploymentInfo.Dict = value;
				iPadDeploymentInfo.Dict = value;
				Update ();
			}
		}
		CustomPropertiesWidget customProperties = new CustomPropertiesWidget ();
		IOSApplicationTargetWidget iOSApplicationTargetWidget = new IOSApplicationTargetWidget ();
		IPhoneDeploymentInfo iPhoneDeploymentInfo = new IPhoneDeploymentInfo ();
		IPadDeploymentInfo iPadDeploymentInfo = new IPadDeploymentInfo ();
		ExpanderList documentTypeList = new ExpanderList (GettextCatalog.GetString ("No Document Types"), GettextCatalog.GetString ("Add Document Type"));
		ExpanderList exportedUTIList = new ExpanderList (GettextCatalog.GetString ("No Exported UTIs"), GettextCatalog.GetString ("Add Exported UTI"));
		ExpanderList importedUTIList = new ExpanderList (GettextCatalog.GetString ("No Imported UTIs"), GettextCatalog.GetString ("Add Imported UTI"));
		ExpanderList urlTypeList = new ExpanderList (GettextCatalog.GetString ("No URL Types"), GettextCatalog.GetString ("Add URL Type"));
		
		public PListEditorWidget (Project proj)
		{
			this.proj = proj;
			this.Build ();
			
			customTargetPropertiesContainer.SetWidget (customProperties);
			
			iosApplicationTargetContainer.SetWidget (iOSApplicationTargetWidget);
			iPhoneDeploymentInfoContainer.SetWidget (iPhoneDeploymentInfo);
			iPadDeploymentInfoContainer.SetWidget (iPadDeploymentInfo);
			
			documentTypeList.CreateNew += delegate {
				var dict = NSDictionary.Get<PArray> ("CFBundleDocumentTypes");
				if (dict == null) {
					NSDictionary.Value["CFBundleDocumentTypes"] = dict = new PArray () { Parent = NSDictionary };
					NSDictionary.QueueRebuild ();
				}
				var newEntry = new PDictionary ();
				dict.Value.Add (newEntry);
				dict.QueueRebuild ();
				
				var dtw = new DocumentTypeWidget (proj, newEntry);
				dtw.Expander = documentTypeList.AddListItem (GettextCatalog.GetString ("Untitled"), dtw);
			};
			
			exportedUTIList.CreateNew += delegate {
				var dict = NSDictionary.Get<PArray> ("UTExportedTypeDeclarations");
				if (dict == null) {
					NSDictionary.Value["UTExportedTypeDeclarations"] = dict = new PArray () { Parent = NSDictionary };
					NSDictionary.QueueRebuild ();
				}
				var newEntry = new PDictionary ();
				dict.Value.Add (newEntry);
				dict.QueueRebuild ();
				
				var dtw = new DocumentTypeWidget (proj, newEntry);
				dtw.Expander = exportedUTIList.AddListItem (GettextCatalog.GetString ("Untitled"), dtw);
			};
			
			importedUTIList.CreateNew += delegate {
				var dict = NSDictionary.Get<PArray> ("UTImportedTypeDeclarations");
				if (dict == null) {
					NSDictionary.Value["UTImportedTypeDeclarations"] = dict = new PArray () { Parent = NSDictionary };
					NSDictionary.QueueRebuild ();
				}
				var newEntry = new PDictionary ();
				dict.Value.Add (newEntry);
				dict.QueueRebuild ();
				
				var dtw = new DocumentTypeWidget (proj, newEntry);
				dtw.Expander = importedUTIList.AddListItem (GettextCatalog.GetString ("Untitled"), dtw);
			};
			
			urlTypeList.CreateNew += delegate {
				var dict = NSDictionary.Get<PArray> ("CFBundleURLTypes");
				if (dict == null) {
					NSDictionary.Value["CFBundleURLTypes"] = dict = new PArray () { Parent = NSDictionary };
					NSDictionary.QueueRebuild ();
				}
				var newEntry = new PDictionary ();
				dict.Value.Add (newEntry);
				dict.QueueRebuild ();
				
				var dtw = new URLTypeWidget (proj, newEntry);
				dtw.Expander = urlTypeList.AddListItem (GettextCatalog.GetString ("Untitled"), dtw);
			};
			
			documentTypeExpander.SetWidget (documentTypeList);
			exportedUTIExpander.SetWidget (exportedUTIList);
			importedUTIExpander.SetWidget (importedUTIList);
			urlTypeExpander.SetWidget (urlTypeList);
		}
		
		void Update ()
		{
			iOSApplicationTargetWidget.Update ();
			iPhoneDeploymentInfo.Update ();
			iPadDeploymentInfo.Update ();
			
			var iphone = NSDictionary.Get<PArray> ("UISupportedInterfaceOrientations");
			iPhoneDeploymentInfoContainer.Visible = iphone != null;
			var ipad   = NSDictionary.Get<PArray> ("UISupportedInterfaceOrientations~ipad");
			iPadDeploymentInfoContainer.Visible = ipad != null;
			
			var documentTypes = NSDictionary.Get<PArray> ("CFBundleDocumentTypes");
			if (documentTypes != null) {
				foreach (var pObject in documentTypes.Value) {
					var dict = (PDictionary)pObject;
					if (dict == null)
						continue;
					string name = GettextCatalog.GetString ("Untitled");
					var dtw = new DocumentTypeWidget (proj, dict);
					dtw.Expander = documentTypeList.AddListItem (name, dtw);
					
				}
			}
			
			var exportedUTIs = NSDictionary.Get<PArray> ("UTExportedTypeDeclarations");
			if (exportedUTIs != null) {
				foreach (var pObject in exportedUTIs.Value) {
					var dict = (PDictionary)pObject;
					if (dict == null)
						continue;
					string name = GettextCatalog.GetString ("Untitled");
					var dtw = new UTIWidget (proj, dict);
					dtw.Expander = exportedUTIList.AddListItem (name, dtw);
				}
			}
			
			var importedUTIs = NSDictionary.Get<PArray> ("UTImportedTypeDeclarations");
			if (importedUTIs != null) {
				foreach (var pObject in importedUTIs.Value) {
					var dict = (PDictionary)pObject;
					if (dict == null)
						continue;
					string name = GettextCatalog.GetString ("Untitled");
					var dtw = new UTIWidget (proj, dict);
					dtw.Expander = importedUTIList.AddListItem (name, dtw);
				}
			}
			
			var urlTypes = NSDictionary.Get<PArray> ("CFBundleURLTypes");
			if (urlTypes != null) {
				foreach (var pObject in urlTypes.Value) {
					var dict = (PDictionary)pObject;
					if (dict == null)
						continue;
					string name = GettextCatalog.GetString ("Untitled");
					var dtw = new URLTypeWidget (proj, dict);
					dtw.Expander = urlTypeList.AddListItem (name, dtw);
				}
			}
		}
	}
}

