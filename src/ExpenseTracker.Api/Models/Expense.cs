namespace ExpenseTracker.Api.Models;

public class Expense
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public int CategoryId { get; set; }
    public string Title { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly SpentOn { get; set; }
    public DateTime CreatedAt { get; set; }

    public Category? Category { get; set; }   // navigation property (the FK)
}
