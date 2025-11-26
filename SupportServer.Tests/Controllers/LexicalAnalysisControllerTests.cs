using Infrastructure.AI;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SupportServer.Controllers;
using Xunit;

namespace SupportServer.Tests.Controllers
{
    public class LexicalAnalysisControllerTests
    {
        private readonly Mock<IPromptClient> _mockPromptClient;
        private readonly LexicalAnalysisController _controller;

        public LexicalAnalysisControllerTests()
        {
            _mockPromptClient = new Mock<IPromptClient>();
            _controller = new LexicalAnalysisController(_mockPromptClient.Object);
        }

        [Fact]
        public async Task Analyze_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new LexicalAnalysisController.LexicalAnalysisRequest
            {
                Text = "Hello world"
            };

            var expectedResponse = @"{
  ""items"": [
    {
      ""chunk"": ""Hello"",
      ""translation"": ""Hei"",
      ""note"": """"
    },
    {
      ""chunk"": ""world"",
      ""translation"": ""verden"",
      ""note"": """"
    }
  ]
}";

            _mockPromptClient
                .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Analyze(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public async Task Analyze_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.Analyze(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Missing required text field.", badRequestResult.Value);
        }

        [Fact]
        public async Task Analyze_WithEmptyText_ReturnsBadRequest()
        {
            // Arrange
            var request = new LexicalAnalysisController.LexicalAnalysisRequest
            {
                Text = ""
            };

            // Act
            var result = await _controller.Analyze(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Missing required text field.", badRequestResult.Value);
        }

        [Fact]
        public async Task Analyze_WithWhitespaceText_ReturnsBadRequest()
        {
            // Arrange
            var request = new LexicalAnalysisController.LexicalAnalysisRequest
            {
                Text = "   "
            };

            // Act
            var result = await _controller.Analyze(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Missing required text field.", badRequestResult.Value);
        }

        [Fact]
        public async Task Analyze_WhenPromptClientReturnsEmpty_ReturnsInternalServerError()
        {
            // Arrange
            var request = new LexicalAnalysisController.LexicalAnalysisRequest
            {
                Text = "Test text"
            };

            _mockPromptClient
                .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(string.Empty);

            // Act
            var result = await _controller.Analyze(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("Failed to generate lexical analysis.", statusCodeResult.Value);
        }

        [Fact]
        public async Task Analyze_WhenPromptClientReturnsNull_ReturnsInternalServerError()
        {
            // Arrange
            var request = new LexicalAnalysisController.LexicalAnalysisRequest
            {
                Text = "Test text"
            };

            _mockPromptClient
                .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string)null);

            // Act
            var result = await _controller.Analyze(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("Failed to generate lexical analysis.", statusCodeResult.Value);
        }

        [Fact]
        public async Task Analyze_SendsCorrectPromptToPromptClient()
        {
            // Arrange
            var request = new LexicalAnalysisController.LexicalAnalysisRequest
            {
                Text = "jeg liker kaffe"
            };

            string capturedPrompt = null;

            _mockPromptClient
                .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((prompt, model) => capturedPrompt = prompt)
                .ReturnsAsync(@"{""items"": []}");

            // Act
            await _controller.Analyze(request);

            // Assert
            Assert.NotNull(capturedPrompt);
            Assert.Contains("lexical segmentation", capturedPrompt);
            Assert.Contains("jeg liker kaffe", capturedPrompt);
            Assert.Contains("chunk", capturedPrompt);
            Assert.Contains("translation", capturedPrompt);
            Assert.Contains("note", capturedPrompt);
        }

        [Fact]
        public async Task Analyze_WithMultiWordExpression_ReturnsOkResult()
        {
            // Arrange
            var request = new LexicalAnalysisController.LexicalAnalysisRequest
            {
                Text = "take care of"
            };

            var expectedResponse = @"{
  ""items"": [
    {
      ""chunk"": ""take care of"",
      ""translation"": ""ta seg av"",
      ""note"": ""Fixed multi-word expression meaning to look after or handle.""
    }
  ]
}";

            _mockPromptClient
                .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Analyze(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public async Task Analyze_WithLongText_ReturnsOkResult()
        {
            // Arrange
            var request = new LexicalAnalysisController.LexicalAnalysisRequest
            {
                Text = "This is a longer sentence with multiple words and phrases that need to be analyzed."
            };

            var expectedResponse = @"{
  ""items"": [
    {
      ""chunk"": ""This"",
      ""translation"": ""Dette"",
      ""note"": """"
    },
    {
      ""chunk"": ""is"",
      ""translation"": ""er"",
      ""note"": """"
    }
  ]
}";

            _mockPromptClient
                .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Analyze(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task Analyze_WhenPromptClientThrows_ThrowsException()
        {
            // Arrange
            var request = new LexicalAnalysisController.LexicalAnalysisRequest
            {
                Text = "Test text"
            };

            _mockPromptClient
                .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("API Error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () => 
                await _controller.Analyze(request));
        }
    }
}
