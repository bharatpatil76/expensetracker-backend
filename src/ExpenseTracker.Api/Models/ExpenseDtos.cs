using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Models;

// What the client sends - no Id, UserId or CreatedAt
public record ExpenseRequest(
    [Required, StringLength(200)] string Title,
    [Range(0.01, 99_999_999.99)] decimal Amount,
    DateOnly SpentOn,
    int CategoryId);

// What the client receives
public record ExpenseResponse(
    int Id,
    string Title,
    decimal Amount,
    DateOnly SpentOn,
    int CategoryId,
    string CategoryName,
    bool HasReceipt,
    bool HasVoiceNote);

public record CategoryTotal(string CategoryName, decimal Total);
