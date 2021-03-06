﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// https://www.spoj.com/problems/ETF/ #factors #formula #math #primes #sieve
// Calculates the value of the totient function (count of relative primes).
public static class ETF
{
    private const int _limit = 1000000;
    private static readonly SieveOfEratosthenesFactorizer _factorizer;

    static ETF()
    {
        _factorizer = new SieveOfEratosthenesFactorizer(_limit);
    }

    // We know a sieve is pretty fast, especially only up to a million. And it can be modified easily
    // to allow retrieving the prime factors of a given number. So combine that with Euler's product
    // formula (see https://en.wikipedia.org/wiki/Euler%27s_totient_function#Euler.27s_product_formula).
    // That's a lot better than my first idea for using a sieve, which was getting the prime factorization
    // of every number below n and comparing it to n's prime factorization.
    public static int Solve(int n)
    {
        int[] distinctPrimeFactors = _factorizer.GetDistinctPrimeFactors(n).ToArray();

        double totient = n;
        foreach (int primeFactor in distinctPrimeFactors)
        {
            totient *= (1 - 1 / (double)primeFactor);
        }

        return (int)Math.Round(totient);
    }
}

public sealed class SieveOfEratosthenesFactorizer
{
    // This sieve is slightly different, rather than storing false for prime (unsieved) and true
    // for not prime (sieved), it stores null for prime and some prime factor (doesn't matter which)
    // that divides the number for not prime. And has entries for evens. Knowing some prime factor
    // that divides n, we can enumerate all its prime factors by dividing it by that factor, the
    // quotient by its factor, etc.
    private readonly IReadOnlyList<int?> _sieveWithSomePrimeFactor;

    public SieveOfEratosthenesFactorizer(int limit)
    {
        Limit = limit;

        int?[] sieveWithSomePrimeFactor = new int?[Limit + 1];
        sieveWithSomePrimeFactor[0] = 0;
        sieveWithSomePrimeFactor[1] = 1;

        // Check for n up to sqrt(Limit), as any non-primes <= Limit with a factor > sqrt(Limit)
        // must also have a factor < sqrt(Limit) (otherwise they'd be > Limit), and so already sieved.
        for (int n = 2; n * n <= Limit; ++n)
        {
            // If we haven't sieved it yet then it's a prime, so sieve its multiples.
            if (!sieveWithSomePrimeFactor[n].HasValue)
            {
                // Multiples of n less than n * n were already sieved from lower primes.
                for (int nextPotentiallyUnsievedMultiple = n * n;
                    nextPotentiallyUnsievedMultiple <= Limit;
                    nextPotentiallyUnsievedMultiple += n)
                {
                    sieveWithSomePrimeFactor[nextPotentiallyUnsievedMultiple] = n;
                }
            }
        }
        _sieveWithSomePrimeFactor = Array.AsReadOnly(sieveWithSomePrimeFactor);
    }

    public int Limit { get; }

    public bool IsPrime(int n)
        => !_sieveWithSomePrimeFactor[n].HasValue;

    public IEnumerable<int> GetDistinctPrimeFactors(int n)
    {
        while (n > 1)
        {
            int somePrimeFactor = _sieveWithSomePrimeFactor[n] ?? n;
            yield return somePrimeFactor;

            while (n % somePrimeFactor == 0)
            {
                n /= somePrimeFactor;
            }
        }
    }
}

public static class Program
{
    private static void Main()
    {
        var output = new StringBuilder();
        int remainingTestCases = int.Parse(Console.ReadLine());
        while (remainingTestCases-- > 0)
        {
            int n = int.Parse(Console.ReadLine());
            output.Append(
                ETF.Solve(n));
            output.AppendLine();
        }

        Console.Write(output);
    }
}
