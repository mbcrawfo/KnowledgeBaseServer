using System.Linq;

namespace KnowledgeBaseServer;

public static class StringExtensions
{
    public static string RemovePunctuation(this string str) => new(str.Where(c => !char.IsPunctuation(c)).ToArray());
}
