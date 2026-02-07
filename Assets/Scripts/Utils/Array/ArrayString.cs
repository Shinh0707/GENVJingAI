using System.Text;

namespace Util
{
    public static class StringExt
    {
        public static string ArrayString<T>(string label, T[] values)
        {
            StringBuilder sb = new();
            ArrayString(sb, label, values);
            return sb.ToString();
        }
        public static void ArrayString<T>(StringBuilder sb,string label, T[] values)
        {
            sb.AppendFormat("{0}:[", label);
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    sb.Append(values[i]);
                    if (i < values.Length - 1)
                    {
                        sb.Append(", ");
                    }
                }
            }
            sb.Append("]");
        }
    }
}