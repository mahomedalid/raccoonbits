namespace RaccoonBitsCore
{
    public class Word
    {
        public string? Tag { get; set; }
        public int Score { get; set; }

        public Word(string word, int score)
        {
            this.Tag = word;
            this.Score = score;
        }
    }
}