using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using VirtualClassroom.Helpers;
using VirtualClassroom.Models;
using VirtualClassroom.Services;

namespace VirtualClassroom.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class TutorsController : ControllerBase
	{
		private readonly IOptions<AppSettings> _appSettings;
		private readonly ITutorService _tutorService;

		public TutorsController(IOptions<AppSettings> appSettings, ITutorService tutorService)
		{
			_appSettings = appSettings;
			_tutorService = tutorService;
		}

		[HttpPost("authenticate")]
		[AllowAnonymous]
		public async Task<IActionResult> AuthenticateAsync([FromBody] AuthenticateModel req)
		{
			if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrEmpty(req.Password))
				return BadRequest("Username and password must not be null or whitespace");

			User user = await _tutorService.AuthenticateTutorAsync(req.Username, req.Password);

			if (user == null)
				return Unauthorized(new { message = "Username or password is incorrect" });

			string tokenString = Methods.GenerateUserToken(user, _appSettings.Value.Secret);

			// return tutor id and authentication token
			return Ok(new
			{
				accessToken = tokenString,
				tutorId = user.Id
			});
		}
	}
}
