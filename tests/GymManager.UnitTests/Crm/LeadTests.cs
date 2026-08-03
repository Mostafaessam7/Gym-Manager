using GymManager.Domain.Crm;
using Xunit;

namespace GymManager.UnitTests.Crm;

public sealed class LeadTests
{
    private static Lead CreateLead() =>
        Lead.Create("Jane Prospect", "jane@example.com", "555-0100", LeadSource.Website, Guid.NewGuid(), null, null);

    [Fact]
    public void Create_Should_Default_To_New_Stage()
    {
        var lead = CreateLead();

        Assert.Equal(LeadStage.New, lead.Stage);
        Assert.Null(lead.ConvertedMemberId);
        Assert.Empty(lead.FollowUps);
    }

    [Fact]
    public void MoveToStage_Should_Update_The_Stage()
    {
        var lead = CreateLead();

        var result = lead.MoveToStage(LeadStage.Contacted);

        Assert.True(result.IsSuccess);
        Assert.Equal(LeadStage.Contacted, lead.Stage);
    }

    [Fact]
    public void MoveToStage_To_Won_Should_Fail()
    {
        var lead = CreateLead();

        var result = lead.MoveToStage(LeadStage.Won);

        Assert.True(result.IsFailure);
        Assert.Equal("Lead.NotWon", result.Error.Code);
    }

    [Fact]
    public void MoveToStage_To_Lost_Should_Fail()
    {
        var lead = CreateLead();

        var result = lead.MoveToStage(LeadStage.Lost);

        Assert.True(result.IsFailure);
        Assert.Equal("Lead.NotWon", result.Error.Code);
    }

    [Fact]
    public void MarkLost_Should_Set_Stage_And_Reason()
    {
        var lead = CreateLead();

        var result = lead.MarkLost("Went with a competitor");

        Assert.True(result.IsSuccess);
        Assert.Equal(LeadStage.Lost, lead.Stage);
        Assert.Equal("Went with a competitor", lead.LostReason);
    }

    [Fact]
    public void Reopen_Should_Move_A_Lost_Lead_Back_To_Contacted()
    {
        var lead = CreateLead();
        lead.MarkLost("Not ready");

        var result = lead.Reopen();

        Assert.True(result.IsSuccess);
        Assert.Equal(LeadStage.Contacted, lead.Stage);
        Assert.Null(lead.LostReason);
    }

    [Fact]
    public void ConvertToMember_Should_Set_Stage_Won_And_Link_MemberId()
    {
        var lead = CreateLead();
        var memberId = Guid.NewGuid();

        var result = lead.ConvertToMember(memberId);

        Assert.True(result.IsSuccess);
        Assert.Equal(LeadStage.Won, lead.Stage);
        Assert.Equal(memberId, lead.ConvertedMemberId);
    }

    [Fact]
    public void ConvertToMember_Should_Fail_If_Already_Converted()
    {
        var lead = CreateLead();
        lead.ConvertToMember(Guid.NewGuid());

        var result = lead.ConvertToMember(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Lead.AlreadyConverted", result.Error.Code);
    }

    [Fact]
    public void MarkLost_Should_Fail_If_Already_Converted()
    {
        var lead = CreateLead();
        lead.ConvertToMember(Guid.NewGuid());

        var result = lead.MarkLost("too late");

        Assert.True(result.IsFailure);
        Assert.Equal("Lead.AlreadyConverted", result.Error.Code);
    }

    [Fact]
    public void AddFollowUp_Should_Add_It_To_The_Lead()
    {
        var lead = CreateLead();

        var followUp = lead.AddFollowUp(FollowUpType.Call, DateTimeOffset.UtcNow.AddDays(1), "Discuss pricing");

        Assert.Single(lead.FollowUps);
        Assert.False(followUp.IsCompleted);
    }

    [Fact]
    public void CompleteFollowUp_Should_Mark_It_Completed()
    {
        var lead = CreateLead();
        var followUp = lead.AddFollowUp(FollowUpType.Call, DateTimeOffset.UtcNow.AddDays(1), null);

        var result = lead.CompleteFollowUp(followUp.Id, DateTimeOffset.UtcNow, "Went well");

        Assert.True(result.IsSuccess);
        Assert.True(lead.FollowUps.Single().IsCompleted);
    }

    [Fact]
    public void CompleteFollowUp_Should_Fail_For_An_Unknown_Id()
    {
        var lead = CreateLead();

        var result = lead.CompleteFollowUp(Guid.NewGuid(), DateTimeOffset.UtcNow, null);

        Assert.True(result.IsFailure);
        Assert.Equal("Lead.FollowUpNotFound", result.Error.Code);
    }

    [Fact]
    public void AssignTo_Should_Set_The_Assigned_User()
    {
        var lead = CreateLead();
        var userId = Guid.NewGuid();

        lead.AssignTo(userId);

        Assert.Equal(userId, lead.AssignedToUserId);
    }
}
