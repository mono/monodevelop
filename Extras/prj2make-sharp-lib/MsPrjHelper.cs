using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using MonoDevelop.Prj2Make.Schema.Prjx;
using MonoDevelop.Prj2Make.Schema.Csproj;

namespace MonoDevelop.Prj2Make
{
	public class SlnMaker
	{ 
		public static string slash;
		static Hashtable projNameInfo = new Hashtable();
		static Hashtable projGuidInfo = new Hashtable();
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
			Regex regex = new Regex(@"Microsoft Visual Studio Solution File, Format Version (\d.\d\d)");
			
			strInput = reader.ReadLine();

			match = regex.Match(strInput);
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
    	
		protected void ParseSolution(string fname)
		{
			FileStream fis = new FileStream(fname,FileMode.Open, FileAccess.Read, FileShare.Read);
			StreamReader reader = new StreamReader(fis);
			Regex regex = new Regex(@"Project\(""\{(.*)\}""\) = ""(.*)"", ""(.*)"", ""(\{.*\})""");
    
			while (true)
			{
				string s = reader.ReadLine();
				Match match;
    
				match = regex.Match(s);
				if (match.Success)
				{
					string projectName = match.Groups[2].Value;
					string csprojPath = match.Groups[3].Value;
					string projectGuid = match.Groups[4].Value;
    
					if (csprojPath.EndsWith (".csproj") && !csprojPath.StartsWith("http://"))
					{
						CsprojInfo pi = new CsprojInfo (m_bIsUnix, m_bIsMcs, projectName, projectGuid, csprojPath);
    
						projNameInfo[projectName] = pi;
						projGuidInfo[projectGuid] = pi;
					}
				}
    
				if (s.StartsWith("Global"))
				{
					break;
				}
			}
		}
    
		public string MsSlnHelper(bool isUnixMode, bool isMcsMode, bool isSln, string slnFile)
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
					ParseSolution (slnFile);
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
							catch(System.NullReferenceException exc)
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
		
		public void CreatePrjxFromCsproj(string csprojFileName)
		{
			int nCnt = 0;
			FileStream fsIn = null;
			XmlSerializer xmlDeSer = null;
			FileStream fsOut = null;
			XmlSerializer xmlSer = null;
			MonoDevelop.Prj2Make.Schema.Csproj.VisualStudioProject csprojObj = null;
			MonoDevelop.Prj2Make.Schema.Prjx.Project prjxObj = new MonoDevelop.Prj2Make.Schema.Prjx.Project();
			prjxFileName = String.Format ("{0}.prjx",
				Path.Combine(Path.GetDirectoryName(csprojFileName),
				Path.GetFileNameWithoutExtension(csprojFileName))
				);
			
			// convert backslashes to slashes    		
			csprojFileName = csprojFileName.Replace("\\", "/");			
			Console.WriteLine(String.Format("Will create project filename:{0}", PrjxFileName));

			// Load the csproj
			fsIn = new FileStream (csprojFileName, FileMode.Open);	    
			xmlDeSer = new XmlSerializer (typeof(VisualStudioProject));
			csprojObj = (VisualStudioProject) xmlDeSer.Deserialize (fsIn);	    
			fsIn.Close();

			// Begin prjxObj population
			prjxObj.name = Path.GetFileNameWithoutExtension(csprojFileName);
			prjxObj.description = "";
			prjxObj.newfilesearch = "None";
			prjxObj.enableviewstate = "True";
			prjxObj.version = (decimal)1.1;
			prjxObj.projecttype = "C#";

			prjxObj.Contents = GetContents (csprojObj.CSHARP.Files.Include);
			prjxObj.References = GetReferences(csprojObj.CSHARP.Build.References);
			prjxObj.DeploymentInformation = new MonoDevelop.Prj2Make.Schema.Prjx.DeploymentInformation();
			prjxObj.DeploymentInformation.target = "";
			prjxObj.DeploymentInformation.script = "";
			prjxObj.DeploymentInformation.strategy = "File";

			nCnt = csprojObj.CSHARP.Build.Settings.Config.Length;
			prjxObj.Configurations = new Configurations();
			prjxObj.Configurations.Configuration = new Configuration[nCnt];
			for(int i = 0; i < nCnt; i++)
			{
				prjxObj.Configurations.Configuration[i] = CreateConfigurationBlock(
					csprojObj.CSHARP.Build.Settings.Config[i],
					csprojObj.CSHARP.Build.Settings.AssemblyName,
					csprojObj.CSHARP.Build.Settings.OutputType
					);
			}
			prjxObj.Configurations.active = prjxObj.Configurations.Configuration[0].name;

			prjxObj.Configuration = prjxObj.Configurations.Configuration[0];

			// Serialize
			fsOut = new FileStream (prjxFileName, FileMode.Create);	    
			xmlSer = new XmlSerializer (typeof(Project));
			xmlSer.Serialize(fsOut, prjxObj);
			fsOut.Close();

			return;
		}

		public void MsSlnToCmbxHelper(string slnFileName)
		{
			int i = 0;
			FileStream fsOut = null;
			XmlSerializer xmlSer = null;
			//StringBuilder MakefileBuilder = new StringBuilder();
			MonoDevelop.Prj2Make.Schema.Cmbx.Combine cmbxObj = new MonoDevelop.Prj2Make.Schema.Cmbx.Combine();
			cmbxFileName = String.Format ("{0}.cmbx",
				Path.Combine(Path.GetDirectoryName(slnFileName),
				Path.GetFileNameWithoutExtension(slnFileName))
				);
			
			Console.WriteLine(String.Format("Will create combine filename:{0}", cmbxFileName));

			string origDir = Directory.GetCurrentDirectory();
			try
			{
				string d = Path.GetDirectoryName(slnFileName);
				if (d != "")
					Directory.SetCurrentDirectory(d);

				// We invoke the ParseSolution 
				// by passing the file obtained
				ParseSolution (slnFileName);

				// Create all of the prjx files form the csproj files
				foreach (CsprojInfo pi in projNameInfo.Values)
				{
					CreatePrjxFromCsproj(pi.csprojpath);
				}

				// Begin prjxObj population
				cmbxObj.name = Path.GetFileNameWithoutExtension(slnFileName);
				cmbxObj.description = "";
				cmbxObj.fileversion = (decimal)1.0;

				// Create and attach the StartMode element
				MonoDevelop.Prj2Make.Schema.Cmbx.StartMode startModeElem = new MonoDevelop.Prj2Make.Schema.Cmbx.StartMode();

				// Create the array of Execute objects
				MonoDevelop.Prj2Make.Schema.Cmbx.Execute[] executeElem = new MonoDevelop.Prj2Make.Schema.Cmbx.Execute[projNameInfo.Count];

				// Populate the Element objects instances
				i = 0;
				foreach (CsprojInfo pi in projNameInfo.Values)
				{
					MonoDevelop.Prj2Make.Schema.Cmbx.Execute execElem = new MonoDevelop.Prj2Make.Schema.Cmbx.Execute();
					execElem.entry = pi.name;
					execElem.type = "None";

                    executeElem[i++] = execElem;
				}

				startModeElem.startupentry = executeElem[0].entry;
				startModeElem.single = "True";
				startModeElem.Execute = executeElem;

				// Attach the StartMode Object to the
				// Combine object
				cmbxObj.StartMode = startModeElem;

				// Gnerate the entries array
				MonoDevelop.Prj2Make.Schema.Cmbx.Entry[] entriesObj = new MonoDevelop.Prj2Make.Schema.Cmbx.Entry[projNameInfo.Count];
				// Populate the Entry objects instances
				i = 0;
				foreach (CsprojInfo pi in projNameInfo.Values)
				{
					MonoDevelop.Prj2Make.Schema.Cmbx.Entry entryObj = new MonoDevelop.Prj2Make.Schema.Cmbx.Entry();
					string PrjxFileName = String.Format (".{0}{1}.prjx",
						Path.DirectorySeparatorChar,
						Path.Combine(Path.GetDirectoryName(pi.csprojpath),
						Path.GetFileNameWithoutExtension(pi.csprojpath))
						);

					entryObj.filename = PrjxFileName; 

					entriesObj[i++] = entryObj;
				}

				// Attach the Entries Object to the
				// Combine object
				cmbxObj.Entries = entriesObj;

				MonoDevelop.Prj2Make.Schema.Cmbx.Configurations configurationsObj = new MonoDevelop.Prj2Make.Schema.Cmbx.Configurations();
				
				// Hack hardcoded configuration value must get the one
				// from analyzing the different configuration entries
				configurationsObj.active = "Debug";

				// Hack hardcoded number of configuration object
				// assuming 2 for Debug and Release
				configurationsObj.Configuration = new MonoDevelop.Prj2Make.Schema.Cmbx.Configuration[2];
				MonoDevelop.Prj2Make.Schema.Cmbx.Configuration confObj1 = new MonoDevelop.Prj2Make.Schema.Cmbx.Configuration();
				configurationsObj.Configuration[0] = confObj1;
				MonoDevelop.Prj2Make.Schema.Cmbx.Configuration confObj2 = new MonoDevelop.Prj2Make.Schema.Cmbx.Configuration();
				configurationsObj.Configuration[1] = confObj2;

				configurationsObj.Configuration[0].name = "Release";
				configurationsObj.Configuration[0].Entry = CreateArrayOfConfEntries();
				configurationsObj.Configuration[1].name = "Debug";
				configurationsObj.Configuration[1].Entry = CreateArrayOfConfEntries();

				// Attach the Configurations object to the 
				// Combine Object
				cmbxObj.Configurations = configurationsObj;

				// Serialize
				fsOut = new FileStream (cmbxFileName, FileMode.Create);	    
				xmlSer = new XmlSerializer (typeof(MonoDevelop.Prj2Make.Schema.Cmbx.Combine));
				xmlSer.Serialize(fsOut, cmbxObj);
				fsOut.Close();

				return;
			}
			catch (Exception e)
			{
				Console.WriteLine("EXCEPTION: {0}\n", e);
				return;
			}
			finally
			{
				Directory.SetCurrentDirectory(origDir);
			}
		}

		protected MonoDevelop.Prj2Make.Schema.Prjx.Reference[] GetReferences(MonoDevelop.Prj2Make.Schema.Csproj.Reference[] References)
		{
			MonoDevelop.Prj2Make.Schema.Prjx.Reference[] theReferences = null;
			int i = 0;

			// Get the GAC path
			string strBasePathMono1_0 = Path.Combine(
					MonoDevelop.Prj2Make.PkgConfigInvoker.GetPkgVariableValue("mono", "libdir").TrimEnd(),
					"mono/1.0");

//			string strBasePathMono2_0 = Path.Combine(
//					MonoDevelop.Prj2Make.PkgConfigInvoker.GetPkgVariableValue("mono", "libdir").TrimEnd(),
//					"mono/2.0");
//
			string strBasePathGtkSharp = Path.Combine(
					MonoDevelop.Prj2Make.PkgConfigInvoker.GetPkgVariableValue("gtk-sharp", "libdir").TrimEnd(),
					"mono/gtk-sharp");

			string strBasePathGtkSharp2_0 = Path.Combine(
					MonoDevelop.Prj2Make.PkgConfigInvoker.GetPkgVariableValue("gtk-sharp-2.0", "libdir").TrimEnd(),
					"mono/gtk-sharp-2.0");

			string strBasePathGeckoSharp = Path.Combine(
					MonoDevelop.Prj2Make.PkgConfigInvoker.GetPkgVariableValue("gecko-sharp", "libdir").TrimEnd(),
					"mono/gecko-sharp");

			string strBasePathGeckoSharp2_0 = Path.Combine(
					MonoDevelop.Prj2Make.PkgConfigInvoker.GetPkgVariableValue("gecko-sharp-2.0", "libdir").TrimEnd(),
					"mono/gecko-sharp-2.0");

			if(References != null && References.Length > 0)
			{
				theReferences = new MonoDevelop.Prj2Make.Schema.Prjx.Reference[References.Length];
			}
			else
			{
				return null;
			}

			// Iterate through the reference collection of the csproj file
			foreach(MonoDevelop.Prj2Make.Schema.Csproj.Reference rf in References)
			{
				MonoDevelop.Prj2Make.Schema.Prjx.Reference rfOut = new MonoDevelop.Prj2Make.Schema.Prjx.Reference();
				string strRefFileName;

				if(rf.Package == null || rf.Package.Length == 0)
				{
					bool bIsWhereExpected = false;

					// HACK - under Unix filenames are case sensitive
					// Under Windows there's no agreement on Xml vs XML ;-)    					
					if(Path.GetFileName(rf.HintPath).CompareTo("System.XML.dll") == 0)
					{
						strRefFileName = Path.Combine (strBasePathMono1_0, Path.GetFileName("System.Xml.dll"));

						// Test to see if file exist in GAC location
						if(System.IO.File.Exists(strRefFileName) == true) {
							try {
								rfOut.refto = System.Reflection.Assembly.LoadFrom(strRefFileName).FullName;
								rfOut.type = MonoDevelop.Prj2Make.Schema.Prjx.ReferenceType.Gac;
								rfOut.localcopy = MonoDevelop.Prj2Make.Schema.Prjx.ReferenceLocalcopy.True;
								bIsWhereExpected = true;
								// increment the iterator value
								theReferences[i++] = rfOut;
								continue;
							} catch (Exception exc) {
								Console.WriteLine ("Error doing Assembly.LoadFrom with File: {0}\nErr Msg: {1}",
									strRefFileName,
									exc.Message );
							}
						}
					} else {					
						//////////////////////////
						// Check on Mono 1.0
						strRefFileName = Path.Combine (strBasePathMono1_0, Path.GetFileName(rf.HintPath));

						// Test to see if file exist in GAC location
						if(System.IO.File.Exists(strRefFileName) == true) {
							try {
								rfOut.refto = System.Reflection.Assembly.LoadFrom(strRefFileName).FullName;
								rfOut.type = MonoDevelop.Prj2Make.Schema.Prjx.ReferenceType.Gac;
								rfOut.localcopy = MonoDevelop.Prj2Make.Schema.Prjx.ReferenceLocalcopy.True;
								bIsWhereExpected = true;
								// increment the iterator value
								theReferences[i++] = rfOut;
								continue;
							} catch (Exception exc) {
								Console.WriteLine ("Error doing Assembly.LoadFrom with File: {0}\nErr Msg: {1}",
									strRefFileName,
									exc.Message );
							}
						}

						//////////////////////////
						// Check on Gtk# 2.0
						strRefFileName = Path.Combine (strBasePathGtkSharp2_0, Path.GetFileName(rf.HintPath));

						// Test to see if file exist in GAC location
						if(System.IO.File.Exists(strRefFileName) == true) {
							try {
								rfOut.refto = System.Reflection.Assembly.LoadFrom(strRefFileName).FullName;
								rfOut.type = MonoDevelop.Prj2Make.Schema.Prjx.ReferenceType.Gac;
								rfOut.localcopy = MonoDevelop.Prj2Make.Schema.Prjx.ReferenceLocalcopy.True;
								bIsWhereExpected = true;
								// increment the iterator value
								theReferences[i++] = rfOut;
								continue;
							} catch (Exception exc) {
								Console.WriteLine ("Error doing Assembly.LoadFrom with File: {0}\nErr Msg: {1}",
									strRefFileName,
									exc.Message );
							}
						}

						//////////////////////////
						// Check on Gtk# 1.0
						strRefFileName = Path.Combine (strBasePathGtkSharp, Path.GetFileName(rf.HintPath));

						// Test to see if file exist in GAC location
						if(System.IO.File.Exists(strRefFileName) == true) {
							try {
								rfOut.refto = System.Reflection.Assembly.LoadFrom(strRefFileName).FullName;
								rfOut.type = MonoDevelop.Prj2Make.Schema.Prjx.ReferenceType.Gac;
								rfOut.localcopy = MonoDevelop.Prj2Make.Schema.Prjx.ReferenceLocalcopy.True;
								bIsWhereExpected = true;
								// increment the iterator value
								theReferences[i++] = rfOut;
								continue;
							} catch (Exception exc) {
								Console.WriteLine ("Error doing Assembly.LoadFrom with File: {0}\nErr Msg: {1}",
									strRefFileName,
									exc.Message );
							}
						}
						
						//////////////////////////
						// Check on Gecko# 2.0
						strRefFileName = Path.Combine (strBasePathGeckoSharp2_0, Path.GetFileName(rf.HintPath));

						// Test to see if file exist in GAC location
						if(System.IO.File.Exists(strRefFileName) == true) {
							try {
								rfOut.refto = System.Reflection.Assembly.LoadFrom(strRefFileName).FullName;
								rfOut.type = MonoDevelop.Prj2Make.Schema.Prjx.ReferenceType.Gac;
								rfOut.localcopy = MonoDevelop.Prj2Make.Schema.Prjx.ReferenceLocalcopy.True;
								bIsWhereExpected = true;
								// increment the iterator value
								theReferences[i++] = rfOut;
								continue;
							} catch (Exception exc) {
								Console.WriteLine ("Error doing Assembly.LoadFrom with File: {0}\nErr Msg: {1}",
									strRefFileName,
									exc.Message );
							}
						}

						//////////////////////////
						// Check on Gecko# 1.0
						strRefFileName = Path.Combine (strBasePathGeckoSharp, Path.GetFileName(rf.HintPath));

						// Test to see if file exist in GAC location
						if(System.IO.File.Exists(strRefFileName) == true) {
							try {
								rfOut.refto = System.Reflection.Assembly.LoadFrom(strRefFileName).FullName;
								rfOut.type = MonoDevelop.Prj2Make.Schema.Prjx.ReferenceType.Gac;
								rfOut.localcopy = MonoDevelop.Prj2Make.Schema.Prjx.ReferenceLocalcopy.True;
								bIsWhereExpected = true;
								// increment the iterator value
								theReferences[i++] = rfOut;
								continue;
							} catch (Exception exc) {
								Console.WriteLine ("Error doing Assembly.LoadFrom with File: {0}\nErr Msg: {1}",
									strRefFileName,
									exc.Message );
							}
						}
						
						if(bIsWhereExpected == false)
						{
							rfOut.refto = Path.GetFileName(rf.HintPath);
							rfOut.type = MonoDevelop.Prj2Make.Schema.Prjx.ReferenceType.Gac;
							rfOut.localcopy = MonoDevelop.Prj2Make.Schema.Prjx.ReferenceLocalcopy.True;
							// increment the iterator value
							theReferences[i++] = rfOut;
							continue;
						}
					}
				}
				else
				{
					rfOut.type = MonoDevelop.Prj2Make.Schema.Prjx.ReferenceType.Project;
					
					rfOut.refto = Path.GetFileName(rf.Name);
					rfOut.localcopy = MonoDevelop.Prj2Make.Schema.Prjx.ReferenceLocalcopy.True;
					// increment the iterator value
					theReferences[i++] = rfOut;
				}
			}

			return theReferences;
		}
		
		protected MonoDevelop.Prj2Make.Schema.Prjx.File[] GetContents(MonoDevelop.Prj2Make.Schema.Csproj.File[] Include)
		{
			MonoDevelop.Prj2Make.Schema.Prjx.File[] theFiles = null;
			int i = 0;

			if(Include != null && Include.Length > 0)
			{
				theFiles = new MonoDevelop.Prj2Make.Schema.Prjx.File[Include.Length];
			}
			else
			{
				return null;
			}

			// Iterate through the file collection of the csproj file
			foreach(MonoDevelop.Prj2Make.Schema.Csproj.File fl in Include)
			{
				MonoDevelop.Prj2Make.Schema.Prjx.File flOut = new MonoDevelop.Prj2Make.Schema.Prjx.File();
				flOut.name = String.Format(".{0}{1}", Path.DirectorySeparatorChar, fl.RelPath);
				
				switch(fl.SubType)
				{
					case "Code":
						flOut.subtype = MonoDevelop.Prj2Make.Schema.Prjx.FileSubtype.Code;
						break;
				}

				switch(fl.BuildAction)
				{
					case MonoDevelop.Prj2Make.Schema.Csproj.FileBuildAction.Compile:
						flOut.buildaction = MonoDevelop.Prj2Make.Schema.Prjx.FileBuildaction.Compile;
						break;
					case MonoDevelop.Prj2Make.Schema.Csproj.FileBuildAction.Content:
						flOut.buildaction = MonoDevelop.Prj2Make.Schema.Prjx.FileBuildaction.Exclude;
						break;
					case MonoDevelop.Prj2Make.Schema.Csproj.FileBuildAction.EmbeddedResource:
						flOut.buildaction = MonoDevelop.Prj2Make.Schema.Prjx.FileBuildaction.EmbedAsResource;
						break;
					case MonoDevelop.Prj2Make.Schema.Csproj.FileBuildAction.None:
						flOut.buildaction = MonoDevelop.Prj2Make.Schema.Prjx.FileBuildaction.Exclude;
						break;				
				}
				flOut.dependson = fl.DependentUpon;
				flOut.data = "";

				// increment the iterator value
				theFiles[i++ ] = flOut;
			}

			return theFiles;
		}

		protected Configuration CreateConfigurationBlock(Config ConfigBlock, string AssemblyName, string OuputType)
		{
			Configuration ConfObj = new Configuration();
			CodeGeneration CodeGenObj = new CodeGeneration();
			Execution ExecutionObj = new Execution();
			Output OutputObj = new Output();

			ConfObj.runwithwarnings = "False";
			ConfObj.name = ConfigBlock.Name;

			// CodeGenObj member population
			CodeGenObj.runtime = "MsNet";
			CodeGenObj.compiler = "Csc";
			CodeGenObj.warninglevel = ConfigBlock.WarningLevel;
			CodeGenObj.nowarn = "";
			CodeGenObj.includedebuginformation = (ConfigBlock.DebugSymbols == true) ? 
				CodeGenerationIncludedebuginformation.True : 
				CodeGenerationIncludedebuginformation.False;
			
			CodeGenObj.optimize = (ConfigBlock.Optimize == true) ? "True" : "False";

			if (ConfigBlock.AllowUnsafeBlocks == true)
			{
				CodeGenObj.unsafecodeallowed = CodeGenerationUnsafecodeallowed.True;
			}
			else
			{
				CodeGenObj.unsafecodeallowed = CodeGenerationUnsafecodeallowed.False;
			}
			if (ConfigBlock.CheckForOverflowUnderflow == true)
			{
				CodeGenObj.generateoverflowchecks = "True";
			}
			else
			{
				CodeGenObj.generateoverflowchecks = "False";
			}
			
			CodeGenObj.mainclass = "";
			CodeGenObj.target = OuputType;
			CodeGenObj.generatexmldocumentation = "False";
			CodeGenObj.win32Icon = "";

			// ExecutionObj member population
			ExecutionObj.commandlineparameters = "";
			ExecutionObj.consolepause = "True";

			// OutputObj member population
			OutputObj.directory = ConfigBlock.OutputPath.Replace("\\", "/");
			OutputObj.assembly = AssemblyName;
			OutputObj.executeScript = "";
			OutputObj.executeBeforeBuild = "";
			OutputObj.executeAfterBuild = "";

			ConfObj.CodeGeneration = CodeGenObj;
			ConfObj.Execution = ExecutionObj;
			ConfObj.Output = OutputObj;

			return ConfObj;
		}
		
		protected MonoDevelop.Prj2Make.Schema.Cmbx.Entry[] CreateArrayOfConfEntries()
		{
			MonoDevelop.Prj2Make.Schema.Cmbx.Entry[] confEntry = new MonoDevelop.Prj2Make.Schema.Cmbx.Entry[projNameInfo.Count];
			// Populate the Entry objects instances
			int i = 0;
			foreach (CsprojInfo pi in projNameInfo.Values)
			{
				MonoDevelop.Prj2Make.Schema.Cmbx.Entry entryObj = new MonoDevelop.Prj2Make.Schema.Cmbx.Entry();
				entryObj.name = pi.name;
				entryObj.configurationname = "Debug";
				entryObj.build = "False";

				confEntry[i++] = entryObj;
			}

			return confEntry;
		}
	}   
}
