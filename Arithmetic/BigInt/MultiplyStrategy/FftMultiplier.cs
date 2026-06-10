using System.Numerics;
using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal sealed class FftMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger left, BetterBigInteger right)
    {
        ReadOnlySpan<uint> leftLimbs = left.GetDigits();
        ReadOnlySpan<uint> rightLimbs = right.GetDigits();

        int spectrumSize = 1;
        int requiredSize = (leftLimbs.Length + rightLimbs.Length) * 2;
        while (spectrumSize < requiredSize)
        {
            spectrumSize <<= 1;
        }

        Complex[] leftSpectrum = new Complex[spectrumSize];
        Complex[] rightSpectrum = new Complex[spectrumSize];

        for (int index = 0; index < leftLimbs.Length; index++)
        {
            leftSpectrum[index * 2] = new Complex(leftLimbs[index] & 0xFFFF, 0);
            leftSpectrum[index * 2 + 1] = new Complex(leftLimbs[index] >> 16, 0);
        }

        for (int index = 0; index < rightLimbs.Length; index++)
        {
            rightSpectrum[index * 2] = new Complex(rightLimbs[index] & 0xFFFF, 0);
            rightSpectrum[index * 2 + 1] = new Complex(rightLimbs[index] >> 16, 0);
        }

        TransformSpectrum(leftSpectrum, invert: false);
        TransformSpectrum(rightSpectrum, invert: false);

        for (int index = 0; index < spectrumSize; index++)
        {
            leftSpectrum[index] *= rightSpectrum[index];
        }

        TransformSpectrum(leftSpectrum, invert: true);

        uint[] product = new uint[leftLimbs.Length + rightLimbs.Length + 1];
        long carry = 0;

        for (int index = 0; index < spectrumSize; index++)
        {
            long value = (long)Math.Round(leftSpectrum[index].Real / spectrumSize) + carry;
            uint chunk = (uint)(value & 0xFFFF);
            carry = value >> 16;

            int limbIndex = index / 2;
            if (limbIndex >= product.Length)
            {
                continue;
            }

            if ((index & 1) == 0)
            {
                product[limbIndex] = chunk;
            }
            else
            {
                product[limbIndex] |= chunk << 16;
            }
        }

        int carryIndex = (spectrumSize + 1) / 2;
        while (carry > 0)
        {
            if (carryIndex >= product.Length)
            {
                Array.Resize(ref product, carryIndex + 1);
            }

            ulong sum = product[carryIndex] + (ulong)carry;
            product[carryIndex] = (uint)sum;
            carry = (long)(sum >> 32);
            carryIndex++;
        }

        bool negative = left.IsNegative != right.IsNegative;
        return new BetterBigInteger(BetterBigInteger.TrimZeros(product), negative);
    }

    private static void TransformSpectrum(Complex[] spectrum, bool invert)
    {
        int length = spectrum.Length;

        for (int i = 1, j = 0; i < length; i++)
        {
            int bit = length >> 1;
            for (; (j & bit) != 0; bit >>= 1)
            {
                j ^= bit;
            }

            j ^= bit;

            if (i < j)
            {
                (spectrum[i], spectrum[j]) = (spectrum[j], spectrum[i]);
            }
        }

        for (int block = 2; block <= length; block <<= 1)
        {
            double angle = 2 * Math.PI / block * (invert ? -1 : 1);
            Complex root = new Complex(Math.Cos(angle), Math.Sin(angle));

            for (int start = 0; start < length; start += block)
            {
                Complex factor = Complex.One;
                int half = block / 2;

                for (int offset = 0; offset < half; offset++)
                {
                    Complex even = spectrum[start + offset];
                    Complex odd = spectrum[start + offset + half] * factor;

                    spectrum[start + offset] = even + odd;
                    spectrum[start + offset + half] = even - odd;
                    factor *= root;
                }
            }
        }
    }
}
