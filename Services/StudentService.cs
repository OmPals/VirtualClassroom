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
		Task<List<AssignmentSubmission>> GetAssignmentSubmissionByFilterAsync(string username, string filterAssignments, string filterSubmissions);
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

		// Authenticate student
		public async Task<User> AuthenticateStudentAsync(string username, string password)
		{
			User user = await _userService.AuthenticateAsync(username, password, Enums.Role.student);

			return user;
		}

		// Create submission in Sumissions and Embed the submission into user document for efficient reads
		public async Task<Submission> CreateSubmissionAsync(string studentUsername, Submission submission)
		{
			DateTime currentTime = DateTime.UtcNow;

			Submission oldSubmission = await _submissionService.GetByAssignmentStudentAsync(submission.AssignmentId, studentUsername);

			if (oldSubmission == null)
			{
				throw new Exception("You have't been assigned to this task");
			}

			else if (oldSubmission.Status == Enums.SubmissionStatus.SUBMITTED.ToString())
			{
				throw new Exception("You have already submitted the assignment");
			}

			Assignment assignment = await _assignmentService.GetAsync(submission.AssignmentId);

			if (assignment == null)
			{
				throw new Exception("Assignment does not exist");
			}

			Submission newSubmission = new Submission()
			{
				Id = oldSubmission.Id,
				StudentUsername = oldSubmission.StudentUsername,
				Status = assignment.DeadlineDate < currentTime ?
								Enums.SubmissionStatus.OVERDUE.ToString() :
								Enums.SubmissionStatus.SUBMITTED.ToString(),
				AssignmentId = oldSubmission.AssignmentId,
				Remark = submission.Remark,
				SubmittedAt = currentTime,
				TutorUsername = oldSubmission.TutorUsername
			};

			await _submissionService.UpdateOneAsync(newSubmission);

			User student = await _userService.GetByUsernameAsync(studentUsername);

			List<AssignmentSubmission> assignmentSubmissions = student.AssignmentSubmissions;

			AssignmentSubmission assignmentSubmission = student.AssignmentSubmissions.FirstOrDefault(x => x.Submission.AssignmentId == submission.AssignmentId);

			int x = assignmentSubmissions.IndexOf(assignmentSubmission);

			if (x != -1)
			{
				assignmentSubmissions[x].Submission = newSubmission;
			}

			student.AssignmentSubmissions = assignmentSubmissions;

			await _userService.UpdateOneAsync(student.Id, student);

			return newSubmission;
		}

		// Filter assignments and submissions by assignmnet status and submission status
		public async Task<List<AssignmentSubmission>> GetAssignmentSubmissionByFilterAsync(string studentUsername, string assignmentStatusFilter, string submissionStatusFilter)
		{
			if (!string.IsNullOrWhiteSpace(submissionStatusFilter) && !Enum.IsDefined(typeof(Enums.SubmissionStatus), submissionStatusFilter))
			{
				throw new Exception("Invalid filter on submissions");
			}

			// Validate filter
			if (!string.IsNullOrWhiteSpace(assignmentStatusFilter) && !Enum.IsDefined(typeof(Enums.AssignmentStatus), assignmentStatusFilter))
			{
				throw new Exception("Invalid filter for assignments");
			}

			User user = await _userService.GetByUsernameAsync(studentUsername);

			List<AssignmentSubmission> assignmentSubmissions = user.AssignmentSubmissions;



			if (!string.IsNullOrWhiteSpace(assignmentStatusFilter))
			{
				assignmentSubmissions = assignmentSubmissions.Where(x => x.Assignment.Status == assignmentStatusFilter).ToList();
			}

			if (submissionStatusFilter != Enums.SubmissionStatus.ALL.ToString() && !string.IsNullOrWhiteSpace(submissionStatusFilter))
			{
				assignmentSubmissions = assignmentSubmissions.Where(x => x.Submission.Status == submissionStatusFilter).ToList();
			}

			return assignmentSubmissions;
		}

		public List<AssignmentSubmission> UpdateAssignmentSubmissions(List<AssignmentSubmission> assignmentSubmissions)
		{
			DateTime curr = DateTime.UtcNow;

			assignmentSubmissions = assignmentSubmissions.Select(x =>
			{
				x.Assignment = _assignmentService.ValidateAssignment(x.Assignment);

				if (x.Assignment.DeadlineDate < curr && x.Submission.Status == Enums.SubmissionStatus.PENDING.ToString())
				{
					x.Submission.Status = Enums.SubmissionStatus.OVERDUE.ToString();
				}

				return x;
			}).ToList();

			return assignmentSubmissions;
		}

		// Get sumission by assignment id and student username 
		public async Task<Submission> GetSubmissionByAssignmentStudentAsync(string assignmentId, string studentUsername)
		{
			Assignment assignment = await _assignmentService.GetAsync(assignmentId);

			if (assignment == null)
			{
				throw new Exception("Assignment not found");
			}

			Submission submission = await _submissionService.GetByAssignmentStudentAsync(assignmentId, studentUsername);

			DateTime curr = DateTime.UtcNow;

			if (assignment.DeadlineDate < curr && submission.Status == Enums.SubmissionStatus.PENDING.ToString())
			{
				submission.Status = Enums.SubmissionStatus.OVERDUE.ToString();
			}

			return submission;
		}
	}
}
