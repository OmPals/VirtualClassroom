using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtualClassroom.Models;

namespace VirtualClassroom.Services
{
	public interface IStudentService
	{
		Task<User> AuthenticateStudentAsync(string username, string password);
		Task<Submission> CreateSubmissionAsync(string username, Submission submission);
	}

	public class StudentService : IStudentService
	{
		private readonly IUserService _userService;
		private readonly ISubmissionService _submissionService;
		private readonly IAssignmentService _assignmentService;

		public StudentService(IUserService userService, ISubmissionService submissionService, IAssignmentService assignmentService)
		{
			_userService = userService;
			_submissionService = submissionService;
			_assignmentService = assignmentService;
		}

		public async Task<User> AuthenticateStudentAsync(string username, string password)
		{
			User user = await _userService.AuthenticateAsync(username, password, Enums.Role.student);

			return user;
		}

		/*public async Task<List<Submission>> GetSubmissions(string username, string status)
		{

		}*/

		public async Task<Submission> CreateSubmissionAsync(string username, Submission submission)
		{
			DateTime currentTime = DateTime.UtcNow;

			Submission oldSubmission = await _submissionService.GetByAssignmentStudentAsync(submission.AssignmentId, username);

			if (oldSubmission == null)
			{
				throw new Exception("You have't been assigned to this task");
			}

			else if (oldSubmission.Status == Enums.SubmissionStatus.SUBMITTED.ToString())
			{
				throw new Exception("You have already submitted the assignment");
			}

			Assignment assignment = await _assignmentService.GetAsync(submission.AssignmentId);

			if(assignment == null)
			{
				throw new Exception("Assignment does not exist");
			}

			Submission newSubmission = new Submission()
			{
				Id = oldSubmission.Id,
				StudentUsername = username,
				Status = assignment.DeadlineDate < currentTime ?
								Enums.SubmissionStatus.OVERDUE.ToString() :
								Enums.SubmissionStatus.SUBMITTED.ToString(),
				AssignmentId = submission.AssignmentId,
				Remark = submission.Remark,
				SubmittedAt = currentTime
			};

			await _submissionService.UpdateOne(newSubmission);

			return newSubmission;
		}
	}
}
