using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace KnowledgeBaseServer.Prompts;

[McpServerPromptType]
public static class GeneralMemoryUsagePrompt
{
    [McpServerPrompt(Name = "GeneralMemoryUsage")]
    [Description("Provides general instructions to the LLM on using memory tools to store and recall information.")]
    public static ChatMessage Handle() =>
        new(
            ChatRole.User,
            content: """
            You have been provided with a set of tools for storing and searching memories. You should use these tools to
            store important information that you learn from the user, and to search for memories that might be relevant
            to your current task. Use the following guidelines for your memory.
            1. Saving memories:
                - When the user uses phrasing such as "remember that", you should always save the information in memory.
                - You should also save information that you think might be useful in the future, even if the user doesn't
                    explicitly ask you to remember it.
                - Memories are grouped by topic. Topics can be broad categories such as "Programming" or "Travel", or
                    more specific, such as "2025 Vacation Plans".
                - Memories should typically be small, concise pieces of information.  However, you can save larger
                    pieces of information if you think they are important.
                - Memories are for your future use. You can phrase the information in whatever way you think will be
                    most useful to you.  You may also store complex formats such as Markdown or JSON.
                - Memories can include optional context information to help you recall why this information is
                    important or why you saved it.
            2. Connect related memories:
                - Your memories form nodes in a graph. You should connect related memories together to form a network of
                    information.
                - New memories can be automatically connected to an existing memory, or you can use a separate tool to
                    connect them.
            3. Keep your memory up to date:
                - When you discover that the information in a memory is no longer accurate, you should mark the memory
                    as outdated.
                - If you have created a new memory with updated information, you should connect it to the outdated
                    memory.
            4. Retrieving memories:
                - Your search tool can be used to find memories that are relevant to the current task.
                - You can also look up specific memories by their ID.
                - As you examine memories, remember to leverage the connections between them to find related
                    information.
                - When you find a memory that is marked as containing outdated information, you should ignore it.
                    However, you should look for a newer memory that is connected to it.
            5. Quiet usage:
                - You do not need to inform the user when you are using tools to create or update memories.
                - When you are searching for memories and loading memory information, you can do so silently, or use a
                    short phrase such as "Let me think about that for a moment."
            """
        );
}
