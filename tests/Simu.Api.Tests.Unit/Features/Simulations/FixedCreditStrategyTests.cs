using FluentAssertions;
using Simu.Api.Features.Simulations.Domain.Strategies;
using Simu.Api.Features.Simulations.StartSimulation.Exceptions;

namespace Simu.Api.Tests.Unit.Features.Simulations;

public class FixedCreditStrategyTests
{
    private readonly FixedCreditStrategy _strategy;

    public FixedCreditStrategyTests()
    {
        _strategy = new FixedCreditStrategy();
    }
    
    [Fact]
    public void ValidateConstraints_ShouldNotThrowException_WhenInputsAreValid()
    {
        // Arrange
        decimal validCapital = 100000;
        int validDuration = 360;
        decimal validIncome = 28000;

        // Act
        var act = () => _strategy.ValidateConstraints(validCapital, validDuration, validIncome);

        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ValidateConstraints_ShouldThrowException_WhenCapitalIsOutOfRange()
    {
        // Arrange
        const decimal invalidCapital = 15000;
        const int validDuration = 360;
        const decimal validIncome = 28000;

        // Act
        Action act = () => _strategy.ValidateConstraints(invalidCapital, validDuration, validIncome);

        // Assert
        act.Should()
            .Throw<SimulationException>()
            .WithMessage("The capital must be between 20,000€ and 310,000€.");
    }
    
    [Fact]
    public void ValidateConstraints_ShouldThrowException_WhenDurationIsOutOfRange()
    {
        // Arrange
        const decimal validCapital = 100000;
        const int invalidDuration = 400;
        const decimal validIncome = 28000;

        // Act
        Action act = () => _strategy.ValidateConstraints(validCapital, invalidDuration, validIncome);

        // Assert
        act.Should()
            .Throw<SimulationException>()
            .WithMessage("The duration must be between 180 and 360 months.");
    }
    
    [Fact]
    public void ValidateConstraints_ShouldThrowException_WhenIncomeIsOutOfRange()
    {
        // Arrange
        const decimal validCapital = 100000;
        const int validDuration = 360;
        const decimal invalidIncome = 60000;

        // Act
        Action act = () => _strategy.ValidateConstraints(validCapital, validDuration, invalidIncome);

        // Assert
        act.Should()
            .Throw<SimulationException>()
            .WithMessage("The income must be between 0€ and 53,900€ per year.");
    }
    
    [Fact]
    public void ValidateConstraints_ShouldThrowException_WithMultipleErrors()
    {
        // Arrange
        const decimal invalidCapital = 15000;
        const int invalidDuration = 400; 
        const decimal invalidIncome = 60000;

        // Act
        Action act = () => _strategy.ValidateConstraints(invalidCapital, invalidDuration, invalidIncome);

        // Assert
        act.Should()
            .Throw<SimulationException>()
            .WithMessage("The capital must be between 20,000€ and 310,000€. The duration must be between 180 and 360 months. The income must be between 0€ and 53,900€ per year.");
    }
    
    [Fact]
    public void GenerateSimulation_ShouldCalculateCorrectAnnualRate()
    {
        // Arrange
        const decimal capital = 100000;
        const int duration = 360;
        const decimal income = 28000;

        // Act
        var result = _strategy.GenerateSimulation(capital, duration, income);

        // Assert
        result.FixedAnnualRate.Should().Be(2.5M);
    }
    
    [Fact]
    public void GenerateSimulation_ShouldCalculateCorrectMonthlyAmount()
    {
        // Arrange
        const decimal capital = 100000;
        const int duration = 360;
        const decimal income = 28000;

        // Act
        var result = _strategy.GenerateSimulation(capital, duration, income);

        // Assert
        result.MonthlyAmount.Should().BeApproximately(395.11M, 0.01M);
    }
    
    [Fact]
    public void GenerateSimulation_ShouldGenerateCorrectDepreciationTable_ForLimitedMonths()
    {
        // Arrange
        const decimal capital = 100000;
        const int duration = 180; // 15 years = 180 months
        const decimal income = 28000;

        // Act
        var result = _strategy.GenerateSimulation(capital, duration, income);

        // Assert
        result.FixedAnnualRate.Should().Be(2.5M);
        result.MonthlyAmount.Should().BeApproximately(666.78M, 0.01M);

        // Validate the first month
        var firstMonth = result.DepreciationTableLines[0];
        firstMonth.MonthlyAmount.Should().BeApproximately(666.78M, 0.01M);
        firstMonth.InterestShare.Should().BeApproximately(208.31M, 0.01M);
        firstMonth.CapitalShare.Should().BeApproximately(458.47M, 0.01M);
        firstMonth.RemainingBalance.Should().BeApproximately(99541.53M, 0.01M);

        // Validate the last month
        var lastMonth = result.DepreciationTableLines[^1];
        lastMonth.MonthlyAmount.Should().BeApproximately(666.78M, 0.01M);
        lastMonth.InterestShare.Should().BeApproximately(1.38M, 0.01M);
        lastMonth.CapitalShare.Should().BeApproximately(665.40M, 0.01M);
        lastMonth.RemainingBalance.Should().Be(0);

        // Validate total depreciation table lines
        result.DepreciationTableLines.Should().HaveCount(180);
    }
    
    [Fact]
    public void GenerateSimulation_ShouldGenerateZeroRemainingBalance_AfterLastMonth()
    {
        // Arrange
        const decimal capital = 100000;
        const int duration = 360; // Full duration
        const decimal income = 28000;

        // Act
        var result = _strategy.GenerateSimulation(capital, duration, income);

        // Assert
        result.DepreciationTableLines.Should().NotBeEmpty();
        var lastMonth = result.DepreciationTableLines.Last();
        lastMonth.RemainingBalance.Should().Be(0);
    }
    
    
    [Theory]
    [InlineData(16400, 1.70)]
    [InlineData(19700, 1.90)]
    [InlineData(23000, 2.10)]
    [InlineData(27800, 2.30)]
    [InlineData(32700, 2.50)]
    [InlineData(43200, 2.70)]
    [InlineData(53900, 2.90)]
    public void GenerateSimulation_ShouldUseCorrectAnnualRate_BasedOnIncome(decimal income, decimal expectedRate)
    {
        // Arrange
        const decimal capital = 100000;
        const int duration = 360;

        // Act
        var result = _strategy.GenerateSimulation(capital, duration, income);

        // Assert
        result.FixedAnnualRate.Should().Be(expectedRate);
    }

}