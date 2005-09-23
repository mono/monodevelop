// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Poul Staugaard" email="poul@staugaard.dk"/>
//     <version value="$version"/>
// </file>
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

using Microsoft.Win32;
using MonoDevelop.Internal.Project;
using MonoDevelop.Core.Services;

namespace MonoDevelop.Gui.Dialogs
{/*
	public class COMReferencePanel : ListView//, IReferencePanel
	{
		private enum RegKind
		{
			RegKind_Default = 0,
			RegKind_Register = 1,
			RegKind_None = 2
		}
		
		[ DllImport( "oleaut32.dll", CharSet = CharSet.Unicode, PreserveSig = false )]
		private static extern void LoadTypeLibEx( String strTypeLibName, RegKind regKind, 
		  [ MarshalAs( UnmanagedType.Interface )] out Object typeLib );
   
		SelectReferenceDialog selectDialog;
		
		public COMReferencePanel(SelectReferenceDialog selectDialog)
		{
			this.selectDialog = selectDialog;
			
			this.Sorting = SortOrder.Ascending;
//			ResourceService resourceService = (ResourceService)ServiceManager.Services.GetService(typeof(IResourceService));
			
			ColumnHeader nameHeader = new ColumnHeader();
			nameHeader.Text  = "name";//resourceService.GetString("Dialog.SelectReferenceDialog.COMReferencePanel.NameHeader");
			nameHeader.Width = 240;
			Columns.Add(nameHeader);
			
			ColumnHeader directoryHeader = new ColumnHeader();
			directoryHeader.Text  = "directory";//resourceService.GetString("Dialog.SelectReferenceDialog.COMReferencePanel.TypelibPath");
			directoryHeader.Width =200;
			Columns.Add(directoryHeader);
			
			View = View.Details;
			Dock = DockStyle.Fill;
			FullRowSelect = true;
			
			ItemActivate += new EventHandler(AddReference);
			PopulateListView();
		}
		
		public void AddReference(object sender, EventArgs e)
		{
			foreach (ListViewItem item in SelectedItems) {
				RegistryKey typelibKey = (RegistryKey )item.Tag;
				string[] versions = typelibKey.GetSubKeyNames();
				// Use the last version
				string version = versions[versions.Length - 1];
				RegistryKey versionKey = typelibKey.OpenSubKey(version);
				
				string name = (string)versionKey.GetValue(null);
					
				string tlbpath = GetTypelibPath(versionKey);
				int guidpos = typelibKey.Name.LastIndexOf('{');
				
				selectDialog.AddReference(ReferenceType.Typelib,
				                          name,
				                          tlbpath + typelibKey.Name.Substring(guidpos));
			}
		}
		
		void PopulateListView()
		{
			ArrayList typelibraries = new ArrayList();
			RegistryKey root = Registry.ClassesRoot;
			RegistryKey typelibsKey = root.OpenSubKey("TypeLib");
			string[] keynames = typelibsKey.GetSubKeyNames();
			if (keynames != null) {
				foreach (string aTypelibKeyName in keynames) {
					RegistryKey typelibKey = typelibsKey.OpenSubKey(aTypelibKeyName);
					if (typelibKey == null) {
						continue;
					}
					string[] versions = typelibKey.GetSubKeyNames();
					if (versions.Length > 0) {
						// Use the last version
						string version = versions[versions.Length - 1];
						RegistryKey versionKey = typelibKey.OpenSubKey(version);
						string name = (string)versionKey.GetValue(null);
						string typelibpath = GetTypelibPath(versionKey);
						if (name != null && name.Length > 0 && typelibpath != null)	{
							ListViewItem newItem = new ListViewItem(new string[] { name, typelibpath });
							newItem.Tag = typelibKey;
							Items.Add(newItem);
						}
					}
				}
			}
		}

		string GetTypelibPath(RegistryKey versionKey)
		{
			// Get the default value of the (typically) 0\win32 subkey:
			string[] subkeys = versionKey.GetSubKeyNames();
			
			if (subkeys == null || subkeys.Length == 0) {
				return null;
			}
			for (int i = 0; i < subkeys.Length; i++)
			{
				try {
					int.Parse(subkeys[i]); // The right key is a number
					RegistryKey NullKey = versionKey.OpenSubKey( subkeys[i]);
					string[] subsubkeys = NullKey.GetSubKeyNames();
					RegistryKey win32Key = NullKey.OpenSubKey("win32");
					
					return win32Key == null || win32Key.GetValue(null) == null ?
						   null : win32Key.GetValue(null).ToString();
				}
				catch (FormatException) {
					// Wrong keys don't parse til int
				}
			}
			return null;			
		}

		public class ConversionEventHandler : ITypeLibImporterNotifySink
		{
   			public void ReportEvent( ImporterEventKind eventKind, int eventCode, string eventMsg )
   			{
      			// handle warning event here...
   			}
   
   			public Assembly ResolveRef( object typeLib )
   			{
      			// resolve reference here and return a correct assembly...
      			return null; 
   			}   
		}		
	}*/
}

