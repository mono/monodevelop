using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

using MonoDevelop.Prj2Make.Schema.Prjx;
using MonoDevelop.Prj2Make.Schema.Csproj;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Core;

using CSharpBinding;

namespace MonoDevelop.Prj2Make
{
	public class SlnMaker
	{ 
		public static string slash;
		Hashtable projNameInfo = new Hashtable();
		Hashtable projGuidInfo = new Hashtable();
		private string prjxFileName;
		private string cmbxFileName;
		private string m_strSlnVer;
		private string m_strCsprojVer;
		private bool m_bIsUnix;
		private bool m_bIsMcs;
		private bool m_bIsUsingLib;
 
		// Flag use to determine if the LIB variable will be used in
		// the Makefile that prj2make generates
		public bool IsUsingLib
		{
			get{ return m_bIsUsingLib; }
			set{ m_bIsUsingLib = value; }
		}


		// Determines if the makefile is intended for nmake or gmake for urposes of path separator character
		public bool IsUnix
		{
			get{ return m_bIsUnix; }
			set{ m_bIsUnix = value; }
		}

		// Determines if using MCS or CSC
		public bool IsMcs
		{
			get{ return m_bIsMcs; }
			set{ m_bIsMcs = value; }
		}

		public string SlnVersion
		{
			get { return m_strSlnVer; }
			set { m_strSlnVer = value; }
		}

		public string CsprojVersion
		{
			get { return m_strCsprojVer; }
			set { m_strCsprojVer = value; }
		}

		// Shuld contain the file name 
		// of the most resent prjx generation
		public string PrjxFileName {
			get { return prjxFileName; }
		}

		// Shuld contain the file name 
		// of the most resent cmbx generation
		public string CmbxFileName {
			get { return cmbxFileName; }
		}


		// Default constructor
		public SlnMaker()
		{
			m_bIsUnix = false;
			m_bIsMcs = false;
			m_bIsUsingLib = false;
		}
		
		// Utility function to determine the sln file version
		protected string GetSlnFileVersion(string strInSlnFile)
		{
			string strVersion = null;
			string strInput = null;
			Match match;
			FileStream fis = new FileStream(strInSlnFile, FileMode.Open, FileAccess.Read, FileShare.Read);
			StreamReader reader = new StreamReader(fis);
			strInput = reader.ReadLine();

			match = SlnVersionRegex.Match(strInput);
			if (match.Success)
			{
				strVersion = match.Groups[1].Value;
			}
			
			// Close the stream
			reader.Close();

			// Close the File Stream
			fis.Close();
    
			return strVersion;
		}
    	
		// Utility function to determine the csproj file version
		protected string GetCsprojFileVersion(string strInCsprojFile)
		{
			string strRetVal = null;
			XmlDocument xmlDoc = new XmlDocument();

			xmlDoc.Load(strInCsprojFile);
			strRetVal = xmlDoc.SelectSingleNode("/VisualStudioProject/CSHARP/@ProductVersion").Value;

			return strRetVal;
		}

		protected void ParseMsCsProj(string fname)
		{
			string projectName = System.IO.Path.GetFileNameWithoutExtension (fname);
			string csprojPath = System.IO.Path.GetFileName (fname);
			string projectGuid = "";
            
			CsprojInfo pi = new CsprojInfo (m_bIsUnix, m_bIsMcs, projectName, projectGuid, csprojPath);
            
			projNameInfo[projectName] = pi;
			projGuidInfo[projectGuid] = pi;
		}
    	
		protected void ParseSolution(string fname, IProgressMonitor monitor)
		{
			FileStream fis = new FileStream(fname,FileMode.Open, FileAccess.Read, FileShare.Read);
			using (StreamReader reader = new StreamReader(fis)) {
				while (true)
				{
					string s = reader.ReadLine();
					Match match;

					match = ProjectRegex.Match(s);
					if (match.Success)
					{
						string projectName = match.Groups[2].Value;
						string csprojPath = match.Groups[3].Value;
						string projectGuid = match.Groups[4].Value;

						try {
							if (csprojPath.EndsWith (".csproj") && !csprojPath.StartsWith("http://"))
							{
								csprojPath = MapPath (Path.GetDirectoryName (fname), csprojPath);
								CsprojInfo pi = new CsprojInfo (m_bIsUnix, m_bIsMcs, projectName, projectGuid, csprojPath);
								projNameInfo[projectName] = pi;
								projGuidInfo[projectGuid] = pi;
							}
						} catch (Exception ex) {
							Console.WriteLine (GettextCatalog.GetString ("Could not import project:") + csprojPath);
							Console.WriteLine (ex.ToString ());
							monitor.ReportError (GettextCatalog.GetString ("Could not import project:") + csprojPath, ex);
							throw;
						}
					}

					if (s.StartsWith("Global"))
					{
						break;
					}
				}
			}
		}
    
		public string MsSlnHelper(bool isUnixMode, bool isMcsMode, bool isSln, string slnFile, IProgressMonitor monitor)
		{
			bool noCommonTargets = false;
			bool noProjectTargets = false;
			bool noFlags = false;
			StringBuilder MakefileBuilder = new StringBuilder();
    
			m_bIsUnix = isUnixMode;
			m_bIsMcs = isMcsMode;
			
			if(m_bIsUnix == true && m_bIsMcs == true)
			{
				m_bIsUsingLib = true;
			}

			if (m_bIsUnix)
			{
				slash = "/";
			}
			else
			{
				slash = "\\";
			}
    		
    		string origDir = Directory.GetCurrentDirectory();
			try
			{
				string d = Path.GetDirectoryName(slnFile);
				if (d != "")
					Directory.SetCurrentDirectory(d);

				if (isSln == true) 
				{
					// Get the sln file version
					m_strSlnVer = GetSlnFileVersion(slnFile);

					// We invoke the ParseSolution 
					// by passing the file obtained
					ParseSolution (slnFile, monitor);
				} 
				else 
				{
					// Get the Csproj version
					m_strCsprojVer = GetCsprojFileVersion(slnFile);

					// We invoke the ParseMsCsProj 
					// by passing the file obtained 
					ParseMsCsProj (slnFile);
				}
    
				if (!noFlags)
				{
					if (m_bIsUnix) // gmake
					{
						MakefileBuilder.Append("ifndef TARGET\n");
						MakefileBuilder.Append("\tTARGET=./bin/Debug\n");        				
						MakefileBuilder.Append("else\n");
						MakefileBuilder.Append("\tTARGET=./bin/$(TARGET)\n");
						MakefileBuilder.Append("endif\n\n");
           				
						if (this.m_bIsMcs == false)
						{
							MakefileBuilder.Append("MCS=csc\n");
							MakefileBuilder.Append("MCSFLAGS=-nologo\n\n");
							MakefileBuilder.Append("ifdef (RELEASE)\n");
							MakefileBuilder.Append("\tMCSFLAGS=$(MCSFLAGS) -optimize+ -d:TRACE\n");
							MakefileBuilder.Append("else\n");
							MakefileBuilder.Append("\tMCSFLAGS=$(MCSFLAGS) -debug+ -d:TRACE,DEBUG\n");
							MakefileBuilder.Append("endif\n");
						}
						else
						{
							MakefileBuilder.Append("MCS=mcs\n");
							MakefileBuilder.Append("ifndef (RELEASE)\n");
							MakefileBuilder.Append("\tMCSFLAGS=-debug --stacktrace\n");
							MakefileBuilder.Append("endif\n");
							// Define and add the information used in the -lib: arguments passed to the
							// compiler to assist in finding non-fullyqualified assembly references.
							if(m_bIsMcs == true)
							{
								string strlibDir = PkgConfigInvoker.GetPkgVariableValue("mono", "libdir");

								if (strlibDir != null)
								{
			    					MakefileBuilder.AppendFormat("LIBS=-lib:{0} -lib:{1}\n\n", 
										Path.Combine(strlibDir.TrimEnd(), "mono/1.0"),
										Path.Combine(strlibDir.TrimEnd(), "mono/gtk-sharp")
									);
								}							
							}
						}        		
					}
					else // nmake
					{
						MakefileBuilder.Append("!if !defined (TARGET)\n");
						MakefileBuilder.Append("TARGET=.\\bin\\Debug\n");        				
						MakefileBuilder.Append("!else\n");
						MakefileBuilder.Append("TARGET=.\\bin\\$(TARGET)\n");
						MakefileBuilder.Append("!endif\n\n");
           				
						if (m_bIsMcs == false)
						{
							MakefileBuilder.Append("MCS=csc\n");
							MakefileBuilder.Append("MCSFLAGS=-nologo\n\n");
							MakefileBuilder.Append("!if !defined(RELEASE)\n");
							MakefileBuilder.Append("MCSFLAGS=$(MCSFLAGS) -optimize+ -d:TRACE\n");
							MakefileBuilder.Append("!else\n");
							MakefileBuilder.Append("MCSFLAGS=$(MCSFLAGS) -debug+ -d:TRACE,DEBUG\n");
							MakefileBuilder.Append("!endif\n");
						}
						else
						{
							MakefileBuilder.Append("MCS=mcs\n");
							MakefileBuilder.Append("!if !defined(RELEASE)\n");
							MakefileBuilder.Append("MCSFLAGS=-debug --stacktrace\n");
							MakefileBuilder.Append("!endif\n");
						}    				
					}
    
					MakefileBuilder.Append("\n");
				}
				else
				{
					MakefileBuilder.Append("!if !defined(MCS)\n");
					MakefileBuilder.Append("!error You must provide MCS when making\n");
					MakefileBuilder.Append("!endif\n\n");
				}
    
				foreach (CsprojInfo pi in projNameInfo.Values)
				{
					MakefileBuilder.AppendFormat("{0}=$(TARGET){1}{2}\n", pi.makename_ext, slash, pi.assembly_name);
					MakefileBuilder.AppendFormat("{0}_PDB=$(TARGET){1}{2}\n", pi.makename, slash, pi.assembly_name.Replace(".dll",".pdb"));
					MakefileBuilder.AppendFormat("{0}_SRC={1}\n", pi.makename, pi.src);
					MakefileBuilder.AppendFormat("{0}_RES={1}\n\n", pi.makename, pi.res);
				}
    
				foreach (CsprojInfo pi in projNameInfo.Values)
				{
					string refs = "";
					string deps = "";
    					
					foreach (MonoDevelop.Prj2Make.Schema.Csproj.Reference rf in pi.Proyecto.CSHARP.Build.References)
					{
						if(rf.Package == null || rf.Package.CompareTo("") == 0)
						{
							// Add space in between references as
							// it becomes necessary
							if (refs != "")
								refs += " ";

							string assemblyName = rf.AssemblyName;

							// HACK - under Unix filenames are case sensitive
							// Under Windows there's no agreement on Xml vs XML ;-)    					
							if (0 == String.Compare(assemblyName, "System.Xml", true))
							{
								assemblyName = "System.Xml";
							}
							refs += "-r:" + assemblyName + ".dll";
						}
						else
						{
							try
							{
								CsprojInfo pi2 = (CsprojInfo)projGuidInfo[rf.Project];

								if (refs != "")
									refs += " ";

								if (deps != "")
									deps += " ";

								refs += "-r:$(" + pi2.makename_ext + ")";
								deps += "$(" + pi2.makename_ext + ")";
							}
							catch(System.NullReferenceException)
							{
								refs += String.Format("-r:{0}.dll", rf.Name);
								deps += String.Format("# Missing dependency project {1} ID:{0}?", rf.Project, 
									rf.Name);
								Console.WriteLine(String.Format(
									"Warning: The project {0}, ID: {1} may be required and appears missing.",
									rf.Name, rf.Project)
									);
							}
						}
					}
    
					MakefileBuilder.AppendFormat("$({0}): $({1}_SRC) {2}\n", pi.makename_ext, pi.makename, deps);
    		
					if (isUnixMode)
					{
						MakefileBuilder.Append("\t-mkdir -p $(TARGET)\n");
					}
					else
					{
						MakefileBuilder.Append("\t-md $(TARGET)\n");
					}

					// Test to see if any configuratino has the Allow unsafe blocks on
					if(pi.AllowUnsafeCode == true ) {
						MakefileBuilder.Append(" -unsafe");
					}

					// Test for LIBS usage
					if(m_bIsUsingLib == true) {
	    				MakefileBuilder.Append(" $(LIBS)");
					}

					MakefileBuilder.AppendFormat(" {2}{3} -out:$({0}) $({1}_RES) $({1}_SRC)\n", 
							pi.makename_ext, pi.makename, refs, pi.switches);
            								
					MakefileBuilder.Append("\n");
				}
    
				if (!noCommonTargets)
				{
					MakefileBuilder.Append("\n");
					MakefileBuilder.Append("# common targets\n\n");
					MakefileBuilder.Append("all:\t");
    
					bool first = true;
    
					foreach (CsprojInfo pi in projNameInfo.Values)
					{
						if (!first)
						{
							MakefileBuilder.Append(" \\\n\t");
						}
						MakefileBuilder.AppendFormat("$({0})", pi.makename_ext);
						first = false;
					}
					MakefileBuilder.Append("\n\n");
    
					MakefileBuilder.Append("clean:\n");
    
					foreach (CsprojInfo pi in projNameInfo.Values)
					{
						if (isUnixMode)
						{
							MakefileBuilder.AppendFormat("\t-rm -f \"$({0})\" 2> /dev/null\n", pi.makename_ext);
							MakefileBuilder.AppendFormat("\t-rm -f \"$({0}_PDB)\" 2> /dev/null\n", pi.makename);
						}
						else
						{
							MakefileBuilder.AppendFormat("\t-del \"$({0})\" 2> nul\n", pi.makename_ext);
							MakefileBuilder.AppendFormat("\t-del \"$({0}_PDB)\" 2> nul\n", pi.makename);
						}
					}
					MakefileBuilder.Append("\n");
				}
    
				if (!noProjectTargets)
				{
					MakefileBuilder.Append("\n");
					MakefileBuilder.Append("# project names as targets\n\n");
					foreach (CsprojInfo pi in projNameInfo.Values)
					{
						MakefileBuilder.AppendFormat("{0}: $({1})\n", pi.name, pi.makename_ext);
					}
				}
    			
				return MakefileBuilder.ToString();
			}
			catch (Exception e)
			{
				Console.WriteLine("EXCEPTION: {0}\n", e);
				return "";
			}
			finally
			{
				Directory.SetCurrentDirectory(origDir);
			}
		}
		
		public string CreatePrjxFromCsproj (string csprojFileName, IProgressMonitor monitor)
		{
			DotNetProject project = CreatePrjxFromCsproj (csprojFileName, monitor, true);
			return project.FileName;
		}

		public DotNetProject CreatePrjxFromCsproj (string csprojFileName, IProgressMonitor monitor, bool save)
		{
			try {
				MonoDevelop.Prj2Make.Schema.Csproj.VisualStudioProject csprojObj = null;
				
				monitor.BeginTask (GettextCatalog.GetString ("Importing project: ") + csprojFileName, 5);
				
				DotNetProject prjxObj = new DotNetProject ("C#", null, null);
				
				prjxFileName = String.Format ("{0}.mdp",
					Path.Combine (Path.GetDirectoryName (csprojFileName),
					Path.GetFileNameWithoutExtension (csprojFileName))
					);

				// Load the csproj
				using (TextFileReader fsIn = new TextFileReader (csprojFileName)) {
					XmlSerializer xmlDeSer = new XmlSerializer (typeof(VisualStudioProject));
					csprojObj = (VisualStudioProject) xmlDeSer.Deserialize (fsIn);
				}
				
				monitor.Step (1);

				// Begin prjxObj population
				prjxObj.FileName = prjxFileName;
				prjxObj.Name = Path.GetFileNameWithoutExtension(csprojFileName);
				prjxObj.Description = "";
				prjxObj.NewFileSearch = NewFileSearch.None;
				prjxObj.DefaultNamespace = csprojObj.CSHARP.Build.Settings.RootNamespace;

				GetContents (prjxObj, csprojObj.CSHARP.Files.Include, prjxObj.Files, monitor);
				monitor.Step (1);
				
				GetReferences (csprojObj.CSHARP.Build.References, prjxObj.References, monitor);
				monitor.Step (1);
				
				prjxObj.Configurations.Clear ();
				foreach (Config cblock in csprojObj.CSHARP.Build.Settings.Config)
				{
					prjxObj.Configurations.Add (CreateConfigurationBlock (
						prjxObj,
						cblock,
						csprojObj.CSHARP.Build.Settings.AssemblyName,
						csprojObj.CSHARP.Build.Settings.OutputType,
						monitor
						));
				}
				monitor.Step (1);
				if (save)
					prjxObj.Save (monitor);
				monitor.Step (1);
				return prjxObj;

			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not import project:") + csprojFileName, ex);
				throw;
			} finally {
				monitor.EndTask ();
			}
		}

		public string MsSlnToCmbxHelper (string slnFileName, IProgressMonitor monitor)
		{
			Solution c = MsSlnToCmbxHelper (slnFileName, monitor, true);
			return c.FileName;
		}

		public Solution MsSlnToCmbxHelper (string slnFileName, IProgressMonitor monitor, bool save)
		{
			Solution solution = new Solution();
			cmbxFileName = String.Format ("{0}.mds",
				Path.Combine(Path.GetDirectoryName(slnFileName),
				Path.GetFileNameWithoutExtension(slnFileName))
				);
			
			monitor.BeginTask (GettextCatalog.GetString ("Importing solution"), 2);
			try
			{
				// We invoke the ParseSolution 
				// by passing the file obtained
				ParseSolution (slnFileName, monitor);

				// Create all of the prjx files form the csproj files
				monitor.BeginTask (null, projNameInfo.Values.Count * 2);
				
				foreach (CsprojInfo pi in projNameInfo.Values) {
					string mappedPath = MapPath (Path.GetDirectoryName (slnFileName), pi.csprojpath);
					if (mappedPath == null) {
						monitor.Step (2);
						monitor.ReportWarning (GettextCatalog.GetString ("Project file not found: ") + pi.csprojpath);
						continue;
					}
					DotNetProject prj = CreatePrjxFromCsproj (mappedPath, monitor, save);
					if (prj == null)
						return null;

					monitor.Step (1);
					if (save) {
						string prjName = prj.FileName;
						if (prjName != null)
							solution.RootFolder.AddItem (prjName, monitor);
						else
							return null;
					} else {
						solution.RootFolder.Items.Add (prj);
					}
					monitor.Step (1);
				}
				
				monitor.EndTask ();
				monitor.Step (1);

				solution.FileName = cmbxFileName;
				if (save)
					solution.Save (cmbxFileName, monitor);

				monitor.Step (1);
				return solution;
			}
			catch (Exception e)
			{
				monitor.ReportError (GettextCatalog.GetString ("The solution could not be imported."), e);
				throw;
			}
			finally
			{
				monitor.EndTask ();
			}
		}
		
		protected void GetReferences (MonoDevelop.Prj2Make.Schema.Csproj.Reference[] References, ProjectReferenceCollection references, IProgressMonitor monitor)
		{
			if (References == null || References.Length == 0)
				return;
			
			monitor.BeginTask (null, 5 + References.Length);
			                      
			try {
				// Get the GAC path
				string strBasePathMono1_0 = GetPackageDirectory ("mono", "mono/1.0");
				
				monitor.Step (1);

				string strBasePathGtkSharp = GetPackageDirectory ("gtk-sharp", "mono/gtk-sharp");
				
				monitor.Step (1);

				string strBasePathGtkSharp2_0 = GetPackageDirectory ("gtk-sharp-2.0", "mono/gtk-sharp-2.0");
				
				monitor.Step (1);

				string strBasePathGeckoSharp = GetPackageDirectory ("gecko-sharp", "mono/gecko-sharp");
				
				monitor.Step (1);

				string strBasePathGeckoSharp2_0 = GetPackageDirectory ("gecko-sharp-2.0", "mono/gecko-sharp-2.0");
				
				string[] monoLibs = new string [] {
					strBasePathMono1_0,
					strBasePathGtkSharp2_0,
					strBasePathGtkSharp,
					strBasePathGeckoSharp2_0,
					strBasePathGeckoSharp
				};

				// Iterate through the reference collection of the csproj file
				foreach (MonoDevelop.Prj2Make.Schema.Csproj.Reference rf in References)
				{
					monitor.Step (1);
					
					ProjectReference rfOut = null;
					
					if (rf.Package != null && rf.Package.Length != 0)
					{
						rfOut = new ProjectReference (MonoDevelop.Projects.ReferenceType.Project, Path.GetFileName (rf.Name));
						rfOut.LocalCopy = true;
						references.Add (rfOut);
					}
					else if (rf.AssemblyName != null)
					{
						string rname = rf.AssemblyName;
						if (rname == "System.XML")
							rname = "System.Xml";
						
						string oref = Runtime.SystemAssemblyService.GetAssemblyFullName (rname);
						if (oref == null) {
							monitor.ReportWarning (GettextCatalog.GetString ("Assembly reference could not be imported: ") + rf.AssemblyName);
							continue;
						}
						rfOut = new ProjectReference (MonoDevelop.Projects.ReferenceType.Gac, oref);
						rfOut.LocalCopy = true;
						references.Add (rfOut);
					}
					else if (rf.HintPath != null)
					{
						// HACK - under Unix filenames are case sensitive
						// Under Windows there's no agreement on Xml vs XML ;-)    					
						if (Path.GetFileName (rf.HintPath) == "System.XML.dll")
						{
							ProjectReference pref = GetMonoReferece (strBasePathMono1_0, "System.Xml.dll");
							if (pref != null) {
								references.Add (pref);
								continue;
							}
						} else {
							foreach (string libDir in monoLibs) {
								if (libDir == null) continue;
								if (rf.HintPath == null)
									continue;
								rfOut = GetMonoReferece (libDir, rf.HintPath);
								if (rfOut != null)
									break;
							}
							
							if (rfOut == null) {
								rfOut = new ProjectReference (MonoDevelop.Projects.ReferenceType.Gac, Path.GetFileName (rf.HintPath));
								rfOut.LocalCopy = true;
							}
							references.Add (rfOut);
						}
					}
					else {
						monitor.ReportWarning (GettextCatalog.GetString ("Assembly reference could not be imported: ") + rf.Name);
					}
				}
			} finally {
				monitor.EndTask ();
			}
		}
		
		string GetPackageDirectory (string package, string subdir)
		{
			string dir = MonoDevelop.Prj2Make.PkgConfigInvoker.GetPkgVariableValue (package, "libdir");
			return dir != null ? Path.Combine (dir.TrimEnd(), subdir) : null;
		}

		ProjectReference GetMonoReferece (string libPath, string reference)
		{
			string strRefFileName = Path.Combine (libPath, Path.GetFileName (reference));

			// Test to see if file exist in GAC location
			if (System.IO.File.Exists (strRefFileName)) {
				ProjectReference rfOut = new ProjectReference (MonoDevelop.Projects.ReferenceType.Gac, Runtime.SystemAssemblyService.GetAssemblyFullName (strRefFileName));
				rfOut.LocalCopy = true;
				return rfOut;
			}
			return null;
		}
		
		protected void GetContents (MonoDevelop.Projects.Project project, MonoDevelop.Prj2Make.Schema.Csproj.File[] Include, ProjectFileCollection files, IProgressMonitor monitor)
		{
			if (Include == null || Include.Length == 0)
				return;

			// Iterate through the file collection of the csproj file
			foreach(MonoDevelop.Prj2Make.Schema.Csproj.File fl in Include)
			{
				ProjectFile flOut = new ProjectFile ();
				
				string name;
				if ((fl.Link == null) || (fl.Link.Length == 0)) {
					name = MapPath (project.BaseDirectory, fl.RelPath);
				} else {
					name = MapPath (null, fl.Link);
				}
				
				if (name == null) {
					monitor.ReportWarning (GettextCatalog.GetString ("Can't import file: ") + fl.RelPath);
					continue;
				}
				flOut.Name = name;
				// Adding here as GetDefaultResourceIdInternal needs flOut.Project
				files.Add (flOut);
				
				switch (fl.SubType)
				{
					case "Code":
						flOut.Subtype = Subtype.Code;
						break;
				}

				switch (fl.BuildAction)
				{
					case MonoDevelop.Prj2Make.Schema.Csproj.FileBuildAction.Compile:
						flOut.BuildAction = BuildAction.Compile;
						break;
					case MonoDevelop.Prj2Make.Schema.Csproj.FileBuildAction.Content:
						flOut.BuildAction = BuildAction.Nothing;
						break;
					case MonoDevelop.Prj2Make.Schema.Csproj.FileBuildAction.EmbeddedResource:
						flOut.BuildAction = BuildAction.EmbedAsResource;
						break;
					case MonoDevelop.Prj2Make.Schema.Csproj.FileBuildAction.None:
						flOut.BuildAction = BuildAction.Nothing;
						break;				
				}
				// DependentUpon is relative to flOut
				flOut.DependsOn = MapPath (Path.GetDirectoryName (flOut.Name), fl.DependentUpon);
				flOut.Data = "";
			}
		}

		protected SolutionItemConfiguration CreateConfigurationBlock (MonoDevelop.Projects.DotNetProject project, Config ConfigBlock, string AssemblyName, string OuputType, IProgressMonitor monitor)
		{
			DotNetProjectConfiguration confObj = project.CreateConfiguration (ConfigBlock.Name) as DotNetProjectConfiguration;

			confObj.RunWithWarnings = false;
			confObj.DebugMode = ConfigBlock.DebugSymbols;
			project.CompileTarget = (CompileTarget) Enum.Parse (typeof(CompileTarget), OuputType, true);
			
			string dir = MapPath (project.BaseDirectory, ConfigBlock.OutputPath);
			if (dir == null) {
				dir = "bin/" + ConfigBlock.Name;
				monitor.ReportWarning (string.Format (GettextCatalog.GetString ("Output directory '{0}' can't be mapped to a local directory. The directory '{1}' will be used instead"), ConfigBlock.OutputPath, dir));
			}
			confObj.OutputDirectory = dir;
			confObj.OutputAssembly = AssemblyName;
			
			CSharpCompilerParameters compilerParams = new CSharpCompilerParameters ();
			compilerParams.WarningLevel = ConfigBlock.WarningLevel;
			compilerParams.NoWarnings = "";
			compilerParams.Optimize = ConfigBlock.Optimize;
			compilerParams.DefineSymbols = ConfigBlock.DefineConstants;
			compilerParams.UnsafeCode = ConfigBlock.AllowUnsafeBlocks; 
			compilerParams.GenerateOverflowChecks = ConfigBlock.CheckForOverflowUnderflow;
			compilerParams.MainClass = "";
			
			return confObj;
		}
		
		internal static string MapPath (string basePath, string relPath)
		{
			if (relPath == null || relPath.Length == 0)
				return null;
			
			string path = relPath;
			if (Path.DirectorySeparatorChar != '\\')
				path = path.Replace ("\\", "/");

			if (char.IsLetter (path [0]) && path.Length > 1 && path[1] == ':')
				return null;
			
			if (basePath != null)
				path = Path.Combine (basePath, path);

			if (System.IO.File.Exists (path)){
				return Path.GetFullPath (path);
			}
				
			if (Path.IsPathRooted (path)) {
					
				// Windows paths are case-insensitive. When mapping an absolute path
				// we can try to find the correct case for the path.
				
				string[] names = path.Substring (1).Split ('/');
				string part = "/";
				
				for (int n=0; n<names.Length; n++) {
					string[] entries;

					if (names [n] == ".."){
						part = Path.GetFullPath (part + "/..");
						continue;
					}
					
					entries = Directory.GetFileSystemEntries (part);
					
					string fpath = null;
					foreach (string e in entries) {
						if (string.Compare (Path.GetFileName (e), names[n], true) == 0) {
							fpath = e;
							break;
						}
					}
					if (fpath == null) {
						// Part of the path does not exist. Can't do any more checking.
						part = Path.GetFullPath (part);
						for (; n < names.Length; n++)
							part += "/" + names[n];
						return part;
					}

					part = fpath;
				}
				return Path.GetFullPath (part);
			} else {
				return Path.GetFullPath (path);
			}
		}

		// static regexes
		static Regex projectRegex = null;
		internal static Regex ProjectRegex {
			get {
				if (projectRegex == null)
					projectRegex = new Regex(@"Project\(""(\{[^}]*\})""\) = ""(.*)"", ""(.*)"", ""(\{[^{]*\})""");
				return projectRegex;
			}
		}

		static Regex slnVersionRegex = null;
		internal static Regex SlnVersionRegex {
			get {
				if (slnVersionRegex == null)
					slnVersionRegex = new Regex (@"Microsoft Visual Studio Solution File, Format Version (\d?\d.\d\d)");
				return slnVersionRegex;
			}
		}
	}   
}
