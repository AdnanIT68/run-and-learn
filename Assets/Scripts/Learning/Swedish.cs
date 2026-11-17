using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FootballRunner.Learning
{
    /// <summary>
    /// Swedish menus for children in Sweden who learn Bosnian. The code writes every menu
    /// text in Bosnian, English and German (LearningProgress.Ui); Swedish is looked up by
    /// the English text in Resources/Locale/sv.json, where {0}, {1} stand for numbers or
    /// names the game fills in. A text missing from the table stays in English.
    /// </summary>
    public static class Swedish
    {
        [Serializable]
        private class Entry
        {
            public string en;
            public string sv;
        }

        [Serializable]
        private class Table
        {
            public Entry[] entries;
        }

        private static readonly Regex Hole = new Regex(@"\{(\d+)\}");
        private static Dictionary<string, string> _plain;
        private static List<(Regex pattern, string sv, int[] order)> _templates;

        /// <summary>The Swedish for an English menu text, or the English itself when none is known.</summary>
        public static string Translate(string english)
        {
            if (string.IsNullOrEmpty(english))
            {
                return english;
            }

            Load();

            if (_plain.TryGetValue(english, out string swedish))
            {
                return swedish;
            }

            foreach ((Regex pattern, string sv, int[] order) in _templates)
            {
                Match match = pattern.Match(english);

                if (!match.Success)
                {
                    continue;
                }

                // Each hole takes what the game put in that place in English.
                return Hole.Replace(sv, m =>
                {
                    int index = int.Parse(m.Groups[1].Value);
                    int group = Array.IndexOf(order, index) + 1;
                    return group > 0 ? match.Groups[group].Value : m.Value;
                });
            }

            return english;
        }

        /// <summary>True when the table has this English text (as is, or as a template with holes).</summary>
        public static bool Knows(string englishKey)
        {
            Load();
            return _plain.ContainsKey(englishKey) || _templates.Exists(t => t.pattern.IsMatch(englishKey));
        }

        private static void Load()
        {
            if (_plain != null)
            {
                return;
            }

            _plain = new Dictionary<string, string>();
            _templates = new List<(Regex, string, int[])>();
            var asset = Resources.Load<TextAsset>("Locale/sv");
            Table table = asset != null ? JsonUtility.FromJson<Table>(asset.text) : null;

            foreach (Entry entry in table?.entries ?? Array.Empty<Entry>())
            {
                if (string.IsNullOrEmpty(entry.en))
                {
                    continue;
                }

                if (Hole.IsMatch(entry.en))
                {
                    string pattern = PatternFor(entry.en, out int[] order);
                    _templates.Add((new Regex(pattern, RegexOptions.Singleline), entry.sv, order));
                }
                else
                {
                    _plain[entry.en] = entry.sv;
                }
            }
        }

        /// <summary>"Catch {0} words" becomes ^Catch (.+?) words$; order lists the hole numbers left to right.</summary>
        private static string PatternFor(string template, out int[] order)
        {
            var holes = new List<int>();
            string pattern = "^" + Hole.Replace(Regex.Escape(template).Replace(@"\{", "{"), m =>
            {
                holes.Add(int.Parse(m.Groups[1].Value));
                return "(.+?)";
            }) + "$";
            order = holes.ToArray();
            return pattern;
        }
    }
}
