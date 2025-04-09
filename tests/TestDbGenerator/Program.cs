using System;
using System.Reflection;
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

var logger = loggerFactory.CreateLogger(Assembly.GetExecutingAssembly().GetName().Name!);
var faker = new Faker();

var topics = faker.Make(count: 10, () => faker.Lorem.Sentence());
foreach (var topic in topics)
{
    Guid? lastMemoryNodeId = null;
    foreach (var memory in faker.Make(count: 10, () => faker.Lorem.Paragraph()))
    {
        var result = CreateMemoryTool.Handle(
            connectionString,
            topic,
            memory,
            faker.Random.Number(min: 0, max: 1),
            faker.Lorem.Paragraph().OrNull(faker),
            lastMemoryNodeId
        );

        if (!result.StartsWith('{'))
        {
            logger.LogError("Failed to create memory: {Result}", result);
            return 1;
        }

        lastMemoryNodeId = AppJsonSerializer.Deserialize<CreatedMemoryDto>(result).Id;
    }
}

return 0;
