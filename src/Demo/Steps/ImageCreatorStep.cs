#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0080

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextToImage;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Demo.Steps;

internal sealed class ImageCreatorStep : KernelProcessStep
{
    public static class Functions
    {
        public const string CreateImageForCopy = nameof(CreateImageForCopy);
    }

    [KernelFunction(Functions.CreateImageForCopy)]
    public static async Task CreateImageForCopyAsync(KernelProcessStepContext context, Kernel kernel, string copy)
    {
        var imageTask = await kernel.GetRequiredService<ITextToImageService>().GenerateImageAsync(copy, 1024, 1024);

        var result = $"Copy: {copy}\n\nImage URL: {imageTask}";

        await context.EmitEventAsync(new() { Id = Events.ImageCreated, Data = result });
    }
}

#pragma warning restore SKEXP0080
#pragma warning restore SKEXP0001
