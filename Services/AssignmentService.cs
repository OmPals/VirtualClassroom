using MongoDB.Driver;
using System;
using System.Collections.Generic;
using VirtualClassroom.Models;

namespace VirtualClassroom.Services
{
	public interface IAssignmentService
	{
		Assignment Get(string assignmentId);
		Assignment CreateOne(Assignment assignment);

		Assignment ValidateAssignment(Assignment assignment);
		/*Assignment Update(string tutorId, string assignmentId, Assignment assignment);
		bool Remove(string tutorId, string assignmentId);*/
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

		public Assignment Get(string id) =>
			_assignments.Find<Assignment>(assignment => assignment.Id == id).FirstOrDefault();

		public Assignment CreateOne(Assignment assignment)
		{
			_assignments.InsertOne(assignment);
			return assignment;
		}

		public Assignment ValidateAssignment(Assignment assignment)
		{
			DateTime currentTime = DateTime.UtcNow;

			if (string.IsNullOrWhiteSpace(assignment.Description))
				throw new Exception("Invalid Description");

			if (assignment.PublishedAt == null)
			{
				throw new Exception("Invalid PublishedAt");
			}

			if(assignment.PublishedAt > currentTime)
			{
				assignment.Status = Enums.AssignmentStatus.SCHEDULED.ToString();
			}
			else
			{
				assignment.Status = Enums.AssignmentStatus.ONGOING.ToString();
			}

			if(assignment.DeadlineDate == null || assignment.DeadlineDate < assignment.PublishedAt)
			{
				throw new Exception("Invalid DeadlineDate, must be non empty and greater published at date");
			}

			if(assignment.Students == null || assignment.Students.Count == 0)
			{
				throw new Exception("Assignment must be assigned to a student(s)");
			}

			return assignment;
		}

		/*public void UpdateOne(string id, Assignment assignmentIn) =>
			_assignments.ReplaceOne(assignment => assignment.Id == id, assignmentIn);

		public void RemoveOne(string id) =>
			_books.DeleteOne(book => book.Id == id);*/

		/*public Book Update(string userId, string bookId, Book bookParam)
		{
			Book book = Get(bookId);

			if (book == null)
				return null;

			if (userId != book.PostedBy)
				return null;

			if (!string.IsNullOrWhiteSpace(bookParam.BookName))
				book.BookName = bookParam.BookName;

			if (!string.IsNullOrWhiteSpace(bookParam.Category))
				book.Category = bookParam.Category;

			if (!string.IsNullOrWhiteSpace(bookParam.Author))
				book.Author = bookParam.Author;

			if (bookParam.Price >= 0)
				book.Price = bookParam.Price;

			UpdateOne(bookId, book);

			return book;
		}

		public bool Remove(string userId, string bookId)
		{
			Book oldBook = Get(bookId);

			if (userId != oldBook.PostedBy)
				return false;

			RemoveOne(bookId);

			return true;
		}*/
	}
}
