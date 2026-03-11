# ARCHITECTURE

## Overview

`EngramMcp` is an MCP server for coding agents that provides a lightweight persistent memory mechanism.

The V1 architecture is intentionally simple:

- one JSON file on disk
- multiple named memory sections inside that file
- shared storage logic for all memory sections
- thin MCP tools that route writes to the correct section

The system is designed to stay small, deterministic, and inspectable by humans.

## Architectural Intent

The server should not be modeled as three separate memory implementations.

Instead, it should be modeled as:

- a catalog of memory objects
- one shared persistence mechanism
- one shared write/eviction workflow
- one shared recall workflow
- four MCP tools mapped onto that shared core

V1 defines three memory types in code:

- `shortTerm`
- `mediumTerm`
- `longTerm`

Additional memory types may be added later by introducing a new memory object and a new MCP tool, without redesigning the core services.

## Runtime Configuration

The server accepts the memory file path at startup via CLI argument:

- `--file <path>`

Example MCP client configuration:

```json
{
  "mcp": {
    "memory": {
      "type": "local",
      "command": [
        "engrammcp --file ~/user/personal-memory.json"
      ]
    }
  }
}
```

Rules:

- the file may exist inside or outside any workspace or repository
- the server must validate and load the file at startup
- if the file does not exist, the server must create it with the default structure
- if the file contains invalid JSON, startup must fail and the MCP server should remain unavailable

## Persisted Data Shape

V1 stores all data in a single JSON file.

Default file content:

```json
{
  "shortTerm": [],
  "mediumTerm": [],
  "longTerm": []
}
```

Each memory entry has exactly two fields:

```json
{
  "timestamp": "2026-03-11T15:04:05",
  "text": "Some remembered fact or note"
}
```

Notes:

- `timestamp` uses local time
- V1 does not add IDs, tags, metadata, or relations
- `text` is stored as plain text

## Core Data Models

### Memory

Represents one configured memory section as a domain object.

Required members:

- `Name`
- `Capacity`
- `Store(...)`
- `Read(...)`

V1 memory objects are created from constants in code.

Example intent:

- `shortTerm` with small capacity
- `mediumTerm` with medium capacity
- `longTerm` with larger capacity

### MemoryEntry

Represents one stored memory item.

Fields:

- `Timestamp`
- `Text`

### MemoryDocument

Represents the JSON file loaded from disk.

V1 shape:

- `Memories: Dictionary<string, List<MemoryEntry>>`

The dictionary key is the memory name from `Memory.Name`.

For V1, the expected keys are:

- `shortTerm`
- `mediumTerm`
- `longTerm`

This keeps the in-memory model generic and allows additional memory sections to be added later with minimal structural change.

## Core Services

### Memory Catalog

Responsibility:

- expose the set of memories supported by the server
- support lookup by memory name
- keep V1 memory instances centralized

V1 behavior:

- memory instances live in code as constants or static readonly values
- capacities are not configurable yet

### MemoryFileStore

Responsibility:

- resolve the configured file path
- create the file if missing
- load and deserialize the memory document
- serialize and persist updates
- validate the basic file structure during startup

Rules:

- no separate implementation per memory type
- no database abstraction in V1
- file IO must be the single source of truth

### MemoryService

Responsibility:

- store a new memory into a named memory section
- enforce capacity using FIFO eviction
- return all memories for recall
- validate input text before storing

Key operations:

- `Store(memoryName, text)`
- `Recall()`

Behavior:

- reject null, empty, or whitespace-only text
- generate timestamp in local time on the server
- resolve the target memory from the catalog
- delegate section-specific read/store behavior to the resolved memory object
- allow duplicate texts in V1

## MCP Tool Surface

V1 exposes four tools.

### `store_shortterm`

- input: plain text
- action: call shared store logic for `shortTerm`

### `store_mediumterm`

- input: plain text
- action: call shared store logic for `mediumTerm`

### `store_longterm`

- input: plain text
- action: call shared store logic for `longTerm`

### `recall`

- input: none
- action: load and return the full current memory content

V1 response shape should be raw structured data, not markdown formatting.

Expected conceptual response:

```json
{
  "shortTerm": [
    {
      "timestamp": "2026-03-11T15:04:05",
      "text": "..."
    }
  ],
  "mediumTerm": [],
  "longTerm": []
}
```

## Control Flow

### Startup Flow

1. Parse CLI arguments and obtain `--file`
2. Load known memories from code
3. Ensure the target file exists; create default file if not
4. Load and validate the JSON document
5. If validation fails, stop startup and expose the error
6. Register MCP tools
7. Start MCP host

### Store Flow

1. MCP tool receives text input
2. Tool maps to one predefined memory name
3. Shared service validates input
4. Shared store loads current file content
5. Service appends a new `MemoryEntry`
6. Service enforces FIFO capacity for the targeted memory section
7. Updated document is written back to disk
8. Tool returns success result

### Recall Flow

1. MCP tool invokes shared recall logic
2. Shared store loads current file content
3. Raw memory document is returned to the caller

## Validation Rules

- `--file` must be provided
- the file must be readable and writable by the server process
- whitespace-only memory text is invalid
- malformed JSON is a startup error
- missing required memory keys in an existing file should be treated as invalid structure in V1

## Error Strategy

The server should fail loudly and predictably.

Rules:

- invalid startup configuration prevents server startup
- malformed JSON prevents server startup
- invalid tool input returns a clear tool-level error
- the server must not silently reset or overwrite malformed files

This is intentionally conservative to protect user memory data.

## Project Structure Intent

The existing solution scaffold should preserve clear boundaries.

Suggested allocation:

- `src/EngramMcp.Core`
  - memory models
  - memory definitions
  - core abstractions if needed
- `src/EngramMcp.Infrastructure`
  - JSON file persistence
  - path/file handling
- `src/EngramMcp.Features`
  - MCP tool registration
  - tool request/response contracts
  - thin adapters into the shared memory service
- `src/EngramMcp.Host`
  - CLI startup
  - argument parsing for `--file`
  - service registration and host bootstrapping
- `tests/EngramMcp.Features.Tests`
  - tool-level behavior tests
  - FIFO and validation scenarios

## Public Contracts To Define In Scaffold

The scaffold phase should define, but not fully implement beyond skeleton level, the following kinds of contracts:

- `Memory`
- `MemoryEntry`
- `MemoryDocument`
- abstraction for loading/saving memory file
- abstraction for memory operations
- request/response contracts for the four MCP tools

These contracts should be stable enough for implementation to proceed without architectural ambiguity.

## Risks And Design Guardrails

### Local Time Timestamps

Using local time is acceptable for V1 because the system is local-first and human-readable.

Risk:

- timestamps become less portable across machines and time zones

Guardrail:

- keep timestamp generation centralized so V2 can switch formats if needed

### Single File Persistence

Using one file keeps the system simple and inspectable.

Risk:

- all memories depend on one file remaining valid

Guardrail:

- strict startup validation
- no silent repair of malformed JSON

### Raw Recall Output

Returning raw data is correct for V1.

Risk:

- consumers may later want more human-friendly formatting

Guardrail:

- keep recall response shape stable; formatting can be added later as a presentation concern

## Non-Goals

V1 should not introduce:

- automatic summarization or promotion between memories
- search or ranking
- memory updates or deletes
- dynamic memory definitions from external config
- rich semantic models
- persistence engines beyond local JSON file storage

## Handoff To Code-Monkey

Implementation phase should:

- preserve the generic shared-memory architecture
- avoid copy-pasted logic across memory types
- implement startup validation for `--file`
- implement deterministic FIFO behavior per memory section
- keep recall raw and section-separated
- reject whitespace-only store input
- keep capacities centralized in code-backed memory instances

Implementation phase should not:

- add extra tools beyond the four agreed tools
- add database or vector dependencies
- add metadata fields beyond `timestamp` and `text`
- introduce automatic promotion or semantic inference

## Definition Of Done

Architectural work is complete when:

- the requirements and architecture are aligned
- the data shape is fixed
- the runtime configuration path is fixed as `--file`
- the component boundaries are clear
- the shared-core design is explicit
- code-monkey can implement without making architecture decisions on the fly

Architect phase complete -> handing over to code-monkey for implementation
