namespace SpeechWebGame.Tools
{
    public class Alternative
    {
        public double Confidence { get; set; }
        public List<VoskResult> Result { get; set; }
        public string Text { get; set; }
    }

    public class VoskResult
    {
        public double End { get; set; }
        public double Start { get; set; }
        public string Word { get; set; }
    }

    public class VoskFinalResult
    {
        public List<Alternative> Alternatives { get; set; }
    }
}
