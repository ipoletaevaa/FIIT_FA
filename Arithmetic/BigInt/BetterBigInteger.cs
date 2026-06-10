using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    internal const int KARATSUBA_THRESHOLD = 64;
    internal const int FFT_THRESHOLD = 1024;

    private int _signBit;
    private uint _smallValue;
    private uint[]? _data;

    internal static IMultiplier SimpleStrategy { get; } = new SimpleMultiplier();
    internal static IMultiplier KaratsubaStrategy { get; } = new KaratsubaMultiplier();
    internal static IMultiplier FftStrategy { get; } = new FftMultiplier();

    public bool IsNegative => _signBit == 1;
    public bool IsZero => _data is null && _smallValue == 0;
    public bool IsOne => _data is null && _smallValue == 1 && !IsNegative;

    public static BetterBigInteger Zero { get; } = new([0]);
    public static BetterBigInteger One { get; } = new([1]);

    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        AssignFromDigits(digits ?? [], isNegative);
    }

    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false)
        : this(digits.ToArray(), isNegative)
    {
    }

    public BetterBigInteger(string value, int radix)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Некорректная строка");
        }

        if (radix is < 2 or > 36)
        {
            throw new ArgumentOutOfRangeException(nameof(radix), "Основание должно быть от 2 до 36");
        }

        bool negative = false;
        int start = 0;

        if (value[0] == '-')
        {
            negative = true;
            start = 1;
        }
        else if (value[0] == '+')
        {
            start = 1;
        }

        if (start >= value.Length)
        {
            throw new ArgumentException("Некорректная строка");
        }

        List<uint> limbs = new();
        for (int index = start; index < value.Length; index++)
        {
            int digit = ParseDigit(value[index], radix);
            ulong carry = (ulong)digit;

            for (int limbIndex = 0; limbIndex < limbs.Count; limbIndex++)
            {
                ulong updated = (ulong)limbs[limbIndex] * (ulong)radix + carry;
                limbs[limbIndex] = (uint)updated;
                carry = updated >> 32;
            }

            while (carry > 0)
            {
                limbs.Add((uint)carry);
                carry >>= 32;
            }
        }

        AssignFromDigits(limbs.ToArray(), negative);
    }

    public ReadOnlySpan<uint> GetDigits()
    {
        if (_data is not null)
        {
            return _data;
        }

        return MemoryMarshal.CreateReadOnlySpan(ref _smallValue, 1);
    }

    public int CompareTo(IBigInteger? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (IsNegative != other.IsNegative)
        {
            return IsNegative ? -1 : 1;
        }

        ReadOnlySpan<uint> left = GetDigits();
        ReadOnlySpan<uint> right = other.GetDigits();

        if (left.Length > right.Length)
        {
            return IsNegative ? -1 : 1;
        }

        if (left.Length < right.Length)
        {
            return IsNegative ? 1 : -1;
        }

        for (int index = left.Length - 1; index >= 0; index--)
        {
            if (left[index] > right[index])
            {
                return IsNegative ? -1 : 1;
            }

            if (left[index] < right[index])
            {
                return IsNegative ? 1 : -1;
            }
        }

        return 0;
    }

    public bool Equals(IBigInteger? other) => other is not null && CompareTo(other) == 0;

    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(_signBit);

        ReadOnlySpan<uint> digits = GetDigits();
        for (int index = 0; index < digits.Length; index++)
        {
            hash.Add(digits[index]);
        }

        return hash.ToHashCode();
    }

    public static BetterBigInteger operator +(BetterBigInteger left, BetterBigInteger right)
    {
        if (left.IsNegative == right.IsNegative)
        {
            return new BetterBigInteger(AddMagnitudes(left.GetDigits(), right.GetDigits()), left.IsNegative);
        }

        int compare = CompareAbsolute(left.GetDigits(), right.GetDigits());
        if (compare == 0)
        {
            return Zero;
        }

        BetterBigInteger larger = compare > 0 ? left : right;
        BetterBigInteger smaller = compare > 0 ? right : left;
        return new BetterBigInteger(
            SubtractMagnitudes(larger.GetDigits(), smaller.GetDigits()),
            larger.IsNegative);
    }

    public static BetterBigInteger operator -(BetterBigInteger left, BetterBigInteger right) => left + (-right);

    public static BetterBigInteger operator -(BetterBigInteger value)
    {
        if (value.IsZero)
        {
            return value;
        }

        return value._data is not null
            ? new BetterBigInteger(value._data, !value.IsNegative)
            : new BetterBigInteger([value._smallValue], !value.IsNegative);
    }

    public static BetterBigInteger operator /(BetterBigInteger left, BetterBigInteger right) =>
        DivRem(left, right).Quotient;

    public static BetterBigInteger operator %(BetterBigInteger left, BetterBigInteger right) =>
        DivRem(left, right).Remainder;

    public static BetterBigInteger operator *(BetterBigInteger left, BetterBigInteger right)
    {
        if (left.IsZero || right.IsZero)
        {
            return Zero;
        }

        if (left.IsOne)
        {
            return right;
        }

        if (right.IsOne)
        {
            return left;
        }

        int maxLength = Math.Max(left.GetDigits().Length, right.GetDigits().Length);
        IMultiplier strategy = maxLength < KARATSUBA_THRESHOLD
            ? SimpleStrategy
            : maxLength < FFT_THRESHOLD
                ? KaratsubaStrategy
                : FftStrategy;

        return strategy.Multiply(left, right);
    }

    public static BetterBigInteger operator ~(BetterBigInteger value) => (-value) - One;

    public static BetterBigInteger operator &(BetterBigInteger left, BetterBigInteger right) =>
        ApplyBitwise(left, right, static (x, y) => x & y);

    public static BetterBigInteger operator |(BetterBigInteger left, BetterBigInteger right) =>
        ApplyBitwise(left, right, static (x, y) => x | y);

    public static BetterBigInteger operator ^(BetterBigInteger left, BetterBigInteger right) =>
        ApplyBitwise(left, right, static (x, y) => x ^ y);

    public static BetterBigInteger operator <<(BetterBigInteger value, int shift)
    {
        if (shift == 0)
        {
            return value;
        }

        if (shift < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shift), "Shift must be non-negative.");
        }

        int limbShift = shift / 32;
        int bitShift = shift % 32;
        ReadOnlySpan<uint> source = value.GetDigits();
        uint[] shifted = new uint[source.Length + limbShift + 1];

        for (int index = 0; index < source.Length; index++)
        {
            uint highPart = source[index] << bitShift;
            uint lowPart = bitShift > 0 && index > 0 ? source[index - 1] >> (32 - bitShift) : 0;
            shifted[index + limbShift] = highPart | lowPart;
        }

        if (bitShift > 0)
        {
            shifted[source.Length + limbShift] = source[^1] >> (32 - bitShift);
        }

        return new BetterBigInteger(shifted, value.IsNegative);
    }

    public static BetterBigInteger operator >>(BetterBigInteger value, int shift)
    {
        if (shift == 0)
        {
            return value;
        }

        if (shift < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shift), "Сдвиг не может быть отрицательным");
        }

        int limbShift = shift / 32;
        int bitShift = shift % 32;
        ReadOnlySpan<uint> source = value.GetDigits();

        if (limbShift >= source.Length)
        {
            return value.IsNegative ? new BetterBigInteger([1], true) : Zero;
        }

        uint[] shifted = new uint[source.Length - limbShift];
        bool droppedBits = false;

        for (int index = 0; index < limbShift; index++)
        {
            if (source[index] != 0)
            {
                droppedBits = true;
            }
        }

        uint remainderMask = (1u << bitShift) - 1;
        if ((source[limbShift] & remainderMask) != 0)
        {
            droppedBits = true;
        }

        for (int index = 0; index < shifted.Length; index++)
        {
            uint highPart = source[index + limbShift] >> bitShift;
            if (bitShift > 0)
            {
                uint lowPart = index + limbShift + 1 < source.Length
                    ? source[index + limbShift + 1] << (32 - bitShift)
                    : 0;
                shifted[index] = highPart | lowPart;
            }
            else
            {
                shifted[index] = highPart;
            }
        }

        if (value.IsNegative && droppedBits)
        {
            shifted = AddMagnitudes([1], shifted);
        }

        return new BetterBigInteger(shifted, value.IsNegative);
    }

    public static bool operator ==(BetterBigInteger left, BetterBigInteger right) => left.Equals(right);
    public static bool operator !=(BetterBigInteger left, BetterBigInteger right) => !left.Equals(right);
    public static bool operator <(BetterBigInteger left, BetterBigInteger right) => left.CompareTo(right) < 0;
    public static bool operator >(BetterBigInteger left, BetterBigInteger right) => left.CompareTo(right) > 0;
    public static bool operator <=(BetterBigInteger left, BetterBigInteger right) => left.CompareTo(right) <= 0;
    public static bool operator >=(BetterBigInteger left, BetterBigInteger right) => left.CompareTo(right) >= 0;

    public override string ToString() => ToString(10);

    public string ToString(int radix)
    {
        if (radix is < 2 or > 36)
        {
            throw new ArgumentOutOfRangeException(nameof(radix));
        }

        if (IsZero)
        {
            return "0";
        }

        StringBuilder builder = new();
        uint[] working = GetDigits().ToArray();
        int lastIndex = working.Length - 1;

        while (lastIndex >= 0)
        {
            ulong remainder = 0;
            for (int index = lastIndex; index >= 0; index--)
            {
                ulong current = (remainder << 32) | working[index];
                working[index] = (uint)(current / (uint)radix);
                remainder = current % (uint)radix;
            }

            builder.Append(remainder < 10
                ? (char)('0' + remainder)
                : (char)('A' + (remainder - 10)));

            while (lastIndex >= 0 && working[lastIndex] == 0)
            {
                lastIndex--;
            }
        }

        if (IsNegative)
        {
            builder.Append('-');
        }

        char[] chars = new char[builder.Length];
        for (int index = 0; index < builder.Length; index++)
        {
            chars[index] = builder[builder.Length - 1 - index];
        }

        return new string(chars);
    }

    public static (BetterBigInteger Quotient, BetterBigInteger Remainder) DivRem(
        BetterBigInteger dividend,
        BetterBigInteger divisor)
    {
        if (divisor.IsZero)
        {
            throw new DivideByZeroException();
        }

        int compare = CompareAbsolute(dividend.GetDigits(), divisor.GetDigits());
        if (compare < 0)
        {
            return (Zero, dividend);
        }

        if (compare == 0)
        {
            bool quotientNegative = dividend.IsNegative ^ divisor.IsNegative;
            return (new BetterBigInteger([1], quotientNegative), Zero);
        }

        bool negativeQuotient = dividend.IsNegative ^ divisor.IsNegative;
        (uint[] quotient, uint[] remainder) = DivideMagnitudes(dividend.GetDigits(), divisor.GetDigits());

        return (
            new BetterBigInteger(quotient, negativeQuotient),
            new BetterBigInteger(remainder, dividend.IsNegative));
    }

    internal static uint[] AddMagnitudes(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
        int maxLength = Math.Max(left.Length, right.Length);
        uint[] result = new uint[maxLength + 1];
        ulong carry = 0;

        for (int index = 0; index < maxLength; index++)
        {
            ulong sum = (index < left.Length ? left[index] : 0ul)
                      + (index < right.Length ? right[index] : 0ul)
                      + carry;
            result[index] = (uint)sum;
            carry = sum >> 32;
        }

        int resultLength = maxLength;
        while (carry > 0)
        {
            if (resultLength >= result.Length)
            {
                Array.Resize(ref result, resultLength + 1);
            }

            result[resultLength++] = (uint)carry;
            carry >>= 32;
        }

        return TrimZeros(result);
    }

    internal static uint[] SubtractMagnitudes(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
        int leftLength = GetSignificantLength(left);
        int rightLength = GetSignificantLength(right);

        if (CompareAbsolute(left, right) < 0)
        {
            throw new InvalidOperationException("Subtrahend exceeds minuend.");
        }

        uint[] result = new uint[leftLength];
        long borrow = 0;

        for (int index = 0; index < leftLength; index++)
        {
            long subtrahend = index < rightLength ? right[index] : 0;
            long difference = left[index] - subtrahend - borrow;

            if (difference < 0)
            {
                difference += 1L << 32;
                borrow = 1;
            }
            else
            {
                borrow = 0;
            }

            result[index] = (uint)difference;
        }

        return TrimZeros(result);
    }

    internal static uint[] TrimZeros(uint[] limbs)
    {
        int length = limbs.Length;
        while (length > 0 && limbs[length - 1] == 0)
        {
            length--;
        }

        if (length == limbs.Length)
        {
            return limbs;
        }

        uint[] trimmed = new uint[length];
        Array.Copy(limbs, trimmed, length);
        return trimmed;
    }

    private void AssignFromDigits(uint[] digits, bool isNegative)
    {
        int length = digits.Length;
        while (length > 0 && digits[length - 1] == 0)
        {
            length--;
        }

        if (length == 0)
        {
            _signBit = 0;
            _smallValue = 0;
            _data = null;
            return;
        }

        _signBit = isNegative ? 1 : 0;

        if (length == 1)
        {
            _smallValue = digits[0];
            _data = null;
            return;
        }

        _smallValue = 0;
        _data = new uint[length];
        Array.Copy(digits, _data, length);
    }

    private static int GetSignificantLength(ReadOnlySpan<uint> magnitude)
    {
        int length = magnitude.Length;
        while (length > 0 && magnitude[length - 1] == 0)
        {
            length--;
        }

        return length;
    }

    private static int CompareAbsolute(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
        int leftLength = GetSignificantLength(left);
        int rightLength = GetSignificantLength(right);

        if (leftLength > rightLength)
        {
            return 1;
        }

        if (leftLength < rightLength)
        {
            return -1;
        }

        for (int index = leftLength - 1; index >= 0; index--)
        {
            if (left[index] > right[index])
            {
                return 1;
            }

            if (left[index] < right[index])
            {
                return -1;
            }
        }

        return 0;
    }

    private static BetterBigInteger ApplyBitwise(
        BetterBigInteger left,
        BetterBigInteger right,
        Func<uint, uint, uint> kernel)
    {
        bool resultNegative = kernel(left.IsNegative ? 1u : 0u, right.IsNegative ? 1u : 0u) == 1;

        ReadOnlySpan<uint> leftDigits = left.GetDigits();
        ReadOnlySpan<uint> rightDigits = right.GetDigits();
        int maxLength = Math.Max(leftDigits.Length, rightDigits.Length);
        uint[] output = new uint[maxLength];

        ulong carryLeft = 1;
        ulong carryRight = 1;

        for (int index = 0; index < maxLength; index++)
        {
            uint leftChunk = index >= leftDigits.Length
                ? (left.IsNegative ? 0xFFFFFFFFu : 0u)
                : leftDigits[index];

            if (left.IsNegative && index < leftDigits.Length)
            {
                ulong updated = (ulong)(~leftChunk) + carryLeft;
                leftChunk = (uint)updated;
                carryLeft = updated >> 32;
            }

            uint rightChunk = index >= rightDigits.Length
                ? (right.IsNegative ? 0xFFFFFFFFu : 0u)
                : rightDigits[index];

            if (right.IsNegative && index < rightDigits.Length)
            {
                ulong updated = (ulong)(~rightChunk) + carryRight;
                rightChunk = (uint)updated;
                carryRight = updated >> 32;
            }

            output[index] = kernel(leftChunk, rightChunk);
        }

        if (resultNegative)
        {
            ulong carry = 1;
            for (int index = 0; index < maxLength; index++)
            {
                ulong updated = (ulong)(~output[index]) + carry;
                output[index] = (uint)updated;
                carry = updated >> 32;
            }
        }

        return new BetterBigInteger(output, resultNegative);
    }

    private static (uint[] Quotient, uint[] Remainder) DivideMagnitudes(ReadOnlySpan<uint> dividend, ReadOnlySpan<uint> divisor)
    {
        uint[] quotient = new uint[dividend.Length - divisor.Length + 1];
        uint[] remainder = new uint[divisor.Length];
        int normalizeShift = BitOperations.LeadingZeroCount(divisor[^1]);
        uint[] normalizedDividend = new uint[dividend.Length + 1];

        if (normalizeShift == 0)
        {
            divisor.CopyTo(remainder);
            dividend.CopyTo(normalizedDividend);
        }
        else
        {
            for (int index = 0; index < divisor.Length; index++)
            {
                remainder[index] = index == 0
                    ? divisor[0] << normalizeShift
                    : (divisor[index] << normalizeShift) | (divisor[index - 1] >> (32 - normalizeShift));
            }

            for (int index = 0; index < dividend.Length; index++)
            {
                normalizedDividend[index] = index == 0
                    ? dividend[0] << normalizeShift
                    : (dividend[index] << normalizeShift) | (dividend[index - 1] >> (32 - normalizeShift));
            }

            normalizedDividend[dividend.Length] = dividend[^1] >> (32 - normalizeShift);
        }

        for (int quotientIndex = quotient.Length - 1; quotientIndex >= 0; quotientIndex--)
        {
            int highIndex = quotientIndex + divisor.Length;
            int lowIndex = highIndex - 1;

            ulong estimatedQuotient = normalizedDividend[quotientIndex + divisor.Length] == remainder[^1]
                ? uint.MaxValue
                : ((ulong)normalizedDividend[highIndex] << 32 | normalizedDividend[lowIndex]) / remainder[^1];

            quotient[quotientIndex] = (uint)estimatedQuotient;
            ulong borrow = 0;

            for (int index = 0; index < divisor.Length; index++)
            {
                ulong product = (ulong)remainder[index] * estimatedQuotient + borrow;
                long difference = (long)normalizedDividend[quotientIndex + index] - (uint)product;
                normalizedDividend[quotientIndex + index] = (uint)difference;
                borrow = (product >> 32) + (difference < 0 ? 1ul : 0ul);
            }

            bool overshot = normalizedDividend[quotientIndex + divisor.Length] < borrow;
            normalizedDividend[quotientIndex + divisor.Length] -= (uint)borrow;

            if (overshot)
            {
                quotient[quotientIndex]--;
                ulong carry = 0;
                for (int index = 0; index < divisor.Length; index++)
                {
                    ulong sum = (ulong)normalizedDividend[quotientIndex + index] + remainder[index] + carry;
                    normalizedDividend[quotientIndex + index] = (uint)sum;
                    carry = sum >> 32;
                }

                normalizedDividend[quotientIndex + divisor.Length] += (uint)carry;
            }
        }

        if (normalizeShift == 0)
        {
            normalizedDividend[..divisor.Length].CopyTo(remainder);
        }
        else
        {
            for (int index = 0; index < divisor.Length; index++)
            {
                remainder[index] = index == divisor.Length - 1
                    ? normalizedDividend[index] >> normalizeShift
                    : (normalizedDividend[index] >> normalizeShift) | (normalizedDividend[index + 1] << (32 - normalizeShift));
            }
        }

        return (TrimZeros(quotient), TrimZeros(remainder));
    }

    private static int ParseDigit(char symbol, int radix)
    {
        int value = symbol switch
        {
            >= '0' and <= '9' => symbol - '0',
            >= 'A' and <= 'Z' => symbol - 'A' + 10,
            >= 'a' and <= 'z' => symbol - 'a' + 10,
            _ => -1
        };

        if (value < 0 || value >= radix)
        {
            throw new FormatException($"Некорректный символ: '{symbol}'");
        }

        return value;
    }
}
