namespace Cspp.Results.Tests;

/// <content>
/// Tests for the asynchronous members and the members on a task of a result.
/// </content>
public partial class ResultExtensionsTests
{
    [Test]
    public async Task MapAsync_OnSuccess_ReturnsMappedValue()
    {
        Result<int> result = 21;

        Result<int> mapped = await result.MapAsync(DoubleAsync);

        Assert.That(mapped.GetValueOrDefault(), Is.EqualTo(42));
    }

    [Test]
    public async Task MapAsync_OnFailure_ReturnsSameFailureWithoutCallingMap()
    {
        Result<int> result = ParseFailure;
        bool called = false;

        Result<int> mapped = await result.MapAsync(value =>
        {
            called = true;
            return DoubleAsync(value);
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(FailureOf(mapped), Is.SameAs(ParseFailure));
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public void MapAsync_WithNullMap_ThrowsArgumentNullException()
    {
        Result<int> result = 42;

        Assert.That(
            () => result.MapAsync((Func<int, Task<int>>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("map"));
    }

    [Test]
    public async Task BindAsync_OnSuccess_ReturnsBindResult()
    {
        Result<string> result = "42";

        Result<int> bound = await result.BindAsync(ParseAsync);

        Assert.That(bound.GetValueOrDefault(), Is.EqualTo(42));
    }

    [Test]
    public async Task BindAsync_OnFailure_ReturnsSameFailureWithoutCallingBind()
    {
        Result<string> result = ParseFailure;
        bool called = false;

        Result<int> bound = await result.BindAsync(text =>
        {
            called = true;
            return ParseAsync(text);
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(FailureOf(bound), Is.SameAs(ParseFailure));
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public void BindAsync_WithNullBind_ThrowsArgumentNullException()
    {
        Result<string> result = "42";

        Assert.That(
            () => result.BindAsync((Func<string, Task<Result<int>>>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("bind"));
    }

    [Test]
    public async Task MatchAsync_OnSuccess_CallsOnSuccessWithValue()
    {
        Result<int> result = 42;

        string message = await result.MatchAsync(DescribeValueAsync, DescribeFailureAsync);

        Assert.That(message, Is.EqualTo("value 42"));
    }

    [Test]
    public async Task MatchAsync_OnFailure_CallsOnFailureWithFailure()
    {
        Result<int> result = ParseFailure;

        string message = await result.MatchAsync(DescribeValueAsync, DescribeFailureAsync);

        Assert.That(message, Is.EqualTo($"failure {ParseFailure.Code}"));
    }

    [Test]
    public void MatchAsync_WithNullOnSuccess_ThrowsArgumentNullException()
    {
        Result<int> result = 42;

        Assert.That(
            () => result.MatchAsync(null!, DescribeFailureAsync),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("onSuccess"));
    }

    [Test]
    public void MatchAsync_WithNullOnFailure_ThrowsArgumentNullException()
    {
        Result<int> result = 42;

        Assert.That(
            () => result.MatchAsync(DescribeValueAsync, null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("onFailure"));
    }

    [Test]
    public async Task MapOnTask_OnSuccess_ReturnsMappedValue()
    {
        Result<int> mapped = await ParseAsync("21").Map(value => value * 2);

        Assert.That(mapped.GetValueOrDefault(), Is.EqualTo(42));
    }

    [Test]
    public async Task MapOnTask_OnFailure_ReturnsSameFailure()
    {
        Result<int> mapped = await ParseAsync("abc").Map(value => value * 2);

        Assert.That(FailureOf(mapped), Is.SameAs(ParseFailure));
    }

    [Test]
    public void MapOnTask_WithNullTask_ThrowsArgumentNullException()
    {
        Task<Result<int>> task = null!;

        Assert.That(
            () => task.Map(value => value * 2),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("task"));
    }

    [Test]
    public void MapOnTask_WithNullMap_ThrowsArgumentNullException()
    {
        Assert.That(
            () => ParseAsync("42").Map((Func<int, int>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("map"));
    }

    [Test]
    public async Task BindOnTask_OnSuccess_ReturnsBindResult()
    {
        Result<int> bound = await ParseAsync("-1").Bind(Positive);

        Assert.That(FailureOf(bound), Is.SameAs(NegativeFailure));
    }

    [Test]
    public async Task BindOnTask_OnFailure_ReturnsSameFailure()
    {
        Result<int> bound = await ParseAsync("abc").Bind(Positive);

        Assert.That(FailureOf(bound), Is.SameAs(ParseFailure));
    }

    [Test]
    public void BindOnTask_WithNullTask_ThrowsArgumentNullException()
    {
        Task<Result<int>> task = null!;

        Assert.That(
            () => task.Bind(Positive),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("task"));
    }

    [Test]
    public void BindOnTask_WithNullBind_ThrowsArgumentNullException()
    {
        Assert.That(
            () => ParseAsync("42").Bind((Func<int, Result<int>>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("bind"));
    }

    [Test]
    public async Task MatchOnTask_OnSuccess_CallsOnSuccessWithValue()
    {
        string message = await ParseAsync("42").Match(value => $"value {value}", failure => failure.Code);

        Assert.That(message, Is.EqualTo("value 42"));
    }

    [Test]
    public async Task MatchOnTask_OnFailure_CallsOnFailureWithFailure()
    {
        string message = await ParseAsync("abc").Match(value => $"value {value}", failure => failure.Code);

        Assert.That(message, Is.EqualTo(ParseFailure.Code));
    }

    [Test]
    public void MatchOnTask_WithNullTask_ThrowsArgumentNullException()
    {
        Task<Result<int>> task = null!;

        Assert.That(
            () => task.Match(value => value, _ => -1),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("task"));
    }

    [Test]
    public void MatchOnTask_WithNullOnSuccess_ThrowsArgumentNullException()
    {
        Assert.That(
            () => ParseAsync("42").Match(null!, _ => -1),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("onSuccess"));
    }

    [Test]
    public void MatchOnTask_WithNullOnFailure_ThrowsArgumentNullException()
    {
        Assert.That(
            () => ParseAsync("42").Match(value => value, null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("onFailure"));
    }

    [Test]
    public async Task MapAsyncOnTask_OnSuccess_ReturnsMappedValue()
    {
        Result<int> mapped = await ParseAsync("21").MapAsync(DoubleAsync);

        Assert.That(mapped.GetValueOrDefault(), Is.EqualTo(42));
    }

    [Test]
    public async Task MapAsyncOnTask_OnFailure_ReturnsSameFailure()
    {
        Result<int> mapped = await ParseAsync("abc").MapAsync(DoubleAsync);

        Assert.That(FailureOf(mapped), Is.SameAs(ParseFailure));
    }

    [Test]
    public void MapAsyncOnTask_WithNullTask_ThrowsArgumentNullException()
    {
        Task<Result<int>> task = null!;

        Assert.That(
            () => task.MapAsync(DoubleAsync),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("task"));
    }

    [Test]
    public void MapAsyncOnTask_WithNullMap_ThrowsArgumentNullException()
    {
        Assert.That(
            () => ParseAsync("42").MapAsync((Func<int, Task<int>>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("map"));
    }

    [Test]
    public async Task BindAsyncOnTask_OnSuccess_ReturnsBindResult()
    {
        Result<int> bound = await ParseAsync("-1").BindAsync(PositiveAsync);

        Assert.That(FailureOf(bound), Is.SameAs(NegativeFailure));
    }

    [Test]
    public async Task BindAsyncOnTask_OnFailure_ReturnsSameFailure()
    {
        Result<int> bound = await ParseAsync("abc").BindAsync(PositiveAsync);

        Assert.That(FailureOf(bound), Is.SameAs(ParseFailure));
    }

    [Test]
    public void BindAsyncOnTask_WithNullTask_ThrowsArgumentNullException()
    {
        Task<Result<int>> task = null!;

        Assert.That(
            () => task.BindAsync(PositiveAsync),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("task"));
    }

    [Test]
    public void BindAsyncOnTask_WithNullBind_ThrowsArgumentNullException()
    {
        Assert.That(
            () => ParseAsync("42").BindAsync((Func<int, Task<Result<int>>>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("bind"));
    }

    [Test]
    public async Task MatchAsyncOnTask_OnSuccess_CallsOnSuccessWithValue()
    {
        string message = await ParseAsync("42").MatchAsync(DescribeValueAsync, DescribeFailureAsync);

        Assert.That(message, Is.EqualTo("value 42"));
    }

    [Test]
    public async Task MatchAsyncOnTask_OnFailure_CallsOnFailureWithFailure()
    {
        string message = await ParseAsync("abc").MatchAsync(DescribeValueAsync, DescribeFailureAsync);

        Assert.That(message, Is.EqualTo($"failure {ParseFailure.Code}"));
    }

    [Test]
    public void MatchAsyncOnTask_WithNullTask_ThrowsArgumentNullException()
    {
        Task<Result<int>> task = null!;

        Assert.That(
            () => task.MatchAsync(DescribeValueAsync, DescribeFailureAsync),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("task"));
    }

    [Test]
    public void MatchAsyncOnTask_WithNullOnSuccess_ThrowsArgumentNullException()
    {
        Assert.That(
            () => ParseAsync("42").MatchAsync(null!, DescribeFailureAsync),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("onSuccess"));
    }

    [Test]
    public void MatchAsyncOnTask_WithNullOnFailure_ThrowsArgumentNullException()
    {
        Assert.That(
            () => ParseAsync("42").MatchAsync(DescribeValueAsync, null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("onFailure"));
    }

    [TestCase("21", "value 42")]
    [TestCase("-1", "failure Validate.Negative")]
    [TestCase("abc", "failure Parse.Invalid")]
    public async Task AsyncChain_WhenChained_StopsAtFirstFailure(string text, string expected)
    {
        string message = await ParseAsync(text)
            .Bind(Positive)
            .MapAsync(DoubleAsync)
            .MatchAsync(DescribeValueAsync, DescribeFailureAsync);

        Assert.That(message, Is.EqualTo(expected));
    }

    // Each helper yields first, so the members under test await a task that hasn't completed yet.
    private static async Task<Result<int>> ParseAsync(string text)
    {
        await Task.Yield();
        return Parse(text);
    }

    private static async Task<Result<int>> PositiveAsync(int value)
    {
        await Task.Yield();
        return Positive(value);
    }

    private static async Task<int> DoubleAsync(int value)
    {
        await Task.Yield();
        return value * 2;
    }

    private static async Task<string> DescribeValueAsync(int value)
    {
        await Task.Yield();
        return $"value {value}";
    }

    private static async Task<string> DescribeFailureAsync(Failure failure)
    {
        await Task.Yield();
        return $"failure {failure.Code}";
    }

    private static Result<int> Positive(int value)
    {
        return Validate(value).Map(() => value);
    }

    private static Failure? FailureOf<T>(Result<T> result)
    {
        return result.TryGetValue(out Failure? failure) ? failure : null;
    }
}