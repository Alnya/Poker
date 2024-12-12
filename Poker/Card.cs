namespace Poker;

public class Card
{
    public Suit Suit { get; set; }
    public long Num { get; set; }
    public string Name { get; set; } = string.Empty;

    public Card(Suit suit, long num)
    {
        Suit = suit;
        Num = num;
        Name = $"{Suit.ToString()}{Num}";
    }
}


public enum Suit
{
    C = 0,
    D = 1,
    H = 2,
    S = 3
}

public enum Hand
{
    Royal = 0,
    StraightFlush = 1,
    Quads = 2,
    FullHouse = 3,
    Flush = 4,
    Straight = 5,
    ThreeCard = 6,
    TwoPair = 7,
    OnePair = 8,
    HighCard = 9
}