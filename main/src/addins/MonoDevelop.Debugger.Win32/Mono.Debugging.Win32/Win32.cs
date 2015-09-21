using System;
using System.Runtime.InteropServices;

[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Unicode)]
struct STARTUPINFO
{
	public int cb;
	public string lpReserved;
	public string lpDesktop;
	public string lpTitle;
	public int dwX;
	public int dwY;
	public int dwXSize;
	public int dwYSize;
	public int dwXCountChars;
	public int dwYCountChars;
	public int dwFillAttribute;
	public int dwFlags;
	public short wShowWindow;
	public short cbReserved2;
	public IntPtr lpReserved2;
	public IntPtr hStdInput;
	public IntPtr hStdOutput;
	public IntPtr hStdError;
}

[StructLayout (LayoutKind.Sequential)]
internal struct PROCESS_INFORMATION
{
	public IntPtr hProcess;
	public IntPtr hThread;
	public int dwProcessId;
	public int dwThreadId;
}

[Flags]
enum CreationFlags
{
	CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
	CREATE_DEFAULT_ERROR_MODE = 0x04000000,
	CREATE_NEW_CONSOLE = 0x00000010,
	CREATE_NEW_PROCESS_GROUP = 0x00000200,
	CREATE_NO_WINDOW = 0x08000000,
	CREATE_PROTECTED_PROCESS = 0x00040000,
	CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
	CREATE_SEPARATE_WOW_VDM = 0x00001000,
	CREATE_SUSPENDED = 0x00000004,
	CREATE_UNICODE_ENVIRONMENT = 0x00000400,
	DEBUG_ONLY_THIS_PROCESS = 0x00000002,
	DEBUG_PROCESS = 0x00000001,
	DETACHED_PROCESS = 0x00000008,
	EXTENDED_STARTUPINFO_PRESENT = 0x00080000
}

