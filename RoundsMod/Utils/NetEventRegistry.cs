using System.Collections.Generic;

namespace RoundsMod.Utils
{
    public static class NetEventRegistry
    {
        private static readonly Dictionary<string, byte> AssignedCodes = new Dictionary<string, byte>();
        private static readonly HashSet<byte> UsedBytes = new HashSet<byte>();

        public static byte GetCode(string key)  
        {
            if (AssignedCodes.TryGetValue(key, out byte existing))
                return existing;

            uint hash = 2166136261u;
            foreach (char c in key)
            {
                hash ^= c;
                hash *= 16777619u;
            }

            byte code = (byte)(100 + (hash % 95));
            while (UsedBytes.Contains(code))
            {
                code = (byte)(100 + ((code - 100 + 1) % 95));
            }

            UsedBytes.Add(code);
            AssignedCodes[key] = code;
            return code;
        }
    }
}
