using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal sealed class KaratsubaMultiplier : IMultiplier
{
    internal delegate uint[] BaseMultiplyHandler(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right);

    private readonly BaseMultiplyHandler _baseMultiply;
    private readonly int _cutoff;

    public KaratsubaMultiplier()
        : this(SimpleMultiplier.MultiplyMagnitudes, BetterBigInteger.KARATSUBA_THRESHOLD)
    {
    }

    public KaratsubaMultiplier(BaseMultiplyHandler baseMultiply, int cutoff = BetterBigInteger.KARATSUBA_THRESHOLD)
    {
        _baseMultiply = baseMultiply ?? throw new ArgumentNullException(nameof(baseMultiply));
        _cutoff = cutoff;
    }

    public BetterBigInteger Multiply(BetterBigInteger left, BetterBigInteger right)
    {
        uint[] product = DivideAndConquerMultiply(left.GetDigits(), right.GetDigits());
        bool negative = left.IsNegative != right.IsNegative;
        return new BetterBigInteger(product, negative);
    }

    private uint[] DivideAndConquerMultiply(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
        int operandSize = Math.Max(left.Length, right.Length);
        if (operandSize < _cutoff)
        {
            return _baseMultiply(left, right);
        }

        int half = (operandSize + 1) / 2;

        ReadOnlySpan<uint> leftLow = left[..Math.Min(half, left.Length)];
        ReadOnlySpan<uint> leftHigh = left.Length > half ? left[half..] : ReadOnlySpan<uint>.Empty;
        ReadOnlySpan<uint> rightLow = right[..Math.Min(half, right.Length)];
        ReadOnlySpan<uint> rightHigh = right.Length > half ? right[half..] : ReadOnlySpan<uint>.Empty;

        uint[] lowProduct = BetterBigInteger.TrimZeros(DivideAndConquerMultiply(leftLow, rightLow));
        uint[] highProduct = BetterBigInteger.TrimZeros(DivideAndConquerMultiply(leftHigh, rightHigh));

        uint[] leftSum = BetterBigInteger.AddMagnitudes(leftLow, leftHigh);
        uint[] rightSum = BetterBigInteger.AddMagnitudes(rightLow, rightHigh);
        uint[] fullProduct = BetterBigInteger.TrimZeros(DivideAndConquerMultiply(leftSum, rightSum));

        uint[] middleTerm = BetterBigInteger.SubtractMagnitudes(fullProduct, highProduct);
        middleTerm = BetterBigInteger.SubtractMagnitudes(middleTerm, lowProduct);

        uint[] result = BetterBigInteger.AddMagnitudes(lowProduct, ShiftLeft(middleTerm, half));
        result = BetterBigInteger.AddMagnitudes(result, ShiftLeft(highProduct, half * 2));

        return BetterBigInteger.TrimZeros(result);
    }

    private static uint[] ShiftLeft(ReadOnlySpan<uint> magnitude, int limbShift)
    {
        if (limbShift == 0)
        {
            return BetterBigInteger.TrimZeros(magnitude.ToArray());
        }

        uint[] shifted = new uint[magnitude.Length + limbShift];
        magnitude.CopyTo(shifted.AsSpan(limbShift));
        return shifted;
    }
}
