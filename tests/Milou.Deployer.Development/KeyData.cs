using System;

namespace Milou.Deployer.Development
{
    public class KeyData
    {
        public byte[] Key { get; }

        public KeyData(byte[] key)
        {
            Key = key;
        }

        public string KeyAsBase64 => Convert.ToBase64String(Key);
    }
}