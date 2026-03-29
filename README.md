![plot](assets/icon.png)

# EngramMcp

A Model Context Protocol server for persistent agent memory.

## Get It as a .NET Tool

[![NuGet](https://img.shields.io/nuget/v/EngramMcp.svg)](https://www.nuget.org/packages/EngramMcp/)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://www.nuget.org/packages/EngramMcp/)

### Installation

```bash
dotnet tool install -g EngramMcp
```

## Configuration

EngramMcp stores memory by default in `.engram/memory.json` under the current workspace directory.

Startup options:

- `--file <path>` stores memory at a fixed location

Use an absolute file path for `--file` when you want the memory location to stay stable across launches.

Example:

```json
{
  "mcp": {
    "memory": {
      "type": "local",
      "command": [
        "engrammcp",
        "--file",
        "/absolute/path/to/memory.json"
      ]
    }
  }
}
```

## What It Is

EngramMcp is a local-first memory MCP for agents that need continuity, not a search engine or knowledge platform.

It gives an agent a small persistent memory with explicit remember, recall, and reinforce flows:

- `recall` loads the current memory set
- `remember_short`, `remember_medium`, and `remember_long` create new memories with different initial strengths
- `reinforce` strengthens memories that proved useful again

The model is intentionally simple:

- one global memory pool
- no sections
- no search
- no embeddings
- no agent-visible scores

## Tools

| Tool | Description |
|---|---|
| `recall` | Decays current memories, deletes weak ones, and returns the surviving list as `id` + `text` |
| `remember_short` | Creates a new short-lived memory |
| `remember_medium` | Creates a new medium-lived memory |
| `remember_long` | Creates a new long-lived memory |
| `reinforce` | Strengthens existing memories by id |

## Memory Model

Each persisted memory contains:

- `id`
- `text`
- `retention`

Example file shape:

```json
{
  "memories": [
    {
      "id": "260329142501",
      "text": "Moldi prefers C#.",
      "retention": 10.0
    },
    {
      "id": "260329142530-2",
      "text": "README should match the implementation state before commit.",
      "retention": 100.0
    }
  ]
}
```

`retention` is internal. It controls:

- recall order
- decay over time
- deletion of weak memories

The agent does not see retention values or formulas.

## Retrieval Model

`recall` is the session-start tool.

On every call it:

1. decays all memories
2. deletes memories below the retention threshold
3. persists those changes
4. returns all surviving memories sorted by strength

The response shape is minimal:

```json
{
  "memories": [
    {
      "id": "260329142501",
      "text": "Moldi prefers C#."
    },
    {
      "id": "260329142530-2",
      "text": "README should match the implementation state before commit."
    }
  ]
}
```

There is no search tool and no section-reading tool.

## Storage Rules

All remember tools:

- always create a new memory
- do not attempt duplicate detection
- persist immediately

Memory text must:

- not be null, empty, or whitespace
- be exactly one line
- not contain `\r` or `\n`
- be at most 1000 characters long

## Reinforcement

`reinforce` accepts a list of memory ids.

- unknown ids fail the whole call
- the list must not be empty
- reinforcement is persisted immediately
- a memory is reinforced at most once per server session, even if repeated by mistake

## System Prompt Guidance

Use the tools roughly like this:

- call `recall` at the start of a session
- use `remember_short` for near-term working state
- use `remember_medium` for context that may matter across future sessions
- use `remember_long` for durable facts and stable preferences
- use `reinforce` only for memories that proved useful again, not memories that were merely present
