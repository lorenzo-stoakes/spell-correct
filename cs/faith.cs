using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;

static class Faithful
{
    private static IEnumerable<string> words(string text)
    {
        return Regex.Matches(text.ToLower(), "[a-z]+")
                    .Cast<Match>()
                    .Select(m => m.Value);
    }

    private static Func<string, int?> train(IEnumerable<string> features)
    {
        var dict = features.GroupBy(f => f)
                           .ToDictionary(g => g.Key, g => g.Count());

        return f => dict.ContainsKey(f) ? dict[f] : (int?)null;
    }

    private static Func<string, int?> NWORDS;

    private const string alphabet = "abcdefghijklmnopqrstuvwxyz";

    private static IEnumerable<string> edits1(string word)
    {
        var splits =     from i in Enumerable.Range(0, word.Length)
                         select new {a = word.Substring(0, i), b = word.Substring(i)};
        var deletes    = from s in splits
                         where s.b != "" // we know it can't be null
                         select s.a + s.b.Substring(1);
        var transposes = from s in splits
                         where s.b.Length > 1
                         select s.a + s.b[1] + s.b[0] + s.b.Substring(2);
        var replaces   = from s in splits
                         from c in alphabet
                         where s.b != ""
                         select s.a + c + s.b.Substring(1);
        var inserts    = from s in splits
                         from c in alphabet
                         select s.a + c + s.b;

        return deletes
        .Union(transposes) // union translates into a set
        .Union(replaces)
        .Union(inserts);
    }

    private static IEnumerable<string> known_edits2(string word)
    {
        return (from e1 in edits1(word)
                from e2 in edits1(e1)
                where NWORDS(e2) != null
                select e2)
               .Distinct();
    }

    private static IEnumerable<string> known(IEnumerable<string> words)
    {
        return words.Where(w => NWORDS(w) != null);
    }

    private static string correct(string word)
    {
        var candidates =
            new[] { known(new[] {word}),
                    known(edits1(word)),
                    known_edits2(word),
                    new[] {word} }
                  .First(s => s.Any());

        return candidates.OrderByDescending(c => NWORDS(c) ?? 1).First();
    }

    private static void ReadFromStdIn()
    {
        string word;
        while (!string.IsNullOrEmpty(word = (Console.ReadLine() ?? "").Trim()))
        {
            Console.WriteLine(correct(word));
        }        
    }

    public static void Main(string[] args)
    {
        if (!File.Exists("big.txt"))
        {
            Console.Error.WriteLine("Cannot find big.txt.");
            return;
        }

        NWORDS = train(words(File.ReadAllText("big.txt")));

        ReadFromStdIn();
    }
}