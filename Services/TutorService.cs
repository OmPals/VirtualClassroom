using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtualClassroom.Models;

namespace VirtualClassroom.Services
{
	public interface ITutorService
	{
		Task<User> AuthenticateTutorAsync(string username, string password);
		Task<Assignment> CreateAssignmentAsync(string tutor, Assignment assignment);
		Task<List<Submission>> GetSubmissionsByAssignmentTutorAsync(string assignmentId, string username);
		Task<List<Assignment>> GetAssignmentsByTutorStatusAsync(string username, string filter);
		Task UpdateOneAssignmentAsync(string assignmentId, Assignment assignment, string username);
		Task DeleteOneAssignmentAsync(string assignmentId, string username);
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

		// Authenticate tutor
		public async Task<User> AuthenticateTutorAsync(string username, string password)
		{
			User user = await _userService.AuthenticateAsync(username, password, Enums.Role.tutor);

			return user;
		}

		// Create assignment
		// Embed assignment in student with a submission
		// Create subbmissions for students included
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

		// Create multiple submissions in a single network call
		public async Task<List<Submission>> CreateBatchSubmissionsAsync(Assignment assignment)
		{
			List<Submission> submissions = new List<Submission>();

			// Initiate AssignmentSubmission
			List<AssignmentSubmission> assignmentSubmissions = new List<AssignmentSubmission>();

			assignment.Students = await _userService.GetValidStudentsAsync(assignment.Students);

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

			await _userService.CreateUsersAssignmentSubmissionsAsync(assignmentSubmissions);

			return submissions;
		}

		// Get list of submissions by assignment id and tutor username
		public async Task<List<Submission>> GetSubmissionsByAssignmentTutorAsync(string assignmentId, string tutorUsername)
		{
			Assignment assignment = await _assignmentService.GetAsync(assignmentId);

			if (assignment == null || assignment.Tutor != tutorUsername)
			{
				throw new Exception("Assignment not found");
			}

			List<Submission> submissions = await _submissionService.GetByAssignmentAsync(assignmentId);

			submissions = submissions.Select(x => _submissionService.ValidateSubmissionStatus(x, assignment.DeadlineDate)).ToList();

			return submissions;
		}

		// Get assignments by status filter
		public async Task<List<Assignment>> GetAssignmentsByTutorStatusAsync(string tutorUsername, string statusFilter)
		{
			// Validate filter
			if (!string.IsNullOrWhiteSpace(statusFilter) && !Enum.IsDefined(typeof(Enums.AssignmentStatus), statusFilter))
			{
				throw new Exception("Invalid filter for assignments");
			}

			List<Assignment> assignments = await _assignmentService.GetByTutorStatusAsync(tutorUsername, statusFilter);

			assignments = assignments.Select(x => _assignmentService.ValidateAssignment(x)).ToList();

			return assignments;
		}

		// Update one assignment
		// Duplicate this update for each student
		// If students get updated then remove the submissions for missing students, 
		// Add submissions for new students and update the embedded assignment for the rest
		public async Task UpdateOneAssignmentAsync(string assignmentId, Assignment assignment, string tutorUsername)
		{
			Assignment oldAssignment = await _assignmentService.GetAsync(assignmentId);

			// Old assignment does not exist
			if (oldAssignment == null || tutorUsername != oldAssignment.Tutor)
			{
				throw new Exception("Assignment does not exist or tutor does not have access to it");
			}

			assignment.Students = await _userService.GetValidStudentsAsync(assignment.Students);

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

			Assignment newAssignment = _assignmentService.ValidateAssignment(oldAssignment);

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
			await _userService.UpdateBatchAssignmentSubmissionsAsync(newAssignment, false);

			foreach (string student in oldStudents)
			{
				if (!newStudents.Contains(student))
				{
					// Delete submission
					deletingStudents.Add(student);
				}
			}

			newAssignment.Students = deletingStudents;
			await _userService.UpdateBatchAssignmentSubmissionsAsync(newAssignment, true);

			// Remove from submission collection
			await _submissionService.RemoveManyAsync(assignmentId, deletingStudents);
		}

		// Delete assignment 
		// Remove the submissions of respective students
		// Remove the Embedded AssignmentSubmission from user
		public async Task DeleteOneAssignmentAsync(string assignmentId, string username)
		{
			Assignment assignment = await _assignmentService.GetAsync(assignmentId);

			if (assignment == null || assignment.Tutor != username)
			{
				throw new Exception("Assignment does not exist or tutor does not have access to it");
			}

			List<string> deletingStudents = assignment.Students;

			// Remove from submission collection
			await _submissionService.RemoveManyAsync(assignmentId, deletingStudents);

			await _userService.UpdateBatchAssignmentSubmissionsAsync(assignment, true);

			await _assignmentService.DeleteOneAsync(assignmentId);
		}
	}
}
