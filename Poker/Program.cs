using System.Text.Json;

namespace Poker;

public class Program
{
    // 全カードのリスト
    public static List<Card> CardList = new();

    private static void Main(string[] args)
    {
        // 出力先のファイルパスを指定します
        const string filePath = "output.txt";

        // カードリストを作る
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            for (var i = 1; i <= 13; i++)
            {
                var card = new Card(suit, i);
                CardList.Add(card);
            }
        }

        // リザルト辞書
        var resultDic = new Dictionary<string, Dictionary<Hand, double>>();
        Console.WriteLine("start");

        // 確率全部出す
        for (var i = 0; i < CardList.Count; i++)
        {
            for (var j = i + 1; j < CardList.Count; j++)
            {
                // マイハンド
                var card1 = CardList[i];
                var card2 = CardList[j];
                var myHands = new HashSet<Card>
                {
                    card1,
                    card2
                };
                // 空のハンド辞書
                var dic = CreateHandDic();
                (_, _, resultDic) = LoopInCount(myHands, dic, 0, resultDic);
                Console.WriteLine($"card1:{card1.Name}, card2:{card2.Name}");
            }
        }

        var json = JsonSerializer.Serialize(resultDic);



        // StreamWriterを使用してファイルに書き込みます
        using (var writer = new StreamWriter(filePath))
        {
            writer.WriteLine(json);
        }

        // ファイルに書き込んだことを確認するためのメッセージをコンソールに表示
        Console.WriteLine($"Message written to {filePath}");
    }

    /// <summary>
    /// 再帰的にハンドの辞書を作っていく
    /// </summary>
    /// <param name="originCards"></param>
    /// <param name="originDic"></param>
    /// <param name="originCount"></param>
    /// <param name="resultDic"></param>
    /// <returns></returns>
    private static (Dictionary<Hand, long> dic, long count, Dictionary<string, Dictionary<Hand, double>> resultDic)
        LoopInCount(HashSet<Card> originCards, Dictionary<Hand, long> originDic, long originCount, Dictionary<string, Dictionary<Hand, double>> resultDic)
    {
        // 7枚から5枚を選んで確率計算。再帰終了地点。
        if (originCards.Count == 7)
        {
            // 7枚から5枚選んでできた役を全て保存しておく辞書
            var dic = CreateHandDic();
            // 確率用辞書
            var probabilityDic = CreateProbabilityDic();
            var originList = originCards.ToList();
            for (var i = 0; i < originList.Count; i++)
            {
                for (var j = i + 1; j < originList.Count; j++)
                {
                    var cards = new HashSet<Card>();
                    var card1 = originList[i];
                    var card2 = originList[j];
                    // card1とcard2を除いた5枚をsetに入れる
                    foreach (var card in originList.Where(card => card.Suit != card1.Suit || card.Num != card1.Num).Where(card => card.Suit != card2.Suit || card.Num != card2.Num))
                    {
                        cards.Add(card);
                    }
                    // ハンドを見て辞書更新
                    dic = CheckHand(cards, dic);
                }
            }
            // 役が一つでもあれば辞書に追加
            foreach (var hand in dic.Keys)
            {
                if (dic[hand] > 0)
                {
                    originDic[hand]++;
                    // リバーなので確率は全部1になる
                    probabilityDic[hand] = 1;
                }
            }
            // カウント加算しておく
            originCount++;
            // 確率を辞書に入れる
            var key = CreateProbabilityKey(originCards);
            resultDic[key] = probabilityDic;
            Console.WriteLine($"key:{key}");
            return (originDic, originCount, resultDic);
        }
        else
        {
            foreach (var card in CardList)
            {
                // 既に含まれているカードだったらcontinue
                if (originCards.Any(c => c.Suit == card.Suit && c.Num == card.Num))
                {
                    continue;
                }

                // originCards+cardのHashSetを作る
                var newSet = new HashSet<Card>(originCards) { card };
                (originDic, originCount, resultDic) = LoopInCount(newSet, originDic, originCount, resultDic);
            }
            // 確率用辞書
            var probabilityDic = CreateProbabilityDic();
            // 確率を計算する

            // 無いとは思うけどZeroDivision怖いので
            if (originCount != 0)
            {
                foreach (var hand in originDic.Keys)
                {
                    // 役の発生を母数で割って確率とします
                    probabilityDic[hand] = (double)originDic[hand] / originCount;
                }
            }

            // 確率を辞書に入れる
            var key = CreateProbabilityKey(originCards);
            resultDic[key] = probabilityDic;
            return (originDic, originCount, resultDic);
        }
    }


    /// <summary>
    /// 役の空辞書を作る
    /// </summary>
    /// <returns></returns>
    private static Dictionary<Hand, long> CreateHandDic()
    {
        var dic = new Dictionary<Hand, long>();
        foreach (Hand hand in Enum.GetValues(typeof(Hand)))
        {
            dic[hand] = 0;
        }
        return dic;
    }

    /// <summary>
    /// 空の確率辞書を作る
    /// </summary>
    /// <returns></returns>
    private static Dictionary<Hand, double> CreateProbabilityDic()
    {
        var dic = new Dictionary<Hand, double>();
        foreach (Hand hand in Enum.GetValues(typeof(Hand)))
        {
            dic[hand] = 0;
        }
        return dic;
    }

    /// <summary>
    /// 確率の辞書に入れるKeyを作る
    /// </summary>
    /// <param name="cardSet"></param>
    /// <returns></returns>
    private static string CreateProbabilityKey(HashSet<Card> cardSet)
    {
        var cards = cardSet.ToList();
        // スート順→数字順になるようにソート
        cards.Sort((card1, card2) =>
        {
            var suitComparison = card1.Suit.CompareTo(card2.Suit);
            return suitComparison == 0 ? card1.Num.CompareTo(card2.Num) : suitComparison;
        });
        // 順番に名前を足してキーを作る
        var key = string.Empty;
        foreach (var card in cards)
        {
            key += card.Name;
        }
        return key;
    }

    /// <summary>
    /// ハンドを見て出来てる役を辞書に入れていく
    /// </summary>
    /// <param name="cards"></param>
    /// <param name="dic"></param>
    /// <returns></returns>
    private static Dictionary<Hand, long> CheckHand(HashSet<Card> cards, Dictionary<Hand, long> dic)
    {
        // cardsの中身がなかったらそのまま返す
        if (cards.Count == 0) return dic;
        if (IsRoyal(cards)) dic[Hand.Royal]++;
        if (IsStraightFlush(cards)) dic[Hand.StraightFlush]++;
        if (IsQuads(cards)) dic[Hand.Quads]++;
        if (IsFullHouse(cards)) dic[Hand.FullHouse]++;
        if (IsFlush(cards)) dic[Hand.Flush]++;
        if (IsStraight(cards)) dic[Hand.Straight]++;
        if (IsThreeCard(cards)) dic[Hand.ThreeCard]++;
        if (IsTwoPair(cards)) dic[Hand.TwoPair]++;
        if (IsOnePair(cards)) dic[Hand.OnePair]++;
        dic[Hand.HighCard]++;
        return dic;
    }

    /// <summary>
    /// ロイヤルストレートフラッシュか？
    /// </summary>
    /// <param name="cards"></param>
    /// <returns></returns>
    private static bool IsRoyal(HashSet<Card> cards)
    {
        // フラッシュかつ10～Aのストレートならtrue
        return IsFlush(cards) && IsStraightTtoA(cards);
    }

    /// <summary>
    /// ストレートフラッシュか？
    /// </summary>
    /// <param name="cards"></param>
    /// <returns></returns>
    private static bool IsStraightFlush(HashSet<Card> cards)
    {
        // フラッシュかつストレートならtrue
        return IsFlush(cards) && IsStraight(cards);
    }

    /// <summary>
    /// フラッシュか？
    /// </summary>
    /// <param name="cards"></param>
    /// <returns></returns>
    private static bool IsFlush(HashSet<Card> cards)
    {
        var suit = cards.First().Suit;
        // 全部同じスートの時だけtrue
        return cards.All(card => suit == card.Suit);
    }

    /// <summary>
    /// ストレートか？
    /// </summary>
    /// <param name="cards"></param>
    /// <returns></returns>
    private static bool IsStraight(HashSet<Card> cards)
    {
        var ok = true;
        // Numの昇順にソートしたリストを作る
        var sortedList = cards.OrderBy(card => card.Num).ToList();
        // まずはA～Kを普通に判定
        var num = sortedList[0].Num;
        for (var i = 1; i < sortedList.Count; i++)
        {
            var card = sortedList[i];
            // 階段になってなかったらその時点でbreak
            if (num + 1 != card.Num)
            {
                ok = false;
                break;
            }
            num++;
        }

        // 普通のストレートor10～Aだったらtrue
        return ok || IsStraightTtoA(cards);
    }

    /// <summary>
    /// 10～Aのストレートか？
    /// </summary>
    /// <param name="cards"></param>
    /// <returns></returns>
    private static bool IsStraightTtoA(HashSet<Card> cards)
    {
        var ok = true;
        // Numの昇順にソートしたリストを作る
        var sortedList = cards.OrderBy(card => card.Num).ToList();
        var num = sortedList[0].Num;

        // Aが無かったらfalse
        if (num != 1) return false;
        // 疑似的にAを9にして、ストレートかどうかチェックしていく
        num = 9;
        for (var i = 1; i < sortedList.Count; i++)
        {
            var card = sortedList[i];
            // 階段になってなかったらその時点でbreak
            if (num + 1 != card.Num)
            {
                ok = false;
                break;
            }
            num++;
        }
        // ストレートだったらtrue
        return ok;
    }

    /// <summary>
    /// フォーカードか？
    /// </summary>
    /// <param name="cards"></param>
    /// <returns></returns>
    private static bool IsQuads(HashSet<Card> cards)
    {
        var numDic = new Dictionary<long, long>();
        // numDicに各数字がどれだけあるか入れる
        foreach (var card in cards)
        {
            if (!numDic.TryAdd(card.Num, 1))
            {
                numDic[card.Num]++;
            }
        }
        var max = numDic.Values.Max();
        // 一番多い要素数が4だったらフォーカード
        return max == 4;
    }

    /// <summary>
    /// フルハウスか？
    /// </summary>
    /// <param name="cards"></param>
    /// <returns></returns>
    private static bool IsFullHouse(HashSet<Card> cards)
    {
        var numSet = new HashSet<long>();
        foreach (var card in cards)
        {
            numSet.Add(card.Num);
        }

        // 数字が2種類かつスリーカードならtrue
        return numSet.Count == 2 && IsThreeCard(cards);
    }

    /// <summary>
    /// スリーカードか？
    /// </summary>
    /// <param name="cards"></param>
    /// <returns></returns>
    private static bool IsThreeCard(HashSet<Card> cards)
    {
        var numDic = new Dictionary<long, long>();
        // numDicに各数字がどれだけあるか入れる
        foreach (var card in cards)
        {
            if (!numDic.TryAdd(card.Num, 1))
            {
                numDic[card.Num]++;
            }
        }
        var max = numDic.Values.Max();
        // 一番多い要素数が3だったらスリーカード
        return max == 3;
    }

    /// <summary>
    /// ツーペアか？
    /// </summary>
    /// <param name="cards"></param>
    /// <returns></returns>
    private static bool IsTwoPair(HashSet<Card> cards)
    {
        var numDic = new Dictionary<long, long>();
        // numDicに各数字がどれだけあるか入れる
        foreach (var card in cards)
        {
            if (!numDic.TryAdd(card.Num, 1))
            {
                numDic[card.Num]++;
            }
        }
        // 要素数が2以上のものの数がペア数
        var pairCount = numDic.Values.Count(count => 2 <= count);
        // ペア数が2以上だったらtrue
        return 2 <= pairCount;
    }

    /// <summary>
    /// ワンペアか？
    /// </summary>
    /// <param name="cards"></param>
    /// <returns></returns>
    private static bool IsOnePair(HashSet<Card> cards)
    {
        var numDic = new Dictionary<long, long>();
        // numDicに各数字がどれだけあるか入れる
        foreach (var card in cards)
        {
            if (!numDic.TryAdd(card.Num, 1))
            {
                numDic[card.Num]++;
            }
        }
        // 要素数が2以上のものの数がペア数
        var pairCount = numDic.Values.Count(count => 2 <= count);
        // ペア数が1以上だったらtrue
        return 1 <= pairCount;
    }
}
