namespace TransferLab.Domain.Entities;

public class Account
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public decimal Balance { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private Account()
    {
        Name = null!;
    }
    
    public Account(string name, decimal openingBalance)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name is required.", nameof(name));

        if (openingBalance < 0)
            throw new ArgumentOutOfRangeException(
                nameof(openingBalance),
                "Opening balance cannot be negative.");

        Id = Guid.NewGuid();
        Name = name;
        Balance = openingBalance;
    }
    public void Credit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Credit amount must be greater than zero.");

        Balance += amount;
    }

    public void Debit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Debit amount must be greater than zero.");
        if (amount > Balance)
            throw new InvalidOperationException(
                    "Insufficient funds.");
        Balance -= amount;
    }
}
