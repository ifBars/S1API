using S1API.Entities;
using S1API.Internal.Entities;

namespace S1API.Tests.NPCs;

public sealed class NPCPrefabDataTests
{
    [Fact]
    public void ADonorThatIsAlreadyADealerOrSupplierGetsDataOfItsOwnKind()
    {
        Assert.Equal(NpcRootRole.Dealer, NPC.DataRoleForComponent(isDealer: true, isSupplier: false));
        Assert.Equal(NpcRootRole.Supplier, NPC.DataRoleForComponent(isDealer: false, isSupplier: true));
    }

    [Fact]
    public void APlainDonorGetsPlainDataWhichTheRolesOwnDataReplacesLater()
    {
        Assert.Equal(NpcRootRole.Plain, NPC.DataRoleForComponent(isDealer: false, isSupplier: false));
    }
}
