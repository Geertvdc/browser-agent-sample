using Microsoft.SemanticKernel;

class PlaywrightPromptFilter : IPromptRenderFilter
{
    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        Console.WriteLine($"Rendering prompt for {context.Function.Name}");
        
        await next(context);
        
        Console.WriteLine($"Rendered prompt: {context.RenderedPrompt}");
    }
}