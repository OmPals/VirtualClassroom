using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtualClassroom.Helpers;
using VirtualClassroom.Models;
using static VirtualClassroom.Models.Enums;

namespace VirtualClassroom.Services
{
	public interface IUserService
	{
		Task<User> AuthenticateAsync(string username, string password, Role role);
		Task<User> CreateAsync(User user, string password);
		Task<User> GetByIdAsync(string userId);
		Task CreateUsersAssignmentSubmissions(List<AssignmentSubmission> assignmentSubmissions);
		Task<User> GetByUsernameAsync(string username);
		Task UpdateOne(string id, User userIn);
		Task UpdateBatchSubmissionsAsync(Assignment newAssignment, bool remove);
		Task<List<string>> GetValidUsersAsync(List<string> users);
	}

	public class UserService : IUserService
	{
		private readonly IMongoCollection<User> _users;

		public UserService(IUserDatabaseSettings settings)
		{
			var client = new MongoClient(settings.ConnectionString);
			var database = client.GetDatabase(settings.DatabaseName);

			_users = database.GetCollection<User>(settings.UsersCollectionName);
		}
		public async Task<User> GetByUsernameAsync(string username)
		{
			var x = await _users.FindAsync<User>(user => user.Username == username);

			return x.FirstOrDefault();
		}

		public async Task<List<string>> GetValidUsersAsync(List<string> users)
		{
			var x = await _users.FindAsync<User>(x => users.Contains(x.Username));

			List<string> validUsers = x.ToList().Select(s => s.Username).ToList();

			return validUsers;
		}

		public async Task<User> GetByIdAsync(string id)
		{
			var x = await _users.FindAsync<User>(user => user.Id == id);

			return x.FirstOrDefault();
		}


		public async Task<User> CreateAsync(User user)
		{
			await _users.InsertOneAsync(user);
			return user;
		}

		public async Task UpdateOne(string id, User userIn)
		{
			await _users.ReplaceOneAsync(user => user.Id == id, userIn);
		}

		public void Remove(string id) =>
			_users.DeleteOne(user => user.Id == id);

		public async Task<User> AuthenticateAsync(string username, string password, Role role)
		{
			User reqUser = await GetByUsernameAsync(username);

			// check if username exists
			if (reqUser == null)
			{
				User user = new User()
				{
					Username = username,
					Role = role.ToString()
				};

				user = await CreateAsync(user, password);

				return user;
			}
			else
			{
				// check if password is correct
				if (!VerifyPasswordHash(password, reqUser.PasswordHash, reqUser.PasswordSalt))
					return null;

				return reqUser;
			}
		}

		public async Task<User> CreateAsync(User user, string password)
		{
			if (string.IsNullOrWhiteSpace(password))
				throw new Exception("password required");

			User user1 = await GetByUsernameAsync(user.Username);
			if (user1 != null)
			{
				return user1;
			}

			CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

			user.PasswordHash = passwordHash;
			user.PasswordSalt = passwordSalt;

			return await CreateAsync(user);
		}

		private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
		{
			if (password == null) throw new ArgumentNullException("password");
			if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
			if (storedHash.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
			if (storedSalt.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");

			using (var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt))
			{
				var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
				for (int i = 0; i < computedHash.Length; i++)
				{
					if (computedHash[i] != storedHash[i]) return false;
				}
			}

			return true;
		}

		private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
		{
			if (password == null) throw new ArgumentNullException("password");
			if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

			using var hmac = new System.Security.Cryptography.HMACSHA512();
			passwordSalt = hmac.Key;
			passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
		}

		public async Task CreateUsersAssignmentSubmissions(List<AssignmentSubmission> assignmentSubmissions)
		{
			var listWrites = new List<WriteModel<User>>();

			foreach (AssignmentSubmission assignmentSubmission in assignmentSubmissions)
			{
				User student = await GetByUsernameAsync(assignmentSubmission.Submission.StudentUsername);

				if (student == null) continue;

				if (student.AssignmentSubmissions == null)
				{
					student.AssignmentSubmissions = new List<AssignmentSubmission>();
				}

				student.AssignmentSubmissions.Add(assignmentSubmission);

				var filter = new FilterDefinitionBuilder<User>().Where(m => m.Id == student.Id);

				listWrites.Add(new ReplaceOneModel<User>(filter, student));
			}

			if (listWrites.Count > 0)
				await _users.BulkWriteAsync(listWrites);
		}

		public async Task UpdateBatchSubmissionsAsync(Assignment assignment, bool remove)
		{
			var listWrites = new List<WriteModel<User>>();

			foreach (string x in assignment.Students)
			{
				User student = await GetByUsernameAsync(x);

				List<AssignmentSubmission> assignmentSubmissions = student.AssignmentSubmissions;

				AssignmentSubmission assignmentSubmission = assignmentSubmissions.FirstOrDefault(x => x.Assignment.Id == assignment.Id);

				int index = assignmentSubmissions.IndexOf(assignmentSubmission);

				if (index != -1)
				{
					if (remove)
					{
						assignmentSubmissions.RemoveAt(index);
					}
					else
					{
						assignmentSubmissions[index].Assignment = assignment;
					}
				}

				student.AssignmentSubmissions = assignmentSubmissions;

				var filter = new FilterDefinitionBuilder<User>().Where(m => m.Id == student.Id);

				listWrites.Add(new ReplaceOneModel<User>(filter, student));
			}

			if (listWrites.Count > 0)
				await _users.BulkWriteAsync(listWrites);
		}
	}
}
