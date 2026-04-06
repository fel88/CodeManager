namespace CodeManager
{
    public static class Extensions
    {
        public static int CountLines(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            int count = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n') count++;
            }
            return count;
        }
    }
}
