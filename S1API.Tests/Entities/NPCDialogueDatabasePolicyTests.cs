using S1API.Internal.Entities;

namespace S1API.Tests.Entities;

public sealed class NPCDialogueDatabasePolicyTests
{
    [Fact]
    public void EmployeeFallbackDoesNotReuseEmployeeDialogueDatabase()
    {
        Assert.False(NPCDataAccess.ShouldReuseSourceDialogueDatabase(sourceIsEmployee: true));
    }

    [Fact]
    public void NonEmployeeSourceRetainsItsDialogueDatabase()
    {
        Assert.True(NPCDataAccess.ShouldReuseSourceDialogueDatabase(sourceIsEmployee: false));
    }
}
