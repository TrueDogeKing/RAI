using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using TransitDesk.Core.Enums;
using TransitDesk.Web.ViewModels;

namespace TransitDesk.Tests;

public class IncidentValidationTests
{
    private static IncidentReportViewModel CreateValidModel() => new()
    {
        LineNumber = "N1",
        VehicleSideNumber = 2072,
        IncidentType = IncidentType.TechnicalFailure,
        Description = "Awaria drzwi środkowych, pojazd zjechał do zajezdni.",
        OccurredAt = DateTime.Now.AddHours(-1)
    };

    // validateAllProperties: true – bez tego sprawdzane byłyby tylko atrybuty [Required].
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void ValidModel_PassesValidation()
    {
        Validate(CreateValidModel()).Should().BeEmpty();
    }

    [Theory]
    [InlineData("6")]
    [InlineData("199")]
    [InlineData("N1")]
    [InlineData("T10A")]
    public void LineNumber_AlphanumericUpTo4Chars_IsValid(string lineNumber)
    {
        var model = CreateValidModel();
        model.LineNumber = lineNumber;

        Validate(model).Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("N-1")]
    [InlineData("1 2")]
    [InlineData("Ł1")]
    public void LineNumber_Invalid_ProducesError(string lineNumber)
    {
        var model = CreateValidModel();
        model.LineNumber = lineNumber;

        Validate(model).Should().ContainSingle()
            .Which.MemberNames.Should().Contain(nameof(IncidentReportViewModel.LineNumber));
    }

    [Theory]
    [InlineData("Za krótki opis")]          // 14 znaków
    [InlineData("1234567890123456789")]     // 19 znaków – tuż poniżej granicy
    public void Description_ShorterThan20Chars_ProducesError(string description)
    {
        var model = CreateValidModel();
        model.Description = description;

        Validate(model).Should().ContainSingle()
            .Which.MemberNames.Should().Contain(nameof(IncidentReportViewModel.Description));
    }

    [Fact]
    public void Description_Exactly20Chars_IsValid()
    {
        var model = CreateValidModel();
        model.Description = new string('x', 20);

        Validate(model).Should().BeEmpty();
    }

    [Fact]
    public void Description_LongerThan500Chars_ProducesError()
    {
        var model = CreateValidModel();
        model.Description = new string('x', 501);

        Validate(model).Should().ContainSingle()
            .Which.MemberNames.Should().Contain(nameof(IncidentReportViewModel.Description));
    }

    [Fact]
    public void OccurredAt_InFuture_ProducesError()
    {
        var model = CreateValidModel();
        model.OccurredAt = DateTime.Now.AddDays(1);

        Validate(model).Should().ContainSingle()
            .Which.MemberNames.Should().Contain(nameof(IncidentReportViewModel.OccurredAt));
    }

    [Fact]
    public void RequiredFields_Missing_ProduceErrors()
    {
        var model = new IncidentReportViewModel { LineNumber = "", Description = "" };

        Validate(model).SelectMany(r => r.MemberNames).Should().BeEquivalentTo(
            nameof(IncidentReportViewModel.LineNumber),
            nameof(IncidentReportViewModel.VehicleSideNumber),
            nameof(IncidentReportViewModel.Description),
            nameof(IncidentReportViewModel.OccurredAt));
    }
}
