// SPDX-License-Identifier: MIT

namespace fennecs.tests;

public class TypeTests
{
    [Fact]
    public void TypeAssigner_Id_Unique()
    {
        Assert.NotEqual(
            TypeIdConverter.TypeIdAssigner<int>.Id,
            TypeIdConverter.TypeIdAssigner<float>.Id);
    }

    [Fact]
    public void TypeAssigner_Id_Same()
    {
        Assert.Equal(
            TypeIdConverter.TypeIdAssigner<int>.Id,
            TypeIdConverter.TypeIdAssigner<int>.Id);
    }
}