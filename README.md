![plot](assets/icon.png)

# EngramMcp

> EngramMcp gives agents a small persistent memory that keeps what continues to matter and lets the rest fade away.

A Model Context Protocol server for persistent agent memory.

It is built for continuity across sessions.
The goal is not perfect recall.
The goal is that an agent comes back with the right context: preferences, durable facts, useful lessons, and work state worth carrying forward.

From the user's side, this should feel like the agent remembers important things without acting like a full archive.

## Get It as a .NET Tool

[![NuGet](https://img.shields.io/nuget/v/EngramMcp.svg)](https://www.nuget.org/packages/EngramMcp/)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://www.nuget.org/packages/EngramMcp/)

### Installation

```bash
dotnet tool install -g EngramMcp
```

## Configuration

By default, EngramMcp stores memory in `.engram/memory.json` under the current workspace directory.

Startup options:

- `--file <path>` stores memory at a fixed location

Use an absolute path for `--file` when you want the memory location to stay stable across launches.

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

EngramMcp is for things like:

- recurring user preferences
- stable facts worth keeping around
- work context that helps resume later
- lessons that proved useful before

It is not:

- a knowledge base
- a search engine
- a vector store
- a full conversation archive

The design stays intentionally small.
There is no search tool and no edit flow.
Memory is meant to be selective.

## A Typical Session

In daily use, the toolset behaves roughly like this:

1. Start with `recall`.
   It brings back the memories still worth carrying into the session.

2. Work with those memories as background context.
   They should help the agent pick up where it left off or adapt to the user without being reminded every time.

3. When something new deserves to survive the session, store it with the remember tier that matches its expected lifetime:
   - `remember_short` for near-future continuation
   - `remember_medium` for recurring but changeable context
   - `remember_long` for stable facts and constraints

4. If a recalled memory materially influenced the session, call `reinforce`.
   That tells the system the memory genuinely helped, not that it merely existed.

5. Memories that stop proving useful gradually fall away.

That is the core experience: not total recall, but continuity shaped by relevance.

## Tools

| Tool | Use it for |
|---|---|
| `recall` | Start of session. Load up to the 100 strongest current memories that are still worth carrying forward. |
| `remember_short` | Recent progress, temporary working context, checkpoints, resume notes. |
| `remember_medium` | Evolving preferences, lessons learned, recurring context. |
| `remember_long` | Stable facts, durable constraints, relationship context. |
| `reinforce` | Strengthen recalled memories that materially influenced the current session. |

## Example Recall Response

`recall` returns a minimal shape:

```json
{
  "memories": [
    {
      "id": "mcvx3n9k",
      "text": "The user prefers C#."
    },
    {
      "id": "mcvx3n9m",
      "text": "Use Roslyn tools proactively for C# exploration."
    }
  ]
}
```

Treat memory ids as opaque values.
They are only meant to be passed back to `reinforce`.

## Prompting Matters

The usefulness of the memory comes as much from the prompt as from the storage.

A good system prompt tells the agent to:

- begin each session with `recall`
- store only what deserves to outlive the current exchange
- choose the remember tier based on expected lifetime
- reinforce only memories that materially helped

Without that judgment, these tools are just storage.
With it, they start to feel like memory.
