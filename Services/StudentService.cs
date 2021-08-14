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
		Task<Submission> GetSubmissionByAssignmentStudentAsync(string assignmentId, string username);
		Task<List<AssignmentSubmission>> GetAssignmentSubmissionByFilter(string username, string filterAssignments, string filterSubmissions);
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
				AssignmentId = oldSubmission.AssignmentId,
				Remark = submission.Remark,
				SubmittedAt = currentTime,
				TutorUsername = oldSubmission.TutorUsername
			};

			await _submissionService.UpdateOne(newSubmission);

			User student = await _userService.GetByUsernameAsync(username);

			List<AssignmentSubmission> assignmentSubmissions = student.AssignmentSubmissions;

			AssignmentSubmission assignmentSubmission = student.AssignmentSubmissions.FirstOrDefault(x => x.Submission.AssignmentId == submission.AssignmentId);

			int x = assignmentSubmissions.IndexOf(assignmentSubmission);

			if(x != -1)
			{
				assignmentSubmissions[x].Submission = newSubmission;
			}

			student.AssignmentSubmissions = assignmentSubmissions;

			await _userService.UpdateOne(student.Id, student);

			return newSubmission;
		}

		public async Task<List<AssignmentSubmission>> GetAssignmentSubmissionByFilter(string username, string filterAssignments, string filterSubmissions)
		{
			if (!string.IsNullOrWhiteSpace(filterSubmissions) && !Enum.IsDefined(typeof(Enums.SubmissionStatus), filterSubmissions))
			{
				throw new Exception("Invalid filter on submissions");
			}

			// Validate filter
			if (!string.IsNullOrWhiteSpace(filterAssignments) && !Enum.IsDefined(typeof(Enums.AssignmentStatus), filterAssignments))
			{
				throw new Exception("Invalid filter for assignments");
			}

			User user = await _userService.GetByUsernameAsync(username);

			List<AssignmentSubmission> assignmentSubmissions = user.AssignmentSubmissions;

			if (!string.IsNullOrWhiteSpace(filterAssignments))
			{
				assignmentSubmissions = assignmentSubmissions.Where(x => x.Assignment.Status == filterAssignments).ToList();
			}

			if (filterSubmissions != Enums.SubmissionStatus.ALL.ToString() && !string.IsNullOrWhiteSpace(filterSubmissions))
			{
				assignmentSubmissions = assignmentSubmissions.Where(x => x.Submission.Status == filterSubmissions).ToList();
			}

			return assignmentSubmissions;
		}

		public async Task<Submission> GetSubmissionByAssignmentStudentAsync(string assignmentId, string username)
		{
			Assignment assignment = await _assignmentService.GetAsync(assignmentId);

			if (assignment == null)
			{
				throw new Exception("Assignment not found");
			}

			Submission submission = await _submissionService.GetByAssignmentStudentAsync(assignmentId, username);

			return submission;
		}
	}
}
