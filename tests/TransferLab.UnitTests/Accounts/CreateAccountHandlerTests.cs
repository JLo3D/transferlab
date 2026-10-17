using TransferLab.Application.Accounts.CreateAccount;
using TransferLab.UnitTests.Fakes;

namespace TransferLab.UnitTests.Accounts;

public class CreateAccountHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidCommand_AddsAccountAndReturnsId()
    {
        var repository = new FakeAccountRepository();
        var unitOfWork = new FakeUnitOfWork();

        var handler = new CreateAccountHandler(
            repository,
            unitOfWork);

        var command = new CreateAccountCommand(
            "Bob",
            250m);

        var accountId = await handler.HandleAsync(command);

        var account = Assert.Single(repository.Accounts);

        Assert.Equal(accountId, account.Id);
        Assert.Equal("Bob", account.Name);
        Assert.Equal(250m, account.Balance);
        Assert.True(unitOfWork.SaveChangesCalled);
    } 
}