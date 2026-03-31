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

EngramMcp gives an agent a small local memory that can survive across sessions.

It is meant for continuity:

- things worth remembering later
- preferences that keep coming back
- stable facts that should not be rediscovered every time

It is not trying to be a knowledge base, a search engine, or a vector store.

The workflow is simple:

- `recall` brings back the memories that are still alive
- `remember_short` stores something that is probably useful only for a while
- `remember_medium` stores something that may matter again later
- `remember_long` stores something that should stick around
- `reinforce` tells the system that a memory proved useful again

## Tools

| Tool | Description |
|---|---|
| `recall` | Decays current memories, deletes weak ones, and returns the surviving list as `id` + `text` |
| `remember_short` | Creates a new short-lived memory |
| `remember_medium` | Creates a new medium-lived memory |
| `remember_long` | Creates a new long-lived memory |
| `reinforce` | Strengthens existing memories by id |

## How Memory Behaves

Memories are not all equal.

- short memories fade quickly unless they keep proving useful
- medium memories have more staying power
- long memories are for durable facts and preferences

You can think of it like this:

- `remember_short` is for active working context
- `remember_medium` is for context you expect to matter again
- `remember_long` is for things that should be hard to lose

`reinforce` is the important part: when a memory is genuinely useful again, reinforcing it helps it stay around.

That means the system naturally drifts toward keeping what continues to matter and forgetting what does not.

Retention is a memory's budget for future recalls. Each memory receives an initial budget based on its retention tier. Every recall spends part of that budget. When a memory proves materially useful, reinforcement restores some of it. This keeps short-term memories temporary, makes long-term memories resilient, and allows useful memories to earn a longer life over time.

With the current defaults, a rough mental model is:

- a short memory lasts about 5 recalls if it is never reinforced
- a medium memory lasts about 25 recalls if it is never reinforced
- a long memory lasts about 100 recalls if it is never reinforced

These are not user-facing scores. They are just the internal mechanics behind what stays and what fades.

## Stored File

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
      "text": "The user prefers C#.",
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

`retention` is internal. It controls which memories stay, which fade, and which disappear.

## Retrieval Model

`recall` is the session-start tool.

From a user point of view, `recall` does two things at once:

- it returns the memories that are still worth keeping around
- it lets old, unused memories continue to fade

So memory is not just read. It is also gently maintained.

The response shape is minimal:

```json
{
  "memories": [
    {
      "id": "260329142501",
      "text": "The user prefers C#."
    },
    {
      "id": "260329142530-2",
      "text": "README should match the implementation state before commit."
    }
  ]
}
```

There is no search tool and no section-reading tool. The design stays intentionally small.

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

Use `reinforce` when a memory turned out to be useful again, not just because it happened to be present.

That keeps the memory set honest: memories survive because they helped, not because they existed.

`reinforce` accepts a list of memory ids.

- unknown ids fail the whole call
- the list must not be empty
- reinforcement is persisted immediately
- a memory is reinforced at most once per server session, even if repeated by mistake

## System Prompt Guidance

Use the tools roughly like this:

- call `recall` at the start of each session
- use `remember_short` for work-related context that helps resume progress in future sessions (completed tasks, checkpoints, important findings)
- use `remember_medium` for personal and work-related information that may evolve over time (preferences, hobbies, working style, favorite tools, music taste)
- use `remember_long` for personal facts about the user or your relationship that are unlikely to change (name, identity, values, personality, vibe)
- after completing a task or reaching a meaningful milestone (e.g. a commit), consider using `reinforce` for any recalled memories  influenced your reasoning, implementation or communication