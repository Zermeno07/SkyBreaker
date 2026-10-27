using System.Collections.Generic;
using UnityEngine;

// Pixel art del juego definido como texto: cada carácter es un píxel de la paleta.
// Los sprites se construyen al iniciar, con filtro Point para que se vean nítidos.
public static class PixelArt
{
    static readonly Dictionary<char, Color32> Palette = new Dictionary<char, Color32>
    {
        { 'K', new Color32(0x1a, 0x1c, 0x2c, 0xff) },
        { 'k', new Color32(0x33, 0x3c, 0x57, 0xff) },
        { 'B', new Color32(0x3b, 0x8b, 0xd8, 0xff) },
        { 'b', new Color32(0x2a, 0x5e, 0xa8, 0xff) },
        { 'L', new Color32(0x7c, 0xca, 0xff, 0xff) },
        { 'W', new Color32(0xff, 0xff, 0xff, 0xff) },
        { 'C', new Color32(0xff, 0xf4, 0xdc, 0xff) },
        { 'c', new Color32(0xe8, 0xd3, 0xb0, 0xff) },
        { 'Y', new Color32(0xff, 0xcd, 0x38, 0xff) },
        { 'O', new Color32(0xf0, 0x8c, 0x1e, 0xff) },
        { 'R', new Color32(0xe8, 0x3b, 0x3b, 0xff) },
        { 'r', new Color32(0xa3, 0x21, 0x35, 0xff) },
        { 'P', new Color32(0xff, 0x8f, 0xb1, 0xff) },
        { 'G', new Color32(0xb8, 0xc8, 0xd8, 0xff) },
        { 'g', new Color32(0x7a, 0x8c, 0xa6, 0xff) },
        { 'D', new Color32(0x4a, 0x54, 0x70, 0xff) },
        { 'E', new Color32(0x38, 0xb7, 0x64, 0xff) },
        { 'e', new Color32(0x25, 0x71, 0x79, 0xff) },
        { 'V', new Color32(0xb5, 0x5d, 0xd6, 0xff) },
        { 'v', new Color32(0x6b, 0x2f, 0x9a, 0xff) },
        { 'S', new Color32(0x41, 0xa6, 0xf6, 0xff) },
        { 's', new Color32(0xb0, 0xf4, 0xff, 0xff) },
        { 'N', new Color32(0xa7, 0x6d, 0x3f, 0xff) },
        { 'n', new Color32(0x6d, 0x45, 0x27, 0xff) },
        { 'T', new Color32(0xff, 0xe9, 0xa3, 0xff) },
        { 'A', new Color32(0x2b, 0x2b, 0x38, 0xff) },
        { 'Z', new Color32(0xff, 0x5e, 0x1a, 0xff) },
        { 'z', new Color32(0xff, 0xd1, 0x66, 0xff) },
        { 'H', new Color32(0x99, 0xe6, 0x5f, 0xff) },
        { 'M', new Color32(0x5a, 0x5a, 0x66, 0xff) },
    };

    static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    // pixelsPerUnit: cuántos píxeles del dibujo ocupan una unidad del mundo.
    public static Sprite Get(string name, string[] rows, float pixelsPerUnit = 18f)
    {
        string key = name + "@" + pixelsPerUnit;
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
        int h = rows.Length, w = rows[0].Length;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            string row = rows[h - 1 - y]; // la textura empieza abajo, el texto arriba
            for (int x = 0; x < w; x++)
            {
                char c = x < row.Length ? row[x] : '.';
                px[y * w + x] = Palette.TryGetValue(c, out var col) ? col : new Color32(0, 0, 0, 0);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        Cache[key] = s;
        return s;
    }

    public static readonly string[] PicoUp =
    {
        "........KKK.......",
        ".......KLBK.......",
        ".....KKKBKKKK.....",
        "....KLLBBBBBBK....",
        "...KKKKBBBBKKKKK..",
        "..KKLLKBBBKWWWWWK.",
        "..KLLLKBBKWWWWWWWK",
        "KKLLLLKBBKWWWKKWWK",
        "KKbLLLLKBKWWWKKWWK",
        "KbKKKKKBBBKWWWWWK.",
        ".KKBBBBBBBBKKKKYYK",
        "..KBBBBBBPPBKYYYYK",
        "..KBBBBKCCCCKOOOK.",
        "...KKBCCCCCCKKKK..",
        "....KKCCCCCK......",
        "......KKKKK.......",
    };

    public static readonly string[] PicoMid =
    {
        "........KKK.......",
        ".......KLBK.......",
        ".....KKKBKKKK.....",
        "....KLLBBBBBBK....",
        "...KLBBBBBBKKKKK..",
        "..KLBBBBBBKWWWWWK.",
        "..KBBBBBBKWWWWWWWK",
        "KKKBBBBBBKWWWKKWWK",
        "KKKKKKKBBKWWWKKWWK",
        "KKLLLLLKBBKWWWWWK.",
        ".KbLLLLKBBBKKKKYYK",
        "..KbbbKBBPPBKYYYYK",
        "..KKKKBKCCCCKOOOK.",
        "...KKBCCCCCCKKKK..",
        "....KKCCCCCK......",
        "......KKKKK.......",
    };

    public static readonly string[] PicoDown =
    {
        "........KKK.......",
        ".......KLBK.......",
        ".....KKKBKKKK.....",
        "....KLLBBBBBBK....",
        "...KLBBBBBBKKKKK..",
        "..KLBBBBBBKWWWWWK.",
        "..KBBBBBBKWWWWWWWK",
        "KKKBBBBBBKWWWKKWWK",
        "KbbKBBBBBKWWWKKWWK",
        "KKKKKKKBBBKWWWWWK.",
        ".KbLLLLKBBBKKKKYYK",
        "..KLLLLKBPPBKYYYYK",
        "..KLLLKKCCCCKOOOK.",
        "...KLLKCCCCCKKKK..",
        "....KKCCCCCK......",
        "......KKKKK.......",
    };

    public static readonly string[] BatA =
    {
        "K................K",
        "KK..............KK",
        "KgK....K..K....KgK",
        "KGgK..KDKKDK..KgGK",
        ".KGgKKDDDDDDKKgGK.",
        ".KGGgKDRRRRDKgGGK.",
        "..KGGKDDWWDDKGGK..",
        "..KGgKDDDDDDKgGK..",
        "...KKKKDGGDKKKK...",
        "......KDDDDK......",
        ".......KKKK.......",
    };

    public static readonly string[] BatB =
    {
        "..................",
        "..................",
        ".......K..K.......",
        "......KDKKDK......",
        "......KDDDDK......",
        "....KKDRRRRDKK....",
        "..KKgKDDWWDDKgKK..",
        ".KGGGKDDDDDDKGGGK.",
        "KGGgKKKDGGDKKKgGGK",
        "KGgK..KDDDDK..KgGK",
        "KgK....KKKK....KgK",
    };

    public static readonly string[] WaspA =
    {
        "......ssss........",
        ".....sWWWss.......",
        "......ssssss......",
        "..K.....KssWss....",
        "...K....K.sss.....",
        "..KKKK..K.........",
        ".KRRDDK.KKKKKKK...",
        "KRRWDDDKYYKYYKYK..",
        "KRRRDDDKYYKYYKYYKK",
        ".KRDDDKKYYKYYKYK..",
        "..KKKK..KKKKKKK...",
        "...K.K...K..K.....",
    };

    public static readonly string[] WaspB =
    {
        "..................",
        "..................",
        "..................",
        "..K.....K.........",
        "...K....K.sss.....",
        "..KKKK..KssWss....",
        ".KRRDDK.KKKKKKK...",
        "KRRWDDDKYYKYYKYK..",
        "KRRRDDDKYYKYYKYYKK",
        ".KRDDDKKYYKYYKYK..",
        "..KKKK..KKKKKKK...",
        "....K.K...K..K....",
    };

    public static readonly string[] DroneA =
    {
        "GGGGGG....GGGGGG",
        "..KK........KK..",
        "...KK......KK...",
        ".....KKKKKK.....",
        "...KKGGGGGGKK...",
        "..KGGGGGGGGGGK..",
        ".KGGGKKKKKKGGGK.",
        ".KGGKRRRRRRKGGK.",
        ".KGGKRRWWRRKGGK.",
        ".KgGKRRRRRRKGgK.",
        "..KgGKKKKKKGgK..",
        "...KggggggggK...",
        "....KKDDDDKK....",
        "......KDDK......",
        ".......KK.......",
    };

    public static readonly string[] DroneB =
    {
        "..GG........GG..",
        "..KK........KK..",
        "...KK......KK...",
        ".....KKKKKK.....",
        "...KKGGGGGGKK...",
        "..KGGGGGGGGGGK..",
        ".KGGGKKKKKKGGGK.",
        ".KGGKRRRRRRKGGK.",
        ".KGGKRRWWRRKGGK.",
        ".KgGKRRRRRRKGgK.",
        "..KgGKKKKKKGgK..",
        "...KggggggggK...",
        "....KKDDDDKK....",
        "......KDDK......",
        ".......KK.......",
    };

    public static readonly string[] CrowA =
    {
        "...........KK.......",
        "..........KkkK......",
        ".........KkvkK......",
        "...KKK..KkvvK.......",
        "..KkkkK.KkvvkK......",
        ".KkWRkkKKkvvkkKKKK..",
        "KYYKkkkkkkkkkkkkkkKK",
        ".KYKkkkkkkkkkkkkkkkK",
        "..KKkkkkkkkkkkkKKKK.",
        "....KKKkkkkkkKK.....",
        ".......KKKKKK.......",
        "........K..K........",
    };

    public static readonly string[] CrowB =
    {
        "....................",
        "....................",
        "....................",
        "...KKK..............",
        "..KkkkK.............",
        ".KkWRkkKKKKKKK.KKKK.",
        "KYYKkkkkkkkkkkKkkkKK",
        ".KYKkkkkkvvvkkkkkkkK",
        "..KKkkkkkvvvkkkKKKK.",
        "....KKKkkvvvkKK.....",
        ".......KkvvkK.......",
        "........KvkK........",
        "........KKK.........",
    };

    public static readonly string[] Heart =
    {
        "............",
        "..KKK..KKK..",
        ".KRRRKKRRRK.",
        "KRWWRRRRRRRK",
        "KRWRRRRRRRRK",
        "KRRRRRRRRRrK",
        "KRRRRRRRRRrK",
        ".KRRRRRRRrK.",
        "..KRRRRRrK..",
        "...KRRRrK...",
        "....KRrK....",
        ".....KK.....",
    };

    public static readonly string[] Cannon =
    {
        "............",
        "............",
        "..KKKKKKKKK.",
        ".KGGGGGGGGGK",
        "KGWWGGGGGKGK",
        "KGgggggggKGK",
        ".KKNNNKKKKK.",
        "..KNnNNK....",
        ".KNnKKnNK...",
        ".KNKYYKNK...",
        ".KNnKKnNK...",
        "..KNNNNK....",
    };

    public static readonly string[] Mushroom =
    {
        "...KKKKKK...",
        ".KKVVWWVVKK.",
        "KVVVWWWVVVVK",
        "KVWVVVVVVWWK",
        "KVVVVVVVVWWK",
        "KvVVVVVVVVvK",
        ".KKKKKKKKKK.",
        "..KCCCCCCK..",
        "..KCKCCKCK..",
        "..KCCCCCCK..",
        "..KcCCCCcK..",
        "...KKKKKK...",
    };

    public static readonly string[] Ghost =
    {
        "....KKKK....",
        "..KKssssKK..",
        ".KsWWsssssK.",
        ".KsWssssssK.",
        "KsssKssKsssK",
        "KsssKssKsssK",
        "KssssssssssK",
        "KsssPssPsssK",
        "KssssKKssssK",
        "KSssssssssSK",
        "KSsKSssKSsSK",
        ".K.K.KK.K.K.",
    };

    public static readonly string[] Shield =
    {
        ".KKKKKKKKKK.",
        "KsSSSSSSSSbK",
        "KsSSSYYSSSbK",
        "KsSSSYYSSSbK",
        "KsSYYYYYYSbK",
        "KsSYYYYYYSbK",
        "KsSSSYYSSSbK",
        ".KsSSYYSSbK.",
        ".KsSSSSSSbK.",
        "..KsSSSSbK..",
        "...KSSSbK...",
        "....KKKK....",
    };

    public static readonly string[] Shot =
    {
        "...zzz..",
        ".zzTTTz.",
        "zzTWWWTz",
        ".zzTTTz.",
        "...zzz..",
    };

    public static readonly string[] Laser =
    {
        ".rRRRRRRr.",
        "rRPWWWWPRr",
        ".rRRRRRRr.",
    };

    public static readonly string[] Stinger =
    {
        "KKKK...",
        "YKYKGGW",
        "KKKK...",
    };

    public static readonly string[] Bomb =
    {
        ".....zT..",
        "....Kz...",
        "...KKK...",
        "..KAAAK..",
        ".KAAAAWK.",
        ".KAAAAAK.",
        ".KAAAAAK.",
        ".KAAAAAK.",
        "..KAAAK..",
        "...KKK...",
    };

    public static readonly string[] Gun =
    {
        "KKKKKK..",
        "KGGGGGKK",
        "KgggggKK",
        "KKKKKK..",
    };

    public static readonly string[] Cigar =
    {
        ".KKKKKKKKKKKK..",
        "KOOOWWWWWWWWGRz",
        "KOOOWWWWWWWWGRZ",
        ".KKKKKKKKKKKK..",
    };

    public static readonly string[] Beer =
    {
        "..WW.WWW....",
        ".WWWWWWWW...",
        "KWWWWWWWWK..",
        "KYYTYYYYYKKK",
        "KYTYYYYYYK.K",
        "KYTYOYYYYK.K",
        "KYTYYYOYYK.K",
        "KYYYYYYYYKKK",
        "KYOYYYYYYK..",
        "KYYYYYYOYK..",
        "KOYYYYYYOK..",
        ".KKKKKKKK...",
    };

    public static readonly string[] Chips =
    {
        ".KKKKKKKKKK.",
        "KrRrRrRrRrRK",
        "KRRRRRRRRRRK",
        "KRWRRRRRRRRK",
        "KRWRKKKKKRRK",
        "KRRKYYYYYKRK",
        "KRRKYzYYzKRK",
        "KRRKYYYOYKRK",
        "KRRRKYYYKRRK",
        "KRRRRKKKRRRK",
        "KRWRRRRRRRRK",
        "KRRRRRRRRRrK",
        "KrRrRrRrRrRK",
        ".KKKKKKKKKK.",
    };

    public static readonly string[] Car =
    {
        "..............................",
        "........KKKKKKKKKKK...........",
        ".......KsssKsssssssK..........",
        "......KssssKssssssssK.........",
        ".....KsssssKsssssssssK........",
        "KKKKKKKKKKKKKKKKKKKKKKKKKKK...",
        "KRRRRRRRRRRRRRRRRRRRRRRRRRRKK.",
        "KRWWRRRRRRRKRRRRRRRRRRRRRRRYYK",
        "KRRRRKKKRRRKRRRRRRRRRRKKKRRRRK",
        "KrrrKAAAKrrKrrrrrrrrrKAAAKrrrK",
        "KKKKAAGAAKKKKKKKKKKKKAAGAAKKK.",
        "...KAGKGAK..........KAGKGAK...",
        "...KAAGAAK..........KAAGAAK...",
        "....KAAAK............KAAAK....",
        ".....KKK..............KKK.....",
    };

    public static readonly string[] Moto =
    {
        "..........................",
        "..................KKK.....",
        ".................KGGK.....",
        "................KGK.......",
        "......KKKKKKK..KRK........",
        ".....KRRRRRRRKKRRK........",
        "..KKKRRWWRRRRRRRRKK.......",
        ".KDDDKRRRRRRRRRRRRRK......",
        "KDDKKKKKKKKKKKKKKKKKKK....",
        "..KAAAK...........KAAAK...",
        ".KAAGAAK.........KAAGAAK..",
        ".KAGKGAK.........KAGKGAK..",
        ".KAAGAAK.........KAAGAAK..",
        "..KAAAK...........KAAAK...",
        "...KKK.............KKK....",
    };

    public static readonly string[] Tree =
    {
        "......KKKK......",
        "....KKEEEEKK....",
        "...KEEHHEEEEK...",
        "..KEEHHEEEEEEK..",
        ".KEEHEEEEEEEEeK.",
        ".KEEEEEEEEEEEeK.",
        "KEEEEEEEEEEEEEeK",
        "KEHEEEEEEEEEEeeK",
        "KEEEEEEEEEEEEeeK",
        ".KEEEEEEEEEEeeK.",
        ".KeEEEEEEEEEeeK.",
        "..KeeEEEEEeeeK..",
        "...KKeeeeeeKK...",
        ".....KKNNKK.....",
        "......KNnK......",
        "......KNnK......",
        "......KNnK......",
        "......KNnK......",
        "......KNnK......",
        ".....KNNnnK.....",
        "....KNNNnnnK....",
        "....KKKKKKKK....",
    };
}
