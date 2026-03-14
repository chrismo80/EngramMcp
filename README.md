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
      "command": ["engrammcp"]
    }
  }
}
```

By default, EngramMcp stores memory in `.engram/memory.json` under the current workspace directory.

Use an absolute file path for `--file` when you want the memory location to stay stable across launches, even outside the workspace.

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
|-----------------------| ---------------------------------------------------------------------------------- |
| **Recall**            | Load the built-in memory overview and list available custom sections               |
| **Store Long-Term**   | Save durable facts and preferences worth keeping indefinitely                      |
| **Store Medium-Term** | Save useful context that may change over time                                      |
| **Store Short-Term**  | Save the recent working state for fast next-session continuation                   |
| **Read Section**      | Read the contents of one specific memory section                                   |
| **Store**             | Save memory into any named section, including custom sections created on first use |
| **Search**            | Search individual memory entries across section names, tags, and text              |

## Memory Model

EngramMcp uses three built-in memory sections with code-defined capacities:

- `long-term` - 20 entries
- `medium-term` - 10 entries
- `short-term` - 5 entries

In addition, agents can create custom sections through `store(section, text, tags?, importance?)`. Custom sections are created lazily on first write and currently use a shared default capacity of 20 entries.

Built-in sections appear first in `recall`. Custom sections are listed afterward as discoverable section names with entry counts, but their contents are not dumped into the default recall output.

When a section exceeds its capacity, retention is applied globally within that section: lower-importance entries are discarded before higher-importance entries, and ties are broken by oldest timestamp first.

Each stored entry contains:

- `timestamp` - local write timestamp
- `text` - a required single-line memory text with a maximum length of 500 characters
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
- `read_section(section)` reads one exact section when you already know its name; section lookup is case-insensitive and trims surrounding whitespace
- `search(query)` searches across section names, tags, and entry text using case-insensitive substring matching

`search` returns individual matching entries, sorted by `importance` descending and then `timestamp` descending.

All write tools support optional `tags` and `importance`, including `store(section, text, tags?, importance?)` and the built-in section writers.


# System Prompt

## Memory

### Retrieval

Prefer using existing memory over asking the user to repeat information.
Check memory before answering questions about the user, preferences, prior work, or ongoing tasks.

- memory_recall: Call at the start of each session.
- memory_search: Use for keyword-based memory retrieval.
- memory_read_section: Use for retrieving the full contents of a section.

### Storage

Store information when meaningful facts or checkpoints are learned, not after every message.

Only store information that is likely to remain useful in future sessions.
Store conclusions, not conversation.
Avoid storing duplicate information.

- memory_store_longterm: Use for personal facts about the user or your relationship that are unlikely to change (name, identity, values, personality, vibe).
- memory_store_mediumterm: Use for personal and work-related information that may evolve over time (preferences, hobbies, working style, favorite tools, music taste).
- memory_store_shortterm: Use for work-related context that helps resume progress in future sessions (completed tasks, checkpoints, important findings).
- memory_store: Use for custom memory sections.