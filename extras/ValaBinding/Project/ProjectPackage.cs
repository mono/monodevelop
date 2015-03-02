//
// ProjectPackage.cs: A pkg-config package
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// This source code is licenced under The MIT License:
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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using Mono.Addins;

using MonoDevelop.Core.Serialization;
using MonoDevelop.Ide;
using MonoDevelop.ValaBinding.Utils;
using MonoDevelop.Core;
using System.Diagnostics;

namespace MonoDevelop.ValaBinding
{
    public class ProjectPackage
    {
        [ItemProperty("file")]
        private string file;

        [ItemProperty("name")]
        private string name;

        [ItemProperty("IsProject")]
        private bool is_project;

        private bool isParsed;

        public string Description
        {
            get
            {
                if (!isParsed) Parse();
                return description;
            }
        }
        private string description;

        public string Version
        {
            get
            {
                if (!isParsed) Parse();
                return version;
            }
        }
        private string version;

        public List<string> Requires
        {
            get
            {
                if (!isParsed) Parse();
                return requires;
            }
        }
        private List<string> requires;

        public IList<string> CopyToOutput
        {
            get
            {
                if (!isParsed) Parse();
                return copyToOutput;
            }
        }
        private IList<string> copyToOutput;

        public static string[] PackagePaths
        {
            get { return packagePaths; }
        }
        private static string[] packagePaths;

        static ProjectPackage()
        {
            packagePaths = ScanPackagePaths();
        }

        protected ProjectPackage()
        {
            requires = new List<string>();
            description = string.Empty;
            version = string.Empty;
        }

        public ProjectPackage(string file, string name, bool isProject)
            : this()
        {
            this.file = file;
            this.name = name;
            this.is_project = isProject;
        }

        public static ProjectPackage CreateBetween2Projects(ValaProject parent, ValaProject child)
        {
            var parentDirectory = FileUtils.GetExactPathName(new DirectoryInfo(parent.FileName).Parent.FullName);
            var childPath = FileUtils.GetExactPathName(child.FileName);

            return new ProjectPackage(FileService.AbsoluteToRelativePath(parentDirectory, childPath),
                child.Name, true);
        }

        public string File
        {
            get { return file; }
            set { file = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public bool IsProject
        {
            get { return is_project; }
            set { is_project = value; }
        }

        public override bool Equals(object o)
        {
            ProjectPackage other = o as ProjectPackage;

            if (other == null) return false;

            return Object.Equals(File, other.File) &&
                Object.Equals(IsProject, other.IsProject) &&
                Object.Equals(Name, other.Name);
        }

        public override int GetHashCode()
        {
            return (File == null ? 0 : File.GetHashCode()) ^
                (Name == null ? 0 : Name.GetHashCode()) ^
                IsProject.GetHashCode();
        }

        public ValaProject GetProject()
        {
            if (!IsProject)
            {
                throw new Exception("Tried to get project from not project package.");
            }

            var projects = IdeApp.Workspace.GetAllProjects();
            foreach (var p in projects)
            {
                if (p is ValaProject && p.Name == Name)
                {
                    return p as ValaProject;
                }
            }

            return null;
        }

        /// <summary>
        /// Insert '\n's to make sure string isn't too long.
        /// </summary>
        /// <param name="desc">
        /// The unprocessed description.
        /// A <see cref="System.String"/>
        /// </param>
        /// <returns>
        /// The processed description.
        /// A <see cref="System.String"/>
        /// </returns>
        public static string ProcessDescription(string desc)
        {
            return Regex.Replace(desc, @"(.{1,80} )", "$&" + Environment.NewLine, RegexOptions.Compiled);
        }

        protected void Parse()
        {
            if (!is_project)
            {
                ParsePackage();
                ParseRequires();
                ParseCopyToOutput();
            }

            isParsed = true;
        }

        /// <summary>
        /// Search for a .pc file for this package, and parse its relevant attributes
        /// </summary>
        protected void ParsePackage()
        {
            string line, pcfile;

            try
            {
                foreach (string path in packagePaths)
                {
                    pcfile = Path.Combine(path, Path.ChangeExtension(Path.GetFileName(file), ".pc"));
                    if (!System.IO.File.Exists(pcfile)) { continue; }
                    using (StreamReader reader = new StreamReader(pcfile))
                    {
                        if (null == reader) { continue; }

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (Regex.IsMatch(line, @"^\s*#", RegexOptions.Compiled))
                                continue;

                            //					if (line.IndexOf ('=') >= 0)
                            //						ParseVar (line);

                            if (line.IndexOf(':') >= 0)
                                ParseProperty(line);
                        }
                        return;
                    }
                }
            }
            catch (FileNotFoundException)
            {
                // We just won't populate some fields
            }
            catch (IOException)
            {
                // We just won't populate some fields
            }
        }

        protected void ParseProperty(string line)
        {
            string[] tokens = line.Split(new char[] { ':' }, 2);

            if (2 != tokens.Length) { return; }

            string key = tokens[0];
            string value = tokens[1].Trim();

            if (value.Length <= 0)
                return;

            switch (key)
            {
                case "Description": ;
                    description = ProcessDescription(value);
                    break;
                case "Version":
                    version = value;
                    break;
            }
        }

        protected void ParseRequires()
        {
            string line;

            try
            {
                using (StreamReader reader = new StreamReader(Path.ChangeExtension(file, ".deps")))
                {
                    if (null == reader) { return; }
                    for (; null != (line = reader.ReadLine()); requires.Add(line)) ;
                }
            }
            catch (FileNotFoundException)
            {
                // We just won't populate requires
            }
            catch (IOException)
            {
                // We just won't populate requires
            }
        }

        protected void ParseCopyToOutput()
        {
            var srcFiles = PkgConfigGetVariable("copy_to_output", Path.GetFileNameWithoutExtension(file));
            if (string.IsNullOrEmpty(srcFiles))
            {
                copyToOutput = null;
                return;
            }

            copyToOutput = srcFiles.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Scans PKG_CONFIG_PATH and a few static directories 
        /// for potential pkg-config repositories
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> array: The potential directories
        /// </returns>
        private static string[] ScanPackagePaths()
        {
            List<string> dirs = new List<string>();
            string pkg_var = Environment.GetEnvironmentVariable("PKG_CONFIG_PATH");
            string[] staticPaths = { "/usr/lib/pkgconfig",
				"/usr/lib64/pkgconfig",
				"/usr/lib/x86_64-linux-gnu/pkgconfig",
				"/usr/share/pkgconfig",
				"/usr/local/lib/pkgconfig",
				"/usr/local/share/pkgconfig"
			};

            if (null != pkg_var) { dirs.AddRange(pkg_var.Split(new char[] { System.IO.Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries)); }

            foreach (string dir in staticPaths)
            {
                if (!dirs.Contains(dir)) { dirs.Add(dir); }
            }

            return dirs.ToArray();
        }

        private static string PkgConfigGetVariable(string variableName, string packageName)
        {
            try
            {
                using (Process pkgconfig = new Process())
                {
                    pkgconfig.StartInfo.FileName = "pkg-config";
                    pkgconfig.StartInfo.Arguments = string.Format("--variable={0} {1}", variableName, packageName);
                    pkgconfig.StartInfo.CreateNoWindow = true;
                    pkgconfig.StartInfo.RedirectStandardOutput = true;
                    pkgconfig.StartInfo.UseShellExecute = false;
                    pkgconfig.Start();
                    var result = pkgconfig.StandardOutput.ReadToEnd().Trim();
                    pkgconfig.WaitForExit();
                    
                    if (pkgconfig.ExitCode == 0)
                    {
                        return result;
                    }

                    return "";
                }
            }
            catch (Exception e)
            {
                MessageService.ShowError("Unable to run pkg-config", string.Format("{0}{1}{2}", e.Message, Environment.NewLine, e.StackTrace));
                return "";
            }
        }
    }
}
