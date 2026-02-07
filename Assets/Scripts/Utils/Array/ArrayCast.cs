namespace Util
{
    using System;
    using System.Numerics;
    using System.Runtime.InteropServices;

    public static partial class ArrayExt
    {
        public static Span<Vector<T>> AsVectorSpan<T>(T[] values) where T : struct
        {
            return MemoryMarshal.Cast<T, Vector<T>>(values);
        }
    }
}