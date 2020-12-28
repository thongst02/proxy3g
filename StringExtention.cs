using System;
using System.Linq;


namespace ShellHelper
{
    public static class StringExtention
    { 
      
        public static string Regx(this string sText, string sRegx)
        {
            System.Text.RegularExpressions.Regex regx = new System.Text.RegularExpressions.Regex(sRegx);
            var mat = regx.Match(sText);
            if (mat.Success)
                return mat.Value;
            return "";
        }
        public static string RemoveSpecialChar(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;
            return text.ReplaceRegx("[^\\w\\._]", "");// System.Text.RegularExpressions.Regex.Replace(text, "[^\\w\\._]", "");
        }
        public static string ReplaceRegx(this string text, string regx, string sTextReplace = "")
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(regx))
                return null;
            return System.Text.RegularExpressions.Regex.Replace(text, regx, sTextReplace);
        }
      
        public static int ToInt(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;
            int nResult = 0;
            int.TryParse(str, out nResult);
            return nResult;
        }
        public static string GetRegNumber(this string str, int nLength)
        {
            var regx = new System.Text.RegularExpressions.Regex("\\d{" + nLength + "}");
            var mat = regx.Match(str);
            if (mat.Success)
            {
                return mat.Value;
            }
            return "";
        }
       
    }
}
