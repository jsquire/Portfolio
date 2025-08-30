using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Squire.NumTic.Benchmark;

/// <summary>
///   Focused benchmark comparing string.Format vs alternative approaches for board template patterns.
/// </summary>
///
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[CategoriesColumn]
public class BoardBuildingBenchmarks
{
    /// <summary>The board template with placeholders for string.Format.</summary>
    private const string BoardTemplate = """
        {0}[]│[/]{1}[]│[/]{2}
             []│[/]     []│[/]
        {3}[]│[/]{4}[]│[/]{5}
        ─────[]┼[/]─────[]┼[/]─────
        {6}[]│[/]{7}[]│[/]{8}
             []│[/]     []│[/]
        """;

    /// <summary>The token strings used for testing.</summary>
    private readonly string[] _tokens = ["  1  ", "     ", "  3  ", "  4  ", "     ", "  6  ", "     ", "  8  ", "  9  "];

    /// <summary>The token objects array for string.Format.</summary>
    private readonly object[] _tokenObjects;

    /// <summary>
    ///   Initializes a new instance of the <see cref="BoardBuildingBenchmarks"/> class.
    /// </summary>
    ///
    public BoardBuildingBenchmarks()
    {
        _tokenObjects = _tokens.Cast<object>().ToArray();
    }

    /// <summary>
    ///   Benchmarks the standard string.Format approach with object array.
    /// </summary>
    ///
    /// <returns>The formatted board string.</returns>
    ///
    [Benchmark(Baseline = true)]
    public string StringFormat() =>
       string.Format(BoardTemplate, _tokenObjects);

    /// <summary>
    ///   Benchmarks string.Format with individual string parameters.
    /// </summary>
    ///
    /// <returns>The formatted board string.</returns>
    ///
    [Benchmark]
    public string StringFormatWithStrings() =>
        string.Format(BoardTemplate,
            _tokens[0], _tokens[1], _tokens[2],
            _tokens[3], _tokens[4], _tokens[5],
            _tokens[6], _tokens[7], _tokens[8]);

    /// <summary>
    ///   Benchmarks a StringBuilder approach with manual template replacement.
    /// </summary>
    ///
    /// <returns>The formatted board string.</returns>
    ///
    [Benchmark]
    public string StringBuilder()
    {
        var builder = new StringBuilder(BoardTemplate.Length);

        // Manual replacement approach.

        var template = BoardTemplate.AsSpan();

        for (var index = 0; index < template.Length; ++index)
        {
            if (template[index] == '{' && index + 2 < template.Length && template[index + 2] == '}')
            {
                var tokenIndex = template[index + 1] - '0';

                if ((tokenIndex >= 0) && (tokenIndex < _tokens.Length))
                {
                    builder.Append(_tokens[tokenIndex]);
                    index += 2; // Skip the {n} pattern
                }
            }
            else
            {
                builder.Append(template[index]);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    ///   Benchmarks a string.Replace approach for template substitution.
    /// </summary>
    ///
    /// <returns>The formatted board string.</returns>
    ///
    [Benchmark]
    public string StringReplace()
    {
        var result = BoardTemplate;

        for (var index = 0; index < _tokens.Length; ++index)
        {
            result = result.Replace($"{{{index}}}", _tokens[index]);
        }
        return result;
    }

    /// <summary>
    ///   Benchmarks a Span-based approach with stackalloc for template substitution.
    /// </summary>
    ///
    /// <returns>The formatted board string.</returns>
    ///
    [Benchmark]
    public string SpanBasedWithStackAlloc()
    {
        // Pre-calculate size needed, assuming a maximum token length of 5 characters.

        var estimatedSize = BoardTemplate.Length + (_tokens.Length * 5);

        var buffer = estimatedSize <= 1024
            ? stackalloc char[estimatedSize]
            : new char[estimatedSize];

        var template = BoardTemplate.AsSpan();
        var bufferPosition = 0;

        for (var index = 0; index < template.Length; ++index)
        {
            if ((template[index] == '{') && (index + 2 < template.Length) && (template[index + 2] == '}'))
            {
                var templateIndex = template[index + 1] - '0';

                if (templateIndex >= 0 && templateIndex < _tokens.Length)
                {
                    var token = _tokens[templateIndex].AsSpan();

                    token.CopyTo(buffer.Slice(bufferPosition));
                    bufferPosition += token.Length;

                    index += 2; // Skip the {n} pattern
                }
            }
            else
            {
                buffer[bufferPosition++] = template[index];
            }
        }

        return new string(buffer.Slice(0, bufferPosition));
    }

    /// <summary>
    ///   Benchmarks string interpolation for template substitution.
    /// </summary>
    ///
    /// <returns>The formatted board string.</returns>
    ///
    [Benchmark]
    public string InterpolatedString()
    {
        // Using string interpolation (compiles to optimized code in .NET 9).

        return $"""
        {_tokens[0]}[]│[/]{_tokens[1]}[]│[/]{_tokens[2]}
             []│[/]     []│[/]
        {_tokens[3]}[]│[/]{_tokens[4]}[]│[/]{_tokens[5]}
        ─────[]┼[/]─────[]┼[/]─────
        {_tokens[6]}[]│[/]{_tokens[7]}[]│[/]{_tokens[8]}
             []│[/]     []│[/]
        """;
    }
}
