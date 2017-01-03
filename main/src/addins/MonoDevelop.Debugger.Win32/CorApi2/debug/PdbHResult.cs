namespace Microsoft.Samples.Debugging.CorDebug
{
    /// <summary>
    /// Taken from https://github.com/crummel/dotnet_coreclr/blob/master/src/ToolBox/SOS/DacTableGen/diautil.cs
    /// </summary>
    public enum PdbHResult
    {
        E_PDB_OK = unchecked((int)0x806d0001),
        E_PDB_USAGE                 ,
        E_PDB_OUT_OF_MEMORY         , // not used, use E_OUTOFMEMORY
        E_PDB_FILE_SYSTEM           ,
        E_PDB_NOT_FOUND             ,
        E_PDB_INVALID_SIG           ,
        E_PDB_INVALID_AGE           ,
        E_PDB_PRECOMP_REQUIRED      ,
        E_PDB_OUT_OF_TI             ,
        E_PDB_NOT_IMPLEMENTED       ,   // use E_NOTIMPL
        E_PDB_V1_PDB                ,
        E_PDB_FORMAT                ,
        E_PDB_LIMIT                 ,
        E_PDB_CORRUPT               ,
        E_PDB_TI16                  ,
        E_PDB_ACCESS_DENIED         ,  // use E_ACCESSDENIED
        E_PDB_ILLEGAL_TYPE_EDIT     ,
        E_PDB_INVALID_EXECUTABLE    ,
        E_PDB_DBG_NOT_FOUND         ,
        E_PDB_NO_DEBUG_INFO         ,
        E_PDB_INVALID_EXE_TIMESTAMP ,
        E_PDB_RESERVED              ,
        E_PDB_DEBUG_INFO_NOT_IN_PDB ,
        E_PDB_SYMSRV_BAD_CACHE_PATH ,
        E_PDB_SYMSRV_CACHE_FULL     ,
        E_PDB_MAX
    }}