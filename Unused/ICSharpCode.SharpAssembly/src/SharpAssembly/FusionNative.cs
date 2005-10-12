/*
 * Managed Fusion API definitions
 * 
 * 2002-09-29: Some API defs corrected, thanks to Rotor and MS KB article 317540
 * 
 */

using System;
using System.Text;
using System.Runtime.InteropServices;

namespace MSjogren.Fusion.Native
{
  [Flags]
  enum ASM_CACHE_FLAGS
  {
    ASM_CACHE_ZAP = 0x1,
    ASM_CACHE_GAC = 0x2,
    ASM_CACHE_DOWNLOAD = 0x4
  }

  [Flags]
  enum ASM_DISPLAY_FLAGS
  {
    VERSION = 0x1,
    CULTURE = 0x2,
    PUBLIC_KEY_TOKEN = 0x4,
    PUBLIC_KEY = 0x8,
    CUSTOM = 0x10,
    PROCESSORARCHITECTURE = 0x20,
    LANGUAGEID = 0x40
  }

  [Flags]
  enum ASM_CMP_FLAGS
  {
    NAME = 0x1,
    MAJOR_VERSION = 0x2,
    MINOR_VERSION = 0x4,
    BUILD_NUMBER = 0x8,
    REVISION_NUMBER = 0x10,
    PUBLIC_KEY_TOKEN = 0x20,
    CULTURE = 0x40,
    CUSTOM = 0x80,
    ALL = NAME | MAJOR_VERSION | MINOR_VERSION |
          REVISION_NUMBER | BUILD_NUMBER |
          PUBLIC_KEY_TOKEN | CULTURE | CUSTOM,
    DEFAULT = 0x100
  }

  [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
  struct FUSION_INSTALL_REFERENCE
  {
    public uint cbSize;
    public uint dwFlags;
    public Guid guidScheme;
    public string szIdentifier;
    public string szNonCannonicalData;
  }

  [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
  struct ASSEMBLY_INFO
  {
    public uint cbAssemblyInfo;
    public uint dwAssemblyFlags;
    public ulong uliAssemblySizeInKB;
    public string pszCurrentAssemblyPathBuf;
    public uint cchBuf;
  }


  [
    ComImport(),
    Guid("E707DCDE-D1CD-11D2-BAB9-00C04F8ECEAE"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)
  ]
  interface IAssemblyCache
  {
    [PreserveSig()]
    int UninstallAssembly(
      uint dwFlags, 
      [MarshalAs(UnmanagedType.LPWStr)] string pszAssemblyName,
      [MarshalAs(UnmanagedType.LPArray)] FUSION_INSTALL_REFERENCE[] pRefData,
      out uint pulDisposition);

    [PreserveSig()]
    int QueryAssemblyInfo(
      uint dwFlags,
      [MarshalAs(UnmanagedType.LPWStr)] string pszAssemblyName,
      ref ASSEMBLY_INFO pAsmInfo);

    [PreserveSig()]
    int CreateAssemblyCacheItem(
      uint dwFlags,
      IntPtr pvReserved,
      out IAssemblyCacheItem ppAsmItem,
      [MarshalAs(UnmanagedType.LPWStr)] string pszAssemblyName);

    [PreserveSig()]
    int CreateAssemblyScavenger(
      [MarshalAs(UnmanagedType.IUnknown)] out object ppAsmScavenger);

    [PreserveSig()]
    int InstallAssembly(
      uint dwFlags,
      [MarshalAs(UnmanagedType.LPWStr)] string pszManifestFilePath,
      [MarshalAs(UnmanagedType.LPArray)] FUSION_INSTALL_REFERENCE[] pRefData);
  }


  [
    ComImport(),
    Guid("9E3AAEB4-D1CD-11D2-BAB9-00C04F8ECEAE"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)
  ]
  interface IAssemblyCacheItem
  {
    void CreateStream(
      uint dwFlags,
      [MarshalAs(UnmanagedType.LPWStr)] string pszStreamName,
      uint dwFormat,
      uint dwFormatFlags,
      out UCOMIStream ppIStream,
      ref long puliMaxSize);

    void Commit(
      uint dwFlags,
      out long pulDisposition);

    void AbortItem();
  }
  

  enum ASM_NAME
  {
    PUBLIC_KEY = 0,          // byte[]
    PUBLIC_KEY_TOKEN,        // byte[8]
    HASH_VALUE,
    NAME,                    // LPWSTR
    MAJOR_VERSION,           // ushort
    MINOR_VERSION,           // ushort
    BUILD_NUMBER,            // ushort
    REVISION_NUMBER,         // ushort
    CULTURE,                 // LPWSTR
    PROCESSOR_ID_ARRAY,
    OSINFO_ARRAY,
    HASH_ALGID,
    ALIAS,
    CODEBASE_URL,            // LPWSTR
    CODEBASE_LASTMOD,        // FILETIME
    NULL_PUBLIC_KEY,
    NULL_PUBLIC_KEY_TOKEN,
    CUSTOM,                  // LPWSTR; ZAP string for NGEN assemblies
    NULL_CUSTOM,                
    MVID,                    // byte[16] / Guid
    //MAX_PARAMS
  }


  [
    ComImport(),
    Guid("CD193BC0-B4BC-11D2-9833-00C04FC31D2E"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)
  ]
  interface IAssemblyName
  {
    [PreserveSig()]
    int SetProperty(
      ASM_NAME PropertyId,
      IntPtr pvProperty,
      uint cbProperty);

    [PreserveSig()]
    int GetProperty(
      ASM_NAME PropertyId,
      IntPtr pvProperty,
      ref uint pcbProperty);

    [PreserveSig()]
    int Finalize();

    [PreserveSig()]
    int GetDisplayName(
      [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder szDisplayName,
      ref uint pccDisplayName,
      ASM_DISPLAY_FLAGS dwDisplayFlags);

    [PreserveSig()]
    int BindToObject(
      ref Guid refIID,
      [MarshalAs(UnmanagedType.IUnknown)] object pUnkSink,
      [MarshalAs(UnmanagedType.IUnknown)] object pUnkContext,   // IApplicationContext
      [MarshalAs(UnmanagedType.LPWStr)] string szCodeBase,
      long llFlags,
      IntPtr pvReserved,
      uint cbReserved,
      out IntPtr ppv);

    [PreserveSig()]
    int GetName(
      ref uint lpcwBuffer,
      [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzName);

    [PreserveSig()]
    int GetVersion(
      out uint pdwVersionHi,
      out uint pdwVersionLow);

    [PreserveSig()]
    int IsEqual(
      IAssemblyName pName,
      ASM_CMP_FLAGS dwCmpFlags);

    [PreserveSig()]
    int Clone(
      out IAssemblyName pName);
  }


  [
    ComImport(),
    Guid("21B8916C-F28E-11D2-A473-00C04F8EF448"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)
  ]
  interface IAssemblyEnum
  {
    [PreserveSig()]
    int GetNextAssembly(
      IntPtr pvReserved,
      out IAssemblyName ppName,
      uint dwFlags);

    [PreserveSig()]
    int Reset();

    [PreserveSig()]
    int Clone(
      out IAssemblyEnum ppEnum);
  }


  [
    ComImport(),
    Guid("1D23DF4D-A1E2-4B8B-93D6-6EA3DC285A54"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)
  ]
  interface IHistoryReader
  {
    [PreserveSig()]
    int GetFilePath(
      [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzFilePath,
      ref uint pdwSize);

    [PreserveSig()]
    int GetApplicationName(
      [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzAppName,
      ref uint pdwSize);

    [PreserveSig()]
    int GetEXEModulePath(
      [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzExePath,
      ref uint pdwSize);

    void GetNumActivations(
      out uint pdwNumActivations);

    void GetActivationDate(
      uint dwIdx,             // One-based!
      out long /* FILETIME */ pftDate);

    [PreserveSig()]
    int GetRunTimeVersion(
      ref long /* FILETIME */ pftActivationDate,
      [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzRunTimeVersion,
      ref uint pdwSize);

    void GetNumAssemblies(
      ref long /* FILETIME */ pftActivationDate,
      out uint pdwNumAsms);

    void GetHistoryAssembly(
      ref long /* FILETIME */ pftActivationDate,
      uint dwIdx,             // One-based!
      [MarshalAs(UnmanagedType.IUnknown)] out object ppHistAsm);

  }


  [
    ComImport(),
    Guid("582dac66-e678-449f-aba6-6faaec8a9394"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)
  ]
  interface IInstallReferenceItem
  {
    [PreserveSig()]
    int GetReference(
      out IntPtr ppRefData,     // FUSION_INSTALL_REFERENCE**
      uint dwFlags,
      IntPtr pvReserved);
  }


  [
    ComImport(),
    Guid("56b1a988-7c0c-4aa2-8639-c3eb5a90226f"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)
  ]
  interface IInstallReferenceEnum
  {
    [PreserveSig()]
    int GetNextInstallReferenceItem(
      out IInstallReferenceItem ppRefItem,
      uint dwFlags,
      IntPtr pvReserved);
  }

  
  
  class FusionApi
  {
    // Install reference scheme identifiers
    public static readonly Guid FUSION_REFCOUNT_UNINSTALL_SUBKEY_GUID = new Guid("8cedc215-ac4b-488b-93c0-a50a49cb2fb8");
    public static readonly Guid FUSION_REFCOUNT_FILEPATH_GUID = new Guid("b02f9d65-fb77-4f7a-afa5-b391309f11c9");
    public static readonly Guid FUSION_REFCOUNT_OPAQUE_STRING_GUID = new Guid("2ec93463-b0c3-45e1-8364-327e96aea856");
    public static readonly Guid FUSION_REFCOUNT_MSI_GUID = new Guid("25df0fc1-7f97-4070-add7-4b13bbfd7cb8");
      
    const uint IASSEMBLYCACHE_INSTALL_FLAG_REFRESH = 0x00000001;
    const uint IASSEMBLYCACHE_INSTALL_FLAG_FORCE_REFRESH = 0x00000002;

    const uint IASSEMBLYCACHE_UNINSTALL_DISPOSITION_UNINSTALLED = 1;
    const uint IASSEMBLYCACHE_UNINSTALL_DISPOSITION_STILL_IN_USE = 2;
    const uint IASSEMBLYCACHE_UNINSTALL_DISPOSITION_ALREADY_UNINSTALLED = 3;
    const uint IASSEMBLYCACHE_UNINSTALL_DISPOSITION_DELETE_PENDING = 4;
    const uint IASSEMBLYCACHE_UNINSTALL_DISPOSITION_HAS_INSTALL_REFERENCES = 5;
    const uint IASSEMBLYCACHE_UNINSTALL_DISPOSITION_REFERENCE_NOT_FOUND = 6;


    [DllImport("fusion.dll", CharSet=CharSet.Unicode, PreserveSig=false)]
    public static extern void GetCachePath(
      ASM_CACHE_FLAGS dwCacheFlags,
      [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzCachePath,
      ref uint pcchPath);

    [DllImport("fusion.dll", PreserveSig=false)]
    public static extern void CreateAssemblyCache(
      out IAssemblyCache ppAsmCache, 
      uint dwReserved);

    [DllImport("fusion.dll", PreserveSig=false)]
    public static extern void CreateAssemblyEnum(
      out IAssemblyEnum ppEnum,
      IntPtr pUnkReserved,
      IAssemblyName pName,
      ASM_CACHE_FLAGS dwFlags,
      IntPtr pvReserved);

    [DllImport("fusion.dll", CharSet=CharSet.Unicode, PreserveSig=false)]
    public static extern void CreateAssemblyNameObject(
      out IAssemblyName ppName,
      string szAssemblyName,
      uint dwFlags,
      IntPtr pvReserved);

    [DllImport("fusion.dll", PreserveSig=false)]
    public static extern void CreateInstallReferenceEnum(
      out IInstallReferenceEnum ppRefEnum,
      IAssemblyName pName,
      uint dwFlags,
      IntPtr pvReserved);


    [DllImport("fusion.dll", CharSet=CharSet.Unicode, PreserveSig=false)]
    public static extern void CreateHistoryReader(
      string wzFilePath,
      out IHistoryReader ppHistReader); 

    // Retrieves the path of the ApplicationHistory folder, typically
    // Documents and Settings\<user>\Local Settings\Application Data\ApplicationHistory
    // containing .ini files that can be read with IHistoryReader.
    // pwdSize appears to be the offset of the last backslash in the returned
    // string after the call.
    [DllImport("fusion.dll", CharSet=CharSet.Unicode, PreserveSig=false)]
    public static extern void GetHistoryFileDirectory(
      [MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzDir,
      ref uint pdwSize);

    [DllImport("fusion.dll", CharSet=CharSet.Unicode, PreserveSig=false)]
    public static extern void LookupHistoryAssembly(
      string pwzFilePath,
      ref FILETIME pftActivationDate,
      string pwzAsmName,
      string pwzPublicKeyToken, 
      string wzCulture,
      string pwzVerRef,
      out IntPtr pHistAsm);     // IHistoryAssembly

    [DllImport("fusion.dll", PreserveSig=false)]
    public static extern void NukeDownloadedCache();

    [DllImport("fusion.dll", PreserveSig=false)]
    public static extern void CreateApplicationContext(
      IAssemblyName pName,
      out IntPtr ppCtx);   // IApplicationContext


    //
    // Brings up the .NET Applicaion Restore wizard
    // Returns S_OK, 0x80131075 (App not run) or 0x80131087 (Fix failed)
    //
    [DllImport("shfusion.dll", CharSet=CharSet.Unicode)]
    public static extern uint PolicyManager(
      IntPtr hWndParent,
      string pwzFullyQualifiedAppPath,
      string pwzAppName,
      int dwFlags);

  }
}
