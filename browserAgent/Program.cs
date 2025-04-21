using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
#pragma warning disable SKEXP0110, SKEXP0001

await using IMcpClient mcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new()
{
    Name = "playwright",
    Command = "npx",
    Arguments = ["-y", "@playwright/mcp@latest"],
    //Arguments = ["-y", "@executeautomation/playwright-mcp-server"],
}));

AIProjectClient client = AzureAIAgent.CreateAzureAIClient("swedencentral.api.azureml.ms;4562d10c-8487-4fcf-8bdf-5d9d729f5775;ai-demos;geert-demos", new AzureCliCredential());
AgentsClient agentsClient = client.GetAgentsClient();


// Retrieve existing agent based on name
var agentsResponse = await agentsClient.GetAgentsAsync();
var agents = agentsResponse.Value;
var agentDefinition = agents.FirstOrDefault(agent => agent.Name == "browserAgent");

if (agentDefinition is null)
{
    // Create a new agent in Azure AI Agents
    agentDefinition = await agentsClient.CreateAgentAsync(
        "gpt-4o",
        name: "browserAgent",
        description: "Agent Helping with web things",
        instructions: "You are an agent that can help with webbrowsing and web automation. You can use the tools available to you to help with this. if you encounter any cookie banners on website. just accept everything");
}
    
AzureAIAgent agent = new(agentDefinition, agentsClient);

// Retrieve the list of tools available on the playwright server
var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);

foreach (var tool in tools)
{
    Console.WriteLine($"{tool.Name}: {tool.Description}");
}

// Add the function and prompt filters to observe execution
agent.Kernel.FunctionInvocationFilters.Add(new PlaywrightFunctionFilter());
agent.Kernel.PromptRenderFilters.Add(new PlaywrightPromptFilter());

// Add the tools to the agent's kernel
agent.Kernel.Plugins.AddFromFunctions("playwright",tools.Select(aiFunction => aiFunction.AsKernelFunction()));

Microsoft.SemanticKernel.Agents.AgentThread agentThread = new AzureAIAgentThread(agent.Client);


ChatMessageContent message = new(AuthorRole.User, "could you check if they sell any books on mcp at amazon in the netherlands and list the titles and prices of these books");
Console.WriteLine("Starting streaming response from agent...");
await foreach (StreamingChatMessageContent response in agent.InvokeStreamingAsync(message, agentThread))
{
    Console.Write(response.Content);
}
Console.WriteLine("\nCompleted agent response.");
