# MCP KnowledgeBase Server

![Build Status](https://github.com/mbcrawfo/KnowledgeBaseServer/actions/workflows/ci.yml/badge.svg?branch=main)

A [Model Context Protocol](https://modelcontextprotocol.io/) (MCP) server that allows LLMS to store memories during a conversation and search them later.  Memories are stored in a [SQLite](https://www.sqlite.org) database, and its full text search features power the memory search.

## Usage

**IMPORTANT** - Due to a bug in the .Net MCP library, the server will hang on shutdown.  You will need to manually kill the docker container after closing Claude Desktop, and may have dangling dotnet processes if running locally.

Two environment variables control the location and filename of the database.

`DATABASE_NAME`: Use the default location, but a custom db filename.  For example, `my_custom_db.sqlite`.

`DATABASE_PATH`: Specify the fully path to the db file (ignores `DATABASE_NAME`).  For example, `/Users/your_name/.db/my_db.sqlite`.

### Docker

When using docker you must create a persistent volume to store the database (advanced users can mount a folder from their file system).  Run `docker volume create knowledgebase` to set it up, then configure the server in your `claude_desktop_config.json` as below.  Note that the `DATABASE_PATH` variable will be the path within the container, which defaults to `/db/knowledgebase.sqlite`.

```json
{
  "mcpServers": {
    "knowledgebase": {
      "command": "docker",
      "args": [
        "run",
        "--interactive",
        "--rm",
        "--volume", "knowledgebase:/db",
        "knowledge-base-server"
      ]
    }
  }
}

```

### Run locally with dotnet cli

1. You will need the [.Net 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0).
2. Clone this repository.
3. Navigate to the repo in your terminal and run `dotnet build`.
4. Add the config below to `claude_desktop_config.json` (if you get errors, try specifying the full path to the dotnet cli).

When running locally the default database location is in your Application Data directory.

```json
{
  "mcpServers": {
    "knowledgebase": {
      "command": "dotnet",
      "args": [
        "run",
        "--project", "/full/path/to/repo/src/KnowledgeBaseServer/KnowledgeBaseServer.csproj",
        "--no-restore",
        "--no-build"
      ]
    }
  }
}

```

## Development

You can use the app with the `--init-db` parameter to create or upgrade databases for testing.  From the KnowledgeBaseServer project directory, run `dotnet run -- --init-db /path/to/db.sqlite`.
