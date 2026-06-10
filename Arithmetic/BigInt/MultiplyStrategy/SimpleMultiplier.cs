using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal sealed class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger left, BetterBigInteger right)
    {
        uint[] product = MultiplyMagnitudes(left.GetDigits(), right.GetDigits());
        bool negative = left.IsNegative != right.IsNegative;
        return new BetterBigInteger(product, negative);
    }

    internal static uint[] MultiplyMagnitudes(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
        uint[] product = new uint[left.Length + right.Length];

        for (int leftIndex = 0; leftIndex < left.Length; leftIndex++)
        {
            ulong carry = 0;
            ulong leftLimb = left[leftIndex];

            for (int rightIndex = 0; rightIndex < right.Length; rightIndex++)
            {
                ulong combined = product[leftIndex + rightIndex] + leftLimb * right[rightIndex] + carry;
                product[leftIndex + rightIndex] = (uint)combined;
                carry = combined >> 32;
            }

            if (carry > 0)
            {
                product[leftIndex + right.Length] = (uint)carry;
            }
        }

        return BetterBigInteger.TrimZeros(product);
    }
}
