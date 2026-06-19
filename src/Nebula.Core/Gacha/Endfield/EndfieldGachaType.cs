using System.ComponentModel;

namespace Nebula.Core.Gacha.Endfield;

public readonly record struct EndfieldGachaType(int Value) : IGachaType
{
    [Description("特许寻访")]
    public const int SpecialCharacter = 1;

    [Description("辉光庆典")]
    public const int JointCharacter = 2;

    [Description("基础寻访")]
    public const int StandardCharacter = 3;

    [Description("启程寻访")]
    public const int BeginnerCharacter = 4;

    [Description("武器寻访")]
    public const int Weapon = 101;


    public string ToLocalization() => Value switch
    {
        SpecialCharacter => "特许寻访",
        JointCharacter => "辉光庆典",
        StandardCharacter => "基础寻访",
        BeginnerCharacter => "启程寻访",
        Weapon => "武器寻访",
        _ => Value.ToString(),
    };


    public override string ToString() => Value.ToString();

    public static implicit operator EndfieldGachaType(int value) => new(value);

    public static implicit operator int(EndfieldGachaType value) => value.Value;
}
