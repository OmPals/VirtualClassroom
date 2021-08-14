using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VirtualClassroom.Models;

namespace VirtualClassroom.Services
{
	public interface ITutorService
	{
		Task<User> AuthenticateTutorAsync(string username, string password);
		Task<Assignment> CreateAssignmentAsync(string tutor, Assignment assignment);
		Task<List<Submission>> GetSubmissionsByAssignmentTutor(string assignmentId, string username);
		Task<List<Assignment>> GetAssignmentsByFilter(string username, string filter);
		Task UpdateAssignment(string assignmentId, Assignment assignment, string username);
		Task DeleteAssignment(string assignmentId, string username);
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

			assignment = _assignmentService.ValidateAssignmentAsync(assignment);

			Assignment newAssignment = await _assignmentService.CreateOneAsync(assignment);

			// Initiate the submission students and set the status as PENDING
			// Location: Submissions
			await CreateBatchSubmissionsAsync(newAssignment);

			return newAssignment;
		}

		public async Task<List<Submission>> CreateBatchSubmissionsAsync(Assignment assignment)
		{
			List<Submission> submissions = new List<Submission>();
			List<AssignmentSubmission> assignmentSubmissions = new List<AssignmentSubmission>();

			assignment.Students = await _userService.GetValidUsersAsync(assignment.Students);

			foreach (string student in assignment.Students)
			{
				Submission submission = new Submission()
				{
					AssignmentId = assignment.Id,
					Status = Enums.SubmissionStatus.PENDING.ToString(),
					StudentUsername = student,
					TutorUsername = assignment.Tutor
				};

				submissions.Add(submission);
			}

			submissions = await _submissionService.CreateBatchSubmissionsAsync(submissions);

			foreach (Submission x in submissions)
			{
				AssignmentSubmission assignmentSubmission = new AssignmentSubmission
				{
					Assignment = assignment,
					Submission = x
				};

				assignmentSubmissions.Add(assignmentSubmission);
			}

			await _userService.CreateUsersAssignmentSubmissions(assignmentSubmissions);

			return submissions;
		}

		public async Task<List<Submission>> GetSubmissionsByAssignmentTutor(string assignmentId, string username)
		{
			Assignment assignment = await _assignmentService.GetAsync(assignmentId);

			if (assignment == null || assignment.Tutor != username)
			{
				throw new Exception("Assignment not found");
			}

			// Paginate
			List<Submission> submissions = await _submissionService.GetByAssignmentAsync(assignmentId);

			return submissions;
		}

		public async Task<List<Assignment>> GetAssignmentsByFilter(string username, string filter)
		{
			// Validate filter
			if (!string.IsNullOrWhiteSpace(filter) && !Enum.IsDefined(typeof(Enums.AssignmentStatus), filter))
			{
				throw new Exception("Invalid filter for assignments");
			}

			// Paginate
			List<Assignment> assignments = await _assignmentService.GetByTutorStatusAsync(username, filter);

			return assignments;
		}

		public async Task UpdateAssignment(string assignmentId, Assignment assignment, string username)
		{
			Assignment oldAssignment = await _assignmentService.GetAsync(assignmentId);

			// Old assignment does not exist
			if (oldAssignment == null || username != oldAssignment.Tutor)
			{
				throw new Exception("Assignment does not exist or tutor does not have access to it");
			}

			assignment.Students = await _userService.GetValidUsersAsync(assignment.Students);

			List<string> oldStudents = oldAssignment.Students;
			List<string> newStudents = assignment.Students;

			oldAssignment.Description = assignment.Description;

			DateTime newPublished = assignment.PublishedAt, newDeadline = assignment.DeadlineDate;

			if (newPublished != null && newDeadline != null)
			{
				if (newPublished < newDeadline)
				{
					oldAssignment.PublishedAt = newPublished;
					oldAssignment.DeadlineDate = newDeadline;
				}
				else
				{
					throw new Exception(Enums.ClientRequestStatus.BadRequest.ToString());
				}
			}

			if (newPublished != null)
			{
				if (newPublished > oldAssignment.DeadlineDate)
				{
					throw new Exception("Published date must be less than deadline date");
				}

				oldAssignment.PublishedAt = newPublished;
			}

			if (newDeadline != null)
			{
				if (newDeadline < oldAssignment.PublishedAt)
				{
					throw new Exception("Deadline date must be greater than published date");
				}

				oldAssignment.DeadlineDate = newDeadline;
			}

			Assignment newAssignment = _assignmentService.ValidateAssignmentAsync(oldAssignment);

			newAssignment.Students = assignment.Students;
			await _assignmentService.UpdateOneAsync(assignmentId, newAssignment);


			List<string> creatingStudents = new List<string>();
			List<string> deletingStudents = new List<string>();
			List<string> updatingStudents = new List<string>();

			foreach (string student in newStudents)
			{
				if (!oldStudents.Contains(student))
				{
					// Create submission
					creatingStudents.Add(student);
				}
				else
				{
					updatingStudents.Add(student);
				}
			}

			newAssignment.Students = creatingStudents;
			await CreateBatchSubmissionsAsync(newAssignment);

			newAssignment.Students = updatingStudents;
			await _userService.UpdateBatchSubmissionsAsync(newAssignment, false);

			foreach (string student in oldStudents)
			{
				if (!newStudents.Contains(student))
				{
					// Delete submission
					deletingStudents.Add(student);
				}
			}

			newAssignment.Students = deletingStudents;
			await _userService.UpdateBatchSubmissionsAsync(newAssignment, true);

			// Remove from submission collection
			await _submissionService.RemoveManyAsync(assignmentId, deletingStudents);
		}

		public async Task DeleteAssignment(string assignmentId, string username)
		{
			Assignment assignment = await _assignmentService.GetAsync(assignmentId);

			if (assignment == null || assignment.Tutor != username)
			{
				throw new Exception("Assignment does not exist or tutor does not have access to it");
			}

			List<string> deletingStudents = assignment.Students;

			// Remove from submission collection
			await _submissionService.RemoveManyAsync(assignmentId, deletingStudents);

			await _userService.UpdateBatchSubmissionsAsync(assignment, true);

			await _assignmentService.DeleteOneAsync(assignmentId);
		}
	}
}
