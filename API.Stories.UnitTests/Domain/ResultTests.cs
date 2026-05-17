using API.Stories.Domain;

namespace API.Stories.UnitTests.Domain;

public sealed class ResultTests
{
    [Fact]
    public void ResultOk_IsSuccessful()
    {
        var result = Result.Ok;

        Assert.True(result.Success);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Result_FromString_IsFailure()
    {
        Result result = "boom";

        Assert.False(result.Success);
        Assert.Equal("boom", result.Error);
    }

    [Fact]
    public void ResultBoolConversion_ReflectsSuccess()
    {
        Result success = Result.Ok;
        Result failure = "nope";

        Assert.True(success);
        Assert.False(failure);
    }

    [Fact]
    public void GenericResult_FromData_IsSuccessful()
    {
        Result<int> result = 42;

        Assert.True(result.Success);
        Assert.Equal(42, result.Data);
    }

    [Fact]
    public void GenericResult_FromError_IsFailure()
    {
        Result<int> result = "bad";

        Assert.False(result.Success);
        Assert.Equal("bad", result.Error);
    }

    [Fact]
    public void GenericResult_ToData_ThrowsWhenFailure()
    {
        Result<int> result = "bad";

        Assert.Throws<NotSupportedException>(() =>
        {
            _ = (int)result;
        });
    }
}
