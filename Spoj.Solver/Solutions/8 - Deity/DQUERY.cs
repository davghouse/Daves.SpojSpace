﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// https://www.spoj.com/problems/DQUERY/ #bit #offline #sorting
// Finds the number of distinct elements in subranges of an array.
public static class DQUERY
{
    public static int[] Solve(int[] sourceArray, DistinctCountQuery[] queries)
    {
        int[] queryResults = new int[queries.Length];

        // Queries are performed in phases, a phase for each of the sourceArray.Length possible
        // query end indices. The query start index doesn't matter, just the fact that all queries
        // in a phase share an end index. The phases will proceed in ascending order of query end
        // indices, which is why the query objects are sorted that way below. A PURQ BIT is queried
        // within phases and updated between them. For any given phase, the PURQ BIT is always in a
        // state such that it can only answer distinct count queries which have an end index equal
        // to the phase's end index. The BIT's underlying array has 0s and 1s, where a 1 at an index
        // means the value there is the latest occurrence of the value up to the phase's end index.
        // The BIT returns sums like normal, but with this construction the sums correspond to the
        // distinct count of values within the queried range. That's because for a given phase, all
        // queries extend up to the phase's end index. So for any value known to be within the queried
        // range, the latest occurrence of the value up to the phase's end index is definitely within
        // the range, and its underlying BIT value accounts for a single 1 added to the returned sum.
        // After a phase is complete, we increment the query end index for the next phase, update the
        // BIT so the value there has a 1 (it's last, so definitely the latest for its value), and
        // turn off any earlier value marked with a 1, since it's no longer the latest.

        // Sort queries by ascending query end index.
        Array.Sort(queries, (q1, q2) => q1.QueryEndIndex.CompareTo(q2.QueryEndIndex));

        var latestOccurrenceBIT = new PURQBinaryIndexedTree(sourceArray.Length);
        var valuesLatestOccurrenceIndices = new Dictionary<int, int>(sourceArray.Length);
        int queryIndex = 0;

        for (int phaseEndIndex = 0;
            phaseEndIndex < sourceArray.Length && queryIndex < queries.Length;
            ++phaseEndIndex)
        {
            int endValue = sourceArray[phaseEndIndex];
            int endValuesPreviousLatestOccurrenceIndex;
            if (valuesLatestOccurrenceIndices.TryGetValue(
                endValue, out endValuesPreviousLatestOccurrenceIndex))
            {
                latestOccurrenceBIT.PointUpdate(endValuesPreviousLatestOccurrenceIndex, -1);
            }
            latestOccurrenceBIT.PointUpdate(phaseEndIndex, 1);
            valuesLatestOccurrenceIndices[endValue] = phaseEndIndex;

            DistinctCountQuery query;
            while (queryIndex < queries.Length
                && (query = queries[queryIndex]).QueryEndIndex == phaseEndIndex)
            {
                queryResults[query.ResultIndex] = latestOccurrenceBIT.SumQuery(
                    query.QueryStartIndex, phaseEndIndex);
                ++queryIndex;
            }
        }

        return queryResults;
    }
}

public struct DistinctCountQuery
{
    public DistinctCountQuery(int queryStartIndex, int queryEndIndex, int resultIndex)
    {
        QueryStartIndex = queryStartIndex;
        QueryEndIndex = queryEndIndex;
        ResultIndex = resultIndex;
    }

    public int QueryStartIndex { get; }
    public int QueryEndIndex { get; }
    public int ResultIndex { get; }
}

// Point update, range query binary indexed tree. This is the original BIT described
// by Fenwick. There are lots of tutorials online but I'd start with these two videos:
// https://www.youtube.com/watch?v=v_wj_mOAlig, https://www.youtube.com/watch?v=CWDQJGaN1gY.
// Those make the querying part clear but don't really describe the update part very well.
// For that, I'd go and read Fenwick's paper. This is all a lot less intuitive than segment trees.
public sealed class PURQBinaryIndexedTree
{
    private readonly int[] _tree;

    public PURQBinaryIndexedTree(int arrayLength)
    {
        _tree = new int[arrayLength + 1];
    }

    // Updates to reflect an addition at an index of the original array (by traversing the update tree).
    public void PointUpdate(int updateIndex, int delta)
    {
        for (++updateIndex;
            updateIndex < _tree.Length;
            updateIndex += updateIndex & -updateIndex)
        {
            _tree[updateIndex] += delta;
        }
    }

    // Computes the sum from the zeroth index through the query index (by traversing the interrogation tree).
    private int SumQuery(int queryEndIndex)
    {
        int sum = 0;
        for (++queryEndIndex;
            queryEndIndex > 0;
            queryEndIndex -= queryEndIndex & -queryEndIndex)
        {
            sum += _tree[queryEndIndex];
        }

        return sum;
    }

    // Computes the sum from the start through the end query index, by removing the part we
    // shouldn't have counted. Fenwick describes a more efficient way to do this, but it's complicated.
    public int SumQuery(int queryStartIndex, int queryEndIndex)
        => SumQuery(queryEndIndex) - SumQuery(queryStartIndex - 1);
}

public static class Program
{
    private static void Main()
    {
        int sourceArrayLength = FastIO.ReadNonNegativeInt();
        int[] sourceArray = new int[sourceArrayLength];
        for (int i = 0; i < sourceArrayLength; ++i)
        {
            sourceArray[i] = FastIO.ReadNonNegativeInt();
        }

        int queryCount = FastIO.ReadNonNegativeInt();
        var queries = new DistinctCountQuery[queryCount];

        for (int q = 0; q < queryCount; ++q)
        {
            queries[q] = new DistinctCountQuery(
                queryStartIndex: FastIO.ReadNonNegativeInt() - 1,
                queryEndIndex: FastIO.ReadNonNegativeInt() - 1,
                resultIndex: q);
        }

        int[] queryResults = DQUERY.Solve(sourceArray, queries);
        foreach (int queryResult in queryResults)
        {
            FastIO.WriteNonNegativeInt(queryResult);
            FastIO.WriteLine();
        }

        FastIO.Flush();
    }
}

// This is based in part on submissions from https://www.codechef.com/status/INTEST.
// It's assumed the input is well-formed, so if you try to read an integer when no
// integers remain in the input, there's undefined behavior (infinite loop).
public static class FastIO
{
    private const byte _null = (byte)'\0';
    private const byte _newLine = (byte)'\n';
    private const byte _minusSign = (byte)'-';
    private const byte _zero = (byte)'0';
    private const int _inputBufferLimit = 8192;
    private const int _outputBufferLimit = 8192;

    private static readonly Stream _inputStream = Console.OpenStandardInput();
    private static readonly byte[] _inputBuffer = new byte[_inputBufferLimit];
    private static int _inputBufferSize = 0;
    private static int _inputBufferIndex = 0;

    private static readonly Stream _outputStream = Console.OpenStandardOutput();
    private static readonly byte[] _outputBuffer = new byte[_outputBufferLimit];
    private static readonly byte[] _digitsBuffer = new byte[11];
    private static int _outputBufferSize = 0;

    private static byte ReadByte()
    {
        if (_inputBufferIndex == _inputBufferSize)
        {
            _inputBufferIndex = 0;
            _inputBufferSize = _inputStream.Read(_inputBuffer, 0, _inputBufferLimit);
            if (_inputBufferSize == 0)
                return _null; // All input has been read.
        }

        return _inputBuffer[_inputBufferIndex++];
    }

    public static int ReadNonNegativeInt()
    {
        byte digit;

        // Consume and discard whitespace characters (their ASCII codes are all < _minusSign).
        do
        {
            digit = ReadByte();
        }
        while (digit < _minusSign);

        // Build up the integer from its digits, until we run into whitespace or the null byte.
        int result = digit - _zero;
        while (true)
        {
            digit = ReadByte();
            if (digit < _zero) break;
            result = result * 10 + (digit - _zero);
        }

        return result;
    }

    public static void WriteNonNegativeInt(int value)
    {
        int digitCount = 0;
        do
        {
            int digit = value % 10;
            _digitsBuffer[digitCount++] = (byte)(digit + _zero);
            value /= 10;
        } while (value > 0);

        if (_outputBufferSize + digitCount > _outputBufferLimit)
        {
            _outputStream.Write(_outputBuffer, 0, _outputBufferSize);
            _outputBufferSize = 0;
        }

        while (digitCount > 0)
        {
            _outputBuffer[_outputBufferSize++] = _digitsBuffer[--digitCount];
        }
    }

    public static void WriteLine()
    {
        if (_outputBufferSize == _outputBufferLimit) // else _outputBufferSize < _outputBufferLimit.
        {
            _outputStream.Write(_outputBuffer, 0, _outputBufferSize);
            _outputBufferSize = 0;
        }

        _outputBuffer[_outputBufferSize++] = _newLine;
    }

    public static void Flush()
    {
        _outputStream.Write(_outputBuffer, 0, _outputBufferSize);
        _outputBufferSize = 0;
        _outputStream.Flush();
    }
}
