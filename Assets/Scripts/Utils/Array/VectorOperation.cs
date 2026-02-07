namespace Util
{
    using System.Numerics;
    public static class VectorExt
    {
        public static Vector<float> Sign(Vector<float> x)
        {
            var gtZero = Vector.GreaterThan<float>(x, Vector<float>.Zero);
            var ltZero = Vector.LessThan<float>(x, Vector<float>.Zero);
            
            var result = Vector.ConditionalSelect(gtZero, Vector<float>.One, Vector<float>.Zero);
            return Vector.ConditionalSelect(ltZero, new Vector<float>(-1.0f), result);
        }
    }
}