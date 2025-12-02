using System.Text.RegularExpressions;
using Framework.Application.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.Email;

/// <summary>
/// File-based email template service
/// Loads templates from disk and renders them with model data
/// </summary>
public partial class FileEmailTemplateService : IEmailTemplateService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<FileEmailTemplateService> _logger;
    private readonly Dictionary<string, EmailTemplate> _templateCache = new();

    public FileEmailTemplateService(
        IOptions<EmailSettings> settings,
        ILogger<FileEmailTemplateService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<RenderedEmail> RenderAsync<TModel>(
        string templateName,
        TModel model,
        CancellationToken cancellationToken = default) where TModel : class
    {
        var template = await LoadTemplateAsync(templateName, cancellationToken);

        if (template == null)
        {
            throw new InvalidOperationException($"Template '{templateName}' not found");
        }

        var subject = RenderContent(template.Subject, model);
        var htmlBody = !string.IsNullOrEmpty(template.HtmlBody)
            ? RenderContent(template.HtmlBody, model)
            : null;
        var textBody = !string.IsNullOrEmpty(template.TextBody)
            ? RenderContent(template.TextBody, model)
            : null;

        return new RenderedEmail
        {
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody,
            TemplateName = templateName
        };
    }

    public bool TemplateExists(string templateName)
    {
        var templatePath = GetTemplatePath(templateName);
        return Directory.Exists(templatePath) || File.Exists($"{templatePath}.html");
    }

    public IEnumerable<string> GetTemplateNames()
    {
        if (!Directory.Exists(_settings.TemplatesPath))
            return Enumerable.Empty<string>();

        var templates = new List<string>();

        // Get directory-based templates
        foreach (var dir in Directory.GetDirectories(_settings.TemplatesPath))
        {
            templates.Add(Path.GetFileName(dir));
        }

        // Get single-file templates
        foreach (var file in Directory.GetFiles(_settings.TemplatesPath, "*.html"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (!templates.Contains(name))
                templates.Add(name);
        }

        return templates.OrderBy(t => t);
    }

    private async Task<EmailTemplate?> LoadTemplateAsync(
        string templateName,
        CancellationToken cancellationToken)
    {
        // Check cache first
        if (_templateCache.TryGetValue(templateName, out var cached))
        {
            return cached;
        }

        var template = new EmailTemplate { Name = templateName };
        var templatePath = GetTemplatePath(templateName);

        // Try directory-based template (with separate files)
        if (Directory.Exists(templatePath))
        {
            var subjectFile = Path.Combine(templatePath, "subject.txt");
            var htmlFile = Path.Combine(templatePath, "body.html");
            var textFile = Path.Combine(templatePath, "body.txt");

            if (File.Exists(subjectFile))
                template.Subject = await File.ReadAllTextAsync(subjectFile, cancellationToken);

            if (File.Exists(htmlFile))
                template.HtmlBody = await File.ReadAllTextAsync(htmlFile, cancellationToken);

            if (File.Exists(textFile))
                template.TextBody = await File.ReadAllTextAsync(textFile, cancellationToken);
        }
        // Try single HTML file with frontmatter
        else if (File.Exists($"{templatePath}.html"))
        {
            var content = await File.ReadAllTextAsync($"{templatePath}.html", cancellationToken);
            ParseTemplateWithFrontmatter(content, template);
        }
        else
        {
            _logger.LogWarning("Template not found: {TemplateName}", templateName);
            return null;
        }

        // Validate template has required fields
        if (string.IsNullOrEmpty(template.Subject))
        {
            template.Subject = templateName; // Fallback to template name
        }

        _templateCache[templateName] = template;
        return template;
    }

    private void ParseTemplateWithFrontmatter(string content, EmailTemplate template)
    {
        // Check for YAML-like frontmatter
        if (content.StartsWith("---"))
        {
            var endIndex = content.IndexOf("---", 3);
            if (endIndex > 0)
            {
                var frontmatter = content[3..endIndex].Trim();
                var body = content[(endIndex + 3)..].Trim();

                // Parse simple frontmatter
                foreach (var line in frontmatter.Split('\n'))
                {
                    var colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        var key = line[..colonIndex].Trim().ToLowerInvariant();
                        var value = line[(colonIndex + 1)..].Trim();

                        switch (key)
                        {
                            case "subject":
                                template.Subject = value;
                                break;
                            case "layout":
                                template.Layout = value;
                                break;
                        }
                    }
                }

                template.HtmlBody = body;
            }
        }
        else
        {
            template.HtmlBody = content;
        }
    }

    private string RenderContent<TModel>(string content, TModel model) where TModel : class
    {
        if (string.IsNullOrEmpty(content))
            return content;

        var result = content;

        // Get all properties from the model
        var properties = typeof(TModel).GetProperties();

        foreach (var prop in properties)
        {
            var value = prop.GetValue(model)?.ToString() ?? string.Empty;

            // Replace {{PropertyName}} placeholders
            result = result.Replace($"{{{{{prop.Name}}}}}", value);

            // Also support {{ PropertyName }} with spaces
            result = result.Replace($"{{{{ {prop.Name} }}}}", value);
        }

        // Handle conditional blocks: {{#if PropertyName}}...{{/if}}
        result = ProcessConditionals(result, model);

        return result;
    }

    private string ProcessConditionals<TModel>(string content, TModel model) where TModel : class
    {
        var result = content;
        var properties = typeof(TModel).GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(model));

        // Simple {{#if PropertyName}}...{{/if}} handling
        var ifPattern = IfBlockPattern();
        var matches = ifPattern.Matches(result);

        foreach (Match match in matches)
        {
            var propName = match.Groups[1].Value;
            var innerContent = match.Groups[2].Value;

            if (properties.TryGetValue(propName, out var value))
            {
                var shouldShow = value switch
                {
                    bool b => b,
                    string s => !string.IsNullOrEmpty(s),
                    null => false,
                    _ => true
                };

                result = result.Replace(match.Value, shouldShow ? innerContent : string.Empty);
            }
            else
            {
                result = result.Replace(match.Value, string.Empty);
            }
        }

        return result;
    }

    private string GetTemplatePath(string templateName)
    {
        return Path.Combine(_settings.TemplatesPath, templateName);
    }

    /// <summary>
    /// Clears the template cache
    /// </summary>
    public void ClearCache()
    {
        _templateCache.Clear();
    }

    [GeneratedRegex(@"\{\{#if\s+(\w+)\}\}(.*?)\{\{/if\}\}", RegexOptions.Singleline)]
    private static partial Regex IfBlockPattern();
}

/// <summary>
/// Internal template representation
/// </summary>
internal class EmailTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
    public string? TextBody { get; set; }
    public string? Layout { get; set; }
}
