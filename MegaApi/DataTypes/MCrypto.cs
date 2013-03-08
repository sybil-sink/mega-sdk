using Mono.Math;
namespace MegaApi.DataTypes
{
    // used by SessionConverter
    public class CheckedSession
    {
        public string SessionId { get; set; }
        public bool IsOk { get; set; }
    }

    // used by CryptoConverter
    public class MCrypto
    {
        public byte[] Value { get; set; }
    }

    public class RSAKey
    {
        public BigInteger P { get; set; }
        public BigInteger Q { get; set; }
        public BigInteger D { get; set; }
        public BigInteger U { get; set; }
        public BigInteger N { get; set; }
    }
}