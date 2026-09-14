namespace Beam.Modules.Organizations.Contracts;

public record OrganizationDto(Guid Id, string Name, OrganizationRole MyRole)
{
    public static OrganizationDto FromEntity(Organization org, OrganizationRole myRole) => new(org.Id, org.Name, myRole);
}

public record CreateOrganizationRequest(string Name);

public record MembershipDto(Guid UserId, string? DisplayName, OrganizationRole Role);

public record AddMemberRequest(string Email, OrganizationRole Role);

public record UpdateMemberRoleRequest(OrganizationRole Role);

public record BrandKitDto(Guid OrganizationId, List<string> Colors, List<string> Fonts, Guid? LogoAssetId, Guid? WatermarkAssetId)
{
    public static BrandKitDto FromEntity(BrandKit kit) => new(kit.OrganizationId, kit.Colors, kit.Fonts, kit.LogoAssetId, kit.WatermarkAssetId);
}

public record UpdateBrandKitRequest(List<string> Colors, List<string> Fonts, Guid? LogoAssetId, Guid? WatermarkAssetId);
