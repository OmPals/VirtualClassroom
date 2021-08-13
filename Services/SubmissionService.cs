using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtualClassroom.Models;

namespace VirtualClassroom.Services
{
	public interface ISubmissionService
	{
		Task<List<Submission>> CreateBatchSubmissionsAsync(List<Submission> submissions);

		Task<List<Submission>> GetSubmissionsByStudent(string studentUsername, string statusFilter);

		Task<Submission> GetByAssignmentStudentAsync(string assignmentId, string studentUsername);

		Task UpdateOne(Submission submissionIn);
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

		public async Task<List<Submission>> GetSubmissionsByStudent(string studentUsername, string statusFilter)
		{
			var x = string.IsNullOrWhiteSpace(statusFilter) ?
						await _submissions.FindAsync(submission => submission.StudentUsername == studentUsername) :
						await _submissions.FindAsync(submission => submission.StudentUsername == studentUsername && submission.Status == statusFilter);

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

			await _submissions.BulkWriteAsync(listWrites);

			return submissions;
		}
	}
}
