using FluentAssertions;
using Moq;
using TaskManager.Application.UserConnections.QueryHandlers;
using TaskManager.Application.UserConnections.Queries;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Tests.Application.UserConnections.QueryHandlers
{
    public class GetActiveUserConnectionsQueryHandlerTests
    {
        private readonly Mock<IUserConnectionRepository> _mockUserConnectionRepository;
        private readonly GetActiveUserConnectionsQueryHandler _handler;

        public GetActiveUserConnectionsQueryHandlerTests()
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserConnectionRepository = new Mock<IUserConnectionRepository>();
            mockUnitOfWork.Setup(u => u.UserConnectionRepository).Returns(_mockUserConnectionRepository.Object);
            _handler = new GetActiveUserConnectionsQueryHandler(mockUnitOfWork.Object);
        }

        [Fact]
        public async Task Handle_ReturnsSuccess_WhenValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new GetActiveUserConnectionsQuery(userId);

            var connection = UserConnection.Create(userId, Guid.NewGuid()).Value;
            var connections = new List<UserConnection> { connection };

            _mockUserConnectionRepository.Setup(r => r.GetConnectionsByOwnerIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(connections);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(1);
        }

        [Fact]
        public async Task Handle_ReturnsFailure_WhenUserIdIsEmpty()
        {
            // Arrange
            var query = new GetActiveUserConnectionsQuery(Guid.Empty);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorCode.Should().Be(ErrorCode.DomainRuleViolation);
        }
    }
}
