using TransferLab.Domain.Entities;

namespace TransferLab.UnitTests.Entities;

public class AccountTests
{
    [Fact]
    public void CanCreateAccount()
    {
        const string name = "Bob";
        const decimal openingBalance = 100m;
        
        var account = new Account(name, openingBalance);
        
        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal(name, account.Name);
        Assert.Equal(openingBalance, account.Balance);
    }
    [Fact]
    public void Constructor_WithNegativeOpeningBalance_ThrowsArgumentOutOfRangeException()
    {
        const string name = "Bob";
        const decimal openingBalance = -100m;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Account(name, openingBalance));

        Assert.Equal("openingBalance", exception.ParamName);
    }
    [Fact]
    public void Credit_WithValidAmount_IncreasesBalance()
    {
        var account = new Account("Bob", 100m);

        account.Credit(50m);
        
        Assert.Equal(150m, account.Balance);
    }
    [Fact]
    public void Credit_WithZeroAmount_ThrowsArgumentOutOfRangeException()
    {
        var account = new Account("Bob", 100m);
        
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => account.Credit(0m));
        
        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Debit_WithValidAmount_DecreasesBalance()
    {
        var account = new Account("Bob", 100m);
        
        account.Debit(50m);
        
        Assert.Equal(50m, account.Balance);
    }

    [Fact]
    public void Debit_WithAmountGreaterThanBalance_ThrowsInvalidOperationException()
    {
        var account = new Account("Bob", 100m);

        var exception = Assert.Throws<InvalidOperationException>(() => account.Debit(150m));
        
        Assert.Equal("Insufficient funds.", exception.Message);
        Assert.Equal(100m, account.Balance);
    }
}