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
		Task CreateUsersAssignmentSubmissionsAsync(List<AssignmentSubmission> assignmentSubmissions);
		Task<User> GetByUsernameAsync(string username);
		Task UpdateOneAsync(string id, User userIn);
		Task UpdateBatchAssignmentSubmissionsAsync(Assignment newAssignment, bool remove);
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

		// Get user by username
		public async Task<User> GetByUsernameAsync(string username)
		{
			var x = await _users.FindAsync<User>(user => user.Username == username);

			return x.FirstOrDefault();
		}

		// Remove unregistered usernames from the list
		// Only keep authenitcated users
		public async Task<List<string>> GetValidUsersAsync(List<string> users)
		{
			var x = await _users.FindAsync<User>(x => users.Contains(x.Username));

			List<string> validUsers = x.ToList().Select(s => s.Username).ToList();

			return validUsers;
		}

		// Get user by id
		public async Task<User> GetByIdAsync(string id)
		{
			var x = await _users.FindAsync<User>(user => user.Id == id);

			return x.FirstOrDefault();
		}

		// Create user
		public async Task<User> CreateAsync(User user)
		{
			await _users.InsertOneAsync(user);
			return user;
		}

		// Update one user
		public async Task UpdateOneAsync(string id, User userIn)
		{
			await _users.ReplaceOneAsync(user => user.Id == id, userIn);
		}

		// Authenitcate user
		// Get the user from database and verify the passwrod hash
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

		// Create a user by user model and a password specified
		// Store hash of the password
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

		// Verify the password hash
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

		// Create the password hash
		private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
		{
			if (password == null) throw new ArgumentNullException("password");
			if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

			using var hmac = new System.Security.Cryptography.HMACSHA512();
			passwordSalt = hmac.Key;
			passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
		}

		// Create multiple AssignmentUsers List for users
		public async Task CreateUsersAssignmentSubmissionsAsync(List<AssignmentSubmission> assignmentSubmissions)
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

		// Update the AssignmentSubmission List from user
		public async Task UpdateBatchAssignmentSubmissionsAsync(Assignment assignment, bool remove)
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
