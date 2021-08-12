using VirtualClassroom.Models;

namespace VirtualClassroom.Services
{
	public interface ITutorService
	{
		public User AuthenticateTutor(string username, string password);

		public Assignment CreateAssignment(string tutorId, Assignment assignment);
	}

	public class TutorService : ITutorService
	{
		private readonly IUserService _userService;
		private readonly IAssignmentService _assignmentService;

		public TutorService(IUserService userService, IAssignmentService assignmentService)
		{
			_userService = userService;
			_assignmentService = assignmentService;
		}

		public User AuthenticateTutor(string username, string password)
		{
			User user = _userService.Authenticate(username, password, Enums.Role.tutor);

			return user;
		}

		public Assignment CreateAssignment(string tutorId, Assignment assignment)
		{
			assignment.TutorId = tutorId;

			assignment = _assignmentService.ValidateAssignment(assignment);

			Assignment newAssignment = _assignmentService.CreateOne(assignment);

			// TODO: Initiat Submission

			return newAssignment;
		}
	}
}
