using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Fasmga.Services;

namespace Fasmga.Controllers;

[ApiController]
[Route("/v1")]
public class ApiController : ControllerBase
{
	private ILogger<ApiController> _logger;
	private readonly UrlService _urlService;
	private readonly UserService _userService;

	public ApiController(UrlService urlService, UserService userService, ILogger<ApiController> logger)
	{
		_urlService = urlService;
		_userService = userService;
		_logger = logger;
	}

	[HttpGet()]
	public IActionResult Get()
	{
		return Ok("Hello world!");
	}

	[HttpGet("test")]
	public IActionResult Test()
	{
		string? apiToken = Environment.GetEnvironmentVariable("TestingApiToken");

		if (apiToken is null) {
			return StatusCode(500, "Invalid apiToken"); // 500 because it's a env variable
		}

		User owner = _userService.Get(apiToken);

		Url url = new(redirect: "https://example.com", nsfw: false, owner: owner);

		_logger.LogInformation(JsonSerializer.Serialize<Url>(url));

		return Ok(JsonSerializer.Serialize<Url>(url));
	}

	[HttpGet("header")]
	[ProducesResponseType(200)]
	[ProducesResponseType(400)]
	public IActionResult Header([FromHeader] string Authentication)
	{
		_logger.LogInformation($"Auth healder: {Authentication}");

		if (!Authentication.StartsWith("Basic ")) {
			return BadRequest("Invalid token type. Use a Basic token!");
		}

		string token = Authentication.Split("Basic ")[1];

		_logger.LogInformation($"Token: {token}");

		return Ok($"Here your token {token}");
	}

	[HttpGet("mongo")]
	public IActionResult Mongo()
	{
		string? apiToken = Environment.GetEnvironmentVariable("TestingApiToken");

		if (apiToken is null) {
			return StatusCode(500, "Invalid apiToken"); // 500 because it's a env variable
		}

		User owner = _userService.Get(apiToken);

		Url url = new(owner, "https://example.com", false);

		for (int i = 0; i < 4; i++)
		{
			url.CheckUnique((UrlUniqueValues) i, _urlService);
		}

		_urlService.Create(url);

		// return NoContent(); // approximately 137ms ( create )

		Url find = _urlService.Get("nnTaX");

		if (find is null) {
			return NotFound("url ID invalid");
		}

		return Ok(find); // approximately 1620 ms ( find + create )
	}
}
