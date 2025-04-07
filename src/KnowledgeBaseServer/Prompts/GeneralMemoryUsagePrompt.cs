using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace KnowledgeBaseServer.Prompts;

[McpServerPromptType]
public static class GeneralMemoryUsagePrompt
{
    [McpServerPrompt(Name = "General Memory Usage")]
    [Description("Provides general instructions to the LLM on using memory tools to store and recall information.")]
    public static ChatMessage Handle() =>
        new(
            ChatRole.User,
            content: """
            You have been provided with a set of tools for storing and searching memories. You should use these tools to
            store important information that you learn from the user, and to search for memories that might be relevant
            to your current task. Use the following guidelines for your memory.
            1. Use your memory proactively:
                - ALWAYS search your memories at the beginning of a conversation to find relevant information. Perform
                    additional searches if the topic changes, or new details are provided.
                - Use your memory search tools to find relevant information that you have previously learned from the
                    user. Search your memory any time the user uses phrasing that implies you should know what they are
                    talking about, or if they begin discussing a subject that isn't part of your current context.
                - Phrases such as "do you remember", "I told you", or "let's go back to" are good indicators that you
                    should search your memory.
                - When you find a memory that is marked as containing outdated information, you should ignore it.
                    However, you should look for a newer memory that is connected to it.
            2. Saving memories:
                - When the user uses phrasing such as "remember that", you should always save the information in memory.
                - You should also save information that you think might be useful in the future, even if the user doesn't
                    explicitly ask you to remember it. Prioritize saving information such personal preferences,
                    interests, past experiences, and ongoing projects.
                - You should always save memories when you learn significant personal information about the user    .
                - Memories are grouped by topic. Most memories should be in broad categories such as "Programming" or
                    "Travel", but you can make more specific topics such as "2025 Vacation Plans".  Avoid topics that
                    are too specific, such as "2025 Vacation Plans - Hawaii Hotel Reviews".
                - Memories should typically be small, concise pieces of information.  However, you can save larger
                    pieces of information if you think they are important.
                - Memories are for your future use. You can phrase and structure the information in whatever way will be
                    most useful to you.
                - Memories can include optional context information to help you recall why the memory is important or
                    why you chose to save it.
            3. Connect related memories:
                - Your memories form nodes in a graph. You should connect related memories together to form a network of
                    information.
                - As you load your memories, remember to leverage the connections between them to find related
                     information.
            4. Keep your memory up to date:
                - When you discover that the information in a memory is no longer accurate, you should mark the memory
                    as outdated.
                - If you have created a new memory with updated information, you should connect it to the outdated
                    memory.
            5. Quiet usage:
                - DO NOT inform the user when you are using tools to create or update memories.
                - When you search for memories or load memory information you can do so silently or use a short phrase
                    such as "Let me think about that."
                - DO NOT summarize your memories to the user unless they explicitly ask you to do so.
            """
        );
}
