using System;
namespace MegaApi
{
    /// <summary>
    ///General errors:
    ///EINTERNAL (-1): 
    ///EARGS (-2): 
    ///EAGAIN (-3) 
    ///ERATELIMIT (-4): 
    ///
    ///Upload errors:
    ///EFAILED (-5): 
    ///ETOOMANY (-6): 
    ///ERANGE (-7):
    ///EEXPIRED (-8):
    ///
    ///Filesystem/Account-level errors:
    ///ENOENT (-9):
    ///ECIRCULAR (-10): 
    ///EACCESS (-11): 
    ///EEXIST (-12):
    ///EINCOMPLETE (-13): 
    ///EKEY (-14): 
    ///ESID (-15): 
    ///EBLOCKED (-16): 
    ///EOVERQUOTA (-17):
    ///ETEMPUNAVAIL (-18): 
    /// </summary>
    public class MegaApiError
    {
        // MEGA.CO.NZ SPECIFIC
        /// <summary>
        /// An internal error has occurred. Please submit a bug report, detailing the exact circumstances in which this error occurred.
        /// </summary>
        public const int EINTERNAL = -1;
        /// <summary>
        /// You have passed invalid arguments to this command.
        /// </summary>
        public const int EARGS = -2;
        /// <summary>
        /// (always at the request level): A temporary congestion or server malfunction prevented your request from being processed. No data was altered. Retry. Retries must be spaced with exponential backoff.
        /// </summary>
        public const int EAGAIN = -3;
        /// <summary>
        /// You have exceeded your command weight per time quota. Please wait a few seconds, then try again (this should never happen in sane real-life applications).
        /// </summary>
        public const int ERATELIMIT = -4;
        /// <summary>
        /// The upload failed. Please restart it from scratch.
        /// </summary>
        public const int EFAILED = -5;
        /// <summary>
        /// Too many concurrent IP addresses are accessing this upload target URL.
        /// </summary>
        public const int ETOOMANY = -6;
        /// <summary>
        ///  The upload file packet is out of range or not starting and ending on a chunk boundary.
        /// </summary>
        public const int ERANGE = -7;
        /// <summary>
        ///  The upload target URL you are trying to access has expired. Please request a fresh one.
        /// </summary>
        public const int EEXPIRED = -8;
        /// <summary>
        ///  Object (typically, node or user) not found
        /// </summary>
        public const int ENOENT = -9;
        /// <summary>
        /// Circular linkage attempted
        /// </summary>
        public const int ECIRCULAR = -10;
        /// <summary>
        /// Access violation (e.g., trying to write to a read-only share)
        /// </summary>
        public const int EACCESS = -11;
        /// <summary>
        ///  Trying to create an object that already exists
        /// </summary>
        public const int EEXIST = -12;
        /// <summary>
        /// Trying to access an incomplete resource
        /// </summary>
        public const int EINCOMPLETE = -13;
        /// <summary>
        /// A decryption operation failed (never returned by the API)
        /// </summary>
        public const int EKEY = -14;
        /// <summary>
        /// Invalid or expired user session, please relogin
        /// </summary>
        public const int ESID = -15;
        /// <summary>
        /// User blocked
        /// </summary>
        public const int EBLOCKED = -16;
        /// <summary>
        ///  Request over quota
        /// </summary>
        public const int EOVERQUOTA = -17;
        /// <summary>
        /// Resource temporarily not available, please try again later
        /// </summary>
        public const int ETEMPUNAVAIL = -18;

        // MEGADESKTOP SPECIFIC
        /// <summary>
        /// Unexpected server response
        /// </summary>
        public const int EUNEXPECTED = -35;
        /// <summary>
        /// Could not restore the session after it was expired. should never happen
        /// </summary>
        public const int EBROKEN = -36;
        /// <summary>
        /// Wrong parameters passed. Please report the bug to the MegaDesktop team
        /// </summary>
        public const int EAPI = -37;
        /// <summary>
        /// The system error
        /// </summary>
        public const int ESYSTEM = -38;
        /// <summary>
        /// Wrong usage of this api
        /// </summary>
        public const int EWRONG = -39;
    }

    public class MegaApiException : Exception
    {
        public int ErrorNumber { get; private set; }
        public MegaApiException (int errno, string message, Exception inner = null) : base(message, inner)
        {
            ErrorNumber = errno;
        }
        public override string ToString()
        {
            return String.Format("{0}: {1}{2}", Message, ErrorNumber, 
                InnerException == null ? "" :
                String.Format(", Inner exception:{0}", InnerException));
        }
    }
}