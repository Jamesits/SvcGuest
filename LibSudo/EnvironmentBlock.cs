using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LibSudo
{
    public static class EnvironmentBlock
    {
        public static IntPtr New(bool unicode, string rawString)
        {
            if (unicode)
            {
                return Marshal.StringToCoTaskMemUni(rawString);
            }
            else
            {
                return Marshal.StringToCoTaskMemAnsi(rawString);
            }
        }

        public static IntPtr New(bool unicode, IDictionary src)
        {
            // concat string
            // https://stackoverflow.com/a/25400104
            var sb = new StringBuilder();
            foreach (DictionaryEntry e in src)
            {
                sb.Append(e.Key);
                sb.Append("=");
                sb.Append(e.Value);
                sb.Length += 1;
            }
            sb.Length += 1;

            // https://stackoverflow.com/a/8855252
            var envBlockStr = sb.ToString();
            return New(unicode, envBlockStr);
        }

        // Copies the environment block from current process
        public static IntPtr NewFromParent(bool unicode)
        {
            return New(unicode, Environment.GetEnvironmentVariables());
        }

        // Creates an empty environment block
        public static IntPtr NewFromEmpty(bool unicode)
        {
            return New(unicode, "\0");
        }

        public static void Free(IntPtr environmentBlock)
        {
            if (environmentBlock != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(environmentBlock);
            }
        }
    }
}
