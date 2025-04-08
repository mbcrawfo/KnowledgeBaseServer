using System;
using System.Collections.Generic;
using System.Diagnostics;
using Bogus;
using KnowledgeBaseServer;
using KnowledgeBaseServer.Dtos;
using KnowledgeBaseServer.Tools;
using Microsoft.Extensions.Logging;

if (args is not [var path])
{
    Console.WriteLine("Usage: dotnet run <path>");
    return 1;
}

var connectionString = ConnectionString.Create(path);
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

var dbSetupSuccess =
    Migrator.InitializeDatabase(loggerFactory, path) && Migrator.ApplyMigrations(loggerFactory, connectionString);
if (!dbSetupSuccess)
{
    return 1;
}

var faker = new Faker();

var topics = faker.Make(10, () => faker.Lorem.Sentence());
foreach (var topic in topics)
{
    Guid? lastMemoryNodeId = null;
    foreach (var memory in faker.Make(10, () => faker.Lorem.Paragraph()))
    {
        var newMemories = AppJsonSerializer.Deserialize<CreatedMemoryDto[]>(
            CreateMemoryTool.Handle(
                connectionString,
                topic,
                memory,
                faker.Lorem.Paragraph().OrNull(faker),
                lastMemoryNodeId
            )
        );

        lastMemoryNodeId = newMemories[0].Id;
    }
}

return 0;
