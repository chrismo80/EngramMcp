using EngramMcp.Core;
using EngramMcp.Features.Tests.TestDoubles;
using EngramMcp.Features.Tools;
using Is.Assertions;
using System.Text.Json;
using Xunit;
using static EngramMcp.Core.BuiltInMemorySections;

namespace EngramMcp.Features.Tests.Tools;

public sealed class ConsolidateToolTests
{
    [Fact]
    public async Task ConsolidateTool_ReadMode_ReturnsRawEntriesAndToken()
    {
        var service = new SpyMemoryService
        {
            MaintenanceReadResult = new MaintenanceSectionReadResult
            {
                Section = ShortTerm,
                Entries =
                [
                    new MaintenanceMemoryEntry
                    {
                        Timestamp = "2026-03-11T12:00:00.0000000+00:00",
                        Text = "short",
                        Importance = "high"
                    }
                ],
                ConsolidationToken = "token-1"
            }
        };
        var tool = new ConsolidateTool(service);

        var result = await tool.ExecuteAsync("read", ShortTerm, cancellationToken: CancellationToken.None);

        service.MaintenanceReadSection.Is(ShortTerm);
        result.Section.Is(ShortTerm);
        result.Entries!.Count.Is(1);
        result.Entries[0].Timestamp.Is("2026-03-11T12:00:00.0000000+00:00");
        result.Entries[0].Text.Is("short");
        result.Entries[0].Importance.Is("high");
        result.ConsolidationToken.Is("token-1");
    }

    [Fact]
    public async Task ConsolidateTool_WriteMode_ReturnsStoredEntriesWithoutToken()
    {
        var service = new SpyMemoryService
        {
            MaintenanceWriteResult = new MaintenanceSectionWriteResult
            {
                Section = "project-x",
                Entries =
                [
                    new MaintenanceMemoryEntry
                    {
                        Timestamp = "2026-03-11T12:00:00.0000000+00:00",
                        Text = "custom"
                    }
                ]
            }
        };
        var tool = new ConsolidateTool(service);

        var result = await tool.ExecuteAsync(
            "write",
            "project-x",
            consolidationToken: "token-1",
            entries:
            [
                new MaintenanceMemoryEntry
                {
                    Timestamp = "2026-03-11T12:00:00.0000000+00:00",
                    Text = "custom"
                }
            ],
            cancellationToken: CancellationToken.None);

        service.MaintenanceWriteSection.Is("project-x");
        service.ConsolidationWriteToken.Is("token-1");
        service.MaintenanceWriteEntries!.Count.Is(1);
        result.Section.Is("project-x");
        result.Entries!.Count.Is(1);
        result.Entries[0].Text.Is("custom");
        result.ConsolidationToken.Is(null);
    }

    [Fact]
    public async Task ConsolidateTool_WriteMode_ReturnsStructuredFailureFromService()
    {
        var service = new SpyMemoryService
        {
            MaintenanceWriteException = MaintenanceSectionWriteException.SectionNotFound("missing section")
        };
        var tool = new ConsolidateTool(service);

        var result = await tool.ExecuteAsync(
            "write",
            "project-x",
            consolidationToken: "token-1",
            entries: [new MaintenanceMemoryEntry { Timestamp = "2026-03-11T12:00:00.0000000+00:00", Text = "custom" }],
            cancellationToken: CancellationToken.None);

        result.Failure!.Category.Is("section_not_found");
        result.Failure.Message.Is("missing section");
        result.Section.Is(null);
        result.Entries.Is(null);
    }

    [Fact]
    public async Task ConsolidateTool_ReadMode_PropagatesUnknownSectionFailure()
    {
        var service = new SpyMemoryService
        {
            MaintenanceReadException = MaintenanceSectionWriteException.SectionNotFound("missing")
        };
        var tool = new ConsolidateTool(service);

        var result = await tool.ExecuteAsync("read", "project-missing", cancellationToken: CancellationToken.None);

        result.Failure!.Category.Is("section_not_found");
        result.Failure.Message.Is("missing");
    }

    [Fact]
    public async Task ConsolidateTool_ThrowsForInvalidMode()
    {
        var tool = new ConsolidateTool(new SpyMemoryService());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => tool.ExecuteAsync("merge", ShortTerm, cancellationToken: CancellationToken.None));

        exception.Message.Is("Consolidation mode must be 'read' or 'write'. (Parameter 'mode')");
    }

    [Fact]
    public async Task ConsolidateTool_WriteMode_ReturnsStructuredFailureWhenConsolidationTokenIsMissing()
    {
        var tool = new ConsolidateTool(new SpyMemoryService());

        var result = await tool.ExecuteAsync(
            "write",
            ShortTerm,
            entries: [],
            cancellationToken: CancellationToken.None);

        result.Section.Is(null);
        result.Entries.Is(null);
        result.Failure!.Category.Is("consolidation_token_missing");
        result.Failure.Message.Is("Consolidation token is required for write mode. Call read first, then submit the full replacement for that same section.");
    }

    [Fact]
    public async Task ConsolidateTool_WriteMode_ReturnsStructuredFailureWhenEntriesAreMissing()
    {
        var tool = new ConsolidateTool(new SpyMemoryService());

        var result = await tool.ExecuteAsync(
            "write",
            ShortTerm,
            consolidationToken: "token-1",
            cancellationToken: CancellationToken.None);

        result.Failure!.Category.Is("validation_failed");
        var detail = result.Failure.Details!.Single();
        detail.Field.Is("entries");
        detail.Message.Is("Entries are required for write mode and must contain the complete consolidated replacement list for that same section.");
    }

    [Fact]
    public void ConsolidateResponse_SerializesStorageShapedEntriesAndOmitsNullOptionals()
    {
        var response = new ConsolidateResponse
        {
            Section = ShortTerm,
            Entries =
            [
                new MaintenanceMemoryEntry
                {
                    Timestamp = "2026-03-11T12:00:00.0000000+00:00",
                    Text = "short"
                }
            ],
            ConsolidationToken = "token-1"
        };

        var json = JsonSerializer.Serialize(response);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("section").GetString().Is(ShortTerm);
        root.GetProperty("consolidationToken").GetString().Is("token-1");
        root.GetProperty("entries").GetArrayLength().Is(1);
        root.GetProperty("entries")[0].GetProperty("timestamp").GetString().Is("2026-03-11T12:00:00.0000000+00:00");
        root.GetProperty("entries")[0].GetProperty("text").GetString().Is("short");
        root.GetProperty("entries")[0].TryGetProperty("tags", out _).IsFalse();
        root.GetProperty("entries")[0].TryGetProperty("importance", out _).IsFalse();
        root.TryGetProperty("failure", out _).IsFalse();
    }

    [Fact]
    public void ConsolidateResponse_SerializesStructuredFailure()
    {
        var response = new ConsolidateResponse
        {
            Failure = new MaintenanceSectionFailure
            {
                Category = "validation_failed",
                Message = "invalid",
                Details =
                [
                    new MaintenanceSectionFailureDetail
                    {
                        Field = "entries",
                        Message = "at least one entry required"
                    }
                ]
            }
        };

        var json = JsonSerializer.Serialize(response);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.TryGetProperty("section", out _).IsFalse();
        root.TryGetProperty("entries", out _).IsFalse();
        root.TryGetProperty("consolidationToken", out _).IsFalse();
        root.GetProperty("failure").GetProperty("category").GetString().Is("validation_failed");
        root.GetProperty("failure").GetProperty("message").GetString().Is("invalid");
        root.GetProperty("failure").GetProperty("details")[0].GetProperty("field").GetString().Is("entries");
    }
}
