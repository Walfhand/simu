using FluentValidation.TestHelper;
using Simu.Api.Features.Simulations.Domain.Strategies;
using Simu.Api.Features.Simulations.StartSimulation.Endpoints;

namespace Simu.Api.Tests.Unit.Features.Simulations;

public class StartSimulationRequestValidatorTests
{
    private readonly StartSimulationRequestValidator _sut;
    
    public StartSimulationRequestValidatorTests()
    {
        _sut = new StartSimulationRequestValidator();
    }
    
    [Fact]
    public void Validator_ShouldNotHaveErrors_WhenRequestIsValid()
    {
        var request = new StartSimulationRequest
        {
            Capital = 100000,
            Duration = 360,
            AnnualIncome = 28000,
            CreditType = CreditType.Fixed
        };

        var result = _sut.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
    
    
    [Fact]
    public void Validator_ShouldHaveErrors_WhenRequiredFieldsAreMissing()
    {
        var request = new StartSimulationRequest();

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Capital);
        result.ShouldHaveValidationErrorFor(x => x.Duration);
        result.ShouldHaveValidationErrorFor(x => x.AnnualIncome);
    }
}