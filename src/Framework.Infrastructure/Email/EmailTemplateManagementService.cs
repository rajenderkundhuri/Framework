using Framework.Application.Common.Models;
using Framework.Application.Email;
using Framework.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using DomainEmailTemplate = Framework.Domain.Email.EmailTemplate;
using DomainEmailTemplateType = Framework.Domain.Email.EmailTemplateType;
using DomainEmailLog = Framework.Domain.Email.EmailLog;
using DomainEmailStatus = Framework.Domain.Email.EmailStatus;

namespace Framework.Infrastructure.Email;

/// <summary>
/// Implementation of the email template management service
/// </summary>
public class EmailTemplateManagementService : IEmailTemplateManagementService
{
    private readonly ApplicationDbContext _context;

    public EmailTemplateManagementService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedList<EmailTemplateResponse>> GetTemplatesAsync(
        EmailTemplateListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EmailTemplates.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(t =>
                t.Name.Contains(request.SearchTerm) ||
                t.Subject.Contains(request.SearchTerm) ||
                (t.Description != null && t.Description.Contains(request.SearchTerm)));
        }

        if (request.Type.HasValue)
        {
            query = query.Where(t => t.Type == request.Type.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(t => t.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrEmpty(request.LanguageCode))
        {
            query = query.Where(t => t.LanguageCode == request.LanguageCode);
        }

        // Apply sorting
        query = request.SortBy.ToLowerInvariant() switch
        {
            "type" => request.SortDescending ? query.OrderByDescending(t => t.Type) : query.OrderBy(t => t.Type),
            "createdat" => request.SortDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
            _ => request.SortDescending ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => MapToResponse(t))
            .ToListAsync(cancellationToken);

        return new PagedList<EmailTemplateResponse>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<EmailTemplateResponse?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        return template != null ? MapToResponse(template) : null;
    }

    public async Task<EmailTemplateResponse?> GetByNameAsync(
        string name,
        string? languageCode = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EmailTemplates
            .Where(t => t.Name == name && t.IsActive);

        if (!string.IsNullOrEmpty(languageCode))
        {
            // Try to find template with specific language
            var template = await query
                .FirstOrDefaultAsync(t => t.LanguageCode == languageCode, cancellationToken);

            if (template != null)
            {
                return MapToResponse(template);
            }
        }

        // Fall back to default (null language code)
        var defaultTemplate = await query
            .FirstOrDefaultAsync(t => t.LanguageCode == null, cancellationToken);

        return defaultTemplate != null ? MapToResponse(defaultTemplate) : null;
    }

    public async Task<Result<Guid>> CreateAsync(CreateEmailTemplateRequest request, CancellationToken cancellationToken = default)
    {
        // Check for duplicate
        var exists = await _context.EmailTemplates
            .AnyAsync(t => t.Name == request.Name && t.LanguageCode == request.LanguageCode, cancellationToken);

        if (exists)
        {
            return Result<Guid>.Failure($"Template with name '{request.Name}' and language '{request.LanguageCode ?? "default"}' already exists");
        }

        var template = new DomainEmailTemplate(
            Guid.NewGuid(),
            request.Name,
            request.Subject,
            request.Body,
            request.Type);

        if (!string.IsNullOrEmpty(request.Description))
        {
            template.Update(request.Subject, request.Body, request.Description);
        }

        if (!string.IsNullOrEmpty(request.LanguageCode))
        {
            template.SetLanguage(request.LanguageCode);
        }

        _context.EmailTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(template.Id);
    }

    public async Task<Result> UpdateAsync(Guid templateId, UpdateEmailTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template == null)
        {
            return Result.Failure("Template not found");
        }

        var subject = request.Subject ?? template.Subject;
        var body = request.Body ?? template.Body;
        var description = request.Description ?? template.Description;

        template.Update(subject, body, description);

        if (request.LanguageCode != null)
        {
            template.SetLanguage(request.LanguageCode);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template == null)
        {
            return Result.Failure("Template not found");
        }

        _context.EmailTemplates.Remove(template);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ActivateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template == null)
        {
            return Result.Failure("Template not found");
        }

        template.Activate();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template == null)
        {
            return Result.Failure("Template not found");
        }

        template.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<PagedList<EmailLogResponse>> GetEmailLogsAsync(
        EmailLogListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EmailLogs.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(l =>
                l.To.Contains(request.SearchTerm) ||
                l.Subject.Contains(request.SearchTerm));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(l => l.Status == request.Status.Value);
        }

        if (request.TemplateId.HasValue)
        {
            query = query.Where(l => l.TemplateId == request.TemplateId.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(l => l.SentAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(l => l.SentAt <= request.ToDate.Value);
        }

        // Apply sorting
        query = request.SortDescending
            ? query.OrderByDescending(l => l.SentAt)
            : query.OrderBy(l => l.SentAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new EmailLogResponse
            {
                Id = l.Id,
                To = l.To,
                Cc = l.Cc,
                Bcc = l.Bcc,
                Subject = l.Subject,
                Body = l.Body,
                TemplateId = l.TemplateId,
                Status = l.Status,
                ErrorMessage = l.ErrorMessage,
                SentAt = l.SentAt,
                RetryCount = l.RetryCount
            })
            .ToListAsync(cancellationToken);

        return new PagedList<EmailLogResponse>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<EmailLogResponse?> GetEmailLogByIdAsync(Guid logId, CancellationToken cancellationToken = default)
    {
        var log = await _context.EmailLogs
            .FirstOrDefaultAsync(l => l.Id == logId, cancellationToken);

        if (log == null) return null;

        return new EmailLogResponse
        {
            Id = log.Id,
            To = log.To,
            Cc = log.Cc,
            Bcc = log.Bcc,
            Subject = log.Subject,
            Body = log.Body,
            TemplateId = log.TemplateId,
            Status = log.Status,
            ErrorMessage = log.ErrorMessage,
            SentAt = log.SentAt,
            RetryCount = log.RetryCount
        };
    }

    private static EmailTemplateResponse MapToResponse(DomainEmailTemplate template)
    {
        return new EmailTemplateResponse
        {
            Id = template.Id,
            Name = template.Name,
            Subject = template.Subject,
            Body = template.Body,
            Type = template.Type,
            IsActive = template.IsActive,
            Description = template.Description,
            LanguageCode = template.LanguageCode,
            CreatedAt = template.CreatedAt,
            CreatedBy = template.CreatedBy,
            LastModifiedAt = template.LastModifiedAt,
            LastModifiedBy = template.LastModifiedBy
        };
    }
}
