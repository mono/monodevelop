// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Poul Staugaard" email="poul@staugaard.dk"/>
//     <version value="$version"/>
// </file>
#if false
using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Diagnostics;

using Microsoft.Win32;

namespace MonoDevelop.Internal.Project
{
	/// <summary>
	/// Wraps code to import a typelibrary
	/// </summary>
	/// <remarks>
	/// 	created by - Poul Staugaard
	/// 	created on - 17-08-2002 14:24:13
	/// </remarks>
	public class TypelibImporter
	{
		enum RegKind {
			RegKind_Default = 0,
			RegKind_Register = 1,
			RegKind_None = 2
		}
		
		[DllImport( "oleaut32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
		private static extern void LoadTypeLibEx(string strTypeLibName, RegKind regKind, [MarshalAs(UnmanagedType.Interface)] out Object typeLib);
		
		/// <summary>
		/// Default constructor - initializes all fields to default values
		/// </summary>
		public TypelibImporter() 
		{
		}
		
		public string Import(ProjectReference refinfo, Project project)
		{
			RegistryKey root = Registry.ClassesRoot;
			RegistryKey typelibsKey = root.OpenSubKey("TypeLib");
			int index = refinfo.Reference.LastIndexOf("{");
			if (index < 0) {
				return null;
			}
			RegistryKey typelibKey = typelibsKey.OpenSubKey(refinfo.Reference.Substring(index, refinfo.Reference.Length - index));
			if (typelibKey == null) {
				return null;
			}
			string[] versions = typelibKey.GetSubKeyNames();
			
			if (versions.Length <= 0) {
				return null;
			}
			// Use the last version
			string version = versions[versions.Length - 1];
			RegistryKey versionKey = typelibKey.OpenSubKey(version);
			
			string tlbname = (string)versionKey.GetValue(null);
			
			string tlpath = GetTypelibPath(versionKey);
			if (tlpath == null) {
				return null;
			}
			string proxyfilename = "Interop." + Path.GetFileNameWithoutExtension(tlpath) + ".dll";
			
			AbstractProjectConfiguration ac = (AbstractProjectConfiguration)project.ActiveConfiguration;
			string fullpath = Path.Combine(ac.OutputDirectory,proxyfilename);
			
			if (!File.Exists(fullpath)) {
				string saveCurrDir = Directory.GetCurrentDirectory();
				if (!Directory.Exists(ac.OutputDirectory)) {
					Directory.CreateDirectory(ac.OutputDirectory);
				}
				Directory.SetCurrentDirectory(ac.OutputDirectory);
				if (!ImportTypelibUsingTlbImpCode(tlpath, ac.OutputDirectory, proxyfilename)) {
					
					MessageBox.Show("Cannot import type library using .Net SDK 1.0. Some, but not all type libraries can succesfully be imported without it. ",
										".Net SDK 1.0 not present ?",
										MessageBoxButtons.OK, 
										MessageBoxIcon.Warning, 
										MessageBoxDefaultButton.Button1);

					Object typeLib;
					LoadTypeLibEx(tlpath, RegKind.RegKind_None, out typeLib);
					
					if( typeLib == null ) {
						throw new System.Exception("LoadTypeLibEx failed.");
					}
										
					TypeLibConverter converter = new TypeLibConverter();
					ConversionEventHandler eventHandler = new ConversionEventHandler();
					
					AssemblyBuilder asm = converter.ConvertTypeLibToAssembly( typeLib,
																			 proxyfilename, 0, eventHandler, null, null, 
																			 Marshal.GetTypeLibName((UCOMITypeLib)typeLib), null );
					
					asm.Save( proxyfilename );
					
				}
				Directory.SetCurrentDirectory(saveCurrDir);
			}
			return fullpath;
		}

		string GetTypelibName(string tlpath)
		{
			Object typeLib;
			LoadTypeLibEx(tlpath, RegKind.RegKind_None, out typeLib);
			
			if( typeLib == null ) {
				throw new System.Exception("LoadTypeLibEx failed.");
			}
			return Marshal.GetTypeLibName((UCOMITypeLib)typeLib);
		}
		
		bool ImportTypelibUsingTlbImpCode(string typelibfile, string outputpath, string assemblyname)
		{
			// TlbImpCode being an undocumented assembly which is only installed with the .Net SDK,
			// use late binding in stead of <iso> to allow the app as a whole to run even if,
			// in its absense, this does not work:
			try {
				//<iso> TlbImpCode.TlbImpOptions options = new TlbImpCode.TlbImpOptions(); </iso>
				Assembly assyTic = Assembly.LoadWithPartialName("TlbImpCode, Version=1.0");
				Type optype = assyTic.GetType("TlbImpCode.TlbImpOptions");
				object options = Activator.CreateInstance(optype);

				//<iso> options.m_strOutputDir = outputpath; </iso>
				optype.GetField("m_strOutputDir").SetValue(options, outputpath);
				
				//<iso> options.m_strTypeLibName = typelibfile; </iso>
				optype.GetField("m_strTypeLibName").SetValue(options, typelibfile); 
				
				//<iso> options.m_strAssemblyName = assemblyname; </iso>
				optype.GetField("m_strAssemblyName").SetValue(options, assemblyname);
				
				//<iso> options.m_strAssemblyNamespace = GetTypelibName(typelibfile); </iso>
				optype.GetField("m_strAssemblyNamespace").SetValue(options, GetTypelibName(typelibfile));


				AppDomainSetup ads = new AppDomainSetup();
				ads.ApplicationBase = outputpath;

				AppDomain ad = AppDomain.CreateDomain("TlbImport", null, ads);
				if (ad == null)
					throw new Exception("Failed to create AppDomain");

				//<iso> string remoteTlbImpAssemblyName = 
				//			typeof(TlbImpCode.RemoteTlbImp).Assembly.GetName().FullName; </iso>
				string remoteTlbImpAssemblyName = assyTic.GetName().FullName;

				//<iso> TlbImpCode.RemoteTlbImp tic = (TlbImpCode.RemoteTlbImp) ...</iso>
				object tic = ad.CreateInstanceAndUnwrap(remoteTlbImpAssemblyName, "TlbImpCode.RemoteTlbImp");
		
				//<iso> tic.Run(options); </osi>
				Type tictype = tic.GetType();
				MethodInfo miRun = tictype.GetMethod("Run");
				object[] targs = new object[1];
				targs[0] = options;
				miRun.Invoke(tic, targs);
					
				return true;
			}
			catch (Exception ex) {
				Debug.WriteLine(ex.ToString());				
			}
			return false;
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
	}
}
#endif
