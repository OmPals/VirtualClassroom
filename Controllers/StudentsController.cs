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
	public class StudentsController : ControllerBase
	{
		private readonly IOptions<AppSettings> _appSettings;
		private readonly IStudentService _studentService;

		public StudentsController(IOptions<AppSettings> appSettings, IStudentService studentService)
		{
			_appSettings = appSettings;
			_studentService = studentService;
		}

		[HttpPost("authenticate")]
		[AllowAnonymous]
		public async Task<IActionResult> AuthenticateAsync([FromBody] AuthenticateModel req)
		{
			if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrEmpty(req.Password))
				return BadRequest("Username and password must not be null or whitespace");

			User user = await _studentService.AuthenticateStudentAsync(req.Username, req.Password);

			if (user == null)
				return Unauthorized(new { message = "Username or password is incorrect" });

			string tokenString = Methods.GenerateUserToken(user, _appSettings.Value.Secret);

			// return student id and authentication token
			return Ok(new
			{
				accessToken = tokenString,
				studentId = user.Id
			});
		}
	}
}
