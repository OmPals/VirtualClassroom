using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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

		[AllowAnonymous]
		[HttpPost("authenticate")]
		public IActionResult Authenticate([FromBody] AuthenticateModel req)
		{
			if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrEmpty(req.Password))
				return BadRequest("Username and password must not be null or whitespace");

			User user = _studentService.AuthenticateStudent(req.Username, req.Password);

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

		[HttpGet("check")]
		[Authorize(Roles = "student")]
		public IActionResult CheckUserRole()
		{
			return Ok();
		}
	}
}
