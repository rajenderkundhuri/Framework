using Framework.CodeGen.Analysis.Models;

namespace Framework.CodeGen.Analysis;

/// <summary>
/// Interface for entity analyzers
/// </summary>
public interface IEntityAnalyzer
{
    /// <summary>
    /// Analyzes an entity and returns its metadata
    /// </summary>
    EntityMetadata Analyze(string entityNameOrPath);
}
