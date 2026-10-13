using System.Security.Claims;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ExpensesController(AppDbContext db) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private IQueryable<Expense> MyExpenses => db.Expenses.Where(e => e.UserId == UserId);

    [HttpGet]                                     // GET /api/expenses
    public async Task<IActionResult> GetAll() =>
        Ok(await MyExpenses
            .OrderByDescending(e => e.SpentOn).ThenByDescending(e => e.Id)
            .Select(e => new ExpenseResponse(e.Id, e.Title, e.Amount, e.SpentOn, e.CategoryId, e.Category!.Name))
            .ToListAsync());

    [HttpGet("{id}")]                             // GET /api/expenses/5
    public async Task<IActionResult> GetById(int id)
    {
        var expense = await MyExpenses
            .Where(e => e.Id == id)
            .Select(e => new ExpenseResponse(e.Id, e.Title, e.Amount, e.SpentOn, e.CategoryId, e.Category!.Name))
            .FirstOrDefaultAsync();
        return expense is null ? NotFound() : Ok(expense);
    }

    [HttpGet("summary")]                          // GET /api/expenses/summary
    public async Task<IActionResult> Summary() =>
        Ok(await MyExpenses
            .GroupBy(e => e.Category!.Name)
            .OrderByDescending(g => g.Sum(e => e.Amount))
            .Select(g => new CategoryTotal(g.Key, g.Sum(e => e.Amount)))
            .ToListAsync());

    [HttpPost]                                    // POST /api/expenses
    public async Task<IActionResult> Create(ExpenseRequest request)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            return BadRequest($"Category {request.CategoryId} does not exist.");

        var expense = new Expense
        {
            UserId = UserId,
            Title = request.Title,
            Amount = request.Amount,
            SpentOn = request.SpentOn,
            CategoryId = request.CategoryId
        };
        db.Expenses.Add(expense);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = expense.Id }, new { expense.Id });
    }

    [HttpPut("{id}")]                             // PUT /api/expenses/5
    public async Task<IActionResult> Update(int id, ExpenseRequest request)
    {
        var expense = await MyExpenses.FirstOrDefaultAsync(e => e.Id == id);
        if (expense is null) return NotFound();

        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            return BadRequest($"Category {request.CategoryId} does not exist.");

        expense.Title = request.Title;
        expense.Amount = request.Amount;
        expense.SpentOn = request.SpentOn;
        expense.CategoryId = request.CategoryId;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]                          // DELETE /api/expenses/5
    public async Task<IActionResult> Delete(int id)
    {
        var expense = await MyExpenses.FirstOrDefaultAsync(e => e.Id == id);
        if (expense is null) return NotFound();

        db.Expenses.Remove(expense);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
