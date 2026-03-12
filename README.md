![plot](assets/icon.png)

# EngramMcp

A Model Context Protocol (MCP) server for persistent agent memory.

## Get It as a .NET Tool


[![NuGet](https://img.shields.io/nuget/v/EngramMcp.svg)](https://www.nuget.org/packages/EngramMcp/)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://www.nuget.org/packages/EngramMcp/)

#### Installation

```bash
dotnet tool install -g EngramMcp
```

#### MCP config (OpenCode)

```json
{
  "mcp": {
    "memory": {
      "type": "local",
      "command": [
        "engrammcp",
        "--file",
        "/Users/your_name/.config/.engram/memory.json"
      ]
    }
  }
}
```

Use an absolute file path so the memory location stays stable across launches.

## What It Is

EngramMcp is a small .NET MCP server that gives AI agents a persistent memory layer backed by a local JSON file. It exposes a narrow set of memory tools so an agent can reload prior context at session start and store new information in the right retention bucket while it works.

## Why It Exists

Most agent sessions are stateless by default. EngramMcp solves that by providing:

- **Persistent recall** - carry important context across sessions
- **Scoped retention** - separate stable facts from changing context and recent work state
- **Readable storage** - keep memory in a plain JSON file you can inspect yourself
- **Fail-fast validation** - reject broken or malformed memory state on startup
- **Serialized writes** - reduce the risk of overlapping file updates corrupting memory

## What You Can Use It For

| Tool                  | Description                                                      |
| --------------------- | ---------------------------------------------------------------- |
| **Recall**            | Load all stored memory at the start of a session                 |
| **Store Long-Term**   | Save durable facts and preferences worth keeping indefinitely    |
| **Store Medium-Term** | Save useful context that may change over time                    |
| **Store Short-Term**  | Save the recent working state for fast next-session continuation |

## Memory Model

EngramMcp currently uses three memory sections with code-defined capacities:

- `long-term` - 40 entries
- `medium-term` - 20 entries
- `short-term` - 10 entries

When a section exceeds its capacity, the oldest entries are discarded.

Each stored entry contains:

- `timestamp` - local write timestamp
- `text` - the stored memory text

Example file shape:

```json
{
  "long-term": [
    {
      "timestamp": "2026-03-10T10:15:30.0000000+01:00",
      "text": "Agent K is my self-identity: not a chatbot, but a gentle coding-buddy"
    },
    {
      "timestamp": "2026-03-11T10:15:30.0000000+01:00",
      "text": "The human prefers to communicate in Spanish."
    }
  ],
  "medium-term": [
    {
      "timestamp": "2026-03-12T10:11:30.0000000+01:00",
      "text": "Always use 'AssertWithIs' package for unit tests."
    }
  ],
  "short-term": [
    {
      "timestamp": "2026-03-12T10:15:30.0000000+01:00",
      "text": "Add the new feature X."
    },
    {
      "timestamp": "2026-03-12T10:16:30.0000000+01:00",
      "text": "Fixed the bug Y."
    }
  ]
}
```