using System.Diagnostics;
using System.Numerics;
using System.Text;
using Arithmetic.BigInt;
using Arithmetic.BigInt.MultiplyStrategy;

try
{
    RunMultiplicationBenchmarks();
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Критическая ошибка: {exception.Message}");
}

static void RunMultiplicationBenchmarks()
{
    Console.WriteLine("=== Сравнение стратегий умножения BetterBigInteger ===");
    Console.WriteLine($"Пороги автовыбора: Karatsuba >= {BetterBigInteger.KARATSUBA_THRESHOLD} limb, FFT >= {BetterBigInteger.FFT_THRESHOLD} limb");
    Console.WriteLine();

    BenchmarkCase[] cases =
    [
        new("Малые числа (столбик)", 30, 200, IncludeFft: false),
        new("Средние числа (Карацуба)", 600, 50, IncludeFft: false),
        new("Большие числа (FFT)", 10_000, 5, IncludeFft: true),
    ];

    SimpleMultiplier simple = new();
    KaratsubaMultiplier karatsuba = new();
    FftMultiplier fft = new();

    foreach (BenchmarkCase benchmarkCase in cases)
    {
        Console.WriteLine($"--- {benchmarkCase.Title} (~{benchmarkCase.DecimalDigits} десятичных цифр) ---");

        BetterBigInteger left = GenerateRandomNumber(benchmarkCase.DecimalDigits);
        BetterBigInteger right = GenerateRandomNumber(benchmarkCase.DecimalDigits);

        int limbCount = Math.Max(left.GetDigits().Length, right.GetDigits().Length);
        Console.WriteLine($"Разрядов (limb): {left.GetDigits().Length} x {right.GetDigits().Length} (max {limbCount})");

        string expected = BuildReferenceProduct(left, right);

        Measure("SimpleMultiplier", benchmarkCase.Iterations, () =>
        {
            EnsureEqual(expected, simple.Multiply(left, right).ToString(), "SimpleMultiplier");
        });

        Measure("KaratsubaMultiplier", benchmarkCase.Iterations, () =>
        {
            EnsureEqual(expected, karatsuba.Multiply(left, right).ToString(), "KaratsubaMultiplier");
        });

        if (benchmarkCase.IncludeFft)
        {
            Measure("FftMultiplier", benchmarkCase.Iterations, () =>
            {
                EnsureEqual(expected, fft.Multiply(left, right).ToString(), "FftMultiplier");
            });
        }

        EnsureEqual(expected, (left * right).ToString(), "operator * (автовыбор)");
        Console.WriteLine("operator * (автовыбор): результат совпадает с эталоном");
        Console.WriteLine();
    }

    Console.WriteLine("Демонстрация завершена.");
}

static string BuildReferenceProduct(BetterBigInteger left, BetterBigInteger right)
{
    BigInteger referenceLeft = ParseBetterBigInteger(left);
    BigInteger referenceRight = ParseBetterBigInteger(right);
    return (referenceLeft * referenceRight).ToString();
}

static BigInteger ParseBetterBigInteger(BetterBigInteger value)
{
    return BigInteger.Parse(value.ToString(10));
}

static void Measure(string name, int iterations, Action action)
{
    if (iterations <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(iterations), "Число итераций должно быть положительным.");
    }

    action();

    Stopwatch stopwatch = Stopwatch.StartNew();
    for (int index = 0; index < iterations; index++)
    {
        action();
    }

    stopwatch.Stop();

    double averageMs = stopwatch.Elapsed.TotalMilliseconds / iterations;
    Console.WriteLine($"{name,-22} {iterations,4} итер. | всего {stopwatch.Elapsed.TotalMilliseconds,8:F2} мс | среднее {averageMs,8:F4} мс");
}

static BetterBigInteger GenerateRandomNumber(int decimalDigits)
{
    if (decimalDigits <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(decimalDigits), "Длина числа должна быть положительной.");
    }

    Random random = Random.Shared;
    StringBuilder builder = new(decimalDigits);
    builder.Append((char)('1' + random.Next(9)));

    for (int index = 1; index < decimalDigits; index++)
    {
        builder.Append((char)('0' + random.Next(10)));
    }

    return new BetterBigInteger(builder.ToString(), 10);
}

static void EnsureEqual(string expected, string actual, string context)
{
    if (!string.Equals(expected, actual, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"Несовпадение результата для {context}.");
    }
}

file readonly record struct BenchmarkCase(string Title, int DecimalDigits, int Iterations, bool IncludeFft);
