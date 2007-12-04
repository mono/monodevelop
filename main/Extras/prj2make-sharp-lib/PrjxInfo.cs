using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;


namespace MonoDevelop.Prj2Make
{
    class PrjxInfo
    {
    	public readonly string name;
    	public readonly string csprojpath;
    	public string makename;
    	public string makename_ext;
    	public string assembly_name;
    	public string res;
    	public string src;
	private bool m_bAllowUnsafeCode;
   	private MonoDevelop.Prj2Make.Schema.Prjx.Project m_projObject;
    
    	public string ext_refs = "";
    	public string switches = "";
    	
    	public MonoDevelop.Prj2Make.Schema.Prjx.Project Proyecto {
    		get { return m_projObject; }
    	}
    
	public bool AllowUnsafeCode
	{
		get { return m_bAllowUnsafeCode; }
	}
    	
    	// Project desirialization
		protected MonoDevelop.Prj2Make.Schema.Prjx.Project LoadPrjFromFile (string strIn)
		{
			FileStream fs = new FileStream (strIn, FileMode.Open);
	    
			XmlSerializer xmlSer = new XmlSerializer (typeof(MonoDevelop.Prj2Make.Schema.Prjx.Project));
			MonoDevelop.Prj2Make.Schema.Prjx.Project prjObj = (MonoDevelop.Prj2Make.Schema.Prjx.Project) xmlSer.Deserialize (fs);
	    
			fs.Close();
	    
			return (prjObj);
		}

		public PrjxInfo(bool isUnixMode, bool isMcsMode, string csprojpath)
	   	{
		MonoDevelop.Prj2Make.Schema.Prjx.Configuration activeConf = null;
    		this.csprojpath = csprojpath;
    		
    		// convert backslashes to slashes    		
    		csprojpath = csprojpath.Replace("\\", "/");
    
		m_bAllowUnsafeCode = false;

    		// loads the file in order to deserialize and
    		// build the object graph
    		try {
    			m_projObject = LoadPrjFromFile (csprojpath);
			} catch (Exception exc) {
			
				Console.WriteLine (
					String.Format ("Could not load the file {0}\nException: {1}",
						csprojpath,
						exc.Message)
					);
				return;			
			}

    		this.name = m_projObject.name;

    		makename = name.Replace('.','_').ToUpper();
    		makename_ext = makename + "_EXT";
    
			// Get the configuration to be used and
			// copy it to a local configuration object
			foreach(MonoDevelop.Prj2Make.Schema.Prjx.Configuration cnfObj in m_projObject.Configurations.Configuration)
			{
				if(cnfObj.name.CompareTo(m_projObject.Configurations.active) == 0)
				{
					// Assign the active configuration
					activeConf = cnfObj;
					break;
				}
			}

			// Establish if the allow unsafe code flag is true
			if(activeConf.CodeGeneration.unsafecodeallowed == MonoDevelop.Prj2Make.Schema.Prjx.CodeGenerationUnsafecodeallowed.True)
			{
				m_bAllowUnsafeCode = true;
			}
			
    		switch (m_projObject.Configuration.CodeGeneration.target)
    		{
    			case "Library":
    				makename_ext = makename + "_DLL";
    				assembly_name = m_projObject.Configuration.Output.assembly + ".dll";
    				switches += " -target:library";
    				break;
    
    			case "Exe":
    				makename_ext = makename + "_EXE";
    				assembly_name = m_projObject.Configuration.Output.assembly + ".exe";
    				switches += " -target:exe";
    				break;
    
    			case "WinExe":
    				makename_ext = makename + "_EXE";
    				assembly_name = m_projObject.Configuration.Output.assembly + ".exe";
    				switches += " -target:winexe";
    				break;
    
    			default:
    				throw new NotSupportedException("Unsupported OutputType: " + m_projObject.Configuration.CodeGeneration.target);
    			
    		}
    
    		src = "";    
    		string basePath = Path.GetDirectoryName(csprojpath);
    		string s;
    		
			// Process Source code files for compiling
    		foreach (MonoDevelop.Prj2Make.Schema.Prjx.File f in m_projObject.Contents)
    		{
    			if(f.buildaction == MonoDevelop.Prj2Make.Schema.Prjx.FileBuildaction.Compile)
    			{
    				if (src != "") {
	    				src += " \\\n\t";
    				}
    			
    				s = System.IO.Path.Combine(basePath, f.name);
    				s = s.Replace("\\", "/");
    				if (CmbxMaker.slash != "/")
	    				s = s.Replace("/", CmbxMaker.slash);

					// Test for spaces
					if (isUnixMode == false)
					{
						// We are in win32 using a cmd.exe or other
						// DOS shell
						if(s.IndexOf(' ') > -1) {
							src += String.Format("\"{0}\"", s);
						} else {
							src += s;
						}
					} else {
						// We are in *NIX or some other
						// GNU like shell
						src += s.Replace(" ", "\\ ");
					}    			
    			}
    		}
    		
    		// Process resources for embedding
    		res = "";
    		string rootNS = this.name; 
    		string relPath;
    		foreach (MonoDevelop.Prj2Make.Schema.Prjx.File f in m_projObject.Contents)
    		{
    			if(f.buildaction == MonoDevelop.Prj2Make.Schema.Prjx.FileBuildaction.EmbedAsResource)
    			{
    				if (src != "") {
	    				src += " \\\n\t";
    				}
    			
	    			relPath = f.name.Replace("\\", "/");
	    			s = System.IO.Path.Combine(basePath, relPath);
	    			s = String.Format("-resource:{0},{1}", s, rootNS + "." + relPath.Replace("/", "."));
	    			s = s.Replace("\\", "/");
	    			if (CmbxMaker.slash != "/")
	    				s = s.Replace("/", CmbxMaker.slash);
	    			res += s;
    			}
    		}
    	}
    }    
}
