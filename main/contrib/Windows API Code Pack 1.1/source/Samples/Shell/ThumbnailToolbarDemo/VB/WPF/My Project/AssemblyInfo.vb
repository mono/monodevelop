' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
#Region "Using directives"

Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Resources
Imports System.Globalization
Imports System.Windows
Imports System.Runtime.InteropServices

#End Region

' General Information about an assembly is controlled through the following 
' set of attributes. Change these attribute values to modify the information
' associated with an assembly.
<Assembly: AssemblyTitle("ImageViewerDemo")>
<Assembly: AssemblyDescription("")>
<Assembly: AssemblyConfiguration("")>
<Assembly: AssemblyCompany("Microsoft")> 
<Assembly: AssemblyProduct("Microsoft Windows API Code Pack for .NET Framework")> 
<Assembly: AssemblyCopyright("Copyright © Microsoft 2009")> 
<Assembly: AssemblyTrademark("")>
<Assembly: AssemblyCulture("")>
<Assembly: ComVisible(False)>

'In order to begin building localizable applications, set 
'<UICulture>CultureYouAreCodingWith</UICulture> in your .csproj file
'inside a <PropertyGroup>.  For example, if you are using US english
'in your source files, set the <UICulture> to en-US.  Then uncomment
'the NeutralResourceLanguage attribute below.  Update the "en-US" in
'the line below to match the UICulture setting in the project file.

'[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]


' Specifies the location in which theme dictionaries are stored for types in an assembly.
	' Specifies the location of system theme-specific resource dictionaries for this project.
	' The default setting in this project is "None" since this default project does not
	' include these user-defined theme files:
	'     Themes\Aero.NormalColor.xaml
	'     Themes\Classic.xaml
	'     Themes\Luna.Homestead.xaml
	'     Themes\Luna.Metallic.xaml
	'     Themes\Luna.NormalColor.xaml
	'     Themes\Royale.NormalColor.xaml
	' Specifies the location of the system non-theme specific resource dictionary:
	'     Themes\generic.xaml
<Assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)>


' Version information for an assembly consists of the following four values:
'
'      Major Version
'      Minor Version 
'      Build Number
'      Revision
'
' You can specify all the values or you can default the Revision and Build Numbers 
' by using the '*' as shown below:
<Assembly: AssemblyVersion("1.0.0.0")>
