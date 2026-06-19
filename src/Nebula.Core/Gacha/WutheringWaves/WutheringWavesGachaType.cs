using System.ComponentModel;

namespace Nebula.Core.Gacha.WutheringWaves;

public readonly record struct WutheringWavesGachaType(int Value) : IGachaType
{
    [Description("角色活动唤取")]
    public const int CharacterEvent = 1;

    [Description("武器活动唤取")]
    public const int WeaponEvent = 2;

    [Description("角色常驻唤取")]
    public const int CharacterPermanent = 3;

    [Description("武器常驻唤取")]
    public const int WeaponPermanent = 4;

    [Description("新手唤取")]
    public const int Beginner = 5;


    public string ToLocalization() => Value switch
    {
        CharacterEvent => "角色活动唤取",
        WeaponEvent => "武器活动唤取",
        CharacterPermanent => "角色常驻唤取",
        WeaponPermanent => "武器常驻唤取",
        Beginner => "新手唤取",
        _ => Value.ToString(),
    };


    public override string ToString() => Value.ToString();
    public static implicit operator WutheringWavesGachaType(int value) => new(value);
    public static implicit operator int(WutheringWavesGachaType value) => value.Value;
}
