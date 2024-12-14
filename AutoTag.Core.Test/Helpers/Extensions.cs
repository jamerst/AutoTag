namespace AutoTag.Core.Test.Helpers;

internal static class Extensions
{
    internal static T OrDefaultMock<T>(this T? obj) where T : class
        => obj ?? new Mock<T>().Object;
}