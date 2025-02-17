using System.Text;

namespace Universe.SqlServerQueryCache.External;

public static class SafeFileName
{
    public static string Get(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return "";
        var keyPath = fileName;

        StringBuilder ret = new StringBuilder();
        int len = keyPath.Length;
        bool isOpen = true;
        for (int i = 0; i < keyPath.Length; i++)
        {
            char c = keyPath[i];
            if (c == '\\') ret.Append((char)0x29F5);
            else if (c == '/') ret.Append((char)0x2215);
            else if (c == ':') ret.Append((char)0xA789);
            else if (c == '"')
            {
                if (isOpen) ret.Append((char)0x201C); else ret.Append((char)0x201D);
                isOpen = !isOpen;
            }
            else
                ret.Append(c);
        }

        return ret.ToString();
    }
}