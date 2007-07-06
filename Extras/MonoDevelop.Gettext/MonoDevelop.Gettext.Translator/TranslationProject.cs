//
// TranslationProject.cs
//
// Author:
//   Rafael 'Monoman' Teixeira
//   David Makovský <yakeen@sannyas-on.net>
//
// Copyright (C) 2006 Rafael 'Monoman' Teixeira
// Copyright (C) 2007 David Makovský
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
using System.Xml;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;


using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.Gettext.Translator
{
	public class TranslationProjectConfiguration : AbstractProjectConfiguration
	{
		string directory = ".";

		public override void CopyFrom (IConfiguration configuration)
		{
			base.CopyFrom (configuration);
			TranslationProjectConfiguration conf = (TranslationProjectConfiguration)configuration;
			this.directory = conf.directory;
		}

		public override string OutputDirectory
		{
			get { return directory; }
			set { directory = value; }
		}
	}

	public class TranslationProject : Project, IDisposable
	{
		//internal List<IDisposable> Windows;

		protected TranslationProject ()
			: base ()
		{
			Name = GettextCatalog.GetString ("Translations");
			//List<IDisposable> Windows = new List<IDisposable> ();
		}

		public TranslationProject (ProjectCreateInformation info, XmlElement projectOptions)
			: base ()
		{
			if (info == null)
				throw new ArgumentNullException ("info");
			Name = info.ProjectName;
			//List<IDisposable> Windows = new List<IDisposable> ();

			//string path = String.IsNullOrEmpty (info.ProjectPath) ? info.ProjectBasePath : Path.Combine (info.CombinePath, info.ProjectPath); 
//			string newPath = info.ProjectBasePath;
//			int pos = newPath.TrimEnd (Path.DirectorySeparatorChar).LastIndexOf (Path.DirectorySeparatorChar);
//			if (pos >= 0)
//			{
//				newPath = newPath.Substring (0, pos);
//			}
//			newPath = Path.Combine (newPath, "po");
//			info.ProjectBasePath = newPath;
//			string path = Path.Combine (info.CombinePath, "po");
//			if (! Directory.Exists (path))
//			{
//				try
//				{
//					Directory.CreateDirectory (path);
//				}
//				catch (Exception e)
//				{
//					// TODO: what to do here to behave nicely?
//				}
//			}
			//FileName = Path.Combine (newPath, info.ProjectName + ".mdp");
			FileName = info.ProjectName;
			TranslationProjectConfiguration conf = (TranslationProjectConfiguration)CreateConfiguration (GettextCatalog.GetString ("All"));
			conf.OutputDirectory = info.ProjectBasePath;
			Configurations.Add (conf);
			ActiveConfiguration = conf;
		}

		public override string ProjectType
		{
			get { return "Translation"; }
		}

		bool CreateFile (string newfile, string content, BuildAction action, IProgressMonitor monitor)
		{
			string filename = newfile.StartsWith (BaseDirectory) ? newfile : Path.Combine (BaseDirectory, newfile);
			
			StreamWriter sw = new StreamWriter (filename, false);
			try
			{
				sw.WriteLine (content);
			}
			finally
			{
				sw.Close ();
			}
			
			if (! IsFileInProject (filename))
				AddFile (filename, action);
			
			Save (monitor);
			return true;
		}

		string GeneratePotfilesFile (Combine solution, IProgressMonitor monitor)
		{
			StringBuilder sb = new StringBuilder ();
			foreach (CombineEntry entry in solution.GetAllProjects ())
			{
				Project project = entry as Project;
				if (project != null && ! (project is TranslationProject))
				{
					Translator.TranslationProjectInfo info =
							project.ExtendedProperties ["MonoDevelop.Gettext.TranslationInfo"] as Translator.TranslationProjectInfo;
					foreach (ProjectFile file in project.ProjectFiles)
					{
						if (info.IsFileExcluded (file.FilePath))
							continue;
						sb.AppendLine (this.GetRelativeChildPath (project.GetAbsoluteChildPath (file.RelativePath)));
					}
				}
			}
			string result;
			result = CreateFile (PotfilesFilePath, sb.ToString (), BuildAction.Compile, monitor) ? null : GettextCatalog.GetString ("Failed Creation of POTFILES file.");
			return result;
		}

		string PotfilesFilePath
		{
			get { return Path.Combine (BaseDirectory, "POTFILES"); }
		}

		public string TemplateFilePath
		{
			get { return Path.Combine (BaseDirectory, Path.GetFileNameWithoutExtension (ParentCombine.Name).ToLower () + ".pot"); }
		}

		const string xgettext = "xgettext";
		string GenerateTemplateFile (IProgressMonitor monitor)
		{
			string result;
			Process proc = Process.Start (xgettext);
			proc.WaitForExit ();
			if (proc.ExitCode != 0)
			{
				result = GettextCatalog.GetString ("Command 'xgettext' was not found. This command is essential for translation projects, please install gettext package.");
				monitor.Log.WriteLine (result);
				return result;
			}

//    print "Building $MODULE.pot...\n" if $VERBOSE;
//
//    open INFILE, $POTFILES_in;
//    unlink "POTFILES.in.temp";
//    open OUTFILE, ">POTFILES.in.temp" or die("Cannot open POTFILES.in.temp for writing");
//
//    my $gettext_support_nonascii = 0;
//
//    # checks for GNU gettext >= 0.12
//    my $dummy = `$XGETTEXT --version --from-code=UTF-8 >$devnull 2>$devnull`;
//    if ($? == 0)
//    {
//	$gettext_support_nonascii = 1;
//    }
//    else
//    {
//	# urge everybody to upgrade gettext
//	print STDERR "WARNING: This version of gettext does not support extracting non-ASCII\n".
//		     "         strings. That means you should install a version of gettext\n".
//		     "         that supports non-ASCII strings (such as GNU gettext >= 0.12),\n".
//		     "         or have to let non-ASCII strings untranslated. (If there is any)\n";
//    }
//
//    my $encoding = "ASCII";
//    my $forced_gettext_code;
//    my @temp_headers;
//    my $encoding_problem_is_reported = 0;
//
//    while (<INFILE>) 
//    {
//	next if (/^#/ or /^\s*$/);
//
//	chomp;
//
//	my $gettext_code;
//
//	if (/^\[\s*encoding:\s*(.*)\s*\]/)
//	{
//	    $forced_gettext_code=$1;
//	}
//	elsif (/\.($xml_support|$ini_support)$/ || /^\[/)
//	{
//	    s/^\[.*]\s*//;
//            print OUTFILE "../$_.h\n";
//	    push @temp_headers, "../$_.h";
//	    $gettext_code = &TextFile_DetermineEncoding ("../$_.h") if ($gettext_support_nonascii and not defined $forced_gettext_code);
//	} 
//	else 
//	{
//            print OUTFILE "$SRCDIR/../$_\n";
//	    $gettext_code = &TextFile_DetermineEncoding ("$SRCDIR/../$_") if ($gettext_support_nonascii and not defined $forced_gettext_code);
//	}
//
//	next if (! $gettext_support_nonascii);
//
//	if (defined $forced_gettext_code)
//	{
//	    $encoding=$forced_gettext_code;
//	}
//	elsif (defined $gettext_code and "$encoding" ne "$gettext_code")
//	{
//	    if ($encoding eq "ASCII")
//	    {
//		$encoding=$gettext_code;
//	    }
//	    elsif ($gettext_code ne "ASCII")
//	    {
//		# Only report once because the message is quite long
//		if (! $encoding_problem_is_reported)
//		{
//		    print STDERR "WARNING: You should use the same file encoding for all your project files,\n".
//				 "         but $PROGRAM thinks that most of the source files are in\n".
//				 "         $encoding encoding, while \"$_\" is (likely) in\n".
//		       		 "         $gettext_code encoding. If you are sure that all translatable strings\n".
//				 "         are in same encoding (say UTF-8), please \e[1m*prepend*\e[0m the following\n".
//				 "         line to POTFILES.in:\n\n".
//				 "                 [encoding: UTF-8]\n\n".
//				 "         and make sure that configure.in/ac checks for $PACKAGE >= 0.27 .\n".
//				 "(such warning message will only be reported once.)\n";
//		    $encoding_problem_is_reported = 1;
//		}
//	    }
//	}
//    }
//
//    close OUTFILE;
//    close INFILE;
//
//    unlink "$MODULE.pot";
//    my @xgettext_argument=("$XGETTEXT",
//			   "--add-comments",
//			   "--directory\=\.",
//			   "--output\=$MODULE\.pot",
//			   "--files-from\=\.\/POTFILES\.in\.temp");
//    my $XGETTEXT_KEYWORDS = &FindPOTKeywords;
//    push @xgettext_argument, $XGETTEXT_KEYWORDS;
//    my $MSGID_BUGS_ADDRESS = &FindMakevarsBugAddress;
//    push @xgettext_argument, "--msgid-bugs-address\=$MSGID_BUGS_ADDRESS" if $MSGID_BUGS_ADDRESS;
//    push @xgettext_argument, "--from-code\=$encoding" if ($gettext_support_nonascii);
//    push @xgettext_argument, $XGETTEXT_ARGS if $XGETTEXT_ARGS;
//    my $xgettext_command = join ' ', @xgettext_argument;
//
//    # intercept xgettext error message
//    print "Running $xgettext_command\n" if $VERBOSE;
//    my $xgettext_error_msg = `$xgettext_command 2>\&1`;
//    my $command_failed = $?;
//
//    unlink "POTFILES.in.temp";
//
//    print "Removing generated header (.h) files..." if $VERBOSE;
//    unlink foreach (@temp_headers);
//    print "done.\n" if $VERBOSE;
//
//    if (! $command_failed)
//    {
//	if (! -e "$MODULE.pot")
//	{
//	    print "None of the files in POTFILES.in contain strings marked for translation.\n" if $VERBOSE;
//	}
//	else
//	{
//	    print "Wrote $MODULE.pot\n" if $VERBOSE;
//	}
//    }
//    else
//    {
//	if ($xgettext_error_msg =~ /--from-code/)
//	{
//	    # replace non-ASCII error message with a more useful one.
//	    print STDERR "ERROR: xgettext failed to generate PO template file because there is non-ASCII\n".
//			 "       string marked for translation. Please make sure that all strings marked\n".
//			 "       for translation are in uniform encoding (say UTF-8), then \e[1m*prepend*\e[0m the\n".
//			 "       following line to POTFILES.in and rerun $PROGRAM:\n\n".
//			 "           [encoding: UTF-8]\n\n";
//	}
//	else
//	{
//	    print STDERR "$xgettext_error_msg";
//	    if (-e "$MODULE.pot")
//	    {
//		# is this possible?
//		print STDERR "ERROR: xgettext failed but still managed to generate PO template file.\n".
//			     "       Please consult error message above if there is any.\n";
//	    }
//	    else
//	    {
//		print STDERR "ERROR: xgettext failed to generate PO template file. Please consult\n".
//			     "       error message above if there is any.\n";
//	    }
//	}
//	exit (1);
//    }
//}
			//TODO: cereate content 
			result = CreateFile (TemplateFilePath, "", BuildAction.Compile, monitor) ? null : GettextCatalog.GetString ("Failed Creation of translation template file.");
			return result;
		}

		string UpdatePoFiles (IProgressMonitor monitor)
		{
			string result = null;
			//TODO: update po files from pot file
			return result;
		}

		const string msgfmt = "msgfmt";
		string GenerateMoFiles (IProgressMonitor monitor)
		{
			string result = null;
			//msgfmt cs.po -o cs.mo
			foreach (ProjectFile file in ProjectFiles)
			{
				if (file != null && file.FilePath.EndsWith (".po"))
				{
				 	string mofile = Path.ChangeExtension (file.FilePath, "mo");
					string lang = Path.GetFileNameWithoutExtension (file.FilePath);
				 	if (File.Exists (mofile))
						File.Delete (mofile);

					monitor.Log.WriteLine (GettextCatalog.GetString ("Translation {0}: Compiling.", lang));

					System.Diagnostics.Process process = new System.Diagnostics.Process ();
					process.StartInfo.FileName = msgfmt;
					process.StartInfo.Arguments = file.FilePath + " -o " + mofile;
					process.StartInfo.UseShellExecute = true;
					process.Start ();
					process.WaitForExit ();
					
					if (process.ExitCode == 0)
					{
						monitor.Log.WriteLine (GettextCatalog.GetString ("Translation {0}: Compilation suceed.", lang));
					} else
					{
						string error = process.StandardError.ReadToEnd ();
						result = String.Format (GettextCatalog.GetString ("Translation {0}: Compilation failed. Reason: {1}"), lang, error);
						monitor.Log.WriteLine (result);
						return result;
					}
				}
			}
			return result;
		}

		public override string GetOutputFileName ()
		{
			CombineEntry main = this.ParentCombine;
			if (main == null)
				main = this;
			return Path.Combine (this.BaseDirectory, Path.GetFileNameWithoutExtension (main.FileName) + ".mo");
		}

		protected override ICompilerResult DoBuild (IProgressMonitor monitor)
		{
			CompilerResults results = new CompilerResults (null);
			string result;

			monitor.Log.WriteLine (GettextCatalog.GetString ("Generating list of translatable files (POTFILES)."));
			result = GeneratePotfilesFile (this.ParentCombine, monitor);
			if (! String.IsNullOrEmpty (result))
			{
				results.Errors.Add (new CompilerError (this.Name, 0, 0, null, result));
				result = null;
			}
			monitor.Log.WriteLine (GettextCatalog.GetString ("Generating translation template file."));
			result = GenerateTemplateFile (monitor);
			if (! String.IsNullOrEmpty (result))
			{
				results.Errors.Add (new CompilerError (this.Name, 0, 0, null, result));
				result = null;
			}
			monitor.Log.WriteLine (GettextCatalog.GetString ("Merging changes to translation files."));
			result = UpdatePoFiles (monitor);
			if (! String.IsNullOrEmpty (result))
			{
				results.Errors.Add (new CompilerError (this.Name, 0, 0, null, result));
				result = null;
			}
			monitor.Log.WriteLine (GettextCatalog.GetString ("Compiling translation files."));
			result = GenerateMoFiles (monitor);
			if (! String.IsNullOrEmpty (result))
			{
				results.Errors.Add (new CompilerError (this.Name, 0, 0, null, result));
				result = null;
			}

			return new DefaultCompilerResult (results, "");
		}

		protected override void OnClean (IProgressMonitor monitor)
		{
			NeedsBuilding = true;
			foreach (ProjectFile file in ProjectFiles)
			{
				if (file != null && file.FilePath.EndsWith (".po"))
				{
				 	string mofile = Path.ChangeExtension (file.FilePath, "mo");
				 	if (File.Exists (mofile))
						File.Delete (mofile);
				}
			}
		}

		protected override void CheckNeedsBuild ()
		{
			NeedsBuilding = true;

			FileInfo finfoExtractListFile = new FileInfo (PotfilesFilePath);
			FileInfo finfoMainExtractionFile = new FileInfo (TemplateFilePath);

			if (! finfoMainExtractionFile.Exists)
				return;

			DateTime lastUpdateToExtractListFile = 
				finfoExtractListFile.Exists ? 
					finfoExtractListFile.LastWriteTime : 
					DateTime.MaxValue;

			DateTime lastUpdateToMainExtractionFile = finfoMainExtractionFile.LastWriteTime;

			if (lastUpdateToMainExtractionFile < lastUpdateToExtractListFile)
				return;

			foreach (ProjectFile file in ProjectFiles)
			{
				if (file != null && file.BuildAction != BuildAction.Exclude)
				{
					if (file.FilePath.EndsWith (".po"))
					{
						FileInfo finfobase = new FileInfo (file.FilePath);
						if (finfobase.Exists)
						{
							if (finfobase.LastWriteTime < lastUpdateToMainExtractionFile)
								return;
				 			string mofile = Path.ChangeExtension (file.FilePath, "mo");
							FileInfo finfo = new FileInfo (mofile);
							if (finfo.Exists && finfo.LastWriteTime < finfobase.LastWriteTime)
								return;
						}
					}
				}
			}
			NeedsBuilding = false;
		}

		public override IConfiguration CreateConfiguration (string name)
		{
			TranslationProjectConfiguration conf = new TranslationProjectConfiguration ();
			conf.Name = name;
			return conf;
		}

		public override void Dispose ()
		{
			base.Dispose ();
//			if (Windows != null)
//			{
//				foreach (IDisposable window in Windows)
//					window.Dispose ();
//			}
		}

#region Static members
		public static TranslationProject FindTranslationProject (Combine combine)
		{
			foreach (CombineEntry entry in combine.Entries)
				if (entry is TranslationProject)
					return (TranslationProject)entry;
			return null;
		}

		static Combine SolutionFrom (CombineEntry solutionOrProject)
		{
			return (solutionOrProject is Project)
				? solutionOrProject.ParentCombine
				: (Combine)solutionOrProject;
		}

		public static bool HasTranslationFiles (CombineEntry solutionOrProject)
		{
			if (solutionOrProject == null)
				return false;
			Combine solution = SolutionFrom (solutionOrProject);
			return TranslationProject.FindTranslationProject (solution) != null;
		}

		public static bool AddLanguage (string language, CombineEntry solutionOrProject, IProgressMonitor monitor)
		{
			if (solutionOrProject == null)
				return false;

			Combine solution = SolutionFrom (solutionOrProject);
			TranslationProject translation = TranslationProject.FindTranslationProject (solution);

			try
			{
				IProgressMonitor nullMonitor = new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ();
				if (! translation.CreateFile (language + ".po", "", BuildAction.Compile, nullMonitor))
					throw new ArgumentException (GettextCatalog.GetString ("Language is already in the translations."));	
				monitor.ReportSuccess (String.Format (GettextCatalog.GetString ("Language '{0}' successfully added."), language));
				monitor.Step (1);
			}
			catch (Exception e)
			{
				monitor.ReportError (String.Format ( GettextCatalog.GetString ("Language '{0}' could not be added: "), language), e);
				return false;
			}
			finally
			{
				monitor.EndTask ();
			}
			return true;
		}

		// TODO: rework
		public static bool GenerateFiles (CombineEntry solutionOrProject, string defaultConf, IProgressMonitor monitor)
		{
			if (solutionOrProject == null)
				return false;

			Combine solution = SolutionFrom (solutionOrProject);
			string projectDir = Path.Combine (solution.BaseDirectory, "po");

			monitor.BeginTask (GettextCatalog.GetString ("Generating Translator files for Solution/Project {0}", solutionOrProject.Name), 1);

			bool dirShouldBeCreated = ! Directory.Exists (projectDir);
			try
			{
				IProgressMonitor nullMonitor = new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ();

				//RemoveOldTranslationProjects (solution);
				monitor.Step (1);
				
				if (dirShouldBeCreated)
					Directory.CreateDirectory (projectDir);

				ProjectCreateInformation info = new ProjectCreateInformation ();
				info.CombinePath = solution.BaseDirectory;
				info.CombineName = solution.Name;
				info.ProjectBasePath = projectDir;
				info.ProjectName = GettextCatalog.GetString ("Translations");
				TranslationProject translation = new TranslationProject (info, null);
				translation.GeneratePotfilesFile (solution, nullMonitor);
				monitor.Step (1);
				solution.AddEntry (translation.FileName, nullMonitor);
				solution.Save (nullMonitor);
				monitor.Step (2);
				monitor.ReportSuccess (GettextCatalog.GetString ("Translation support files were successfully generated."));
			}
			catch (Exception e)
			{
				monitor.ReportError (GettextCatalog.GetString ("Translation support files could not be generated: \n") + e.StackTrace.ToString () + "\n>> ", e);
				try
				{
					if (dirShouldBeCreated && Directory.Exists (projectDir))
						Directory.Delete (projectDir, true);
				}
				catch
				{
				}
				return false;
			}
			finally
			{
				monitor.EndTask ();
			}
			return true;
		}

		public static bool GenerateFiles (CombineEntry solutionOrProject, IProgressMonitor monitor)
		{
			 return GenerateFiles (solutionOrProject, solutionOrProject.ActiveConfiguration.Name, monitor);
		}

//		static TranslationProject NextTranslationProject (Combine solution)
//		{
//			return TranslationProject.FindTranslationProject (solution);
//		}

//		static void RemoveOldTranslationProjects (Combine solution)
//		{
//			TranslationProject oldVersion;
//			while ((oldVersion = NextTranslationProject (solution)) != null)
//				solution.RemoveEntry (oldVersion);
//		}
#endregion
	}
}
