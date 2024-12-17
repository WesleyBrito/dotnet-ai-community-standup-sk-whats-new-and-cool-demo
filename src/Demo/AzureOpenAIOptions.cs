using System.ComponentModel.DataAnnotations;

namespace Demo;

internal class AzureOpenAIOptions
{
    /// <summary>
    /// Gets the key credential used to authenticate to an LLM resource.
    /// </summary>
    [Required]
    public required string ApiKey { get; init; }

    [Required]
    public required string ApiVersion { get; init; }

    [Required]
    public required string Endpoint { get; init; }

    /// <summary>
    /// Gets the model deployment name on the LLM (for example OpenAI) to use for chat.
    /// </summary>
    [Required]
    public required string ChatModelDeploymentName { get; init; }

    /// <summary>
    /// Gets the name (sort of a unique identifier) of the model to use for chat.
    /// </summary>
    [Required]
    public required string ChatModelName { get; init; }

    [Required]
    public required string ImageModelDeploymentName { get; init; }

    public required string ImageModelName { get; init; }
}
