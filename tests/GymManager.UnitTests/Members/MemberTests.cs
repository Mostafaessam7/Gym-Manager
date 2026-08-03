using GymManager.Domain.Members;
using Xunit;

namespace GymManager.UnitTests.Members;

public sealed class MemberTests
{
    private static Member CreateMember() =>
        Member.Register("MEM-000001", Guid.NewGuid(), "John", "Smith", "555-0100", null, null, Gender.Male, null);

    [Fact]
    public void Register_Should_Raise_MemberRegisteredDomainEvent_And_Generate_CheckInCode()
    {
        var member = CreateMember();

        Assert.Single(member.DomainEvents);
        Assert.NotEmpty(member.CheckInCode);
        Assert.Equal(MemberStatus.Active, member.Status);
    }

    [Fact]
    public void Freeze_Should_Fail_When_Already_Frozen()
    {
        var member = CreateMember();
        member.Freeze();

        var result = member.Freeze();

        Assert.True(result.IsFailure);
        Assert.Equal("Member.AlreadyFrozen", result.Error.Code);
    }

    [Fact]
    public void Unfreeze_Should_Fail_When_Not_Frozen()
    {
        var member = CreateMember();

        var result = member.Unfreeze();

        Assert.True(result.IsFailure);
        Assert.Equal("Member.NotFrozen", result.Error.Code);
    }

    [Fact]
    public void Freeze_Then_Unfreeze_Should_Restore_Active_Status()
    {
        var member = CreateMember();
        member.Freeze();

        var result = member.Unfreeze();

        Assert.True(result.IsSuccess);
        Assert.Equal(MemberStatus.Active, member.Status);
    }

    [Fact]
    public void RegenerateCheckInCode_Should_Produce_A_Different_Value()
    {
        var member = CreateMember();
        var original = member.CheckInCode;

        member.RegenerateCheckInCode();

        Assert.NotEqual(original, member.CheckInCode);
    }

    [Fact]
    public void UpdateMedicalInfo_Should_Store_The_Given_Value()
    {
        var member = CreateMember();
        var medicalInfo = MedicalInfo.Create("O+", "Asthma", "Peanuts", "Inhaler", "Carries an EpiPen");

        member.UpdateMedicalInfo(medicalInfo);

        Assert.Equal("O+", member.MedicalInfo!.BloodType);
        Assert.Equal("Asthma", member.MedicalInfo.Conditions);
        Assert.Equal("Peanuts", member.MedicalInfo.Allergies);
    }

    [Fact]
    public void UpdateMedicalInfo_Should_Allow_Clearing_It_Back_To_Null()
    {
        var member = CreateMember();
        member.UpdateMedicalInfo(MedicalInfo.Create("O+", null, null, null, null));

        member.UpdateMedicalInfo(null);

        Assert.Null(member.MedicalInfo);
    }

    [Fact]
    public void AddDocument_Should_Add_It_To_The_Documents_Collection()
    {
        var member = CreateMember();

        var document = member.AddDocument("waiver.pdf", "/files/waiver.pdf", MemberDocumentType.Waiver, "staff@gym.io");

        Assert.Single(member.Documents);
        Assert.Equal(document.Id, member.Documents.Single().Id);
        Assert.Equal(MemberDocumentType.Waiver, member.Documents.Single().DocumentType);
    }

    [Fact]
    public void RemoveDocument_Should_Remove_The_Matching_Document()
    {
        var member = CreateMember();
        var document = member.AddDocument("waiver.pdf", "/files/waiver.pdf", MemberDocumentType.Waiver, "staff@gym.io");

        var result = member.RemoveDocument(document.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(member.Documents);
    }

    [Fact]
    public void RemoveDocument_Should_Fail_For_An_Unknown_Id()
    {
        var member = CreateMember();

        var result = member.RemoveDocument(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Member.DocumentNotFound", result.Error.Code);
    }
}
