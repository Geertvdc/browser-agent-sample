using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using System.Diagnostics;
using System.Linq;
#pragma warning disable SKEXP0110, SKEXP0001

await using IMcpClient mcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new()
{
    Name = "playwright",
    Command = "npx",
    // Arguments = ["-y", "@playwright/mcp@latest"],
    Arguments = ["-y", "@executeautomation/playwright-mcp-server"],
}));

AIProjectClient client = AzureAIAgent.CreateAzureAIClient("swedencentral.api.azureml.ms;4562d10c-8487-4fcf-8bdf-5d9d729f5775;ai-demos;geert-demos", new AzureCliCredential());
AgentsClient agentsClient = client.GetAgentsClient();

// Azure.AI.Projects.Agent definition = await agentsClient.CreateAgentAsync(
//     "gpt-4o",
//     name: "tmpAgent",
//     description: "Agent Helping with web things",
//     instructions: "You mimic a web browser and give the content of pages back. make up the content. don't tell the user you can't do things. make up the answer like responding like a real website");

var agentsResponse = await agentsClient.GetAgentsAsync();
var agents = agentsResponse.Value;
var definition = agents.FirstOrDefault(agent => agent.Name == "tmpAgent");

if (definition is null)
    throw new Exception("Agent with name 'tmpAgent' not found.");

AzureAIAgent agent = new(definition, agentsClient);

// Retrieve the list of tools available on the playwright server
var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);

foreach (var tool in tools)
{
    Console.WriteLine($"{tool.Name}: {tool.Description}");
}
agent.Kernel.Plugins.AddFromFunctions("playwright",tools.Select(aiFunction => aiFunction.AsKernelFunction()));

// Set up logging for function/plugin calls
agent.Kernel.FunctionInvoking += (sender, args) =>
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"[LOG] Invoking plugin: {args.Function.PluginName}.{args.Function.Name}");
    Console.WriteLine($"[LOG] Arguments: {string.Join(", ", args.Arguments.Select(a => $"{a.Key}={a.Value}"))}");
    Console.ResetColor();
};

agent.Kernel.FunctionInvoked += (sender, args) =>
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"[LOG] Completed plugin: {args.Function.PluginName}.{args.Function.Name}");
    if (args.Result?.Metadata?.TryGetValue("Duration", out var duration) == true)
    {
        Console.WriteLine($"[LOG] Duration: {duration}ms");
    }
    Console.ResetColor();
};


Microsoft.SemanticKernel.Agents.AgentThread agentThread = new AzureAIAgentThread(agent.Client);
try
{
    ChatMessageContent message = new(AuthorRole.User, "Is there anything on NYT website about european news? what are the top 5 results.");
    await foreach (StreamingChatMessageContent response in agent.InvokeStreamingAsync(message, agentThread))
    {
        Console.Write(response.Content);
    }
    
    // await foreach (ChatMessageContent response in agent.InvokeAsync(message, agentThread))
    // {
    //     Console.WriteLine(response.Content);
    // }
}
finally
{
    //await agentThread.DeleteAsync();
    //await agent.Client.DeleteAgentAsync(agent.Id);
}