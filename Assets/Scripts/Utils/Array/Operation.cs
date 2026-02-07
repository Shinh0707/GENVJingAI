using System;
using System.Numerics;

namespace Util
{
    public static partial class Operation
    {
        public delegate float FUnaryOperation(float a);
        public delegate Vector<float> FVUnaryOperation(Vector<float> a);
        public delegate float FBinaryOperation(float a, float b);
        public delegate Vector<float> FVBinaryOperation(Vector<float> a, Vector<float> b);
    }
}