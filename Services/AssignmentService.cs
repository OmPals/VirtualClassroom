using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VirtualClassroom.Models;

namespace VirtualClassroom.Services
{
	public interface IAssignmentService
	{
		Task<Assignment> GetAsync(string assignmentId);
		Task<Assignment> CreateOneAsync(Assignment assignment);
		Assignment ValidateAssignmentAsync(Assignment assignment);
		Task<List<Assignment>> GetByTutorStatusAsync(string username, string filter);
		Task UpdateOneAsync(string id, Assignment assignmentIn);
		Task DeleteOneAsync(string id);
	}

	public class AssignmentService : IAssignmentService
	{
		private readonly IMongoCollection<Assignment> _assignments;

		public AssignmentService(IAssignmentDatabaseSettings settings)
		{
			var client = new MongoClient(settings.ConnectionString);
			var database = client.GetDatabase(settings.DatabaseName);

			_assignments = database.GetCollection<Assignment>(settings.AssignmentsCollectionName);
		}

		// Get assignment by Id from Assignments
		public async Task<Assignment> GetAsync(string id)
		{
			var x = await _assignments.FindAsync<Assignment>(assignment => assignment.Id == id);

			return x.FirstOrDefault();
		}

		// Get list of assignments by tutor's user name and status filter
		public async Task<List<Assignment>> GetByTutorStatusAsync(string tutorUsername, string statusFilter)
		{
			var x = !string.IsNullOrWhiteSpace(statusFilter) ?
						await _assignments.FindAsync<Assignment>(assignment => assignment.Tutor == tutorUsername && assignment.Status == statusFilter) :
						await _assignments.FindAsync<Assignment>(assignment => assignment.Tutor == tutorUsername);

			return x.ToList();
		}

		// Create assignment
		public async Task<Assignment> CreateOneAsync(Assignment assignment)
		{
			await _assignments.InsertOneAsync(assignment);
			return assignment;
		}

		// Validate the properties of assignment
		public Assignment ValidateAssignmentAsync(Assignment assignment)
		{
			DateTime currentTime = DateTime.UtcNow;

			if (string.IsNullOrWhiteSpace(assignment.Description))
				throw new Exception("Invalid Description");

			if (assignment.PublishedAt == null)
			{
				throw new Exception("Invalid PublishedAt");
			}

			if (assignment.PublishedAt > currentTime)
			{
				assignment.Status = Enums.AssignmentStatus.SCHEDULED.ToString();
			}
			else
			{
				assignment.Status = Enums.AssignmentStatus.ONGOING.ToString();
			}

			if (assignment.DeadlineDate == null || assignment.DeadlineDate < assignment.PublishedAt)
			{
				throw new Exception("Invalid DeadlineDate, must be non empty and greater published at date");
			}

			if (assignment.Students == null || assignment.Students.Count == 0)
			{
				throw new Exception("Assignment must be assigned to a student(s)");
			}

			return assignment;
		}

		// Update one assignment by id
		public async Task UpdateOneAsync(string id, Assignment assignmentIn)
		{
			await _assignments.ReplaceOneAsync(assignment => assignment.Id == id, assignmentIn);
		}

		// Delete one assignment by id
		public async Task DeleteOneAsync(string id)
		{
			await _assignments.DeleteOneAsync(x => x.Id == id);
		}
	}
}
