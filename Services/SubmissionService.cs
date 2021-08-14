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
		Task UpdateOne(Submission submissionIn);
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

		public async Task<Submission> GetByAssignmentStudentAsync(string assignmentId, string studentUsername)
		{
			var x = await _submissions.FindAsync(submission => submission.AssignmentId == assignmentId && submission.StudentUsername == studentUsername);

			return x.FirstOrDefault();
		}

		public async Task<List<Submission>> GetByAssignmentAsync(string assignmentId)
		{
			var x = await _submissions.FindAsync(submission => submission.AssignmentId == assignmentId);

			return x.ToList();
		}

		public async Task UpdateOne(Submission submissionIn)
		{
			await _submissions.ReplaceOneAsync(submission =>
				submission.Id == submissionIn.Id, submissionIn);
		}

		public async Task<List<Submission>> CreateBatchSubmissionsAsync(List<Submission> submissions)
		{
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

		public async Task RemoveManyAsync(string assignmentId, List<string> deletingStudents)
		{
			await _submissions.DeleteManyAsync(x => x.AssignmentId == assignmentId && deletingStudents.Contains(x.StudentUsername));
		}
	}
}
