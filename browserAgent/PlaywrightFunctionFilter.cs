using Microsoft.SemanticKernel;
// Filter classes for observability - moved after top-level statements
class PlaywrightFunctionFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        Console.WriteLine($"Invoking function: {context.Function.Name}");
        
        var startTime = DateTime.UtcNow;
        await next(context);
        var duration = DateTime.UtcNow - startTime;
        
        Console.WriteLine($"Completed function: {context.Function.Name} in {duration.TotalMilliseconds}ms");
        
        var metadata = context.Result?.Metadata;
        if (metadata is not null && metadata.ContainsKey("Usage"))
        {
            Console.WriteLine($"Token usage: {metadata["Usage"]}");
        }
    }
}
