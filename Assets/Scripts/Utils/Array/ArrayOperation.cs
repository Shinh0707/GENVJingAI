namespace Util
{
    using System.Numerics;
    public static partial class ArrayExt
    {
        public static float[] ApplyBinaryOperation(float[] a, float[] b, Operation.FVBinaryOperation voperation, Operation.FBinaryOperation operation)
        {
            int length = a.Length;
            var newData = new float[length];
            int vecSize = Vector<float>.Count;
            int iterations = length / vecSize;
            var spanA = AsVectorSpan(a);
            var spanB = AsVectorSpan(b);
            var spanNew = AsVectorSpan(newData);
            for (int i = 0; i < iterations; i++)
            {
                spanNew[i] = voperation(spanA[i], spanB[i]);
            }
            int remainderIndex = iterations * vecSize;
            for (int j = remainderIndex; j < length; j++)
            {
                newData[j] = operation(a[j], b[j]);
            }
            return newData;
        }
        public static float[] ApplyUnaryOperation(float[] a, Operation.FVUnaryOperation voperation, Operation.FUnaryOperation operation)
        {
            int length = a.Length;
            var newData = new float[length];
            int vecSize = Vector<float>.Count;
            int iterations = length / vecSize;

            var spanA = AsVectorSpan(a);
            var spanNew = AsVectorSpan(newData);

            for (int i = 0; i < iterations; i++)
            {
                spanNew[i] = voperation(spanA[i]);
            }

            int remainderIndex = iterations * vecSize;
            for (int j = remainderIndex; j < length; j++)
            {
                newData[j] = operation(a[j]);
            }

            return newData;
        }
        public static float[] ApplyUnaryOperation(float[] a, Operation.FUnaryOperation operation)
        {
            int length = a.Length;
            var newData = new float[length];
            for (int i = 0; i < length; i++)
            {
                newData[i] = operation(a[i]);
            }
            return newData;
        }
    }
}