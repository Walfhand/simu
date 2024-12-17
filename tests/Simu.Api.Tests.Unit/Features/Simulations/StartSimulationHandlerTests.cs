using FluentAssertions;
using Newtonsoft.Json;
using NSubstitute;
using Simu.Api.Features.Simulations.Domain.Strategies;
using Simu.Api.Features.Simulations.StartSimulation.Endpoints;
using Simu.Api.Features.Simulations.StartSimulation.Results;
using Simu.Api.Shared.Services;

namespace Simu.Api.Tests.Unit.Features.Simulations;

public class StartSimulationHandlerTests
{
    private readonly ICacheService _cacheService;
    private readonly StartSimulationEndpointHandler _sut;
    
    public StartSimulationHandlerTests()
    {
        _cacheService = Substitute.For<ICacheService>();
        _sut = new StartSimulationEndpointHandler(_cacheService, new CreditStrategyResolver());
    }
    
    
    [Fact]
    public async Task Handle_ShouldReturnCachedResult_WhenResultExistsInCache()
    {
        // Arrange
        var request = new StartSimulationRequest
        {
            Capital = 100000,
            Duration = 360,
            AnnualIncome = 28000,
            CreditType = CreditType.Fixed
        };

        var cacheKey = "Simulation_100000_28000_360";
        var cachedResult = new SimulationResult
        {
            FixedAnnualRate = 2.5M,
            MonthlyAmount = 395.11M,
            DepreciationTableLines = []
        };

        _cacheService.GetAsync(cacheKey)
            .Returns(JsonConvert.SerializeObject(cachedResult));

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(cachedResult);
        await _cacheService.Received(1).GetAsync(cacheKey);
        await _cacheService.DidNotReceiveWithAnyArgs().SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>());
    }
    
    [Fact]
    public async Task Handle_ShouldGenerateResult_WhenCacheIsEmpty()
    {
        // Arrange
        var request = new StartSimulationRequest
        {
            Capital = 100000,
            Duration = 360,
            AnnualIncome = 28000,
            CreditType = CreditType.Fixed
        };

        var cacheKey = "Simulation_100000_28000_360";

        _cacheService.GetAsync(cacheKey).Returns((string)null!);

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.FixedAnnualRate.Should().Be(2.5M);
        result.MonthlyAmount.Should().BeApproximately(395.11M, 0.01M);
        result.DepreciationTableLines.Should().NotBeEmpty();

        await _cacheService.Received(1).SetAsync(
            cacheKey,
            Arg.Any<string>(),
            TimeSpan.FromDays(1));
    }
    
    [Fact]
    public async Task Handle_ShouldThrowException_WhenCreditTypeIsUnknown()
    {
        // Arrange
        var request = new StartSimulationRequest
        {
            Capital = 100000,
            Duration = 360,
            AnnualIncome = 28000,
            CreditType = (CreditType)999 // Unknown type
        };

        // Act
        Func<Task> act = async () => await _sut.Handle(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}