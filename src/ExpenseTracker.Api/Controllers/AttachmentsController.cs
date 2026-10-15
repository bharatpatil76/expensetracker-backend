using System.Security.Claims;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Models;
using ExpenseTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/expenses/{expenseId:int}/attachments")]
public class AttachmentsController(AppDbContext db, BlobFileStore files) : ControllerBase
{
    private const long MaxBytes = 5 * 1024 * 1024;   // 5 MB

    // Allowed content types per kind -> file extension used in the blob name
    private static readonly Dictionary<string, Dictionary<string, string>> Allowed = new()
    {
        [AttachmentKind.Receipt] = new()
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["application/pdf"] = ".pdf",
        },
        [AttachmentKind.VoiceNote] = new()
        {
            ["audio/webm"] = ".webm",
            ["audio/ogg"] = ".ogg",
            ["audio/mp4"] = ".m4a",
            ["audio/mpeg"] = ".mp3",
        },
    };

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private Task<bool> OwnsExpense(int expenseId) =>
        db.Expenses.AnyAsync(e => e.Id == expenseId && e.UserId == UserId);

    [HttpGet]                                     // GET /api/expenses/5/attachments
    public async Task<IActionResult> GetAll(int expenseId)
    {
        if (!await OwnsExpense(expenseId)) return NotFound();

        var attachments = await db.ExpenseAttachments.AsNoTracking()
            .Where(a => a.ExpenseId == expenseId)
            .ToListAsync();

        var result = new List<AttachmentResponse>();
        foreach (var a in attachments)
        {
            var url = await files.GetReadUrlAsync(a.BlobName, TimeSpan.FromMinutes(10));
            result.Add(new AttachmentResponse(a.Kind, a.ContentType, a.SizeBytes, a.UploadedAt, url));
        }
        return Ok(result);
    }

    [HttpPost("receipt")]                         // POST /api/expenses/5/attachments/receipt
    [RequestSizeLimit(MaxBytes + 64 * 1024)]
    public Task<IActionResult> UploadReceipt(int expenseId, IFormFile file) =>
        Upload(expenseId, AttachmentKind.Receipt, file);

    [HttpPost("voice-note")]                      // POST /api/expenses/5/attachments/voice-note
    [RequestSizeLimit(MaxBytes + 64 * 1024)]
    public Task<IActionResult> UploadVoiceNote(int expenseId, IFormFile file) =>
        Upload(expenseId, AttachmentKind.VoiceNote, file);

    [HttpDelete("{kind}")]                        // DELETE /api/expenses/5/attachments/Receipt
    public async Task<IActionResult> Delete(int expenseId, string kind)
    {
        if (!await OwnsExpense(expenseId)) return NotFound();

        var attachment = await db.ExpenseAttachments.FirstOrDefaultAsync(a => a.ExpenseId == expenseId && a.Kind == kind);
        if (attachment is null) return NotFound();

        db.ExpenseAttachments.Remove(attachment);
        await db.SaveChangesAsync();
        await files.DeleteAsync(attachment.BlobName);
        return NoContent();
    }

    private async Task<IActionResult> Upload(int expenseId, string kind, IFormFile file)
    {
        if (!await OwnsExpense(expenseId)) return NotFound();

        if (file is null || file.Length == 0) return BadRequest("File is empty.");
        if (file.Length > MaxBytes) return BadRequest("File is larger than 5 MB.");

        // Browsers send e.g. "audio/webm;codecs=opus" - compare only the base type
        var contentType = (file.ContentType ?? "").Split(';')[0].Trim().ToLowerInvariant();
        if (!Allowed[kind].TryGetValue(contentType, out var extension))
            return BadRequest($"File type '{contentType}' is not allowed for {kind}.");

        // {user}/{expenseId}/receipt.jpg - '|' in the Auth0 user id is not URL-friendly
        var userFolder = UserId.Replace('|', '_');
        var fileName = kind == AttachmentKind.Receipt ? "receipt" : "voice-note";
        var blobName = $"{userFolder}/{expenseId}/{fileName}{extension}";

        await using (var stream = file.OpenReadStream())
        {
            await files.UploadAsync(blobName, stream, contentType);
        }

        // One attachment per kind: replace the existing one
        var existing = await db.ExpenseAttachments.FirstOrDefaultAsync(a => a.ExpenseId == expenseId && a.Kind == kind);
        if (existing is null)
        {
            db.ExpenseAttachments.Add(new ExpenseAttachment
            {
                ExpenseId = expenseId,
                Kind = kind,
                BlobName = blobName,
                ContentType = contentType,
                SizeBytes = file.Length
            });
        }
        else
        {
            if (existing.BlobName != blobName)
                await files.DeleteAsync(existing.BlobName);   // e.g. .png replaced by .jpg

            existing.BlobName = blobName;
            existing.ContentType = contentType;
            existing.SizeBytes = file.Length;
            existing.UploadedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();

        return NoContent();
    }
}
