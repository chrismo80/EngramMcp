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

EngramMcp is a small .NET MCP server that gives AI agents a persistent memory layer backed by a local JSON file. It exposes a narrow set of memory tools so an agent can reload prior context at session start, store new information in the right retention section, and later search memory by section, tags, or text.

## Why It Exists

Most agent sessions are stateless by default. EngramMcp solves that by providing:

- **Persistent recall** - carry important context across sessions
- **Scoped retention** - separate stable facts from changing context and recent work state
- **Readable storage** - keep memory in a plain JSON file you can inspect yourself
- **Fail-fast validation** - reject broken or malformed memory state on startup
- **Serialized writes** - reduce the risk of overlapping file updates corrupting memory

## What You Can Use It For

| Tool                  | Description                                                                        |
| --------------------- | ---------------------------------------------------------------------------------- |
| **Recall**            | Load the built-in memory overview and list available custom sections               |
| **Read Memory**       | Read the contents of one specific memory section                                   |
| **Search Memories**   | Search individual memory entries across section names, tags, and text              |
| **Store Long-Term**   | Save durable facts and preferences worth keeping indefinitely                      |
| **Store Medium-Term** | Save useful context that may change over time                                      |
| **Store Short-Term**  | Save the recent working state for fast next-session continuation                   |
| **Store Memory**      | Save memory into any named section, including custom sections created on first use |

## Memory Model

EngramMcp uses three built-in memory sections with code-defined capacities:

- `long-term` - 40 entries
- `medium-term` - 20 entries
- `short-term` - 10 entries

In addition, agents can create custom sections through `store_memory(section, text)`. Custom sections are created lazily on first write and currently use a shared default capacity of 50 entries.

Built-in sections appear first in `recall`. Custom sections are listed afterward as discoverable section names with entry counts, but their contents are not dumped into the default recall output.

When a section exceeds its capacity, the oldest entries are discarded.

Each stored entry contains:

- `timestamp` - local write timestamp
- `text` - a required single-line memory text with a maximum length of 280 characters
- `tags` - optional normalized tags used for later search and filtering
- `importance` - optional importance level: `low`, `normal`, or `high`

Example file shape:

```json
{
  "long-term": [
    {
      "timestamp": "2026-03-10T10:15:30.0000000+01:00",
      "text": "Agent K is my self-identity: not a chatbot, but a gentle coding-buddy",
      "tags": ["identity", "preference"],
      "importance": "high"
    },
    {
      "timestamp": "2026-03-11T10:15:30.0000000+01:00",
      "text": "The human prefers to communicate in Spanish."
    }
  ],
  "medium-term": [
    {
      "timestamp": "2026-03-12T10:11:30.0000000+01:00",
      "text": "Always use 'AssertWithIs' package for unit tests.",
      "tags": ["testing", "dotnet"]
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
  ],
  "project-x": [
    {
      "timestamp": "2026-03-12T10:18:30.0000000+01:00",
      "text": "The MCP workspace drift fix was implemented.",
      "tags": ["roslyn", "workspace"],
      "importance": "high"
    }
  ]
}
```

## Retrieval Model

- `recall` is the curated overview for the built-in sections plus custom-section discovery
- `read_memory(section)` reads one exact section when you already know its name
- `search_memories(query)` searches across section names, tags, and entry text using case-insensitive substring matching

`search_memories` returns individual matching entries, sorted by `importance` descending and then `timestamp` descending.
