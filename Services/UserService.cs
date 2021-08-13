using MongoDB.Driver;
using System;
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

		public void UpdateOne(string id, User userIn) =>
			_users.ReplaceOne(user => user.Id == id, userIn);

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
	}
}
