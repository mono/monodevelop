var MAJOR_VERSION = 2;
var MINOR_VERSION = 4;
var POINT_VERSION = 1;
var BUILD_VERSION = 0;

var sh = new ActiveXObject("WScript.Shell");
var fs = new ActiveXObject("Scripting.FileSystemObject");
var MONO_LIBS = "C:\\Program Files\\MonoLibraries\\2.6\\lib\\mono\\2.0";
var MD_DIR = "..\\..";
var MD_EXTRAS_DIR = "..\\..\\extras";
var PRODUCT_VERSION = "" + MAJOR_VERSION + "." + MINOR_VERSION + "." + POINT_VERSION + (BUILD_VERSION != 0 ? "." + BUILD_VERSION : "");
var PRODUCT_VERSION_TEXT = "" + MAJOR_VERSION + "." + MINOR_VERSION + (POINT_VERSION != 0 || BUILD_VERSION != 0 ? "." + POINT_VERSION : "") + (BUILD_VERSION != 0 ? "." + BUILD_VERSION : "");
var MONO_PRODUCT_VERSION = "" + MAJOR_VERSION + format (MINOR_VERSION, 2) + format (POINT_VERSION, 2) + format (BUILD_VERSION, 3);

// Build the main solution and the windows-specific add-ins

if (build ("main\\main.sln /p:Configuration=DebugWin32 /p:Platform=\"x86\"") != 0)
	WScript.Quit (1);
if (build ("extras\\VersionControl.Subversion.Win32\\VersionControl.Subversion.Win32.sln") != 0)
	WScript.Quit (1);
if (build ("extras\\MonoDevelop.Debugger.Win32\\MonoDevelop.Debugger.Win32.sln") != 0)
	WScript.Quit (1);

// Copy support assemblies

if (!fs.FolderExists ("Libraries"))
	fs.CreateFolder ("Libraries");

fs.CopyFile (MONO_LIBS + "\\Mono.Addins.dll", "Libraries\\Mono.Addins.dll");
fs.CopyFile (MONO_LIBS + "\\Mono.Addins.Gui.dll", "Libraries\\Mono.Addins.Gui.dll");
fs.CopyFile (MONO_LIBS + "\\Mono.Addins.Setup.dll", "Libraries\\Mono.Addins.Setup.dll");
fs.CopyFile (MONO_LIBS + "\\Mono.Addins.CecilReflector.dll", "Libraries\\Mono.Addins.CecilReflector.dll");
fs.CopyFile (MONO_LIBS + "\\ICSharpCode.SharpZipLib.dll", "Libraries\\ICSharpCode.SharpZipLib.dll");
fs.CopyFile (MONO_LIBS + "\\Mono.GetOptions.dll", "Libraries\\Mono.GetOptions.dll");
fs.CopyFile (MONO_LIBS + "\\monodoc.dll", "Libraries\\monodoc.dll");

// Copy support files

if (!fs.FolderExists ("ExtraFiles")) {
	WScript.Echo ("./ExtraFiles folder not found.\nYou can get the contents of this folder from:\nhttp://monodevelop.com/files/setup/ExtraFiles.zip");
	WScript.Quit (1);
}

fs.CopyFile ("ExtraFiles\\*", MD_DIR + "\\main\\build\\bin\\");
fs.CopyFolder ("ExtraFiles\\*", MD_DIR + "\\main\\build\\bin\\");

// Set the version numbers

regexreplace ("Product.wxs", /ProductVersionText = ".*?"/g, "ProductVersionText = \"" + PRODUCT_VERSION_TEXT + "\"");
regexreplace ("Product.wxs", /ProductVersion = ".*?"/g, "ProductVersion = \"" + PRODUCT_VERSION + "\"");

// Create the updateinfo file

var f = fs.CreateTextFile ("updateinfo", true);
f.WriteLine ("E55A5A70-C6F6-4845-8A01-89DAA5B6DA43 " + MONO_PRODUCT_VERSION);
f.Close ();


// Build the setup
build ("setup\\WixSetup\\WixSetup.sln");

WScript.Echo ("Setup successfully generated");

function build (file)
{
	if (sh.run ("C:\\Windows\\Microsoft.NET\\Framework\\v3.5\\msbuild.exe " + MD_DIR + "\\" + file, 5, true) != 0) {
		WScript.Echo ("Build failed");
		return 1;
	}
	return 0;
}

function regexreplace (file, regex, replacement)
{
   var f = fs.OpenTextFile (file, 1);
   var content = f.ReadAll ();
   f.Close ();
   content = content.replace (regex, replacement);
   f = fs.CreateTextFile (file, true);
   f.Write (content);
   f.Close ();
}

function format (num, len)
{
	var res = num.toString ();
	while (res.length < len)
		res = "0" + res;
	return res;
}
