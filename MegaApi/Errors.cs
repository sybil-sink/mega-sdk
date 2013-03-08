/// <summary>
///General errors:
///EINTERNAL (-1): An internal error has occurred. Please submit a bug report, detailing the exact circumstances in which this error occurred.
///EARGS (-2): You have passed invalid arguments to this command.
///EAGAIN (-3) (always at the request level): A temporary congestion or server malfunction prevented your request from being processed. No data was altered. Retry. Retries must be spaced with exponential backoff.
///ERATELIMIT (-4): You have exceeded your command weight per time quota. Please wait a few seconds, then try again (this should never happen in sane real-life applications).
///
///Upload errors:
///EFAILED (-5): The upload failed. Please restart it from scratch.
///ETOOMANY (-6): Too many concurrent IP addresses are accessing this upload target URL.
///ERANGE (-7): The upload file packet is out of range or not starting and ending on a chunk boundary.
///EEXPIRED (-8): The upload target URL you are trying to access has expired. Please request a fresh one.
///
///Filesystem/Account-level errors:
///ENOENT (-9): Object (typically, node or user) not found
///ECIRCULAR (-10): Circular linkage attempted
///EACCESS (-11): Access violation (e.g., trying to write to a read-only share)
///EEXIST (-12): Trying to create an object that already exists
///EINCOMPLETE (-13): Trying to access an incomplete resource
///EKEY (-14): A decryption operation failed (never returned by the API)
///ESID (-15): Invalid or expired user session, please relogin
///EBLOCKED (-16): User blocked
///EOVERQUOTA (-17): Request over quota
///ETEMPUNAVAIL (-18): Resource temporarily not available, please try again later
/// </summary>
public class MegaApiError
{
    public const int EINTERNAL = -1;
    public const int EARGS = -2;
    public const int EAGAIN = -3;
    public const int ERATELIMIT = -4;
    public const int EFAILED = -5;
    public const int ETOOMANY = -6;
    public const int ERANGE = -7;
    public const int EEXPIRED = -8;
    public const int ENOENT = -9;
    public const int ECIRCULAR = -10;
    public const int EACCESS = -11;
    public const int EEXIST = -12;
    public const int EINCOMPLETE = -13;
    public const int EKEY = -14;
    public const int ESID = -15;
    public const int EBLOCKED = -16;
    public const int EOVERQUOTA = -17;
    public const int ETEMPUNAVAIL = -18;

    // unexpected server response
    public const int EUNEXPECTED = -35;
    // could not restore the session after it was expired. should never happen
    public const int EBROKEN = -36;
    // Wrong parameters passed. Our fault.
    public const int EAPI = -37;
    // Could not access the local file
    public const int ELOCAL = -38;
}