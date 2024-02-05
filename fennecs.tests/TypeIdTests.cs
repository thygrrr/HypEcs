// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;

namespace fennecs.tests;

public class TypeIdTests
{
    private struct Type1;

    private struct Type2;

    private struct Type3Backlink : IRelationBacklink;

    [Fact]
    public void BackLink_has_Distinct_Negative_TypeNumber()
    {
        var id = TypeId.Create<Type3Backlink>();
        Assert.True(id.TypeNumber < 0);
        Assert.True(id.isBacklink);
    }

    [Fact]
    public void Non_BackLink_has_Positive_TypeNumber()
    {
        var id = TypeId.Create<Type2>();
        Assert.False(id.isBacklink);
        Assert.True(id.TypeNumber > 0);
    }

    [Fact]
    public void TypeId_is_64_bits()
    {
        Assert.Equal(64 / 8, Marshal.SizeOf<TypeId>());
    }

    [Fact]
    public void Identity_is_64_bits()
    {
        Assert.Equal(64 / 8, Marshal.SizeOf<Identity>());
    }

    

    /*
    [Theory]
    [InlineData(0)]
    [InlineData(1848)]
    [InlineData(0x7FFF)]
    [InlineData(0xFFFF)]
    [InlineData(0x1111)]
    [InlineData(0xABCD)]
    public void TypeId_Generation_is_Congruent(ushort input)
    {
        var id = new TypeId {Generation = input};
        Assert.Equal(input, id.Generation);
        Assert.Equal(input, id.Target.Generation);
        Assert.Equal(0, id.TypeNumber);
        Assert.Equal(0, id.Target.Id);
        Assert.Equal(0, id.Id);
    }

    [Fact]
    public void TypeId_Identity_is_Congruent()
    {
        var id = new TypeId {Id = 900069420};
        Assert.Equal(900069420, id.Id);
        Assert.Equal(900069420, id.Target.Id);
        Assert.Equal(0, id.TypeNumber);
        Assert.Equal(0, id.Generation);
        Assert.Equal(0, id.Target.Generation);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42069)]
    [InlineData(0x7FFF)]
    [InlineData(0xFFFF)]
    [InlineData(0x1111)]
    [InlineData(0xABCD)]
    public void TypeId_TypeNumber_is_Congruent(ushort input)
    {
        var id = new TypeId {TypeNumber = input};
        Assert.Equal(input, id.TypeNumber);
        Assert.Equal(0, id.Generation);
        Assert.Equal(0, id.Target.Generation);
        Assert.Equal(0, id.Target.Id);
        Assert.Equal(0, id.Id);
    }
    */

    [Fact]
    public void TypeAssigner_Id_Unique()
    {
        Assert.NotEqual(
            TypeIdConverter.TypeIdAssigner<int>.Id,
            TypeIdConverter.TypeIdAssigner<string>.Id);

        Assert.NotEqual(
            TypeIdConverter.TypeIdAssigner<ushort>.Id,
            TypeIdConverter.TypeIdAssigner<short>.Id);

        Assert.NotEqual(
            TypeIdConverter.TypeIdAssigner<Type1>.Id,
            TypeIdConverter.TypeIdAssigner<Type2>.Id);
    }

    [Fact]
    public void TypeAssigner_Id_Same_For_Same_Type()
    {
        Assert.Equal(
            TypeIdConverter.TypeIdAssigner<int>.Id,
            TypeIdConverter.TypeIdAssigner<int>.Id);

        Assert.Equal(
            TypeIdConverter.TypeIdAssigner<Type1>.Id,
            TypeIdConverter.TypeIdAssigner<Type1>.Id);

        Assert.Equal(
            TypeIdConverter.TypeIdAssigner<Type2>.Id,
            TypeIdConverter.TypeIdAssigner<Type2>.Id);

        Assert.Equal(
            TypeIdConverter.TypeIdAssigner<Dictionary<string, string>>.Id,
            TypeIdConverter.TypeIdAssigner<Dictionary<string, string>>.Id);
    }

    [Fact]
    public void TypeAssigner_None_Matches_Identical()
    {
        var id1 = TypeId.Create<int>();
        var id2 = TypeId.Create<int>();

        Assert.True(id1.Matches(id2));
    }

    [Fact]
    public void TypeAssigner_None_Matches_Default()
    {
        var id1 = TypeId.Create<int>();
        // Keeping the default case to ensure it remains at default
        // ReSharper disable once RedundantArgumentDefaultValue
        var id2 = TypeId.Create<int>(default);
        var id3 = TypeId.Create<int>(Identity.None);

        Assert.True(id1.Matches(id2));
        Assert.True(id1.Matches(id3));
        Assert.True(id2.Matches(id3));
        Assert.True(id3.Matches(id2));
    }

    [Fact]
    public void TypeAssigner_does_not_Match_Identical()
    {
        var id1 = TypeId.Create<int>();
        var id2 = TypeId.Create<float>();

        Assert.False(id1.Matches(id2));
    }

    [Fact]
    public void TypeAssigner_None_does_not_match_Any()
    {
        var id1 = TypeId.Create<int>();
        var id2 = TypeId.Create<int>(new Identity(123));
        var id3 = TypeId.Create<int>(Identity.Any);

        Assert.False(id1.Matches(id2));
        Assert.False(id1.Matches(id3));
    }

}