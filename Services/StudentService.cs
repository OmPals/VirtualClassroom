using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtualClassroom.Models;

namespace VirtualClassroom.Services
{
	public interface IStudentService
	{
		public User AuthenticateStudent(string username, string password);
	}

	public class StudentService : IStudentService
	{
		private readonly IUserService _userService;

		public StudentService(IUserService userService)
		{
			_userService = userService;
		}

		public User AuthenticateStudent(string username, string password)
		{
			User user = _userService.Authenticate(username, password, Enums.Role.student);

			return user;
		}
	}
}
