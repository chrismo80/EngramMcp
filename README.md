![plot](assets/icon.png)

# EngramMcp

A Model Context Protocol (MCP) server for persistent agent memory.

## Get It as a .NET Tool


[![NuGet](https://img.shields.io/nuget/v/EngramMcp.svg)](https://www.nuget.org/packages/EngramMcp/)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://www.nuget.org/packages/EngramMcp/)

### Installation

```bash
dotnet tool install -g EngramMcp
```

### Configuration

EngramMcp stores memory by default in `.engram/memory.json` under the current workspace directory.

Startup options:

- `--file <path>` keeps the memory file at a fixed location
- `--size <small|normal|big>` selects the memory capacity profile; default is `small`

Use an absolute file path for `--file` when you want the memory location to stay stable across launches, even outside the workspace.

Example with an explicit file path and larger memory budget:

```json
{
  "mcp": {
    "memory": {
      "type": "local",
      "command": [
        "engrammcp",
        "--file",
        "/absolute/path/to/memory.json",
        "--size",
        "big"
      ]
    }
  }
}
```

## What It Is

EngramMcp is a local-first memory MCP for agents that need continuity, not a full knowledge platform. It gives coding agents and personal assistants a small, inspectable, structured memory they can carry across sessions without relying on hosted infrastructure.

Built for trust and predictability, it focuses on durable facts, evolving context, and recent work state instead of embeddings, document ingestion, or opaque retrieval pipelines.

The goal is not to build a general knowledge system. The goal is to give an agent a memory that is durable, inspectable, and simple enough to trust.

## Who It Is For

EngramMcp is built for developers and local-first AI users who want persistent memory without handing that memory off to a hosted platform.

It is a strong fit for people who want:

- persistent memory for a coding agent or personal assistant
- local, human-readable storage they can inspect themselves
- a small and predictable memory model instead of a larger knowledge stack
- clear separation between durable facts, evolving context, and recent work state

## Best-Fit Use Cases

EngramMcp works best for workflows like:

- remembering user preferences, conventions, and working style
- carrying project context forward across days or weeks
- preserving recent progress so the next session can continue quickly
- storing important facts and decisions in a form the human can audit
- giving a local coding agent lightweight continuity without extra infrastructure

## Who It Is Not For

EngramMcp is intentionally narrow. It is not designed to be:

- a vector database or semantic retrieval engine
- a document indexing or RAG platform
- a collaborative team knowledge base
- a high-scale multi-user write-heavy service
- an enterprise platform for permissions, governance, or hosted memory

## What It Optimizes For

EngramMcp optimizes for a specific kind of memory:

- local-first
- transparent
- structured
- durable for everyday agent workflows
- small enough to reason about
- simple enough to trust

## What You Can Use It For

| Tool                  | Description                                                                        |
|-----------------------| ---------------------------------------------------------------------------------- |
| **Recall**            | Load the built-in memory overview and list available custom sections               |
| **Store Long-Term**   | Save durable facts and preferences worth keeping indefinitely                      |
| **Store Medium-Term** | Save useful context that may change over time                                      |
| **Store Short-Term**  | Save the recent working state for fast next-session continuation                   |
| **Read Section**      | Read the contents of one specific memory section                                   |
| **Maintain Section**  | Read or replace one existing section with a token-guarded maintenance workflow     |
| **Store**             | Save memory into any named section, including custom sections created on first use |
| **Search**            | Search individual memory entries across section names, tags, and text              |

## Memory Model

EngramMcp uses three built-in memory sections with capacities derived from the selected size profile:

| Size | `long-term` | `medium-term` | `short-term` | custom sections |
|------|-------------|---------------|--------------|-----------------|
| `small` | 20 | 10 | 5 | 20 |
| `normal` | 40 | 20 | 10 | 40 |
| `big` | 80 | 40 | 20 | 80 |

Agents can create custom sections through `store(section, text, tags?, importance?)`. Custom sections are created on first write and use the same capacity as `long-term` for the active size profile.

Built-in sections appear first in `recall`. Custom sections are listed afterward with their names and memory counts, but their contents are not included in the default recall output.

When a section exceeds its capacity, retention is applied globally within that section using a weighted score: older entries become more likely eviction candidates over time, while higher importance reduces that score and protects entries longer. The current weights are `low = 1`, `normal = 3`, and `high = 8`.

Each stored entry contains:

- `timestamp` - local write timestamp
- `text` - a required single-line memory text with a maximum length of 500 characters
- `tags` - optional normalized tags used for later search and filtering
- `importance` - optional importance level: `low`, `normal`, or `high`; unknown values currently fall back to `normal`

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
- `search(query)` splits the query on whitespace and matches each term case-insensitively as a substring against section names, tags, and entry text

`search` returns individual matching entries when at least one query term matches. Results are ranked by the number of distinct query terms matched, then sorted by `importance` descending and `timestamp` descending.

Retrieval tools return structured JSON responses. These responses expose only the fields that belong to the retrieval contract:

- timestamps are never included in `recall`, `read_section`, or `search`
- `tags` are included only when present
- `importance` is included only for `high`, serialized as `"high"`
- `recall` always includes the built-in sections in the same order, even when empty
- `recall.customSections` appears only when there are custom sections to list

Example `recall` response shape:

```json
{
  "memories": {
    "long-term": [
      {
        "memory": "Agent K is my self-identity: not a chatbot, but a gentle coding-buddy",
        "tags": ["identity", "preference"],
        "importance": "high"
      }
    ],
    "medium-term": [],
    "short-term": []
  },
  "customSections": [
    {
      "name": "project-x",
      "memories": 1
    }
  ]
}
```

Example `read_section(section)` success response shape:

```json
{
  "memories": {
    "project-x": [
      {
        "memory": "The MCP workspace drift fix was implemented.",
        "tags": ["roslyn", "workspace"],
        "importance": "high"
      }
    ]
  }
}
```

If `read_section(section)` receives a valid but unknown section name, it returns a normal success response with that exact requested section name mapped to an empty list. Invalid section identifiers still fail validation.

Example `read_section(section)` empty response shape:

```json
{
  "memories": {
    "project-x": []
  }
}
```

Example `search(query)` response shape:

```json
{
  "results": [
    {
      "memory": "The MCP workspace drift fix was implemented.",
      "section": "project-x",
      "tags": ["roslyn", "workspace"],
      "importance": "high"
    }
  ]
}
```

All write tools support optional `tags` and `importance`, including `store(section, text, tags?, importance?)` and the built-in section writers.

`maintain_section` is the only maintenance tool. It works on exactly one existing section at a time:

- `mode = "read"` returns the canonical section name, raw storage-shaped `entries`, and a `maintenanceToken`
- `mode = "write"` requires the matching token and replaces that section with the provided raw `entries`
- maintenance reads fail for unknown sections and do not issue tokens
- maintenance writes do not create sections, do not run retention, and fail when the replacement list exceeds section capacity


## System Prompt

## Memory

### Retrieval

Prefer using existing memory over asking the user to repeat information.
Check memory before answering questions about the user, preferences, prior work, or ongoing tasks.

- memory_recall: Call at the start of each session.
- memory_search: Use for loose keyword-based memory retrieval; short focused queries work best.
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
