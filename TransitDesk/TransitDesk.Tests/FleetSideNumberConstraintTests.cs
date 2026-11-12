using FluentAssertions;
using Microsoft.AspNetCore.Routing;
using TransitDesk.Web.Routing.Constraints;

namespace TransitDesk.Tests;

public class FleetSideNumberConstraintTests
{
    private const string RouteKey = "sideNumber";
    private readonly FleetSideNumberConstraint _constraint = new();

    private bool Match(object? value) =>
        _constraint.Match(
            httpContext: null,
            route: null,
            routeKey: RouteKey,
            values: new RouteValueDictionary { [RouteKey] = value },
            routeDirection: RouteDirection.IncomingRequest);

    [Theory]
    [InlineData("1000")]
    [InlineData("2072")]
    [InlineData("9999")]
    public void Match_ValidSideNumber_ReturnsTrue(string value)
    {
        Match(value).Should().BeTrue();
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(9999)]
    public void Match_ValidSideNumberAsInt_ReturnsTrue(int value)
    {
        Match(value).Should().BeTrue();
    }

    [Theory]
    [InlineData("999")]
    [InlineData("10000")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("-1000")]
    [InlineData("+2072")]
    [InlineData(" 2072")]
    [InlineData("20.72")]
    [InlineData(null)]
    public void Match_InvalidSideNumber_ReturnsFalse(string? value)
    {
        Match(value).Should().BeFalse();
    }

    [Fact]
    public void Match_MissingRouteValue_ReturnsFalse()
    {
        var result = _constraint.Match(null, null, RouteKey, new RouteValueDictionary(), RouteDirection.IncomingRequest);

        result.Should().BeFalse();
    }
}
