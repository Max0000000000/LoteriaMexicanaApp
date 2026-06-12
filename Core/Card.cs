namespace LoteriaMexicanaApp.Core
{
    public class Card
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public string Riddle { get; set; } = string.Empty;
        public string BackgroundColorCode { get; set; } = "#FFFFFF";

        public Card() { }

        public Card(int id, string name, string emoji, string riddle, string color)
        {
            Id = id;
            Name = name;
            Emoji = emoji;
            Riddle = riddle;
            BackgroundColorCode = color;
        }

        public override string ToString()
        {
            return $"#{Id} - {Name}";
        }
    }
}
