using System.Collections.Generic;

namespace Challenge;

public static class Solution
{
    public static bool IsBalanced(string text)
    {
        var openers = new Stack<char>();

        foreach (var character in text)
        {
            switch (character)
            {
                case '(':
                case '[':
                case '{':
                    openers.Push(character);
                    break;
                case ')':
                    if (openers.Count == 0 || openers.Pop() != '(')
                    {
                        return false;
                    }
                    break;
                case ']':
                    if (openers.Count == 0 || openers.Pop() != '[')
                    {
                        return false;
                    }
                    break;
                case '}':
                    if (openers.Count == 0 || openers.Pop() != '{')
                    {
                        return false;
                    }
                    break;
            }
        }

        return openers.Count == 0;
    }
}
