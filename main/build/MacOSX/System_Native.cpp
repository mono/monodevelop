
#include <assert.h>
#include <cstddef>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <errno.h>
#include <sys/syslimits.h>

extern "C" int32_t SystemNative_HasOSXSupport (void);
extern "C" int32_t SystemNative_HasOSXSupport (void)
{
    return 1;
}

extern "C" char* SystemNative_RealPath(const char* path);
extern "C" char* SystemNative_RealPath(const char* path)
{
    assert(path != nullptr);
    return realpath(path, nullptr);
}

/**
 * Constants to pass into pathconf.
 *
 * Note - these differ per OS so these values are the PAL-specific
 *        values; they must be converted to the correct platform
 *        values before passing to pathconf.
 */
enum PathConfName : int32_t
{
    PAL_PC_LINK_MAX = 1,
    PAL_PC_MAX_CANON = 2,
    PAL_PC_MAX_INPUT = 3,
    PAL_PC_NAME_MAX = 4,
    PAL_PC_PATH_MAX = 5,
    PAL_PC_PIPE_BUF = 6,
    PAL_PC_CHOWN_RESTRICTED = 7,
    PAL_PC_NO_TRUNC = 8,
    PAL_PC_VDISABLE = 9,
};

extern "C" int64_t SystemNative_PathConf(const char* path, PathConfName name);
extern "C" int64_t SystemNative_PathConf(const char* path, PathConfName name)
{
    int32_t confValue = -1;
    switch (name)
    {
        case PAL_PC_LINK_MAX:
            confValue = _PC_LINK_MAX;
            break;
        case PAL_PC_MAX_CANON:
            confValue = _PC_MAX_CANON;
            break;
        case PAL_PC_MAX_INPUT:
            confValue = _PC_MAX_INPUT;
            break;
        case PAL_PC_NAME_MAX:
            confValue = _PC_NAME_MAX;
            break;
        case PAL_PC_PATH_MAX:
            confValue = _PC_PATH_MAX;
            break;
        case PAL_PC_PIPE_BUF:
            confValue = _PC_PIPE_BUF;
            break;
        case PAL_PC_CHOWN_RESTRICTED:
            confValue = _PC_CHOWN_RESTRICTED;
            break;
        case PAL_PC_NO_TRUNC:
            confValue = _PC_NO_TRUNC;
            break;
        case PAL_PC_VDISABLE:
            confValue = _PC_VDISABLE;
            break;
    }

    if (confValue == -1)
    {
        assert(false && "Unknown PathConfName");
        errno = EINVAL;
        return -1;
    }

    return pathconf(path, confValue);
}

extern "C" int64_t SystemNative_GetMaximumPath();
extern "C" int64_t SystemNative_GetMaximumPath()
{
    int64_t result = pathconf("/", _PC_PATH_MAX);
    if (result == -1)
    {
        result = PATH_MAX;
    }

    return result;
}

/**
 * Error codes returned via ConvertErrno.
 *
 * Only the names (without the PAL_ prefix) are specified by POSIX.
 *
 * The values chosen below are simply assigned arbitrarily (originally
 * in alphabetical order they appear in the spec, but they can't change so
 * add new values to the end!).
 *
 * Also, the values chosen are deliberately outside the range of
 * typical UNIX errnos (small numbers), HRESULTs (negative for errors)
 * and Win32 errors (0x0000 - 0xFFFF). This isn't required for
 * correctness, but may help debug a caller that is interpreting a raw
 * int incorrectly.
 *
 * Wherever the spec says "x may be the same value as y", we do use
 * the same value so that callers cannot not take a dependency on
 * being able to distinguish between them.
 */
enum Error : int32_t
{
    PAL_SUCCESS = 0,

    PAL_E2BIG = 0x10001,           // Argument list too long.
    PAL_EACCES = 0x10002,          // Permission denied.
    PAL_EADDRINUSE = 0x10003,      // Address in use.
    PAL_EADDRNOTAVAIL = 0x10004,   // Address not available.
    PAL_EAFNOSUPPORT = 0x10005,    // Address family not supported.
    PAL_EAGAIN = 0x10006,          // Resource unavailable, try again (same value as EWOULDBLOCK),
    PAL_EALREADY = 0x10007,        // Connection already in progress.
    PAL_EBADF = 0x10008,           // Bad file descriptor.
    PAL_EBADMSG = 0x10009,         // Bad message.
    PAL_EBUSY = 0x1000A,           // Device or resource busy.
    PAL_ECANCELED = 0x1000B,       // Operation canceled.
    PAL_ECHILD = 0x1000C,          // No child processes.
    PAL_ECONNABORTED = 0x1000D,    // Connection aborted.
    PAL_ECONNREFUSED = 0x1000E,    // Connection refused.
    PAL_ECONNRESET = 0x1000F,      // Connection reset.
    PAL_EDEADLK = 0x10010,         // Resource deadlock would occur.
    PAL_EDESTADDRREQ = 0x10011,    // Destination address required.
    PAL_EDOM = 0x10012,            // Mathematics argument out of domain of function.
    PAL_EDQUOT = 0x10013,          // Reserved.
    PAL_EEXIST = 0x10014,          // File exists.
    PAL_EFAULT = 0x10015,          // Bad address.
    PAL_EFBIG = 0x10016,           // File too large.
    PAL_EHOSTUNREACH = 0x10017,    // Host is unreachable.
    PAL_EIDRM = 0x10018,           // Identifier removed.
    PAL_EILSEQ = 0x10019,          // Illegal byte sequence.
    PAL_EINPROGRESS = 0x1001A,     // Operation in progress.
    PAL_EINTR = 0x1001B,           // Interrupted function.
    PAL_EINVAL = 0x1001C,          // Invalid argument.
    PAL_EIO = 0x1001D,             // I/O error.
    PAL_EISCONN = 0x1001E,         // Socket is connected.
    PAL_EISDIR = 0x1001F,          // Is a directory.
    PAL_ELOOP = 0x10020,           // Too many levels of symbolic links.
    PAL_EMFILE = 0x10021,          // File descriptor value too large.
    PAL_EMLINK = 0x10022,          // Too many links.
    PAL_EMSGSIZE = 0x10023,        // Message too large.
    PAL_EMULTIHOP = 0x10024,       // Reserved.
    PAL_ENAMETOOLONG = 0x10025,    // Filename too long.
    PAL_ENETDOWN = 0x10026,        // Network is down.
    PAL_ENETRESET = 0x10027,       // Connection aborted by network.
    PAL_ENETUNREACH = 0x10028,     // Network unreachable.
    PAL_ENFILE = 0x10029,          // Too many files open in system.
    PAL_ENOBUFS = 0x1002A,         // No buffer space available.
    PAL_ENODEV = 0x1002C,          // No such device.
    PAL_ENOENT = 0x1002D,          // No such file or directory.
    PAL_ENOEXEC = 0x1002E,         // Executable file format error.
    PAL_ENOLCK = 0x1002F,          // No locks available.
    PAL_ENOLINK = 0x10030,         // Reserved.
    PAL_ENOMEM = 0x10031,          // Not enough space.
    PAL_ENOMSG = 0x10032,          // No message of the desired type.
    PAL_ENOPROTOOPT = 0x10033,     // Protocol not available.
    PAL_ENOSPC = 0x10034,          // No space left on device.
    PAL_ENOSYS = 0x10037,          // Function not supported.
    PAL_ENOTCONN = 0x10038,        // The socket is not connected.
    PAL_ENOTDIR = 0x10039,         // Not a directory or a symbolic link to a directory.
    PAL_ENOTEMPTY = 0x1003A,       // Directory not empty.
    PAL_ENOTRECOVERABLE = 0x1003B, // State not recoverable.
    PAL_ENOTSOCK = 0x1003C,        // Not a socket.
    PAL_ENOTSUP = 0x1003D,         // Not supported (same value as EOPNOTSUP).
    PAL_ENOTTY = 0x1003E,          // Inappropriate I/O control operation.
    PAL_ENXIO = 0x1003F,           // No such device or address.
    PAL_EOVERFLOW = 0x10040,       // Value too large to be stored in data type.
    PAL_EOWNERDEAD = 0x10041,      // Previous owner died.
    PAL_EPERM = 0x10042,           // Operation not permitted.
    PAL_EPIPE = 0x10043,           // Broken pipe.
    PAL_EPROTO = 0x10044,          // Protocol error.
    PAL_EPROTONOSUPPORT = 0x10045, // Protocol not supported.
    PAL_EPROTOTYPE = 0x10046,      // Protocol wrong type for socket.
    PAL_ERANGE = 0x10047,          // Result too large.
    PAL_EROFS = 0x10048,           // Read-only file system.
    PAL_ESPIPE = 0x10049,          // Invalid seek.
    PAL_ESRCH = 0x1004A,           // No such process.
    PAL_ESTALE = 0x1004B,          // Reserved.
    PAL_ETIMEDOUT = 0x1004D,       // Connection timed out.
    PAL_ETXTBSY = 0x1004E,         // Text file busy.
    PAL_EXDEV = 0x1004F,           // Cross-device link.
    PAL_ESOCKTNOSUPPORT = 0x1005E, // Socket type not supported.
    PAL_EPFNOSUPPORT = 0x10060,    // Protocol family not supported.
    PAL_ESHUTDOWN = 0x1006C,       // Socket shutdown.
    PAL_EHOSTDOWN = 0x10070,       // Host is down.
    PAL_ENODATA = 0x10071,         // No data available.

    // POSIX permits these to have the same value and we make them
    // always equal so that we cannot introduce a dependency on
    // distinguishing between them that would not work on all
    // platforms.
    PAL_EOPNOTSUPP = PAL_ENOTSUP, // Operation not supported on socket
    PAL_EWOULDBLOCK = PAL_EAGAIN, // Operation would block

    // This one is not part of POSIX, but is a catch-all for the case
    // where we cannot convert the raw errno value to something above.
    PAL_ENONSTANDARD = 0x1FFFF,
};

extern "C" Error SystemNative_ConvertErrorPlatformToPal(int32_t platformErrno);
extern "C" Error SystemNative_ConvertErrorPlatformToPal(int32_t platformErrno)
{
    switch (platformErrno)
    {
        case 0:
            return PAL_SUCCESS;
        case E2BIG:
            return PAL_E2BIG;
        case EACCES:
            return PAL_EACCES;
        case EADDRINUSE:
            return PAL_EADDRINUSE;
        case EADDRNOTAVAIL:
            return PAL_EADDRNOTAVAIL;
        case EAFNOSUPPORT:
            return PAL_EAFNOSUPPORT;
        case EAGAIN:
            return PAL_EAGAIN;
        case EALREADY:
            return PAL_EALREADY;
        case EBADF:
            return PAL_EBADF;
        case EBADMSG:
            return PAL_EBADMSG;
        case EBUSY:
            return PAL_EBUSY;
        case ECANCELED:
            return PAL_ECANCELED;
        case ECHILD:
            return PAL_ECHILD;
        case ECONNABORTED:
            return PAL_ECONNABORTED;
        case ECONNREFUSED:
            return PAL_ECONNREFUSED;
        case ECONNRESET:
            return PAL_ECONNRESET;
        case EDEADLK:
            return PAL_EDEADLK;
        case EDESTADDRREQ:
            return PAL_EDESTADDRREQ;
        case EDOM:
            return PAL_EDOM;
        case EDQUOT:
            return PAL_EDQUOT;
        case EEXIST:
            return PAL_EEXIST;
        case EFAULT:
            return PAL_EFAULT;
        case EFBIG:
            return PAL_EFBIG;
        case EHOSTUNREACH:
            return PAL_EHOSTUNREACH;
        case EIDRM:
            return PAL_EIDRM;
        case EILSEQ:
            return PAL_EILSEQ;
        case EINPROGRESS:
            return PAL_EINPROGRESS;
        case EINTR:
            return PAL_EINTR;
        case EINVAL:
            return PAL_EINVAL;
        case EIO:
            return PAL_EIO;
        case EISCONN:
            return PAL_EISCONN;
        case EISDIR:
            return PAL_EISDIR;
        case ELOOP:
            return PAL_ELOOP;
        case EMFILE:
            return PAL_EMFILE;
        case EMLINK:
            return PAL_EMLINK;
        case EMSGSIZE:
            return PAL_EMSGSIZE;
        case EMULTIHOP:
            return PAL_EMULTIHOP;
        case ENAMETOOLONG:
            return PAL_ENAMETOOLONG;
        case ENETDOWN:
            return PAL_ENETDOWN;
        case ENETRESET:
            return PAL_ENETRESET;
        case ENETUNREACH:
            return PAL_ENETUNREACH;
        case ENFILE:
            return PAL_ENFILE;
        case ENOBUFS:
            return PAL_ENOBUFS;
        case ENODEV:
            return PAL_ENODEV;
        case ENOENT:
            return PAL_ENOENT;
        case ENOEXEC:
            return PAL_ENOEXEC;
        case ENOLCK:
            return PAL_ENOLCK;
        case ENOLINK:
            return PAL_ENOLINK;
        case ENOMEM:
            return PAL_ENOMEM;
        case ENOMSG:
            return PAL_ENOMSG;
        case ENOPROTOOPT:
            return PAL_ENOPROTOOPT;
        case ENOSPC:
            return PAL_ENOSPC;
        case ENOSYS:
            return PAL_ENOSYS;
        case ENOTCONN:
            return PAL_ENOTCONN;
        case ENOTDIR:
            return PAL_ENOTDIR;
        case ENOTEMPTY:
            return PAL_ENOTEMPTY;
#ifdef ENOTRECOVERABLE // not available in NetBSD
        case ENOTRECOVERABLE:
            return PAL_ENOTRECOVERABLE;
#endif
        case ENOTSOCK:
            return PAL_ENOTSOCK;
        case ENOTSUP:
            return PAL_ENOTSUP;
        case ENOTTY:
            return PAL_ENOTTY;
        case ENXIO:
            return PAL_ENXIO;
        case EOVERFLOW:
            return PAL_EOVERFLOW;
#ifdef EOWNERDEAD // not available in NetBSD
        case EOWNERDEAD:
            return PAL_EOWNERDEAD;
#endif
        case EPERM:
            return PAL_EPERM;
        case EPIPE:
            return PAL_EPIPE;
        case EPROTO:
            return PAL_EPROTO;
        case EPROTONOSUPPORT:
            return PAL_EPROTONOSUPPORT;
        case EPROTOTYPE:
            return PAL_EPROTOTYPE;
        case ERANGE:
            return PAL_ERANGE;
        case EROFS:
            return PAL_EROFS;
        case ESPIPE:
            return PAL_ESPIPE;
        case ESRCH:
            return PAL_ESRCH;
        case ESTALE:
            return PAL_ESTALE;
        case ETIMEDOUT:
            return PAL_ETIMEDOUT;
        case ETXTBSY:
            return PAL_ETXTBSY;
        case EXDEV:
            return PAL_EXDEV;
        case ESOCKTNOSUPPORT:
            return PAL_ESOCKTNOSUPPORT;
        case EPFNOSUPPORT:
            return PAL_EPFNOSUPPORT;
        case ESHUTDOWN:
            return PAL_ESHUTDOWN;
        case EHOSTDOWN:
            return PAL_EHOSTDOWN;
        case ENODATA:
            return PAL_ENODATA;

// #if because these will trigger duplicate case label warnings when
// they have the same value, which is permitted by POSIX and common.
#if EOPNOTSUPP != ENOTSUP
        case EOPNOTSUPP:
            return PAL_EOPNOTSUPP;
#endif
#if EWOULDBLOCK != EAGAIN
        case EWOULDBLOCK:
            return PAL_EWOULDBLOCK;
#endif
    }

    return PAL_ENONSTANDARD;
}

extern "C" int32_t SystemNative_ConvertErrorPalToPlatform(Error error);
extern "C" int32_t SystemNative_ConvertErrorPalToPlatform(Error error)
{
    switch (error)
    {
        case PAL_SUCCESS:
            return 0;
        case PAL_E2BIG:
            return E2BIG;
        case PAL_EACCES:
            return EACCES;
        case PAL_EADDRINUSE:
            return EADDRINUSE;
        case PAL_EADDRNOTAVAIL:
            return EADDRNOTAVAIL;
        case PAL_EAFNOSUPPORT:
            return EAFNOSUPPORT;
        case PAL_EAGAIN:
            return EAGAIN;
        case PAL_EALREADY:
            return EALREADY;
        case PAL_EBADF:
            return EBADF;
        case PAL_EBADMSG:
            return EBADMSG;
        case PAL_EBUSY:
            return EBUSY;
        case PAL_ECANCELED:
            return ECANCELED;
        case PAL_ECHILD:
            return ECHILD;
        case PAL_ECONNABORTED:
            return ECONNABORTED;
        case PAL_ECONNREFUSED:
            return ECONNREFUSED;
        case PAL_ECONNRESET:
            return ECONNRESET;
        case PAL_EDEADLK:
            return EDEADLK;
        case PAL_EDESTADDRREQ:
            return EDESTADDRREQ;
        case PAL_EDOM:
            return EDOM;
        case PAL_EDQUOT:
            return EDQUOT;
        case PAL_EEXIST:
            return EEXIST;
        case PAL_EFAULT:
            return EFAULT;
        case PAL_EFBIG:
            return EFBIG;
        case PAL_EHOSTUNREACH:
            return EHOSTUNREACH;
        case PAL_EIDRM:
            return EIDRM;
        case PAL_EILSEQ:
            return EILSEQ;
        case PAL_EINPROGRESS:
            return EINPROGRESS;
        case PAL_EINTR:
            return EINTR;
        case PAL_EINVAL:
            return EINVAL;
        case PAL_EIO:
            return EIO;
        case PAL_EISCONN:
            return EISCONN;
        case PAL_EISDIR:
            return EISDIR;
        case PAL_ELOOP:
            return ELOOP;
        case PAL_EMFILE:
            return EMFILE;
        case PAL_EMLINK:
            return EMLINK;
        case PAL_EMSGSIZE:
            return EMSGSIZE;
        case PAL_EMULTIHOP:
            return EMULTIHOP;
        case PAL_ENAMETOOLONG:
            return ENAMETOOLONG;
        case PAL_ENETDOWN:
            return ENETDOWN;
        case PAL_ENETRESET:
            return ENETRESET;
        case PAL_ENETUNREACH:
            return ENETUNREACH;
        case PAL_ENFILE:
            return ENFILE;
        case PAL_ENOBUFS:
            return ENOBUFS;
        case PAL_ENODEV:
            return ENODEV;
        case PAL_ENOENT:
            return ENOENT;
        case PAL_ENOEXEC:
            return ENOEXEC;
        case PAL_ENOLCK:
            return ENOLCK;
        case PAL_ENOLINK:
            return ENOLINK;
        case PAL_ENOMEM:
            return ENOMEM;
        case PAL_ENOMSG:
            return ENOMSG;
        case PAL_ENOPROTOOPT:
            return ENOPROTOOPT;
        case PAL_ENOSPC:
            return ENOSPC;
        case PAL_ENOSYS:
            return ENOSYS;
        case PAL_ENOTCONN:
            return ENOTCONN;
        case PAL_ENOTDIR:
            return ENOTDIR;
        case PAL_ENOTEMPTY:
            return ENOTEMPTY;
#ifdef ENOTRECOVERABLE // not available in NetBSD
        case PAL_ENOTRECOVERABLE:
            return ENOTRECOVERABLE;
#endif
        case PAL_ENOTSOCK:
            return ENOTSOCK;
        case PAL_ENOTSUP:
            return ENOTSUP;
        case PAL_ENOTTY:
            return ENOTTY;
        case PAL_ENXIO:
            return ENXIO;
        case PAL_EOVERFLOW:
            return EOVERFLOW;
#ifdef EOWNERDEAD // not available in NetBSD
        case PAL_EOWNERDEAD:
            return EOWNERDEAD;
#endif
        case PAL_EPERM:
            return EPERM;
        case PAL_EPIPE:
            return EPIPE;
        case PAL_EPROTO:
            return EPROTO;
        case PAL_EPROTONOSUPPORT:
            return EPROTONOSUPPORT;
        case PAL_EPROTOTYPE:
            return EPROTOTYPE;
        case PAL_ERANGE:
            return ERANGE;
        case PAL_EROFS:
            return EROFS;
        case PAL_ESPIPE:
            return ESPIPE;
        case PAL_ESRCH:
            return ESRCH;
        case PAL_ESTALE:
            return ESTALE;
        case PAL_ETIMEDOUT:
            return ETIMEDOUT;
        case PAL_ETXTBSY:
            return ETXTBSY;
        case PAL_EXDEV:
            return EXDEV;
        case PAL_EPFNOSUPPORT:
            return EPFNOSUPPORT;
        case PAL_ESOCKTNOSUPPORT:
            return ESOCKTNOSUPPORT;
        case PAL_ESHUTDOWN:
            return ESHUTDOWN;
        case PAL_EHOSTDOWN:
            return EHOSTDOWN;
        case PAL_ENODATA:
            return ENODATA;
        case PAL_ENONSTANDARD:
            break; // fall through to assert
    }

    // We should not use this function to round-trip platform -> pal
    // -> platform. It's here only to synthesize a platform number
    // from the fixed set above. Note that the assert is outside the
    // switch rather than in a default case block because not
    // having a default will trigger a warning (as error) if there's
    // an enum value we haven't handled. Should that trigger, make
    // note that there is probably a corresponding missing case in the
    // other direction above, but the compiler can't warn in that case
    // because the platform values are not part of an enum.
    assert(false && "Unknown error code");
    return -1;
}

extern "C" const char* SystemNative_StrErrorR(int32_t platformErrno, char* buffer, int32_t bufferSize);
extern "C" const char* SystemNative_StrErrorR(int32_t platformErrno, char* buffer, int32_t bufferSize)
{
    assert(buffer != nullptr);
    assert(bufferSize > 0);

    if (bufferSize < 0)
        return nullptr;

// Note that we must use strerror_r because plain strerror is not
// thread-safe.
//
// However, there are two versions of strerror_r:
//    - GNU:   char* strerror_r(int, char*, size_t);
//    - POSIX: int   strerror_r(int, char*, size_t);
//
// The former may or may not use the supplied buffer, and returns
// the error message string. The latter stores the error message
// string into the supplied buffer and returns an error code.

#if 0
    const char* message = strerror_r(platformErrno, buffer, UnsignedCast(bufferSize));
    assert(message != nullptr);
    return message;
#else
    int error = strerror_r(platformErrno, buffer, (uint32_t)(bufferSize));
    if (error == ERANGE)
    {
        // Buffer is too small to hold the entire message, but has
        // still been filled to the extent possible and null-terminated.
        return nullptr;
    }

    // The only other valid error codes are 0 for success or EINVAL for
    // an unknown error, but in the latter case a reasonable string (e.g
    // "Unknown error: 0x123") is returned.
    assert(error == 0 || error == EINVAL);
    return buffer;
#endif
}

extern "C" void SystemNative_Sync();
extern "C" void SystemNative_Sync()
{
    sync();
}
