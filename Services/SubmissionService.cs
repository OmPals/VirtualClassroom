using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtualClassroom.Models;

namespace VirtualClassroom.Services
{
	public interface ISubmissionService
	{
		Task<List<Submission>> CreateBatchSubmissionsAsync(List<Submission> submissions);
		Task<Submission> GetByAssignmentStudentAsync(string assignmentId, string studentUsername);
		Task UpdateOneAsync(Submission submissionIn);
		Task<List<Submission>> GetByAssignmentAsync(string assignmentId);
		Task RemoveManyAsync(string assignmentId, List<string> deletingStudents);
	}

	public class SubmissionService : ISubmissionService
	{
		private readonly IMongoCollection<Submission> _submissions;

		public SubmissionService(ISubmissionDatabaseSettings settings)
		{
			var client = new MongoClient(settings.ConnectionString);
			var database = client.GetDatabase(settings.DatabaseName);

			_submissions = database.GetCollection<Submission>(settings.SubmissionsCollectionName);
		}

		// Get submission by assignment id and student username
		public async Task<Submission> GetByAssignmentStudentAsync(string assignmentId, string studentUsername)
		{
			var x = await _submissions.FindAsync(submission => submission.AssignmentId == assignmentId && submission.StudentUsername == studentUsername);

			return x.FirstOrDefault();
		}

		// Get submissions by assignment id
		public async Task<List<Submission>> GetByAssignmentAsync(string assignmentId)
		{
			var x = await _submissions.FindAsync(submission => submission.AssignmentId == assignmentId);

			return x.ToList();
		}

		// Update one submission by id
		public async Task UpdateOneAsync(Submission submissionIn)
		{
			await _submissions.ReplaceOneAsync(submission =>
				submission.Id == submissionIn.Id, submissionIn);
		}

		// Create multiple submission documents in single network call
		public async Task<List<Submission>> CreateBatchSubmissionsAsync(List<Submission> submissions)
		{
			// Initiate list writes
			var listWrites = new List<WriteModel<Submission>>();

			foreach (Submission submission in submissions)
			{
				Submission oldSubmission = await GetByAssignmentStudentAsync(submission.AssignmentId, submission.StudentUsername);

				if (oldSubmission == null)
				{
					listWrites.Add(new InsertOneModel<Submission>(submission));
				}
			}

			if (listWrites.Count > 0)
				await _submissions.BulkWriteAsync(listWrites);

			return submissions;
		}

		// Remove multiple submissions by assignment and student username
		public async Task RemoveManyAsync(string assignmentId, List<string> deletingStudents)
		{
			await _submissions.DeleteManyAsync(x => x.AssignmentId == assignmentId && deletingStudents.Contains(x.StudentUsername));
		}
	}
}
