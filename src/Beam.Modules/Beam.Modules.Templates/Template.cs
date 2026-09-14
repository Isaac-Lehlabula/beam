using Beam.Shared.Entities;

namespace Beam.Modules.Templates;

public enum TemplateCategory
{
    Gaming = 0,
    Podcast = 1,
    JustChatting = 2,
    Church = 3,
    Sports = 4,
    News = 5,
    Corporate = 6,
    Conference = 7,
    DjMusic = 8,
    Interview = 9,
    Education = 10,
    ProductLaunch = 11,
}

public enum TemplateScope
{
    System = 0,
    Organization = 1,
}

/// <summary>
/// Stores a definition snapshot (see Contracts/TemplateDefinition.cs) rather than normalized
/// scene/element rows -- ARCHITECTURE.md §14's documented trade-off: simplest to implement and
/// version, at the cost of templates not staying "live-linked" to instantiated projects (not
/// needed for MVP).
/// </summary>
public class Template : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public TemplateCategory Category { get; set; }

    public TemplateScope Scope { get; set; }

    /// <summary>Null for System scope; required for Organization scope.</summary>
    public Guid? OrganizationId { get; set; }

    public string DefinitionJson { get; set; } = "{}";
}
