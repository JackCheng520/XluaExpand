using System;
using System.Globalization;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;

namespace XLuaExpand
{
    public static class StringUtil
    {
        private class IllegalCharacterData
        {
            public UInt32 from;
            public UInt32 end;
        }
        static private List<IllegalCharacterData> IllegalCharacterList = new List<IllegalCharacterData>();
        static private List<char> illegalCharList = new List<char>();
        public static string Format(string source, params object[] paras)
        {
            string returnString = "";
            if (source == null)
            {
                Debug.LogError("[StringUtil.Format] input source string is null");
            }
            else if (source == "")
            {
                Debug.LogWarning("[StringUtil.Format] input source string is empty");
            }
            else
            {
                try
                {
                    returnString = string.Format(source, paras);
                }
                catch (System.FormatException e)
                {
                    Debug.LogErrorFormat("[StringUtil.Format] System.FormatException, input source string : {0}, Exception Info: {1}", source, e.Message);
                }
                catch (System.NullReferenceException e)
                {
                    Debug.LogErrorFormat("[StringUtil.Format] System.NullReferenceException, input source string : {0}, Exception Info: {1}", source, e.Message);
                }
            }
            return returnString;
        }

        public static void LogFormat(string source, params object[] paras)
        {
            Debug.LogFormat(source, paras);
        }

        public static int CompareString(string first, string second)
        {
            CultureInfo lastculCultureInfo = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("zh-cn");
            int compareResult = string.Compare(first, second, StringComparison.CurrentCulture);
            Thread.CurrentThread.CurrentCulture = lastculCultureInfo;

            return compareResult;
        }

        private static string GetCodes(string str)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < str.Length; ++i)
            {
                int code = Convert.ToInt32(str[i]);
                if (i == 0)
                    result.Append(code);
                else
                {
                    result.Append("," + code);
                }
            }
            return result.ToString();
        }

        public static string TruncString(string source, int limitLength = 24)
        {
            StringBuilder sb = new StringBuilder();
            int strLen = 0;
            for (int i = 0; i < source.Length; ++i)
            {
                int curCharLength = GetCharLength(source[i]);
                if (strLen + curCharLength > limitLength)
                    break;
                strLen += GetCharLength(source[i]);
                sb.Append(source[i]);
            }
            return sb.ToString();
        }

        public static int GetNameLength(string name)
        {
            int length = 0;
            for (int i = 0; i < name.Length; ++i)
            {
                if (char.GetUnicodeCategory((char)name[i]) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }
                length += GetCharLength(name[i]);
            }
            return length;
        }

        private static bool IsChineseChar(char c) { return c >= 0x4e00 && c <= 0x9fbb; }
        private static int GetCharLength(char c)
        {
            if (IsChineseChar(c))
                return 2;
            else if (IsTChar(c))
                return 1;
            return 1;
        }
        private static bool IsTChar(char c) { return c >= 0x0e00 && c <= 0x0e7f; }

        /// <summary>
        /// 用于解决Mono下string.StartsWith性能差的问题，此函数不忽略大小写
        /// </summary>
        public static bool StartsWith(string s, string ss)
        {
            int sslen = ss.Length;
            int slen = s.Length;
            if (sslen > 0 && slen >= sslen)
            {

                for (int i = 0; i < sslen; ++i)
                {
                    int sascii = s[i];
                    int esascii = ss[i];
                    if (sascii != esascii)
                        return false;
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// 用于解决Mono下string.EndsWith性能差的问题，此函数不忽略大小写
        /// </summary>
        public static bool EndsWith(string s, string es)
        {
            int eslen = es.Length;
            int slen = s.Length;
            if (eslen > 0 && slen >= eslen)
            {

                for (int i = 1; i <= eslen; ++i)
                {
                    int sascii = s[slen - i];
                    int esascii = es[eslen - i];
                    if (sascii != esascii)
                        return false;
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// 用于解决Mono下string.StartsWith性能差的问题，此函数忽略大小写
        /// </summary>
        public static bool StartsWithIgnoreCase(string s, string ss)
        {
            int sslen = ss.Length;
            int slen = s.Length;
            if (sslen > 0 && slen >= sslen)
            {

                for (int i = 0; i < sslen; ++i)
                {
                    int sascii = s[i];
                    int esascii = ss[i];
                    if (sascii != esascii)
                    {
                        if (sascii >= 'a' && sascii <= 'z')
                            sascii -= 32;
                        if (esascii >= 'a' && esascii <= 'z')
                            esascii -= 32;

                        if (sascii != esascii)
                            return false;
                    }
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// 用于解决Mono下string.EndsWith性能差的问题，此函数不忽略大小写
        /// </summary>
        public static bool EndsWithIgnoreCase(string s, string es)
        {
            int eslen = es.Length;
            int slen = s.Length;
            if (eslen > 0 && slen >= eslen)
            {

                for (int i = 1; i <= eslen; ++i)
                {
                    int sascii = s[slen - i];
                    int esascii = es[eslen - i];
                    if (sascii != esascii)
                    {
                        if (sascii >= 'a' && sascii <= 'z')
                            sascii -= 32;
                        if (esascii >= 'a' && esascii <= 'z')
                            esascii -= 32;

                        if (sascii != esascii)
                            return false;
                    }
                }

                return true;
            }
            return false;
        }


        //获取时间戳
        public static string GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

    }
}
