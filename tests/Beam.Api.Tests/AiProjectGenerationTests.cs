using Beam.Infrastructure;
using Beam.Modules.AiGeneration.Contracts;
using Beam.Modules.AiGeneration.Services;
using Beam.Modules.Organizations.Services;
using Beam.Modules.Projects.Services;
using Beam.Modules.Scenes.Services;
using Beam.Modules.Templates.Services;
using Beam.Shared.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Beam.Api.Tests;

/// <summary>
/// The roadmap's explicit ask for Phase 11: "schema-validation test that generated output
/// always conforms to valid Template shape". These exercise the real parsing/validation
/// pipeline (AiProjectGenerationService -> ElementConfigSerializer -> ITemplateService) with a
/// canned model response standing in for the live Anthropic call, which needs no API key.
/// </summary>
public class AiProjectGenerationTests
{
    private static AiProjectGenerationService CreateService(string modelResponse, out Guid userId)
    {
        userId = Guid.NewGuid();
        var currentUser = new FakeCurrentUserContext(userId);
        var options = new DbContextOptionsBuilder<BeamDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new BeamDbContext(options, currentUser);

        var auditLogger = new Beam.Modules.Audit.Services.AuditLogger(db, currentUser);
        var organizationAccess = new OrganizationService(db, currentUser, new FakeUserLookupService(), auditLogger, auditLogger);
        var projectAccess = new ProjectService(db, currentUser, organizationAccess);
        var templateService = new TemplateService(db, db, db, db, projectAccess, organizationAccess, currentUser);

        return new AiProjectGenerationService(new FakeAnthropicClient(modelResponse), templateService);
    }

    private const string ValidResponse = """
        {
          "scenes": [
            {
              "name": "Starting Soon",
              "elements": [
                {
                  "type": "Text", "name": "Title", "x": 100, "y": 400, "width": 1720, "height": 150,
                  "rotation": 0, "opacity": 1,
                  "config": { "content": "The Final Whistle", "fontFamily": "Inter", "fontSize": 64, "fontWeight": "700", "textAlign": "center", "color": "#ffd700", "backgroundColor": null }
                }
              ]
            },
            {
              "name": "Main",
              "elements": [
                {
                  "type": "LowerThird", "name": "Host banner", "x": 60, "y": 900, "width": 600, "height": 100,
                  "rotation": 0, "opacity": 1,
                  "config": { "primaryText": "Alex", "secondaryText": "Host", "accentColor": "#ffd700", "textColor": "#ffffff", "backgroundColor": "rgba(0,0,0,0.7)" }
                }
              ]
            }
          ]
        }
        """;

    [Fact]
    public async Task Valid_ai_response_produces_a_real_fully_editable_project()
    {
        var service = CreateService(ValidResponse, out _);

        var project = await service.GenerateProjectAsync(new GenerateProjectRequest("A football podcast called The Final Whistle, black and gold"));

        project.Name.Should().NotBeNullOrEmpty();
        project.ResolutionWidth.Should().Be(1920);
    }

    [Fact]
    public async Task Response_wrapped_in_markdown_code_fences_is_still_parsed()
    {
        var service = CreateService($"```json\n{ValidResponse}\n```", out _);

        var act = () => service.GenerateProjectAsync(new GenerateProjectRequest("A show"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Malformed_json_from_the_model_fails_clearly_instead_of_crashing()
    {
        var service = CreateService("not json at all { broken", out _);

        var act = () => service.GenerateProjectAsync(new GenerateProjectRequest("A show"));

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task Unsupported_element_type_is_rejected_not_silently_dropped()
    {
        const string response = """
            { "scenes": [ { "name": "Main", "elements": [
                { "type": "Video", "name": "x", "x": 0, "y": 0, "width": 100, "height": 100, "rotation": 0, "opacity": 1, "config": {} }
            ] } ] }
            """;
        var service = CreateService(response, out _);

        var act = () => service.GenerateProjectAsync(new GenerateProjectRequest("A show"));

        await act.Should().ThrowAsync<ValidationAppException>().WithMessage("*unsupported element type*");
    }

    [Fact]
    public async Task Invalid_config_for_an_allowed_type_is_rejected_by_the_same_validator_hand_authored_elements_use()
    {
        const string response = """
            { "scenes": [ { "name": "Main", "elements": [
                { "type": "Text", "name": "x", "x": 0, "y": 0, "width": 100, "height": 100, "rotation": 0, "opacity": 1,
                  "config": { "content": "hi", "fontFamily": "Inter", "fontSize": 32, "fontWeight": "700", "textAlign": "diagonal", "color": "#fff", "backgroundColor": null } }
            ] } ] }
            """;
        var service = CreateService(response, out _);

        var act = () => service.GenerateProjectAsync(new GenerateProjectRequest("A show"));

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task Empty_scene_list_is_rejected()
    {
        var service = CreateService("""{ "scenes": [] }""", out _);

        var act = () => service.GenerateProjectAsync(new GenerateProjectRequest("A show"));

        await act.Should().ThrowAsync<ValidationAppException>();
    }
}
