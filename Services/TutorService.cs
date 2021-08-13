using System.Collections.Generic;
using System.Threading.Tasks;
using VirtualClassroom.Models;

namespace VirtualClassroom.Services
{
	public interface ITutorService
	{
		public Task<User> AuthenticateTutorAsync(string username, string password);

		public Task<Assignment> CreateAssignmentAsync(string tutor, Assignment assignment);
	}

	public class TutorService : ITutorService
	{
		private readonly IUserService _userService;
		private readonly IAssignmentService _assignmentService;
		private readonly ISubmissionService _submissionService;

		public TutorService(IUserService userService, IAssignmentService assignmentService, ISubmissionService submissionService)
		{
			_userService = userService;
			_assignmentService = assignmentService;
			_submissionService = submissionService;
		}

		public async Task<User> AuthenticateTutorAsync(string username, string password)
		{
			User user = await _userService.AuthenticateAsync(username, password, Enums.Role.tutor);

			return user;
		}

		public async Task<Assignment> CreateAssignmentAsync(string tutor, Assignment assignment)
		{
			assignment.Tutor = tutor;

			assignment = _assignmentService.ValidateAssignment(assignment);

			Assignment newAssignment = await _assignmentService.CreateOneAsync(assignment);

			// Initiate the submission students and set the status as PENDING
			// Location: Submissions
			await CreateBatchSubmissionsAsync(newAssignment);

			return newAssignment;
		}

		public async Task<List<Submission>> CreateBatchSubmissionsAsync(Assignment assignment)
		{
			List<Submission> submissions = new List<Submission>();

			foreach (string student in assignment.Students)
			{
				Submission submission = new Submission()
				{
					AssignmentId = assignment.Id,
					Status = Enums.SubmissionStatus.PENDING.ToString(),
					StudentUsername = student
				};

				submissions.Add(submission);
			}

			return await _submissionService.CreateBatchSubmissionsAsync(submissions);
		}
	}
}
