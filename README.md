![plot](assets/icon.png)

# EngramMcp

> EngramMcp gives agents a small persistent memory that keeps what continues to matter and lets the rest fade away.

Persistent memory for Model Context Protocol agents.

It is built for continuity across sessions.
Not perfect recall. Not a transcript.
Just the context an agent should still have next time: preferences, durable facts, useful lessons, and work state worth carrying forward.

For the user, that should feel simple: the agent remembers what matters without pretending to remember everything.

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

## What It Is For

Use EngramMcp for:

- recurring user preferences
- stable facts worth keeping around
- work context that helps resume later
- lessons that proved useful before

It is not:

- a knowledge base
- a search engine
- a vector store
- a full conversation archive

The toolset stays intentionally small.
No search. No edit flow.
Memory is meant to stay selective.

## How It Feels in Practice

A typical session looks like this:

1. Start with `recall`.
   It brings back the memories still worth carrying into the session.

2. Use those memories as background context.
   They help the agent pick up where it left off and adapt to the user without being reminded each time.

3. When something new deserves to survive the session, store it with the remember tier that matches its expected lifetime:
   - `remember_short` for near-future continuation
   - `remember_medium` for recurring but changeable context
   - `remember_long` for stable facts and constraints

4. If a recalled memory materially influenced the session, call `reinforce`.
   That tells the system the memory genuinely helped, not that it merely existed.

5. Memories that stop proving useful gradually fall away.

That is the core experience: continuity shaped by relevance, not by accumulation.

## Tools

| Tool | Use it for                                                                                            |
|---|-------------------------------------------------------------------------------------------------------|
| `recall` | Start of session. By default returns up to 50 memories. You can optionally request a different count (e.g. `recall(maxCount: 200)`), and the response includes `selectedCount` and `totalCount`. |
| `remember_short` | Recent progress, temporary working context, checkpoints, resume notes.                                |
| `remember_medium` | Evolving preferences, lessons learned, recurring context.                                             |
| `remember_long` | Stable facts, durable constraints, relationship context.                                              |
| `reinforce` | Strengthen recalled memories that materially influenced the current session.                          |

## Example Recall Response

`recall` returns a minimal shape:

```json
{
  "selectedCount": 2,
  "totalCount": 2,
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

These tools work best when the system prompt teaches the agent how to use them well.

A good default is:

- begin each session with `recall`
- use `remember_short` for work-related context that helps resume progress in future sessions (completed tasks, checkpoints, important findings)
- use `remember_medium` for personal and work-related information that may evolve over time (preferences, hobbies, working style, favorite tools, music taste)
- use `remember_long` for personal facts about the user or your relationship that are unlikely to change (name, identity, values, personality, vibe)
- after completing a task or reaching a meaningful milestone (e.g. a commit), consider using `reinforce` for any recalled memories influenced your reasoning, implementation, or communication

Without that judgment, this is just storage.
With it, it starts to feel like memory.
