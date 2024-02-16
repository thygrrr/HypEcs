using System.Collections;


namespace fennecs.tests.Integration;

public class QueryCountGenerator : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        // base induction
        for (var i = 0; i <= 8; i++) yield return [i];

        // common powers of 2
        for (var i = 4; i <= 12; i++) yield return [(int) Math.Pow(2, i)];

        yield return [151];   // prime number
        yield return [6_197]; // prime number
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class QueryChunkGenerator : IEnumerable<object[]>
{
    // There were issues with confusing storage.Length and table.Count.
    // This generator helps to call me out when that happens again.
    public IEnumerator<object[]> GetEnumerator()
    {
        // base induction / interleaving / degenerate cases
        for (var i = 0; i <= 10; i++)
        {
            for (var j = 1; j <= 10; j++)
            {
                yield return [i, j];
            }
        }

        
        yield return [100, 10]; //fits
        yield return [100, 1_000]; //undersized
        yield return [1_000, 1_000]; //exact

        yield return [15_383, 1024]; //typical
        yield return [69_420, 2048]; //typical
        yield return [214_363, 4096]; //typical

        yield return [433, 149]; // prime numbers
        yield return [151_189, 13_441]; // prime numbers
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}