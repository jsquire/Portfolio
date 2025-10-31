namespace Squire.NumTic.Players;

/// <summary>
///   The set of options for configuring an <see cref="OpenAIPlayer" />.
/// </summary>
///
public class OpenAIPlayerOptions
{
    /// <summary>The default set of options./summary>
    internal static readonly OpenAIPlayerOptions Default = new();

    /// <summary>The name of the model to use with the OpenAI API.</summary>
    private string _modelName = "gpt-4.1";

    /// <summary>The maximum number retries of retries to attempt when the model returns a malformed response or invalid move.</summary>
    private int _maxMoveRetries = 5;

    /// <summary>
    ///   The difficulty level of the bot player.
    /// </summary>
    ///
    public Difficulty Difficulty { get; set; } = Difficulty.Perfect;

    /// <summary>
    ///   The name of the model to use with the OpenAI API.
    /// </summary>
    ///
    /// <exception cref="ArgumentNullException">Occurs when the <see cref="ModelName" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Occurs when the <see cref="ModelName" /> is empty.</exception>"
    ///
    public string ModelName
    {
        get => _modelName;

        set
        {
            ArgumentNullException.ThrowIfNull(value, nameof(ModelName));
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(ModelName));

            _modelName = value;
        }
    }

    /// <summary>
    ///   The maximum number retries of retries to attempt when the model returns a malformed response or invalid move.
    /// </summary>
    ///
    /// <exception cref="ArgumentOutOfRangeException">Occurs when the <see cref="MaxMoveRetries" /> is negative.</exception>
    ///
    public int MaxMoveRetries
    {
        get => _maxMoveRetries;

        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(MaxMoveRetries), "MaxMoveRetries must be non-negative.");
            }

            _maxMoveRetries = value;
        }
    }

    /// <summary>
    ///   Clones this instance.
    /// </summary>
    ///
    /// <returns>A new options instance with the same member values.</returns>
    ///
    internal OpenAIPlayerOptions Clone() =>
        new()
        {
            Difficulty = this.Difficulty,
            ModelName = this.ModelName
        };
}