using System;
using System.Collections.Generic;

namespace TypeGameMod
{
    internal sealed class ArticleProgress
    {
        internal readonly string Text;
        readonly List<int> starts = new List<int>(), ends = new List<int>();
        internal int Position;
        int cutoff;
        uint cycle;
        internal ArticleProgress(string text, int position, uint skillCycle)
        {
            Text = text;
            cycle = skillCycle;
            Position = Math.Max(0, Math.Min(text.Length, position));
            cutoff = Position;
            for (int i = 0; i < text.Length;)
            {
                if (!Letter(text[i]))
                {
                    i++;
                    continue;
                }
                int start = i++;
                while (i < text.Length && (Letter(text[i]) || ((text[i] == '\'' || text[i] == '-') &&
                                                               i + 1 < text.Length && Letter(text[i + 1]))))
                    i++;
                starts.Add(start);
                ends.Add(i);
            }
        }

        static bool Letter(char c)
        {
            return c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z';
        }

        internal int Observe(int position, uint skillCycle, bool charging)
        {
            position = Math.Max(0, Math.Min(Text.Length, position));
            if (skillCycle != cycle)
            {
                cycle = skillCycle;
                cutoff = Position;
            }
            if (position < Position)
            {
                Position = 0;
                cutoff = 0;
            }
            int count = 0;
            if (charging)
            {
                for (int i = 0; i < ends.Count; i++)
                    if (starts[i] >= cutoff && ends[i] > Position && ends[i] <= position)
                        count++;
            }
            else
                cutoff = position;
            Position = position;
            return count;
        }
    }
}
